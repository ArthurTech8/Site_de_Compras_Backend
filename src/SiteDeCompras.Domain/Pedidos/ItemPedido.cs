namespace SiteDeCompras.Domain.Pedidos;

/// <summary>
/// Cópia do item no momento da compra: mudanças futuras no produto não alteram o pedido.
/// </summary>
public class ItemPedido
{
    public int Id { get; private set; }
    public Guid PedidoId { get; private set; }
    public int ProdutoId { get; private set; }
    public string NomeProduto { get; private set; } = string.Empty;
    public string? ImagemUrl { get; private set; }
    public decimal PrecoUnitario { get; private set; }
    public int Quantidade { get; private set; }

    public decimal Subtotal => PrecoUnitario * Quantidade;

    private ItemPedido() { }

    internal ItemPedido(Guid pedidoId, int produtoId, string nomeProduto, string? imagemUrl, decimal precoUnitario, int quantidade)
    {
        PedidoId = pedidoId;
        ProdutoId = produtoId;
        NomeProduto = nomeProduto;
        ImagemUrl = imagemUrl;
        PrecoUnitario = precoUnitario;
        Quantidade = quantidade;
    }
}
