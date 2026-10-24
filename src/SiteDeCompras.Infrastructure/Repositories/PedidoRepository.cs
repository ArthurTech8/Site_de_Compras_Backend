using Microsoft.EntityFrameworkCore;
using SiteDeCompras.Application.Pedidos;
using SiteDeCompras.Domain.Pedidos;
using SiteDeCompras.Infrastructure.Persistence;

namespace SiteDeCompras.Infrastructure.Repositories;

public class PedidoRepository(AppDbContext db) : IPedidoRepository
{
    public Task<Pedido?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        db.Pedidos.Include(p => p.Itens).FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Pedido>> ListarDoUsuarioAsync(Guid usuarioId, CancellationToken ct = default) =>
        await db.Pedidos.AsNoTracking()
            .Include(p => p.Itens)
            .Where(p => p.UsuarioId == usuarioId)
            .OrderByDescending(p => p.CriadoEm)
            .ToListAsync(ct);

    public void Adicionar(Pedido pedido) => db.Pedidos.Add(pedido);
}
