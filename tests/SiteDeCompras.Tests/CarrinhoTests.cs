using SiteDeCompras.Domain.Carrinhos;
using SiteDeCompras.Domain.Common;
using SiteDeCompras.Domain.Produtos;

namespace SiteDeCompras.Tests;

public class CarrinhoTests
{
    private static Produto NovoProduto(decimal preco = 10m, int estoque = 5) => new("Caneca", preco, estoque);

    [Fact]
    public void Adicionar_o_mesmo_produto_soma_a_quantidade()
    {
        var carrinho = new Carrinho();
        var produto = NovoProduto();

        carrinho.AdicionarItem(produto, 1);
        carrinho.AdicionarItem(produto, 2);

        var item = Assert.Single(carrinho.Itens);
        Assert.Equal(3, item.Quantidade);
        Assert.Equal(30m, carrinho.Total);
    }

    [Fact]
    public void Nao_permite_quantidade_acima_do_estoque()
    {
        var carrinho = new Carrinho();
        var produto = NovoProduto(estoque: 2);
        carrinho.AdicionarItem(produto, 2);

        Assert.Throws<DomainException>(() => carrinho.AdicionarItem(produto, 1));
    }

    [Fact]
    public void Nao_permite_produto_desativado()
    {
        var produto = NovoProduto();
        produto.Desativar();

        Assert.Throws<DomainException>(() => new Carrinho().AdicionarItem(produto, 1));
    }

    [Fact]
    public void Limpar_zera_o_total()
    {
        var carrinho = new Carrinho();
        carrinho.AdicionarItem(NovoProduto(), 2);

        carrinho.Limpar();

        Assert.Empty(carrinho.Itens);
        Assert.Equal(0m, carrinho.Total);
    }

    [Theory]
    [InlineData("", 10, 1)]
    [InlineData("Caneca", 0, 1)]
    [InlineData("Caneca", 10, -1)]
    public void Produto_invalido_lanca_excecao(string nome, decimal preco, int estoque)
    {
        Assert.Throws<DomainException>(() => new Produto(nome, preco, estoque));
    }
}
