using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SiteDeCompras.Api.Infrastructure;
using SiteDeCompras.Application;
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

builder.Services.AddCors(o => o.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:Origens").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Configuration.GetValue<bool>("Banco:AplicarMigrationsNaInicializacao"))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

app.UseCors();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Exposto para os testes de integração (WebApplicationFactory).
public partial class Program;
