using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SiteDeCompras.Application.Carrinhos;
using SiteDeCompras.Application.Common;
using SiteDeCompras.Application.Produtos;
using SiteDeCompras.Infrastructure.Persistence;
using SiteDeCompras.Infrastructure.Repositories;

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
        return services;
    }
}
