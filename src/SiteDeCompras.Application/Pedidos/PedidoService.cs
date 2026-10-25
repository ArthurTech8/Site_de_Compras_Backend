using SiteDeCompras.Application.Carrinhos;
using SiteDeCompras.Application.Common;
using SiteDeCompras.Application.Logistica;
using SiteDeCompras.Application.Pagamentos;
using SiteDeCompras.Application.Produtos;
using SiteDeCompras.Domain.Logistica;
using SiteDeCompras.Domain.Pedidos;

namespace SiteDeCompras.Application.Pedidos;

public class PedidoService(
    IPedidoRepository pedidos,
    ICarrinhoRepository carrinhos,
    IProdutoRepository produtos,
    IEnvioRepository envios,
    ConfiguracaoPagamento config,
    IUnitOfWork uow,
    TimeProvider relogio)
{
    /// <summary>
    /// Fecha o carrinho em um pedido. O pagamento é simulado: os valores são recalculados aqui a
    /// partir do carrinho e a transação é aprovada na hora, sem gateway. Pago, o pedido já abre o
    /// envio na fila do centro de distribuição.
    /// </summary>
    public async Task<PedidoDto> FecharAsync(FecharPedidoRequest req, Guid? usuarioId = null, CancellationToken ct = default)
    {
        var carrinho = await carrinhos.ObterPorIdAsync(req.CarrinhoId, ct)
            ?? throw new NotFoundException("Carrinho", req.CarrinhoId);
        var produtosDoCarrinho = await produtos.ListarPorIdsAsync(carrinho.Itens.Select(i => i.ProdutoId), ct);

        var pagamento = PagamentoService.Calcular(
            carrinho, produtosDoCarrinho, req.FormaPagamento!.Value, req.Parcelas, config.TaxaJurosMensalCredito);
        var pedido = Pedido.Fechar(carrinho, produtosDoCarrinho, pagamento, req.EnderecoEntrega!.ParaDominio(), usuarioId);

        pedidos.Adicionar(pedido);
        envios.Adicionar(Envio.Criar(pedido, relogio.GetUtcNow().UtcDateTime));
        await uow.SalvarAsync(ct);
        return PedidoDto.De(pedido);
    }

    /// <summary>
    /// O pedido de outra pessoa responde como inexistente (404), para não confirmar que o id existe.
    /// </summary>
    public async Task<PedidoDto> ObterAsync(Guid id, Guid? usuarioId = null, bool admin = false, CancellationToken ct = default)
    {
        var pedido = await pedidos.ObterPorIdAsync(id, ct);
        return pedido is not null && pedido.PodeSerVistoPor(usuarioId, admin)
            ? PedidoDto.De(pedido)
            : throw new NotFoundException("Pedido", id);
    }

    public async Task<IReadOnlyList<PedidoResumoDto>> ListarDoUsuarioAsync(Guid usuarioId, CancellationToken ct = default) =>
        (await pedidos.ListarDoUsuarioAsync(usuarioId, ct)).Select(PedidoResumoDto.De).ToList();
}
