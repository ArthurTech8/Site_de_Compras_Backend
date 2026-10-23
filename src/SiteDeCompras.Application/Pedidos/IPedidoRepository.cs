using SiteDeCompras.Domain.Pedidos;

namespace SiteDeCompras.Application.Pedidos;

public interface IPedidoRepository
{
    Task<Pedido?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    void Adicionar(Pedido pedido);
}
