using System.ComponentModel.DataAnnotations;
using SiteDeCompras.Application.Enderecos;
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
    CondicaoPagamentoDto Pagamento,
    EnderecoDto? EnderecoEntrega)
{
    public static PedidoDto De(Pedido p) => new(
        p.Id,
        p.Status,
        p.CriadoEm,
        p.Itens
            .OrderBy(i => i.Id)
            .Select(i => new ItemPedidoDto(i.ProdutoId, i.NomeProduto, i.ImagemUrl, i.PrecoUnitario, i.Quantidade, i.Subtotal))
            .ToList(),
        CondicaoPagamentoDto.De(p.Pagamento),
        p.EnderecoEntrega is null ? null : EnderecoDto.De(p.EnderecoEntrega));
}

/// <summary>Uma linha de "Meus pedidos".</summary>
public record PedidoResumoDto(
    Guid Id,
    StatusPedido Status,
    DateTime CriadoEm,
    decimal Total,
    int QuantidadeItens,
    string PrimeiroItem,
    string? ImagemUrl)
{
    public static PedidoResumoDto De(Pedido p)
    {
        var primeiro = p.Itens.OrderBy(i => i.Id).First();
        return new(p.Id, p.Status, p.CriadoEm, p.Pagamento.Total, p.Itens.Sum(i => i.Quantidade), primeiro.NomeProduto, primeiro.ImagemUrl);
    }
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

    [Required]
    public EnderecoRequest? EnderecoEntrega { get; init; }
}
