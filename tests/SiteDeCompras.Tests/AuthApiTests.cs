using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using SiteDeCompras.Application.Produtos;
using SiteDeCompras.Application.Usuarios;
using SiteDeCompras.Domain.Usuarios;
using SiteDeCompras.Infrastructure.Persistence;

namespace SiteDeCompras.Tests;

public class AuthApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private const string SenhaBoa = "Cafe-com-pao-de-queijo";

    private static string EmailNovo() => $"cliente_{Guid.NewGuid():N}@exemplo.com";

    private static Task<HttpResponseMessage> Cadastrar(HttpClient client, string email, string senha = SenhaBoa, string nome = "Maria Silva") =>
        client.PostAsJsonAsync("/api/v1/auth/cadastro", new CadastroRequest { Nome = nome, Email = email, Senha = senha });

    private static Task<HttpResponseMessage> Entrar(HttpClient client, string email, string senha) =>
        client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest { Email = email, Senha = senha });

    [Fact]
    public async Task Cadastro_cria_cliente_abre_sessao_e_nao_devolve_dados_sensiveis()
    {
        var client = factory.CreateClient();
        var email = EmailNovo();

        var resposta = await Cadastrar(client, email);

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var corpo = await resposta.Content.ReadAsStringAsync();
        foreach (var campo in new[] { "senha", "hash", "carimbo", "falhas", "bloqueado" })
            Assert.DoesNotContain(campo, corpo, StringComparison.OrdinalIgnoreCase);

        var cookie = Assert.Single(resposta.Headers.GetValues("Set-Cookie"));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);

        var conta = await client.GetFromJsonAsync<UsuarioDto>("/api/v1/conta", Json);
        Assert.Equal(email, conta!.Email);
        Assert.Equal(PapelUsuario.Cliente, conta.Papel);
    }

    [Fact]
    public async Task Senha_fica_guardada_como_hash_pbkdf2_com_sal()
    {
        var email = EmailNovo();
        await Cadastrar(factory.CreateClient(), email);
        await Cadastrar(factory.CreateClient(), EmailNovo());

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hashes = db.Usuarios.Where(u => u.Papel == PapelUsuario.Cliente).Select(u => u.SenhaHash).ToList();

        Assert.All(hashes, h => Assert.DoesNotContain(SenhaBoa, h));
        // Mesma senha, hashes diferentes: cada um tem o próprio sal.
        Assert.Equal(hashes.Count, hashes.Distinct().Count());
        // Formato v3 do Identity: byte 0x01, PRF HMAC-SHA512 (2) e 210.000 iterações.
        var bytes = Convert.FromBase64String(hashes[0]);
        Assert.Equal(0x01, bytes[0]);
        Assert.Equal(2, (bytes[1] << 24) | (bytes[2] << 16) | (bytes[3] << 8) | bytes[4]);
        Assert.Equal(210_000, (bytes[5] << 24) | (bytes[6] << 16) | (bytes[7] << 8) | bytes[8]);
    }

    [Fact]
    public async Task Cadastro_ignora_papel_enviado_pelo_cliente()
    {
        var client = factory.CreateClient();
        var resposta = await client.PostAsJsonAsync("/api/v1/auth/cadastro",
            new { nome = "Invasor", email = EmailNovo(), senha = SenhaBoa, papel = "Admin" });

        var usuario = await resposta.Content.ReadFromJsonAsync<UsuarioDto>(Json);
        Assert.Equal(PapelUsuario.Cliente, usuario!.Papel);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/v1/produtos",
            new SalvarProdutoRequest { Nome = "Golpe", Preco = 1, Estoque = 1 })).StatusCode);
    }

    [Fact]
    public async Task Email_e_unico_sem_diferenciar_maiusculas_e_espacos()
    {
        var email = EmailNovo();
        await Cadastrar(factory.CreateClient(), email);

        var resposta = await Cadastrar(factory.CreateClient(), $"  {email.ToUpperInvariant()} ");

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Theory]
    [InlineData("curta1")]
    [InlineData("12345678")]
    [InlineData("senha123")]
    [InlineData("aaaaaaaaaaaa")]
    [InlineData("mariasilva2026")]
    public async Task Senha_fraca_retorna_400(string senha)
    {
        var resposta = await Cadastrar(factory.CreateClient(), EmailNovo(), senha, nome: "Maria Silva");

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Login_aceita_email_com_outra_caixa_e_erros_tem_a_mesma_mensagem()
    {
        var email = EmailNovo();
        await Cadastrar(factory.CreateClient(), email);

        var ok = await Entrar(factory.CreateClient(), email.ToUpperInvariant(), SenhaBoa);
        var senhaErrada = await Entrar(factory.CreateClient(), email, "Outra-senha-qualquer");
        var semConta = await Entrar(factory.CreateClient(), EmailNovo(), "Outra-senha-qualquer");

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, senhaErrada.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, semConta.StatusCode);
        Assert.Equal(await senhaErrada.Content.ReadAsStringAsync(), await semConta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Cinco_senhas_erradas_bloqueiam_a_conta_mesmo_com_a_senha_certa_depois()
    {
        var email = EmailNovo();
        await Cadastrar(factory.CreateClient(), email);
        var client = factory.CreateClient();

        for (var i = 0; i < Usuario.MaximoFalhasLogin; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await Entrar(client, email, $"Errada-{i}-xyz")).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await Entrar(client, email, SenhaBoa)).StatusCode);
    }

    [Fact]
    public async Task Logout_encerra_a_sessao()
    {
        var client = factory.CreateClient();
        await Cadastrar(client, EmailNovo());

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/v1/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/conta")).StatusCode);
    }

    [Fact]
    public async Task Trocar_a_senha_derruba_as_sessoes_abertas()
    {
        var client = factory.CreateClient();
        var email = EmailNovo();
        await Cadastrar(client, email);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IHasherSenha>();
            var usuario = db.Usuarios.Single(u => u.EmailNormalizado == email);
            usuario.AlterarSenhaHash(hasher.Gerar("Nova-senha-bem-longa"));
            await db.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/conta")).StatusCode);
    }

    [Fact]
    public async Task Rotas_de_admin_exigem_login_e_papel_admin()
    {
        var produto = new SalvarProdutoRequest { Nome = "Teste", Preco = 1, Estoque = 1 };

        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().PostAsJsonAsync("/api/v1/produtos", produto)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().DeleteAsync("/api/v1/produtos/1")).StatusCode);

        var cliente = factory.CreateClient();
        await Cadastrar(cliente, EmailNovo());
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PutAsJsonAsync("/api/v1/produtos/1", produto)).StatusCode);

        var admin = await factory.ClienteAdminAsync();
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync("/api/v1/produtos", produto)).StatusCode);
    }

    [Fact]
    public async Task Requisicao_que_altera_dados_sem_cabecalho_anti_csrf_e_recusada()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Remove("X-Requested-With");

        var resposta = await client.PostAsync("/api/v1/carrinhos", null);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/produtos")).StatusCode);
    }

    [Fact]
    public async Task Respostas_da_api_tem_cabecalhos_de_seguranca()
    {
        var resposta = await factory.CreateClient().GetAsync("/api/v1/produtos");

        Assert.Equal("nosniff", resposta.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", resposta.Headers.GetValues("X-Frame-Options").Single());
        Assert.Contains("frame-ancestors 'none'", resposta.Headers.GetValues("Content-Security-Policy").Single());
    }
}

public class LimiteLoginTests(ApiFactoryLimiteBaixo factory) : IClassFixture<ApiFactoryLimiteBaixo>
{
    [Fact]
    public async Task Muitas_tentativas_de_login_retornam_429()
    {
        var client = factory.CreateClient();
        var login = new LoginRequest { Email = "alguem@exemplo.com", Senha = "Qualquer-senha-1" };

        for (var i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/login", login)).StatusCode);

        var bloqueada = await client.PostAsJsonAsync("/api/v1/auth/login", login);
        Assert.Equal(HttpStatusCode.TooManyRequests, bloqueada.StatusCode);
        Assert.True(bloqueada.Headers.Contains("Retry-After"));
    }
}
