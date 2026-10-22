using System.ComponentModel.DataAnnotations;
using SiteDeCompras.Domain.Carrinhos;

namespace SiteDeCompras.Application.Carrinhos;

public record ItemCarrinhoDto(
    int ProdutoId,
    string NomeProduto,
    string? ImagemUrl,
    decimal PrecoUnitario,
    int Quantidade,
    decimal Subtotal);

public record CarrinhoDto(
    Guid Id,
    IReadOnlyList<ItemCarrinhoDto> Itens,
    int QuantidadeTotal,
    decimal Total,
    DateTime AtualizadoEm)
{
    public static CarrinhoDto De(Carrinho c) => new(
        c.Id,
        c.Itens
            .OrderBy(i => i.Id)
            .Select(i => new ItemCarrinhoDto(i.ProdutoId, i.NomeProduto, i.ImagemUrl, i.PrecoUnitario, i.Quantidade, i.Subtotal))
            .ToList(),
        c.QuantidadeTotal,
        c.Total,
        c.AtualizadoEm);
}

public record AdicionarItemRequest
{
    [Range(1, int.MaxValue)]
    public int ProdutoId { get; init; }

    [Range(1, 999)]
    public int Quantidade { get; init; } = 1;
}

public record AlterarQuantidadeRequest
{
    [Range(1, 999)]
    public int Quantidade { get; init; }
}
