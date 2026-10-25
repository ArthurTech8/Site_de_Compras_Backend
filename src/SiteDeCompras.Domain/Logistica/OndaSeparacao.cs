using SiteDeCompras.Domain.Common;

namespace SiteDeCompras.Domain.Logistica;

public enum StatusOnda
{
    Aberta,
    Concluida
}

/// <summary>
/// Um lote de envios separados de uma vez, numa só volta pelos corredores, como nas ondas de
/// separação da Amazon. Os envios apontam para a onda em <see cref="Envio.OndaId"/>.
/// </summary>
public class OndaSeparacao
{
    /// <summary>Envios por onda: o bastante para uma volta, sem encher o carrinho de coleta.</summary>
    public const int TamanhoPadrao = 10;
    public const int TamanhoMaximo = 30;

    public int Id { get; private set; }
    public StatusOnda Status { get; private set; }
    public DateTime CriadaEm { get; private set; }
    public DateTime? ConcluidaEm { get; private set; }

    private OndaSeparacao() { }

    public OndaSeparacao(DateTime agoraUtc)
    {
        Status = StatusOnda.Aberta;
        CriadaEm = agoraUtc;
    }

    /// <summary>Só fecha quando todos os envios que ficaram na onda foram separados.</summary>
    public void Concluir(IReadOnlyCollection<Envio> envios, DateTime agoraUtc)
    {
        if (Status == StatusOnda.Concluida)
            throw new DomainException("A onda já foi concluída.");
        var pendentes = envios.Count(e => e.Status == StatusEnvio.EmSeparacao);
        if (pendentes > 0)
            throw new DomainException(
                pendentes == 1 ? "Ainda falta separar 1 envio da onda." : $"Ainda falta separar {pendentes} envios da onda.");

        Status = StatusOnda.Concluida;
        ConcluidaEm = agoraUtc;
    }
}
