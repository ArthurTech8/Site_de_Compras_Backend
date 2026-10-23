using SiteDeCompras.Domain.Pagamentos;

namespace SiteDeCompras.Application.Pagamentos;

/// <summary>As três formas de pagamento calculadas sobre o total do carrinho.</summary>
public record OpcoesPagamentoDto(
    Guid CarrinhoId,
    decimal Subtotal,
    OpcaoPixDto Pix,
    OpcaoDebitoDto Debito,
    OpcaoCreditoDto Credito);

/// <param name="DescontoPercentual">O maior desconto entre os produtos do carrinho, aplicado uma vez.</param>
/// <param name="ProdutoDoDesconto">O produto que deu o maior desconto.</param>
/// <param name="DescontosPorProduto">O desconto de cada produto, para o cliente ver de onde veio.</param>
public record OpcaoPixDto(
    decimal DescontoPercentual,
    decimal Desconto,
    decimal Total,
    DescontoPixProdutoDto? ProdutoDoDesconto,
    IReadOnlyList<DescontoPixProdutoDto> DescontosPorProduto);

public record DescontoPixProdutoDto(int ProdutoId, string NomeProduto, decimal DescontoPercentual);

public record OpcaoDebitoDto(decimal Total);

public record OpcaoCreditoDto(
    decimal TaxaJurosMensal,
    int ParcelasMinimas,
    int ParcelasMaximas,
    IReadOnlyList<ParcelamentoDto> Parcelamentos);

/// <param name="Juros">Quanto o total parcelado fica acima do total do carrinho.</param>
public record ParcelamentoDto(int Parcelas, decimal ValorParcela, decimal Total, decimal Juros)
{
    public static ParcelamentoDto De(CondicaoPagamento c) => new(c.Parcelas, c.ValorParcela, c.Total, c.Juros);
}

public record CondicaoPagamentoDto(
    FormaPagamento Forma,
    decimal Subtotal,
    decimal DescontoPercentual,
    decimal Desconto,
    decimal TaxaJurosMensal,
    decimal Juros,
    decimal Total,
    int Parcelas,
    decimal ValorParcela)
{
    public static CondicaoPagamentoDto De(CondicaoPagamento c) => new(
        c.Forma, c.Subtotal, c.DescontoPercentual, c.Desconto, c.TaxaJurosMensal, c.Juros, c.Total, c.Parcelas, c.ValorParcela);
}
