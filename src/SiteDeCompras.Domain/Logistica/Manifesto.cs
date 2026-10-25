using SiteDeCompras.Domain.Common;

namespace SiteDeCompras.Domain.Logistica;

public enum StatusManifesto
{
    Aberto,
    Fechado,
    Coletado
}

/// <summary>
/// O romaneio de coleta: a lista de pacotes que a transportadora leva da doca. Os envios apontam
/// para o manifesto em <see cref="Envio.ManifestoId"/>.
/// </summary>
public class Manifesto
{
    public int Id { get; private set; }
    public string Transportadora { get; private set; } = string.Empty;
    public StatusManifesto Status { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime? FechadoEm { get; private set; }
    public DateTime? ColetadoEm { get; private set; }

    private Manifesto() { }

    public Manifesto(string transportadora, DateTime agoraUtc)
    {
        Transportadora = transportadora;
        Status = StatusManifesto.Aberto;
        CriadoEm = agoraUtc;
    }

    public void GarantirAberto()
    {
        if (Status != StatusManifesto.Aberto)
            throw new DomainException("O manifesto já foi fechado.");
    }

    public void Fechar(int quantidadeEnvios, DateTime agoraUtc)
    {
        GarantirAberto();
        if (quantidadeEnvios == 0)
            throw new DomainException("Bipe pelo menos um pacote antes de fechar o manifesto.");
        Status = StatusManifesto.Fechado;
        FechadoEm = agoraUtc;
    }

    /// <summary>A transportadora levou os pacotes. Cada envio do manifesto é expedido junto.</summary>
    public void Coletar(IReadOnlyCollection<Envio> envios, DateTime agoraUtc)
    {
        if (Status != StatusManifesto.Fechado)
            throw new DomainException(Status == StatusManifesto.Coletado
                ? "O manifesto já foi coletado."
                : "Feche o manifesto antes de confirmar a coleta.");

        foreach (var envio in envios)
            envio.Expedir(agoraUtc);
        Status = StatusManifesto.Coletado;
        ColetadoEm = agoraUtc;
    }
}
