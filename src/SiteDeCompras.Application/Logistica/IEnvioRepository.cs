using SiteDeCompras.Application.Common;
using SiteDeCompras.Domain.Logistica;

namespace SiteDeCompras.Application.Logistica;

public interface IEnvioRepository
{
    Task<Envio?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<Envio?> ObterPorCodigoAsync(string codigoRastreio, CancellationToken ct = default);
    Task<IReadOnlyList<Envio>> ListarDoPedidoAsync(Guid pedidoId, CancellationToken ct = default);
    Task<PagedResult<Envio>> ListarAsync(EnvioFiltro filtro, DateTime agoraUtc, CancellationToken ct = default);
    Task<IReadOnlyDictionary<StatusEnvio, int>> ContarPorStatusAsync(CancellationToken ct = default);
    /// <summary>Envios ainda no centro de distribuição cujo prazo de despacho termina antes de <paramref name="limiteUtc"/>.</summary>
    Task<int> ContarNoCentroComPrazoAteAsync(DateTime limiteUtc, CancellationToken ct = default);
    Task<int> ContarEntreguesEntreAsync(DateTime inicioUtc, DateTime fimUtc, CancellationToken ct = default);
    void Adicionar(Envio envio);
}
