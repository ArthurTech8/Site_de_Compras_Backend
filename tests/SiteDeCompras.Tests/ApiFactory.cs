using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SiteDeCompras.Tests;

/// <summary>
/// Sobe a API com um banco SQLite temporário e exclusivo para cada classe de teste.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _arquivoBanco = Path.Combine(Path.GetTempPath(), $"site_de_compras_testes_{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_arquivoBanco};Pooling=False");
        builder.UseSetting("Banco:AplicarMigrationsNaInicializacao", "true");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        File.Delete(_arquivoBanco);
    }
}
