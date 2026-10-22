using Microsoft.Extensions.DependencyInjection;
using SiteDeCompras.Application.Carrinhos;
using SiteDeCompras.Application.Produtos;

namespace SiteDeCompras.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ProdutoService>();
        services.AddScoped<CarrinhoService>();
        return services;
    }
}
