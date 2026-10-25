using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SiteDeCompras.Application.Carrinhos;
using SiteDeCompras.Application.Common;
using SiteDeCompras.Application.Enderecos;
using SiteDeCompras.Application.Logistica;
using SiteDeCompras.Application.Pedidos;
using SiteDeCompras.Application.Produtos;
using SiteDeCompras.Application.Usuarios;
using SiteDeCompras.Infrastructure.Persistence;
using SiteDeCompras.Infrastructure.Repositories;
using SiteDeCompras.Infrastructure.Seguranca;

namespace SiteDeCompras.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("A connection string 'Default' não foi configurada.");

        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IProdutoRepository, ProdutoRepository>();
        services.AddScoped<ICarrinhoRepository, CarrinhoRepository>();
        services.AddScoped<IPedidoRepository, PedidoRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IEnvioRepository, EnvioRepository>();
        services.AddScoped<IEnderecoRepository, EnderecoRepository>();
        services.AddSingleton<IHasherSenha, HasherSenha>();
        return services;
    }
}
