using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SiteDeCompras.Application.Carrinhos;
using SiteDeCompras.Application.Enderecos;
using SiteDeCompras.Application.Logistica;
using SiteDeCompras.Application.Pagamentos;
using SiteDeCompras.Application.Pedidos;
using SiteDeCompras.Application.Produtos;
using SiteDeCompras.Application.Usuarios;

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
        services.AddScoped<AuthService>();
        services.AddScoped<EnderecoService>();
        services.AddScoped<LogisticaService>();
        services.AddScoped<AcompanhamentoPedidos>();
        services.AddScoped<EstoqueService>();
        services.AddScoped<SeparacaoService>();
        services.AddScoped<EmbalagemService>();
        services.AddScoped<ExpedicaoService>();
        services.TryAddSingleton(TimeProvider.System);
        return services;
    }
}
