using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SiteDeCompras.Application.Usuarios;
using SiteDeCompras.Domain.Usuarios;

namespace SiteDeCompras.Api.Seguranca;

/// <summary>
/// A sessão é um cookie HttpOnly cifrado pelo ASP.NET Core (Data Protection). O JavaScript da
/// página não consegue ler o cookie, então um XSS não leva o login embora, como aconteceria com
/// um token no localStorage.
/// </summary>
public static class Sessao
{
    public const string NomeCookie = "loja_sessao";
    private const string ClaimCarimbo = "carimbo";

    public static Task EntrarAsync(HttpContext http, Usuario usuario)
    {
        var identidade = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nome),
            new Claim(ClaimTypes.Role, usuario.Papel.ToString()),
            new Claim(ClaimCarimbo, usuario.CarimboSeguranca),
        ], CookieAuthenticationDefaults.AuthenticationScheme);
        return http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identidade));
    }

    public static Task SairAsync(HttpContext http) =>
        http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    public static Guid? UsuarioId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public static bool EhAdmin(this ClaimsPrincipal user) => user.IsInRole(nameof(PapelUsuario.Admin));

    public static void Configurar(CookieAuthenticationOptions o, bool desenvolvimento)
    {
        o.Cookie.Name = NomeCookie;
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Strict;
        // Em produção o cookie só trafega por HTTPS; em desenvolvimento o Vite e a API rodam em http.
        o.Cookie.SecurePolicy = desenvolvimento ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
        o.SlidingExpiration = true;

        // É uma API: sem redirecionar para página de login, só o status.
        o.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        o.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };

        // A cada requisição confere no banco se a sessão ainda vale: usuário existe, não está
        // bloqueado e o carimbo é o mesmo (trocar senha ou papel derruba as sessões antigas).
        o.Events.OnValidatePrincipal = async ctx =>
        {
            var id = ctx.Principal?.UsuarioId();
            var carimbo = ctx.Principal?.FindFirstValue(ClaimCarimbo);
            var auth = ctx.HttpContext.RequestServices.GetRequiredService<AuthService>();
            var usuario = id is null || carimbo is null
                ? null
                : await auth.ValidarSessaoAsync(id.Value, carimbo, ctx.HttpContext.RequestAborted);
            if (usuario is null)
            {
                ctx.RejectPrincipal();
                await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    }
}
