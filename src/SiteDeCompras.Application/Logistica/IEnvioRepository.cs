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

    // ---------- Centro de distribuição ----------

    /// <summary>
    /// Os envios mais urgentes na fila de separação, deixando de fora os que têm algum produto em
    /// <paramref name="produtosBloqueados"/> (com divergência de estoque aberta).
    /// </summary>
    Task<IReadOnlyList<Envio>> ListarParaOndaAsync(int quantidade, IReadOnlyCollection<int> produtosBloqueados, CancellationToken ct = default);
    Task<IReadOnlyList<Envio>> ListarDaOndaAsync(int ondaId, CancellationToken ct = default);
    Task<IReadOnlyList<Envio>> ListarDoManifestoAsync(int manifestoId, CancellationToken ct = default);

    /// <summary>Pacotes etiquetados da transportadora que ainda não entraram em nenhum manifesto.</summary>
    Task<int> ContarProntosSemManifestoAsync(string transportadora, CancellationToken ct = default);

    /// <summary>O envio do código, para alterar (o <see cref="ObterPorCodigoAsync"/> é só leitura).</summary>
    Task<Envio?> ObterParaAlterarPorCodigoAsync(string codigoRastreio, CancellationToken ct = default);

    /// <summary>
    /// Unidades vendidas que ainda estão na prateleira, por produto: as dos envios na fila ou na
    /// separação que ainda não foram separadas.
    /// </summary>
    Task<IReadOnlyDictionary<int, int>> ContarReservadoPorProdutoAsync(CancellationToken ct = default);
}
