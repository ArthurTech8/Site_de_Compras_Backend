using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using SiteDeCompras.Api.Infrastructure;
using SiteDeCompras.Api.Seguranca;
using SiteDeCompras.Application;
using SiteDeCompras.Application.Pagamentos;
using SiteDeCompras.Application.Usuarios;
using SiteDeCompras.Infrastructure;
using SiteDeCompras.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    var xml = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
    o.IncludeXmlComments(xml);
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ExcecaoHandler>();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

// Só as origens listadas; nunca "*". Em produção o ideal é servir o site e a API no mesmo
// domínio (proxy reverso), e aí o CORS nem entra em jogo.
builder.Services.AddCors(o => o.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:Origens").Get<string[]>() ?? [])
    .WithHeaders("Content-Type", CabecalhosSeguranca.CabecalhoAntiCsrf)
    .WithMethods("GET", "POST", "PUT", "DELETE")
    .AllowCredentials()));

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o => Sessao.Configurar(o, builder.Environment.IsDevelopment()));
builder.Services.AddAuthorization();
builder.Services.AddLimitesRequisicao(builder.Configuration);

builder.Services.AddApplication(
    builder.Configuration.GetSection("Pagamento").Get<ConfiguracaoPagamento>());
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

if (app.Configuration.GetValue<bool>("Banco:AplicarMigrationsNaInicializacao"))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

// O primeiro admin vem da configuração (dotnet user-secrets ou variáveis de ambiente), nunca do código.
using (var scope = app.Services.CreateScope())
{
    var admin = app.Configuration.GetSection("AdminInicial");
    var criado = await scope.ServiceProvider.GetRequiredService<AuthService>()
        .GarantirAdminInicialAsync(admin["Nome"], admin["Email"], admin["Senha"]);
    if (!criado)
        app.Logger.LogWarning("AdminInicial:Email já pertence a uma conta de cliente; nenhum admin foi criado.");
}

app.UseCors();
app.UseCabecalhosSeguranca();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Exposto para os testes de integração (WebApplicationFactory).
public partial class Program;
