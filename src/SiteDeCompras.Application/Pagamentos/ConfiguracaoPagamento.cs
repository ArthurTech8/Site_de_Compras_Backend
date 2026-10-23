namespace SiteDeCompras.Application.Pagamentos;

/// <summary>
/// Parâmetros do pagamento, lidos da seção "Pagamento" do appsettings.
/// </summary>
public record ConfiguracaoPagamento
{
    /// <summary>
    /// Juros mensais do crédito parcelado (0,0299 = 2,99% a.m.). É a faixa que o varejo online
    /// costuma cobrar no "parcelado com juros" (1,99% a 3,49% a.m.).
    /// </summary>
    public decimal TaxaJurosMensalCredito { get; init; } = 0.0299m;
}
