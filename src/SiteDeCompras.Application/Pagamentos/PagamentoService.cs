using SiteDeCompras.Application.Carrinhos;
using SiteDeCompras.Application.Common;
using SiteDeCompras.Application.Produtos;
using SiteDeCompras.Domain.Carrinhos;
using SiteDeCompras.Domain.Common;
using SiteDeCompras.Domain.Pagamentos;
using SiteDeCompras.Domain.Produtos;

namespace SiteDeCompras.Application.Pagamentos;

/// <summary>
/// Calcula as formas de pagamento a partir do carrinho guardado no servidor. O cliente nunca
/// envia valores: só escolhe a forma e, no crédito, o número de parcelas.
/// </summary>
public class PagamentoService(ICarrinhoRepository carrinhos, IProdutoRepository produtos, ConfiguracaoPagamento config)
{
    public async Task<OpcoesPagamentoDto> ObterOpcoesAsync(Guid carrinhoId, CancellationToken ct = default)
    {
        var carrinho = await ObterCarrinhoAsync(carrinhoId, ct);
        var descontos = DescontosPix(carrinho, await ProdutosDoCarrinhoAsync(carrinho, ct));
        var maior = MaiorDesconto(descontos);

        var pix = CalculadoraPagamento.Pix(carrinho.Total, maior?.DescontoPercentual ?? 0m);
        var debito = CalculadoraPagamento.Debito(carrinho.Total);
        var credito = CalculadoraPagamento.OpcoesCredito(carrinho.Total, config.TaxaJurosMensalCredito);

        return new OpcoesPagamentoDto(
            carrinho.Id,
            carrinho.Total,
            new OpcaoPixDto(pix.DescontoPercentual, pix.Desconto, pix.Total, maior, descontos),
            new OpcaoDebitoDto(debito.Total),
            new OpcaoCreditoDto(
                config.TaxaJurosMensalCredito,
                CalculadoraPagamento.ParcelasMinimas,
                CalculadoraPagamento.ParcelasMaximas,
                credito.Select(ParcelamentoDto.De).ToList()));
    }

    /// <summary>Calcula a condição da forma escolhida. Usado ao fechar o pedido.</summary>
    public static CondicaoPagamento Calcular(
        Carrinho carrinho, IReadOnlyCollection<Produto> produtosDoCarrinho, FormaPagamento forma, int? parcelas, decimal taxaJurosMensal) =>
        forma switch
        {
            FormaPagamento.Pix => CalculadoraPagamento.Pix(
                carrinho.Total, MaiorDesconto(DescontosPix(carrinho, produtosDoCarrinho))?.DescontoPercentual ?? 0m),
            FormaPagamento.Debito => CalculadoraPagamento.Debito(carrinho.Total),
            FormaPagamento.Credito => CalculadoraPagamento.Credito(
                carrinho.Total,
                parcelas ?? throw new DomainException("Informe o número de parcelas."),
                taxaJurosMensal),
            _ => throw new DomainException("Forma de pagamento inválida."),
        };

    private static List<DescontoPixProdutoDto> DescontosPix(Carrinho carrinho, IReadOnlyCollection<Produto> produtosDoCarrinho) =>
        carrinho.Itens
            .OrderBy(i => i.Id)
            .Select(i => new DescontoPixProdutoDto(
                i.ProdutoId,
                i.NomeProduto,
                produtosDoCarrinho.FirstOrDefault(p => p.Id == i.ProdutoId)?.DescontoPix ?? 0m))
            .ToList();

    // Os descontos não acumulam: vale só o maior. Em empate fica o primeiro item do carrinho.
    private static DescontoPixProdutoDto? MaiorDesconto(IEnumerable<DescontoPixProdutoDto> descontos) =>
        descontos.OrderByDescending(d => d.DescontoPercentual).FirstOrDefault();

    private Task<IReadOnlyList<Produto>> ProdutosDoCarrinhoAsync(Carrinho carrinho, CancellationToken ct) =>
        produtos.ListarPorIdsAsync(carrinho.Itens.Select(i => i.ProdutoId), ct);

    private async Task<Carrinho> ObterCarrinhoAsync(Guid id, CancellationToken ct) =>
        await carrinhos.ObterPorIdAsync(id, ct) ?? throw new NotFoundException("Carrinho", id);
}
