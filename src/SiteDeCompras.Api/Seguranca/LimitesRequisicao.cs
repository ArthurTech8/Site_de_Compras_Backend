using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace SiteDeCompras.Api.Seguranca;

/// <summary>
/// Limite de requisições por IP. Login e cadastro têm um limite bem menor, contra força bruta
/// e teste de senhas vazadas em massa (credential stuffing).
/// </summary>
public static class LimitesRequisicao
{
    public const string PoliticaAuth = "auth";

    public static IServiceCollection AddLimitesRequisicao(this IServiceCollection services, IConfiguration config)
    {
        var porMinuto = config.GetValue("LimiteRequisicoes:PorMinuto", 300);
        var authPorMinuto = config.GetValue("LimiteRequisicoes:AuthPorMinuto", 10);

        return services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(http =>
                RateLimitPartition.GetFixedWindowLimiter(Ip(http), _ => Janela(porMinuto)));
            o.AddPolicy(PoliticaAuth, http =>
                RateLimitPartition.GetFixedWindowLimiter(Ip(http), _ => Janela(authPorMinuto)));
            o.OnRejected = async (ctx, ct) =>
            {
                ctx.HttpContext.Response.Headers.RetryAfter = "60";
                await ctx.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Muitas requisições",
                    Detail = "Você fez muitas tentativas seguidas. Aguarde um minuto e tente de novo.",
                }, options: null, contentType: "application/problem+json", ct);
            };
        });
    }

    // Atrás de um proxy reverso, ligue ProxyReverso:Habilitado (Program.cs) para que este seja o IP real do cliente.
    private static string Ip(HttpContext http) => http.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";

    private static FixedWindowRateLimiterOptions Janela(int limite) => new()
    {
        PermitLimit = limite,
        Window = TimeSpan.FromMinutes(1),
        QueueLimit = 0,
    };
}
