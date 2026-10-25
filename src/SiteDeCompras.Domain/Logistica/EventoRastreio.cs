namespace SiteDeCompras.Domain.Logistica;

/// <summary>
/// Um passo do histórico do envio. O histórico só cresce: nenhum evento é editado ou apagado.
/// </summary>
public class EventoRastreio
{
    public int Id { get; private set; }
    public Guid EnvioId { get; private set; }
    public DateTime Data { get; private set; }
    public StatusEnvio Status { get; private set; }
    public string Descricao { get; private set; } = string.Empty;

    /// <summary>Cidade e UF onde o evento aconteceu, como "São Paulo, SP".</summary>
    public string Local { get; private set; } = string.Empty;

    private EventoRastreio() { }

    internal EventoRastreio(Guid envioId, DateTime data, StatusEnvio status, string descricao, string local)
    {
        EnvioId = envioId;
        Data = data;
        Status = status;
        Descricao = descricao;
        Local = local;
    }
}
