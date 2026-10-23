using Microsoft.EntityFrameworkCore;
using SiteDeCompras.Application.Usuarios;
using SiteDeCompras.Domain.Usuarios;
using SiteDeCompras.Infrastructure.Persistence;

namespace SiteDeCompras.Infrastructure.Repositories;

public class UsuarioRepository(AppDbContext db) : IUsuarioRepository
{
    public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<Usuario?> ObterPorEmailAsync(string emailNormalizado, CancellationToken ct = default) =>
        db.Usuarios.FirstOrDefaultAsync(u => u.EmailNormalizado == emailNormalizado, ct);

    public Task<bool> ExisteEmailAsync(string emailNormalizado, CancellationToken ct = default) =>
        db.Usuarios.AnyAsync(u => u.EmailNormalizado == emailNormalizado, ct);

    public Task<bool> ExisteAdminAsync(CancellationToken ct = default) =>
        db.Usuarios.AnyAsync(u => u.Papel == PapelUsuario.Admin, ct);

    public void Adicionar(Usuario usuario) => db.Usuarios.Add(usuario);
}
