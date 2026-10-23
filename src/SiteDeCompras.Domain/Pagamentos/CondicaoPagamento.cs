namespace SiteDeCompras.Domain.Pagamentos;

/// <summary>
/// Quanto o cliente paga numa forma de pagamento: o subtotal do carrinho, o desconto ou os juros
/// aplicados e como o total fica dividido. Todos os valores já vêm arredondados em centavos.
/// </summary>
public record CondicaoPagamento(
    FormaPagamento Forma,
    decimal Subtotal,
    decimal DescontoPercentual,
    decimal Desconto,
    decimal TaxaJurosMensal,
    decimal Juros,
    decimal Total,
    int Parcelas,
    decimal ValorParcela);
