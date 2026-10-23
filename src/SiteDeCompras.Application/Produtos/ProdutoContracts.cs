using System.ComponentModel.DataAnnotations;
using SiteDeCompras.Domain.Produtos;

namespace SiteDeCompras.Application.Produtos;

public record ProdutoDto(
    int Id,
    string Nome,
    string? Descricao,
    decimal Preco,
    string? ImagemUrl,
    int Estoque,
    decimal DescontoPix,
    DateTime CriadoEm,
    DateTime AtualizadoEm)
{
    public static ProdutoDto De(Produto p) =>
        new(p.Id, p.Nome, p.Descricao, p.Preco, p.ImagemUrl, p.Estoque, p.DescontoPix, p.CriadoEm, p.AtualizadoEm);
}

public enum OrdenacaoProduto
{
    Nome,
    MenorPreco,
    MaiorPreco,
    MaisRecentes
}

public class ProdutoFiltro
{
    /// <summary>Busca por parte do nome.</summary>
    [MaxLength(100)]
    public string? Busca { get; init; }

    [Range(0, double.MaxValue)]
    public decimal? PrecoMin { get; init; }

    [Range(0, double.MaxValue)]
    public decimal? PrecoMax { get; init; }

    public OrdenacaoProduto Ordenacao { get; init; } = OrdenacaoProduto.Nome;

    [Range(1, int.MaxValue)]
    public int Pagina { get; init; } = 1;

    [Range(1, 100)]
    public int TamanhoPagina { get; init; } = 20;
}

public record SalvarProdutoRequest
{
    [Required, StringLength(150, MinimumLength = 2)]
    public string Nome { get; init; } = string.Empty;

    [StringLength(2000)]
    public string? Descricao { get; init; }

    // Limites lidos em cultura invariante: num Windows em pt-BR, "0.01" não seria entendido.
    [Range(typeof(decimal), "0.01", "9999999", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Preco { get; init; }

    [StringLength(500)]
    public string? ImagemUrl { get; init; }

    [Range(0, int.MaxValue)]
    public int Estoque { get; init; }

    /// <summary>Desconto (%) no pagamento via Pix, de 5 a 15.</summary>
    [Range(typeof(decimal), "5", "15", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal DescontoPix { get; init; } = Produto.DescontoPixMinimo;
}
