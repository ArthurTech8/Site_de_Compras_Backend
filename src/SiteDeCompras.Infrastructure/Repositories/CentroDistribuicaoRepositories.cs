using Microsoft.EntityFrameworkCore;
using SiteDeCompras.Application.Logistica;
using SiteDeCompras.Domain.Estoque;
using SiteDeCompras.Domain.Logistica;
using SiteDeCompras.Infrastructure.Persistence;

namespace SiteDeCompras.Infrastructure.Repositories;

public class OndaRepository(AppDbContext db) : IOndaRepository
{
    public Task<OndaSeparacao?> ObterPorIdAsync(int id, CancellationToken ct = default) =>
        db.OndasSeparacao.FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<IReadOnlyList<OndaSeparacao>> ListarRecentesAsync(int concluidas, CancellationToken ct = default)
    {
        var abertas = await db.OndasSeparacao.Where(o => o.Status == StatusOnda.Aberta).OrderByDescending(o => o.Id).ToListAsync(ct);
        var fechadas = await db.OndasSeparacao.Where(o => o.Status == StatusOnda.Concluida).OrderByDescending(o => o.Id).Take(concluidas).ToListAsync(ct);
        return [.. abertas, .. fechadas];
    }

    public void Adicionar(OndaSeparacao onda) => db.OndasSeparacao.Add(onda);
}

public class ManifestoRepository(AppDbContext db) : IManifestoRepository
{
    public Task<Manifesto?> ObterPorIdAsync(int id, CancellationToken ct = default) =>
        db.Manifestos.FirstOrDefaultAsync(m => m.Id == id, ct);

    public Task<Manifesto?> ObterAbertoAsync(string transportadora, CancellationToken ct = default) =>
        db.Manifestos.FirstOrDefaultAsync(m => m.Transportadora == transportadora && m.Status == StatusManifesto.Aberto, ct);

    public async Task<IReadOnlyList<Manifesto>> ListarRecentesAsync(int coletados, CancellationToken ct = default)
    {
        var naDoca = await db.Manifestos.Where(m => m.Status != StatusManifesto.Coletado).OrderByDescending(m => m.Id).ToListAsync(ct);
        var saidos = await db.Manifestos.Where(m => m.Status == StatusManifesto.Coletado).OrderByDescending(m => m.Id).Take(coletados).ToListAsync(ct);
        return [.. naDoca, .. saidos];
    }

    public async Task<IReadOnlyDictionary<int, (int Volumes, int PesoGramas)>> TotaisAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default)
    {
        var lista = ids.ToList();
        var totais = await db.Envios
            .Where(e => e.ManifestoId != null && lista.Contains(e.ManifestoId.Value))
            .GroupBy(e => e.ManifestoId!.Value)
            .Select(g => new { g.Key, Volumes = g.Count(), Peso = g.Sum(e => e.Pacote == null ? 0 : e.Pacote.PesoGramas) })
            .ToListAsync(ct);
        return totais.ToDictionary(t => t.Key, t => (t.Volumes, t.Peso));
    }

    public void Adicionar(Manifesto manifesto) => db.Manifestos.Add(manifesto);
}

public class DivergenciaRepository(AppDbContext db) : IDivergenciaRepository
{
    public async Task<IReadOnlyList<DivergenciaEstoque>> ListarAbertasAsync(CancellationToken ct = default) =>
        await db.DivergenciasEstoque.Where(d => d.ResolvidaEm == null).ToListAsync(ct);

    public async Task<IReadOnlyList<DivergenciaEstoque>> ListarAbertasDoProdutoAsync(int produtoId, CancellationToken ct = default) =>
        await db.DivergenciasEstoque.Where(d => d.ProdutoId == produtoId && d.ResolvidaEm == null).ToListAsync(ct);

    public void Adicionar(DivergenciaEstoque divergencia) => db.DivergenciasEstoque.Add(divergencia);
}
