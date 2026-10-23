using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SiteDeCompras.Api.Seguranca;
using SiteDeCompras.Application.Usuarios;

namespace SiteDeCompras.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthController(AuthService service) : ControllerBase
{
    /// <summary>Cria uma conta de cliente e já abre a sessão.</summary>
    [HttpPost("cadastro")]
    [EnableRateLimiting(LimitesRequisicao.PoliticaAuth)]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<UsuarioDto>> Cadastrar(CadastroRequest req, CancellationToken ct)
    {
        var usuario = await service.CadastrarAsync(req, ct);
        await Sessao.EntrarAsync(HttpContext, usuario);
        return CreatedAtAction(nameof(ContaController.Obter), "Conta", null, UsuarioDto.De(usuario));
    }

    /// <summary>
    /// Abre a sessão (cookie HttpOnly). Após 5 senhas erradas seguidas a conta fica bloqueada por
    /// 15 minutos; a resposta de erro é a mesma em todos os casos.
    /// </summary>
    [HttpPost("login")]
    [EnableRateLimiting(LimitesRequisicao.PoliticaAuth)]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<UsuarioDto> Entrar(LoginRequest req, CancellationToken ct)
    {
        var usuario = await service.AutenticarAsync(req, ct);
        await Sessao.EntrarAsync(HttpContext, usuario);
        return UsuarioDto.De(usuario);
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Sair()
    {
        await Sessao.SairAsync(HttpContext);
        return NoContent();
    }
}
