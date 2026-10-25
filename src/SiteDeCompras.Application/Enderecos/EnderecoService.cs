using SiteDeCompras.Application.Common;
using SiteDeCompras.Domain.Common;
using SiteDeCompras.Domain.Enderecos;

namespace SiteDeCompras.Application.Enderecos;

/// <summary>Endereços da conta. Sempre do usuário logado: o id de outra conta nunca entra aqui.</summary>
public class EnderecoService(IEnderecoRepository enderecos, IUnitOfWork uow, TimeProvider relogio)
{
    public async Task<IReadOnlyList<EnderecoSalvoDto>> ListarAsync(Guid usuarioId, CancellationToken ct = default) =>
        (await enderecos.ListarDoUsuarioAsync(usuarioId, ct)).Select(EnderecoSalvoDto.De).ToList();

    public async Task<EnderecoSalvoDto> AdicionarAsync(Guid usuarioId, EnderecoRequest req, CancellationToken ct = default)
    {
        if (await enderecos.ContarDoUsuarioAsync(usuarioId, ct) >= EnderecoSalvo.MaximoPorUsuario)
            throw new DomainException($"Cada conta guarda até {EnderecoSalvo.MaximoPorUsuario} endereços. Apague um para salvar outro.");

        var endereco = new EnderecoSalvo(usuarioId, req.ParaDominio(), relogio.GetUtcNow().UtcDateTime);
        enderecos.Adicionar(endereco);
        await uow.SalvarAsync(ct);
        return EnderecoSalvoDto.De(endereco);
    }

    public async Task RemoverAsync(Guid usuarioId, int id, CancellationToken ct = default)
    {
        var endereco = await enderecos.ObterDoUsuarioAsync(usuarioId, id, ct)
            ?? throw new NotFoundException("Endereço", id);
        enderecos.Remover(endereco);
        await uow.SalvarAsync(ct);
    }
}
