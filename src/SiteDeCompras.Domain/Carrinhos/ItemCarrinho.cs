using SiteDeCompras.Domain.Common;

namespace SiteDeCompras.Domain.Carrinhos;

public class ItemCarrinho
{
    public int Id { get; private set; }
    public Guid CarrinhoId { get; private set; }
    public int ProdutoId { get; private set; }
    public string NomeProduto { get; private set; } = string.Empty;
    public string? ImagemUrl { get; private set; }
    public decimal PrecoUnitario { get; private set; }
    public int Quantidade { get; private set; }

    public decimal Subtotal => PrecoUnitario * Quantidade;

    private ItemCarrinho() { }

    internal ItemCarrinho(Guid carrinhoId, int produtoId, string nomeProduto, string? imagemUrl, decimal precoUnitario, int quantidade)
    {
        CarrinhoId = carrinhoId;
        ProdutoId = produtoId;
        NomeProduto = nomeProduto;
        ImagemUrl = imagemUrl;
        PrecoUnitario = precoUnitario;
        DefinirQuantidade(quantidade);
    }

    internal void DefinirQuantidade(int quantidade)
    {
        if (quantidade <= 0)
            throw new DomainException("A quantidade deve ser maior que zero.");
        Quantidade = quantidade;
    }
}
