using SiteDeCompras.Application.Common;
using SiteDeCompras.Application.Produtos;
using SiteDeCompras.Domain.Estoque;
using SiteDeCompras.Domain.Produtos;

namespace SiteDeCompras.Application.Logistica;

/// <summary>Endereçamento e inventário dos produtos no centro de distribuição.</summary>
public class EstoqueService(
    IProdutoRepository produtos,
    IEnvioRepository envios,
    IDivergenciaRepository divergencias,
    IUnitOfWork uow,
    TimeProvider relogio)
{
    public async Task<IReadOnlyList<PosicaoEstoqueDto>> ListarAsync(CancellationToken ct = default)
    {
        var ativos = await produtos.ListarAtivosAsync(ct);
        var reservado = await envios.ContarReservadoPorProdutoAsync(ct);
        var abertas = (await divergencias.ListarAbertasAsync(ct)).ToLookup(d => d.ProdutoId);
        return ativos.Select(p => Posicao(p, reservado.GetValueOrDefault(p.Id), abertas[p.Id].ToList())).ToList();
    }

    public async Task<PosicaoEstoqueDto> MoverAsync(int produtoId, MoverProdutoRequest req, CancellationToken ct = default)
    {
        var produto = await ObterAtivoAsync(produtoId, ct);
        produto.MoverPara(req.Local);
        await uow.SalvarAsync(ct);
        return await PosicaoAsync(produto, ct);
    }

    /// <summary>
    /// Registra a contagem da prateleira. O disponível para venda é o contado menos o que já foi
    /// vendido e ainda não saiu da prateleira. Se a contagem cobre os pedidos, a divergência do
    /// produto é resolvida e os envios dele voltam a entrar nas ondas.
    /// </summary>
    public async Task<InventarioDto> InventariarAsync(int produtoId, InventarioRequest req, CancellationToken ct = default)
    {
        var produto = await ObterAtivoAsync(produtoId, ct);
        var reservado = (await envios.ContarReservadoPorProdutoAsync(ct)).GetValueOrDefault(produto.Id);
        var anterior = produto.Estoque;
        var disponivel = req.QuantidadeContada - reservado;

        string mensagem;
        if (disponivel < 0)
        {
            produto.AjustarPorInventario(0);
            mensagem = $"Foram contadas {req.QuantidadeContada} unidades, mas {reservado} já foram vendidas e esperam a separação. " +
                       $"Faltam {-disponivel}; a divergência continua aberta até a reposição.";
        }
        else
        {
            produto.AjustarPorInventario(disponivel);
            var agora = relogio.GetUtcNow().UtcDateTime;
            foreach (var divergencia in await divergencias.ListarAbertasDoProdutoAsync(produto.Id, ct))
                divergencia.Resolver(agora);
            mensagem = anterior == disponivel
                ? $"Contagem confere: {disponivel} disponíveis para venda."
                : $"Disponível para venda passou de {anterior} para {disponivel}.";
        }

        await uow.SalvarAsync(ct);
        return new InventarioDto(await PosicaoAsync(produto, ct), produto.Estoque - anterior, mensagem);
    }

    private async Task<PosicaoEstoqueDto> PosicaoAsync(Produto produto, CancellationToken ct)
    {
        var reservado = (await envios.ContarReservadoPorProdutoAsync(ct)).GetValueOrDefault(produto.Id);
        var abertas = await divergencias.ListarAbertasDoProdutoAsync(produto.Id, ct);
        return Posicao(produto, reservado, abertas.Where(d => d.Aberta).ToList());
    }

    private static PosicaoEstoqueDto Posicao(Produto p, int reservado, IReadOnlyCollection<DivergenciaEstoque> abertas) => new(
        p.Id, p.Sku, p.Nome, p.ImagemUrl, p.LocalEstoque, p.Estoque, reservado,
        abertas.Sum(d => d.Quantidade), abertas.Count == 0 ? null : abertas.Min(d => d.CriadaEm));

    private async Task<Produto> ObterAtivoAsync(int id, CancellationToken ct)
    {
        var produto = await produtos.ObterPorIdAsync(id, ct);
        return produto is { Ativo: true } ? produto : throw new NotFoundException("Produto", id);
    }
}
