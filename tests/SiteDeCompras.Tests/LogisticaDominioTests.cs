using SiteDeCompras.Domain.Carrinhos;
using SiteDeCompras.Domain.Common;
using SiteDeCompras.Domain.Logistica;
using SiteDeCompras.Domain.Pagamentos;
using SiteDeCompras.Domain.Pedidos;
using SiteDeCompras.Domain.Produtos;

namespace SiteDeCompras.Tests;

public class LogisticaDominioTests
{
    private static Pedido PedidoPago()
    {
        var produto = new Produto("Caneca", 10m, 5);
        var carrinho = new Carrinho();
        carrinho.AdicionarItem(produto, 1);
        return Pedido.Fechar(carrinho, [produto], CalculadoraPagamento.Debito(carrinho.Total), Exemplos.Endereco());
    }

    // 23/10/2026 é sexta-feira. Horários em Brasília (UTC-3).
    [Theory]
    [InlineData("2026-10-23T11:59:00-03:00", "2026-10-23T18:00:00-03:00")] // antes do corte: sai hoje
    [InlineData("2026-10-23T12:00:00-03:00", "2026-10-26T18:00:00-03:00")] // depois do corte de sexta: segunda
    [InlineData("2026-10-24T09:00:00-03:00", "2026-10-26T18:00:00-03:00")] // sábado: segunda
    [InlineData("2026-10-23T23:30:00-03:00", "2026-10-26T18:00:00-03:00")] // sexta à noite já é sábado em UTC
    public void Prazo_de_despacho_respeita_o_horario_de_corte_e_os_dias_uteis(string pago, string esperado)
    {
        var prazo = CalendarioLogistica.PrazoDespacho(DateTimeOffset.Parse(pago).UtcDateTime);

        Assert.Equal(DateTimeOffset.Parse(esperado).UtcDateTime, prazo);
    }

    [Fact]
    public void Previsao_soma_os_dias_uteis_de_transporte_da_regiao()
    {
        var despacho = DateTimeOffset.Parse("2026-10-23T18:00:00-03:00").UtcDateTime; // sexta

        // Belo Horizonte (CEP 3xxxx): 3 dias úteis, pula o fim de semana.
        Assert.Equal(DateTimeOffset.Parse("2026-10-28T18:00:00-03:00").UtcDateTime,
            CalendarioLogistica.PrevisaoEntrega(despacho, "30130010"));
    }

    [Fact]
    public void Envio_nasce_na_fila_com_o_endereco_copiado_e_um_evento()
    {
        var pedido = PedidoPago();

        var envio = Envio.Criar(pedido, DateTime.UtcNow);

        Assert.Equal(StatusEnvio.AguardandoSeparacao, envio.Status);
        Assert.Equal("30130010", envio.Destino.Cep);
        Assert.NotSame(pedido.EnderecoEntrega, envio.Destino);
        Assert.Null(envio.CodigoRastreio);
        Assert.Single(envio.Eventos);
    }

    [Theory]
    [InlineData(StatusEnvio.AguardandoSeparacao, StatusPedido.Pago)]
    [InlineData(StatusEnvio.EmSeparacao, StatusPedido.EmPreparacao)]
    [InlineData(StatusEnvio.ProntoParaEnvio, StatusPedido.EmPreparacao)]
    [InlineData(StatusEnvio.Expedido, StatusPedido.Enviado)]
    [InlineData(StatusEnvio.SaiuParaEntrega, StatusPedido.Enviado)]
    [InlineData(StatusEnvio.Entregue, StatusPedido.Entregue)]
    public void Pedido_acompanha_o_envio(StatusEnvio envio, StatusPedido esperado)
    {
        var pedido = PedidoPago();

        pedido.AcompanharEnvios([envio]);

        Assert.Equal(esperado, pedido.Status);
    }

    [Fact]
    public void Pedido_com_varios_envios_fica_na_etapa_do_mais_atrasado()
    {
        var pedido = PedidoPago();

        pedido.AcompanharEnvios([StatusEnvio.Entregue, StatusEnvio.EmTransito]);

        Assert.Equal(StatusPedido.Enviado, pedido.Status);
    }

    [Theory]
    [InlineData("1234567")]
    [InlineData("ABCDEFGH")]
    public void Endereco_recusa_cep_invalido(string cep)
    {
        Assert.Throws<DomainException>(() => Exemplos.Endereco(cep));
    }

    [Fact]
    public void Endereco_recusa_uf_inexistente()
    {
        Assert.Throws<DomainException>(() => (Exemplos.EnderecoRequest() with { Uf = "XX" }).ParaDominio());
    }
}
