namespace SiteDeCompras.Api.Seguranca;

/// <summary>
/// Cabeçalhos de segurança e a defesa contra CSRF das rotas da API.
/// </summary>
public static class CabecalhosSeguranca
{
    /// <summary>
    /// Toda requisição que altera algo precisa deste cabeçalho. Um formulário ou imagem de outro
    /// site não consegue enviá-lo, e um fetch de outra origem com ele exige um preflight de CORS,
    /// que só as origens configuradas passam. Soma-se ao cookie SameSite=Strict.
    /// </summary>
    public const string CabecalhoAntiCsrf = "X-Requested-With";

    private static readonly HashSet<string> MetodosSeguros = ["GET", "HEAD", "OPTIONS"];

    public static IApplicationBuilder UseCabecalhosSeguranca(this IApplicationBuilder app) => app.Use(async (http, next) =>
    {
        var cabecalhos = http.Response.Headers;
        cabecalhos.XContentTypeOptions = "nosniff";
        cabecalhos.XFrameOptions = "DENY";
        cabecalhos["Referrer-Policy"] = "no-referrer";

        var ehApi = http.Request.Path.StartsWithSegments("/api");
        if (ehApi)
        {
            // A API só devolve JSON: nada nela deve rodar script nem ser embutido em outra página.
            cabecalhos.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
            // Respostas com dados da conta não ficam em cache de navegador ou proxy.
            cabecalhos.CacheControl = "no-store";
        }

        if (ehApi && !MetodosSeguros.Contains(http.Request.Method) && !http.Request.Headers.ContainsKey(CabecalhoAntiCsrf))
        {
            http.Response.StatusCode = StatusCodes.Status400BadRequest;
            await Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Requisição recusada",
                detail: $"Envie o cabeçalho {CabecalhoAntiCsrf} nas requisições que alteram dados.").ExecuteAsync(http);
            return;
        }

        await next();
    });
}
