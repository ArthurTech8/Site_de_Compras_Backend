using Microsoft.EntityFrameworkCore;
using SiteDeCompras.Application.Common;
using SiteDeCompras.Application.Logistica;
using SiteDeCompras.Domain.Logistica;
using SiteDeCompras.Infrastructure.Persistence;

namespace SiteDeCompras.Infrastructure.Repositories;

public class EnvioRepository(AppDbContext db) : IEnvioRepository
{
    // O status é gravado como texto, então "ainda no centro de distribuição" vira uma lista, não um "<".
    private static readonly List<StatusEnvio> NoCentro = StatusEnvioExtensions.EtapasNoCentroDeDistribuicao.ToList();

    public Task<Envio?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        db.Envios.Include(e => e.Eventos).FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<Envio?> ObterPorCodigoAsync(string codigoRastreio, CancellationToken ct = default) =>
        db.Envios.AsNoTracking().Include(e => e.Eventos).FirstOrDefaultAsync(e => e.CodigoRastreio == codigoRastreio, ct);

    public async Task<IReadOnlyList<Envio>> ListarDoPedidoAsync(Guid pedidoId, CancellationToken ct = default) =>
        await db.Envios.Include(e => e.Eventos)
            .Where(e => e.PedidoId == pedidoId)
            .OrderBy(e => e.CriadoEm)
            .ToListAsync(ct);

    public async Task<PagedResult<Envio>> ListarAsync(EnvioFiltro filtro, DateTime agoraUtc, CancellationToken ct = default)
    {
        var query = db.Envios.AsNoTracking();

        if (filtro.Status is { } status)
            query = query.Where(e => e.Status == status);
        if (filtro.Atrasados)
            query = query.Where(e => NoCentro.Contains(e.Status) && e.PrazoDespacho < agoraUtc);
        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            var busca = filtro.Busca.Trim();
            var termo = $"%{busca}%";
            // O número do pedido mostrado na loja é o começo do GUID, e o SQLite guarda GUIDs em maiúsculas.
            var inicioPedido = $"{busca.ToUpperInvariant()}%";
            query = query.Where(e =>
                EF.Functions.Like(e.CodigoRastreio!, termo)
                || EF.Functions.Like(e.Destino.Destinatario, termo)
                || EF.Functions.Like(e.Destino.Cidade, termo)
                || EF.Functions.Like(e.PedidoId.ToString(), inicioPedido));
        }

        // Os mais urgentes primeiro: menor prazo de despacho.
        query = query.OrderBy(e => e.PrazoDespacho).ThenBy(e => e.CriadoEm);

        var total = await query.CountAsync(ct);
        var itens = await query
            .Skip((filtro.Pagina - 1) * filtro.TamanhoPagina)
            .Take(filtro.TamanhoPagina)
            .ToListAsync(ct);
        return new PagedResult<Envio>(itens, filtro.Pagina, filtro.TamanhoPagina, total);
    }

    public async Task<IReadOnlyDictionary<StatusEnvio, int>> ContarPorStatusAsync(CancellationToken ct = default) =>
        await db.Envios.GroupBy(e => e.Status).Select(g => new { g.Key, Total = g.Count() }).ToDictionaryAsync(g => g.Key, g => g.Total, ct);

    public Task<int> ContarNoCentroComPrazoAteAsync(DateTime limiteUtc, CancellationToken ct = default) =>
        db.Envios.CountAsync(e => NoCentro.Contains(e.Status) && e.PrazoDespacho < limiteUtc, ct);

    public Task<int> ContarEntreguesEntreAsync(DateTime inicioUtc, DateTime fimUtc, CancellationToken ct = default) =>
        db.Envios.CountAsync(e => e.Status == StatusEnvio.Entregue && e.AtualizadoEm >= inicioUtc && e.AtualizadoEm < fimUtc, ct);

    public void Adicionar(Envio envio) => db.Envios.Add(envio);
}
