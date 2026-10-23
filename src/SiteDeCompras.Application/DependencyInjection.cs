using Microsoft.Extensions.DependencyInjection;
using SiteDeCompras.Application.Carrinhos;
using SiteDeCompras.Application.Pagamentos;
using SiteDeCompras.Application.Pedidos;
using SiteDeCompras.Application.Produtos;

namespace SiteDeCompras.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, ConfiguracaoPagamento? pagamento = null)
    {
        services.AddSingleton(pagamento ?? new ConfiguracaoPagamento());
        services.AddScoped<ProdutoService>();
        services.AddScoped<CarrinhoService>();
        services.AddScoped<PagamentoService>();
        services.AddScoped<PedidoService>();
        return services;
    }
}
