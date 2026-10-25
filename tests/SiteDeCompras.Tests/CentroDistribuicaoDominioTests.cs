using SiteDeCompras.Domain.Carrinhos;
using SiteDeCompras.Domain.Common;
using SiteDeCompras.Domain.Logistica;
using SiteDeCompras.Domain.Pagamentos;
using SiteDeCompras.Domain.Pedidos;
using SiteDeCompras.Domain.Produtos;

namespace SiteDeCompras.Tests;

public class CentroDistribuicaoDominioTests
{
    private static readonly DateTime Agora = new(2026, 10, 26, 13, 0, 0, DateTimeKind.Utc);

    private static (Envio Envio, Produto Produto) EnvioDe(int quantidade = 2)
    {
        var produto = new Produto("Caneca", 10m, 10);
        typeof(Produto).GetProperty(nameof(Produto.Id))!.SetValue(produto, 5);
        var carrinho = new Carrinho();
        carrinho.AdicionarItem(produto, quantidade);
        var pedido = Pedido.Fechar(carrinho, [produto], CalculadoraPagamento.Debito(carrinho.Total), Exemplos.Endereco());
        return (Envio.Criar(pedido, Agora), produto);
    }

    private static Pacote PacoteM() => new("M", 1200, 15, 20, 30);

    [Fact]
    public void Envio_nasce_com_os_itens_do_pedido()
    {
        var (envio, produto) = EnvioDe(quantidade: 3);

        var item = Assert.Single(envio.Itens);
        Assert.Equal(produto.Id, item.ProdutoId);
        Assert.Equal(3, item.Quantidade);
        Assert.Equal(0, item.QuantidadeSeparada);
    }

    [Fact]
    public void Envio_passa_por_todas_as_etapas_ate_a_entrega()
    {
        var (envio, produto) = EnvioDe();

        envio.IniciarSeparacao(ondaId: 1, Agora);
        envio.Separar(produto.Id, Agora);
        Assert.Equal(StatusEnvio.EmSeparacao, envio.Status);
        envio.Separar(produto.Id, Agora);
        Assert.Equal(StatusEnvio.Separado, envio.Status);

        envio.Conferir(produto.Id);
        envio.Conferir(produto.Id);
        envio.Embalar(PacoteM(), Agora);
        Assert.Null(envio.CodigoRastreio);
        envio.Etiquetar(Agora);
        Assert.Matches(@"^SC\d{9}BR$", envio.CodigoRastreio);
        Assert.Equal(Envio.TransportadoraPadrao, envio.Transportadora);

        var manifesto = new Manifesto(Envio.TransportadoraPadrao, Agora);
        envio.IncluirNoManifesto(manifesto);
        manifesto.Fechar(1, Agora);
        manifesto.Coletar([envio], Agora);
        Assert.Equal(StatusEnvio.Expedido, envio.Status);

        while (envio.Status != StatusEnvio.Entregue)
            envio.Avancar(Agora);

        Assert.Equal(Enum.GetValues<StatusEnvio>(), envio.Eventos.Select(e => e.Status));
        Assert.Throws<DomainException>(() => envio.Avancar(Agora));
    }

    [Fact]
    public void Avancar_nao_vale_dentro_do_cd()
    {
        var (envio, _) = EnvioDe();

        var erro = Assert.Throws<DomainException>(() => envio.Avancar(Agora));
        Assert.Contains("separação, embalagem e expedição", erro.Message);
    }

    [Fact]
    public void Separar_alem_do_pedido_e_recusado()
    {
        var (envio, produto) = EnvioDe(quantidade: 1);
        envio.IniciarSeparacao(1, Agora);
        envio.Separar(produto.Id, Agora);

        Assert.Throws<DomainException>(() => envio.Separar(produto.Id, Agora));
    }

    [Fact]
    public void Falta_devolve_o_envio_para_a_fila_com_um_evento_interno()
    {
        var (envio, produto) = EnvioDe();
        envio.IniciarSeparacao(7, Agora);
        envio.Separar(produto.Id, Agora);

        envio.RegistrarFalta(produto.Nome, Agora);

        Assert.Equal(StatusEnvio.AguardandoSeparacao, envio.Status);
        Assert.Null(envio.OndaId);
        Assert.All(envio.Itens, i => Assert.Equal(0, i.QuantidadeSeparada));
        var ultimo = envio.Eventos.Last();
        Assert.False(ultimo.Publico);
        Assert.Contains("Caneca", ultimo.Descricao);
    }

