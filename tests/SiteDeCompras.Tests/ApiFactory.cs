using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using SiteDeCompras.Application.Usuarios;

namespace SiteDeCompras.Tests;

/// <summary>
/// Sobe a API com um banco SQLite temporário e exclusivo para cada classe de teste.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    public const string EmailAdmin = "admin@loja.teste";
    public const string SenhaAdmin = "Gerente-da-loja-2026";

    private readonly string _arquivoBanco = Path.Combine(Path.GetTempPath(), $"site_de_compras_testes_{Guid.NewGuid():N}.db");

    /// <summary>Limite de login/cadastro por minuto. Alto por padrão para não atrapalhar os outros testes.</summary>
    protected virtual int LimiteAuthPorMinuto => 1000;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_arquivoBanco};Pooling=False");
        builder.UseSetting("Banco:AplicarMigrationsNaInicializacao", "true");
        builder.UseSetting("AdminInicial:Email", EmailAdmin);
        builder.UseSetting("AdminInicial:Senha", SenhaAdmin);
        builder.UseSetting("LimiteRequisicoes:PorMinuto", "100000");
        builder.UseSetting("LimiteRequisicoes:AuthPorMinuto", LimiteAuthPorMinuto.ToString());
    }

    /// <summary>Como o frontend: toda requisição leva o cabeçalho anti-CSRF.</summary>
    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        client.DefaultRequestHeaders.Add("X-Requested-With", "fetch");
    }

    /// <summary>Um cliente HTTP com a sessão do admin criado na inicialização.</summary>
    public async Task<HttpClient> ClienteAdminAsync()
    {
        var client = CreateClient();
        var resposta = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest { Email = EmailAdmin, Senha = SenhaAdmin });
        resposta.EnsureSuccessStatusCode();
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        File.Delete(_arquivoBanco);
    }
}

/// <summary>Variante com limite baixo de login, para testar o 429.</summary>
public class ApiFactoryLimiteBaixo : ApiFactory
{
    protected override int LimiteAuthPorMinuto => 3;
}
