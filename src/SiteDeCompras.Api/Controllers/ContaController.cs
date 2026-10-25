using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteDeCompras.Api.Seguranca;
using SiteDeCompras.Application.Enderecos;
using SiteDeCompras.Application.Pedidos;
using SiteDeCompras.Application.Usuarios;

namespace SiteDeCompras.Api.Controllers;

/// <summary>Dados de quem está logado. Tudo aqui é sempre do próprio usuário, nunca por id.</summary>
[ApiController]
[Authorize]
[Route("api/v1/conta")]
[Produces("application/json")]
public class ContaController(AuthService service, PedidoService pedidos, EnderecoService enderecos) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<UsuarioDto> Obter(CancellationToken ct) => service.ObterAsync(User.UsuarioId()!.Value, ct);

    /// <summary>Pedidos feitos com esta conta, do mais recente para o mais antigo.</summary>
    [HttpGet("pedidos")]
    [ProducesResponseType<IReadOnlyList<PedidoResumoDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<IReadOnlyList<PedidoResumoDto>> Pedidos(CancellationToken ct) =>
        pedidos.ListarDoUsuarioAsync(User.UsuarioId()!.Value, ct);

    [HttpGet("enderecos")]
    [ProducesResponseType<IReadOnlyList<EnderecoSalvoDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<IReadOnlyList<EnderecoSalvoDto>> Enderecos(CancellationToken ct) =>
        enderecos.ListarAsync(User.UsuarioId()!.Value, ct);

    /// <summary>Guarda um endereço para escolher no checkout. Até 10 por conta.</summary>
    [HttpPost("enderecos")]
    [ProducesResponseType<EnderecoSalvoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<EnderecoSalvoDto>> AdicionarEndereco(EnderecoRequest req, CancellationToken ct)
    {
        var endereco = await enderecos.AdicionarAsync(User.UsuarioId()!.Value, req, ct);
        return CreatedAtAction(nameof(Enderecos), null, endereco);
    }

    [HttpDelete("enderecos/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoverEndereco(int id, CancellationToken ct)
    {
        await enderecos.RemoverAsync(User.UsuarioId()!.Value, id, ct);
        return NoContent();
    }
}
