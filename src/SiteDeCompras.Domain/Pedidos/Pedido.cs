using SiteDeCompras.Domain.Carrinhos;
using SiteDeCompras.Domain.Common;
using SiteDeCompras.Domain.Pagamentos;
using SiteDeCompras.Domain.Produtos;

namespace SiteDeCompras.Domain.Pedidos;

public class Pedido
{
    private readonly List<ItemPedido> _itens = [];

    public Guid Id { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public StatusPedido Status { get; private set; }

    /// <summary>Dono do pedido. Nulo quando a compra foi feita sem login.</summary>
    public Guid? UsuarioId { get; private set; }
    public CondicaoPagamento Pagamento { get; private set; } = null!;
    public IReadOnlyCollection<ItemPedido> Itens => _itens.AsReadOnly();

    private Pedido() { }

    /// <summary>
    /// Fecha o carrinho em um pedido pago: copia os itens, baixa o estoque e esvazia o carrinho.
    /// </summary>
    /// <param name="produtos">Os produtos dos itens do carrinho, para baixar o estoque.</param>
    /// <param name="pagamento">Condição calculada sobre o total deste carrinho.</param>
    /// <param name="usuarioId">Quem está logado, se houver.</param>
    public static Pedido Fechar(
        Carrinho carrinho, IReadOnlyCollection<Produto> produtos, CondicaoPagamento pagamento, Guid? usuarioId = null)
    {
        if (carrinho.Itens.Count == 0)
            throw new DomainException("O carrinho está vazio.");
        if (pagamento.Subtotal != carrinho.Total)
            throw new DomainException("O pagamento não corresponde ao total do carrinho.");

        var pedido = new Pedido
        {
            Id = Guid.NewGuid(),
            CriadoEm = DateTime.UtcNow,
            Status = StatusPedido.Pago,
            Pagamento = pagamento,
            UsuarioId = usuarioId,
        };

        foreach (var item in carrinho.Itens.OrderBy(i => i.Id))
        {
            var produto = produtos.FirstOrDefault(p => p.Id == item.ProdutoId)
                ?? throw new DomainException($"O produto '{item.NomeProduto}' não está disponível.");
            produto.BaixarEstoque(item.Quantidade);
            pedido._itens.Add(new ItemPedido(
                pedido.Id, item.ProdutoId, item.NomeProduto, item.ImagemUrl, item.PrecoUnitario, item.Quantidade));
        }

        carrinho.Limpar();
        return pedido;
    }

    /// <summary>
    /// Pedido feito sem login: qualquer um com o id (um GUID aleatório) consegue ver. Com dono:
    /// só o dono ou um admin.
    /// </summary>
    public bool PodeSerVistoPor(Guid? usuarioId, bool admin) => admin || UsuarioId is null || UsuarioId == usuarioId;
}
