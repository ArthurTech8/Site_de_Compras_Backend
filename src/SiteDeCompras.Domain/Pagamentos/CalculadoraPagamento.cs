using SiteDeCompras.Domain.Common;

namespace SiteDeCompras.Domain.Pagamentos;

/// <summary>
/// Regras de preço de cada forma de pagamento. Não acessa banco nem gateway: recebe o subtotal
/// do carrinho e devolve a condição calculada.
/// </summary>
public static class CalculadoraPagamento
{
    public const int ParcelasMinimas = 6;
    public const int ParcelasMaximas = 16;

    /// <summary>
    /// Pix: o desconto é aplicado uma única vez sobre o subtotal. Os descontos dos produtos não
    /// se somam; quem chama passa o maior deles.
    /// </summary>
    public static CondicaoPagamento Pix(decimal subtotal, decimal descontoPercentual)
    {
        GarantirSubtotal(subtotal);
        if (descontoPercentual is < 0 or >= 100)
            throw new DomainException("Desconto do Pix inválido.");

        var desconto = Arredondar(subtotal * descontoPercentual / 100m);
        var total = subtotal - desconto;
        return new CondicaoPagamento(FormaPagamento.Pix, subtotal, descontoPercentual, desconto, 0m, 0m, total, 1, total);
    }

    /// <summary>Débito: à vista, sem desconto nem juros.</summary>
    public static CondicaoPagamento Debito(decimal subtotal)
    {
        GarantirSubtotal(subtotal);
        return new CondicaoPagamento(FormaPagamento.Debito, subtotal, 0m, 0m, 0m, 0m, subtotal, 1, subtotal);
    }

    /// <summary>
    /// Crédito parcelado com juros compostos pela Tabela Price: parcelas fixas
    /// PMT = PV · i / (1 − (1 + i)^−n). O total é a parcela arredondada vezes o número de parcelas.
    /// </summary>
    public static CondicaoPagamento Credito(decimal subtotal, int parcelas, decimal taxaJurosMensal)
    {
        GarantirSubtotal(subtotal);
        if (parcelas is < ParcelasMinimas or > ParcelasMaximas)
            throw new DomainException($"O crédito deve ser parcelado de {ParcelasMinimas} a {ParcelasMaximas} vezes.");
        if (taxaJurosMensal < 0)
            throw new DomainException("A taxa de juros não pode ser negativa.");

        var valorParcela = taxaJurosMensal == 0
            ? subtotal / parcelas
            : subtotal * taxaJurosMensal / (1 - 1 / Potencia(1 + taxaJurosMensal, parcelas));
        valorParcela = Arredondar(valorParcela);

        var total = valorParcela * parcelas;
        return new CondicaoPagamento(
            FormaPagamento.Credito, subtotal, 0m, 0m, taxaJurosMensal, total - subtotal, total, parcelas, valorParcela);
    }

    /// <summary>Todas as opções de parcelamento permitidas, da menor para a maior.</summary>
    public static IReadOnlyList<CondicaoPagamento> OpcoesCredito(decimal subtotal, decimal taxaJurosMensal) =>
        Enumerable.Range(ParcelasMinimas, ParcelasMaximas - ParcelasMinimas + 1)
            .Select(n => Credito(subtotal, n, taxaJurosMensal))
            .ToList();

    private static void GarantirSubtotal(decimal subtotal)
    {
        if (subtotal <= 0)
            throw new DomainException("O carrinho está vazio.");
    }

    // Potência em decimal, para não perder centavos com double.
    private static decimal Potencia(decimal @base, int expoente)
    {
        var resultado = 1m;
        for (var i = 0; i < expoente; i++)
            resultado *= @base;
        return resultado;
    }

    private static decimal Arredondar(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}