    [Fact]
    public void Embalar_exige_todos_os_itens_conferidos()
    {
        var (envio, produto) = EnvioDe();
        envio.IniciarSeparacao(1, Agora);
        envio.Separar(produto.Id, Agora);
        envio.Separar(produto.Id, Agora);
        envio.Conferir(produto.Id);

        var erro = Assert.Throws<DomainException>(() => envio.Embalar(PacoteM(), Agora));
        Assert.Contains("1 unidade", erro.Message);
    }

    [Fact]
    public void Manifesto_so_aceita_pacote_etiquetado_e_nao_fecha_vazio()
    {
        var (envio, _) = EnvioDe();
        var manifesto = new Manifesto(Envio.TransportadoraPadrao, Agora);

        Assert.Throws<DomainException>(() => envio.IncluirNoManifesto(manifesto));
        Assert.Throws<DomainException>(() => manifesto.Fechar(0, Agora));
        Assert.Throws<DomainException>(() => manifesto.Coletar([], Agora));
    }

    [Fact]
    public void Onda_so_conclui_com_tudo_separado()
    {
        var (envio, produto) = EnvioDe(quantidade: 1);
        var onda = new OndaSeparacao(Agora);
        envio.IniciarSeparacao(1, Agora);

        Assert.Throws<DomainException>(() => onda.Concluir([envio], Agora));
        envio.Separar(produto.Id, Agora);
        onda.Concluir([envio], Agora);
        Assert.Equal(StatusOnda.Concluida, onda.Status);
    }

    // Celular: 6×10×18 cm. Caneca: 11×11×13 cm, que já não cabe na altura de 10 cm da caixa P.
    [Theory]
    [InlineData(6, 10, 18, 1, "P")]
    [InlineData(11, 11, 13, 1, "M")]
    [InlineData(11, 11, 13, 6, "G")]
    public void Sugere_a_menor_caixa_onde_tudo_cabe(int altura, int largura, int comprimento, int unidades, string caixa)
    {
        Assert.Equal(caixa, Embalagem.Sugerir([new VolumeItem(altura, largura, comprimento, 400, unidades)]).Codigo);
    }

    [Fact]
    public void Produto_maior_que_as_caixas_vai_avulso()
    {
        var geladeira = new VolumeItem(175, 65, 72, 68000, 1);

        Assert.Equal(Embalagem.CodigoAvulso, Embalagem.Sugerir([geladeira]).Codigo);
        Assert.Throws<DomainException>(() => Embalagem.Obter("GG", [geladeira]));
    }

    [Theory]
    [InlineData(1000, true)]
    [InlineData(1190, true)]
    [InlineData(1300, false)]
    [InlineData(700, false)]
    public void Peso_da_balanca_precisa_ficar_perto_do_esperado(int balanca, bool aceito)
    {
        var erro = Record.Exception(() => Embalagem.ConferirPeso(balanca, 1000));

        Assert.Equal(aceito, erro is null);
    }

    [Theory]
    [InlineData("a-03-2", "A-03-2")]
    [InlineData(" B-11-1 ", "B-11-1")]
    public void Produto_aceita_endereco_no_formato_do_cd(string local, string esperado)
    {
        var produto = new Produto("Caneca", 10m, 1);

        produto.MoverPara(local);

        Assert.Equal(esperado, produto.LocalEstoque);
    }

    [Theory]
    [InlineData("A3-2")]
    [InlineData("corredor 3")]
    public void Produto_recusa_endereco_fora_do_formato(string local)
    {
        Assert.Throws<DomainException>(() => new Produto("Caneca", 10m, 1).MoverPara(local));
    }

    [Fact]
    public void Produto_novo_ganha_um_sku_e_o_sku_e_validado()
    {
        var produto = new Produto("Caneca", 10m, 1);

        Assert.Matches("^PRD-[0-9A-F]{8}$", produto.Sku);
        produto.DefinirDadosLogisticos(" can-0099 ", 300, null, null, null);
        Assert.Equal("CAN-0099", produto.Sku);
        Assert.Throws<DomainException>(() => produto.DefinirDadosLogisticos("x", null, null, null, null));
    }
}
