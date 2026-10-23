using SiteDeCompras.Domain.Usuarios;

namespace SiteDeCompras.Application.Usuarios;

public interface IUsuarioRepository
{
    Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<Usuario?> ObterPorEmailAsync(string emailNormalizado, CancellationToken ct = default);
    Task<bool> ExisteEmailAsync(string emailNormalizado, CancellationToken ct = default);
    Task<bool> ExisteAdminAsync(CancellationToken ct = default);
    void Adicionar(Usuario usuario);
}
