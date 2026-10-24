using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteDeCompras.Api.Seguranca;
using SiteDeCompras.Application.Usuarios;

namespace SiteDeCompras.Api.Controllers;

/// <summary>Dados de quem está logado. Tudo aqui é sempre do próprio usuário, nunca por id.</summary>
[ApiController]
[Authorize]
[Route("api/v1/conta")]
[Produces("application/json")]
public class ContaController(AuthService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<UsuarioDto> Obter(CancellationToken ct) => service.ObterAsync(User.UsuarioId()!.Value, ct);
}
