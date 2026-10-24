namespace SiteDeCompras.Domain.Logistica;

/// <summary>
/// Etapas de um envio, na ordem em que acontecem. As cinco primeiras são dentro do centro de
/// distribuição; a partir de <see cref="Expedido"/> o pacote está com a transportadora.
/// </summary>
public enum StatusEnvio
{
    AguardandoSeparacao,
    EmSeparacao,
    Separado,
    Embalado,
    ProntoParaEnvio,
    Expedido,
    EmTransito,
    SaiuParaEntrega,
    Entregue
}

public static class StatusEnvioExtensions
{
    /// <summary>Ainda no centro de distribuição, ou seja, conta para o prazo de despacho.</summary>
    public static bool NoCentroDeDistribuicao(this StatusEnvio status) => status < StatusEnvio.Expedido;

    public static IReadOnlyList<StatusEnvio> EtapasNoCentroDeDistribuicao { get; } =
        Enum.GetValues<StatusEnvio>().Where(s => s.NoCentroDeDistribuicao()).ToList();
}
