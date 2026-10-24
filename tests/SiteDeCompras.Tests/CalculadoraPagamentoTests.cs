using SiteDeCompras.Domain.Carrinhos;
using SiteDeCompras.Domain.Common;
using SiteDeCompras.Domain.Pagamentos;
using SiteDeCompras.Domain.Pedidos;
using SiteDeCompras.Domain.Produtos;

namespace SiteDeCompras.Tests;

public class CalculadoraPagamentoTests
{
    [Fact]
    public void Pix_aplica_o_desconto_uma_vez_sobre_o_total()
    {
        var pix = CalculadoraPagamento.Pix(2418m, 10m);

        Assert.Equal(241.80m, pix.Desconto);
        Assert.Equal(2176.20m, pix.Total);
        Assert.Equal(1, pix.Parcelas);
    }

    [Fact]
    public void Debito_nao_altera_o_total()
    {
        var debito = CalculadoraPagamento.Debito(2418m);

        Assert.Equal(2418m, debito.Total);
        Assert.Equal(0m, debito.Desconto);
        Assert.Equal(0m, debito.Juros);
    }

    [Theory]
    [InlineData(6, 184.54, 1107.24)]
    [InlineData(10, 117.17, 1171.70)]
    [InlineData(12, 100.40, 1204.80)]
    [InlineData(16, 79.55, 1272.80)]
    public void Credito_segue_a_tabela_price(int parcelas, decimal valorParcela, decimal total)
    {
        var credito = CalculadoraPagamento.Credito(1000m, parcelas, 0.0299m);

        Assert.Equal(valorParcela, credito.ValorParcela);
        Assert.Equal(total, credito.Total);
        Assert.Equal(total - 1000m, credito.Juros);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(17)]
    public void Credito_so_aceita_de_6_a_16_parcelas(int parcelas)
    {
        Assert.Throws<DomainException>(() => CalculadoraPagamento.Credito(1000m, parcelas, 0.0299m));
    }

    [Fact]
    public void Opcoes_de_credito_vao_de_6_a_16_parcelas()
    {
        var opcoes = CalculadoraPagamento.OpcoesCredito(1000m, 0.0299m);

        Assert.Equal(Enumerable.Range(6, 11), opcoes.Select(o => o.Parcelas));
    }

    [Theory]
    [InlineData(4.99)]
    [InlineData(15.01)]
    public void Desconto_pix_do_produto_fica_entre_5_e_15(decimal desconto)
    {
        Assert.Throws<DomainException>(() => new Produto("Caneca", 10m, 1, descontoPix: desconto));
    }

    [Fact]
    public void Fechar_pedido_baixa_o_estoque_e_esvazia_o_carrinho()
    {
        var produto = new Produto("Caneca", 10m, 5);
        var carrinho = new Carrinho();
        carrinho.AdicionarItem(produto, 2);

        var pedido = Pedido.Fechar(carrinho, [produto], CalculadoraPagamento.Debito(carrinho.Total), Exemplos.Endereco());

        Assert.Equal(StatusPedido.Pago, pedido.Status);
        Assert.Equal(2, Assert.Single(pedido.Itens).Quantidade);
        Assert.Equal(3, produto.Estoque);
        Assert.Empty(carrinho.Itens);
    }

    [Fact]
    public void Nao_fecha_pedido_com_pagamento_de_outro_total()
    {
        var produto = new Produto("Caneca", 10m, 5);
        var carrinho = new Carrinho();
        carrinho.AdicionarItem(produto, 2);

        Assert.Throws<DomainException>(() => Pedido.Fechar(carrinho, [produto], CalculadoraPagamento.Debito(99m), Exemplos.Endereco()));
    }
}
