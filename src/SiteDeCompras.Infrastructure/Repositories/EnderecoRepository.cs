using Microsoft.EntityFrameworkCore;
using SiteDeCompras.Application.Enderecos;
using SiteDeCompras.Domain.Enderecos;
using SiteDeCompras.Infrastructure.Persistence;

namespace SiteDeCompras.Infrastructure.Repositories;

public class EnderecoRepository(AppDbContext db) : IEnderecoRepository
{
    public async Task<IReadOnlyList<EnderecoSalvo>> ListarDoUsuarioAsync(Guid usuarioId, CancellationToken ct = default) =>
        await db.EnderecosSalvos.AsNoTracking()
            .Where(e => e.UsuarioId == usuarioId)
            .OrderByDescending(e => e.CriadoEm)
            .ThenByDescending(e => e.Id)
            .ToListAsync(ct);

    public Task<int> ContarDoUsuarioAsync(Guid usuarioId, CancellationToken ct = default) =>
        db.EnderecosSalvos.CountAsync(e => e.UsuarioId == usuarioId, ct);

    public Task<EnderecoSalvo?> ObterDoUsuarioAsync(Guid usuarioId, int id, CancellationToken ct = default) =>
        db.EnderecosSalvos.FirstOrDefaultAsync(e => e.UsuarioId == usuarioId && e.Id == id, ct);

    public void Adicionar(EnderecoSalvo endereco) => db.EnderecosSalvos.Add(endereco);

    public void Remover(EnderecoSalvo endereco) => db.EnderecosSalvos.Remove(endereco);
}
