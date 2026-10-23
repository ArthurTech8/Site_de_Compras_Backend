using System.ComponentModel.DataAnnotations;
using SiteDeCompras.Application.Pagamentos;
using SiteDeCompras.Domain.Pagamentos;
using SiteDeCompras.Domain.Pedidos;

namespace SiteDeCompras.Application.Pedidos;

public record ItemPedidoDto(
    int ProdutoId,
    string NomeProduto,
    string? ImagemUrl,
    decimal PrecoUnitario,
    int Quantidade,
    decimal Subtotal);

public record PedidoDto(
    Guid Id,
    StatusPedido Status,
    DateTime CriadoEm,
    IReadOnlyList<ItemPedidoDto> Itens,
    CondicaoPagamentoDto Pagamento)
{
    public static PedidoDto De(Pedido p) => new(
        p.Id,
        p.Status,
        p.CriadoEm,
        p.Itens
            .OrderBy(i => i.Id)
            .Select(i => new ItemPedidoDto(i.ProdutoId, i.NomeProduto, i.ImagemUrl, i.PrecoUnitario, i.Quantidade, i.Subtotal))
            .ToList(),
        CondicaoPagamentoDto.De(p.Pagamento));
}

public record FecharPedidoRequest
{
    [Required]
    public Guid CarrinhoId { get; init; }

    [Required]
    public FormaPagamento? FormaPagamento { get; init; }

    /// <summary>Obrigatório no crédito: de 6 a 16.</summary>
    [Range(1, 99)]
    public int? Parcelas { get; init; }
}
