using SiteDeCompras.Application.Common;
using SiteDeCompras.Domain.Produtos;

namespace SiteDeCompras.Application.Produtos;

public class ProdutoService(IProdutoRepository produtos, IUnitOfWork uow)
{
    public async Task<PagedResult<ProdutoDto>> ListarAsync(ProdutoFiltro filtro, CancellationToken ct = default)
    {
        var pagina = await produtos.ListarAsync(filtro, ct);
        return new PagedResult<ProdutoDto>(
            pagina.Itens.Select(ProdutoDto.De).ToList(), pagina.Pagina, pagina.TamanhoPagina, pagina.TotalItens);
    }

    public async Task<ProdutoDto> ObterAsync(int id, CancellationToken ct = default) =>
        ProdutoDto.De(await ObterAtivoAsync(id, ct));

    public async Task<ProdutoDto> CriarAsync(SalvarProdutoRequest req, CancellationToken ct = default)
    {
        var produto = new Produto(req.Nome, req.Preco, req.Estoque, req.Descricao, req.ImagemUrl);
        produtos.Adicionar(produto);
        await uow.SalvarAsync(ct);
        return ProdutoDto.De(produto);
    }

    public async Task<ProdutoDto> AtualizarAsync(int id, SalvarProdutoRequest req, CancellationToken ct = default)
    {
        var produto = await ObterAtivoAsync(id, ct);
        produto.Atualizar(req.Nome, req.Preco, req.Estoque, req.Descricao, req.ImagemUrl);
        await uow.SalvarAsync(ct);
        return ProdutoDto.De(produto);
    }

    public async Task RemoverAsync(int id, CancellationToken ct = default)
    {
        var produto = await ObterAtivoAsync(id, ct);
        produto.Desativar();
        await uow.SalvarAsync(ct);
    }

    private async Task<Produto> ObterAtivoAsync(int id, CancellationToken ct)
    {
        var produto = await produtos.ObterPorIdAsync(id, ct);
        return produto is { Ativo: true } ? produto : throw new NotFoundException("Produto", id);
    }
}
