using SiteDeCompras.Domain.Enderecos;

namespace SiteDeCompras.Application.Enderecos;

public interface IEnderecoRepository
{
    Task<IReadOnlyList<EnderecoSalvo>> ListarDoUsuarioAsync(Guid usuarioId, CancellationToken ct = default);
    Task<int> ContarDoUsuarioAsync(Guid usuarioId, CancellationToken ct = default);
    Task<EnderecoSalvo?> ObterDoUsuarioAsync(Guid usuarioId, int id, CancellationToken ct = default);
    void Adicionar(EnderecoSalvo endereco);
    void Remover(EnderecoSalvo endereco);
}
