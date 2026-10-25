using SiteDeCompras.Application.Common;
using SiteDeCompras.Application.Pedidos;
using SiteDeCompras.Domain.Logistica;

namespace SiteDeCompras.Application.Logistica;

/// <summary>
/// Atualiza o status dos pedidos depois que os envios mudam de etapa, na mesma transação. As
/// consultas devolvem as instâncias já carregadas, então o status ainda não salvo é o que conta.
/// </summary>
public class AcompanhamentoPedidos(IEnvioRepository envios, IPedidoRepository pedidos)
{
    public async Task AtualizarAsync(IEnumerable<Envio> alterados, CancellationToken ct = default)
    {
        foreach (var pedidoId in alterados.Select(e => e.PedidoId).Distinct())
        {
            var pedido = await pedidos.ObterPorIdAsync(pedidoId, ct) ?? throw new NotFoundException("Pedido", pedidoId);
            var doPedido = await envios.ListarDoPedidoAsync(pedidoId, ct);
            pedido.AcompanharEnvios(doPedido.Select(e => e.Status));
        }
    }
}
