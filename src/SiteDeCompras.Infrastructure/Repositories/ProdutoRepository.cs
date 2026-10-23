using Microsoft.EntityFrameworkCore;
using SiteDeCompras.Application.Common;
using SiteDeCompras.Application.Produtos;
using SiteDeCompras.Domain.Produtos;
using SiteDeCompras.Infrastructure.Persistence;

namespace SiteDeCompras.Infrastructure.Repositories;

public class ProdutoRepository(AppDbContext db) : IProdutoRepository
{
    public Task<Produto?> ObterPorIdAsync(int id, CancellationToken ct = default) =>
        db.Produtos.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Produto>> ListarPorIdsAsync(IEnumerable<int> ids, CancellationToken ct = default)
    {
        var lista = ids.Distinct().ToList();
        return await db.Produtos.Where(p => lista.Contains(p.Id)).ToListAsync(ct);
    }

    public async Task<PagedResult<Produto>> ListarAsync(ProdutoFiltro filtro, CancellationToken ct = default)
    {
        var query = db.Produtos.AsNoTracking().Where(p => p.Ativo);

        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            var termo = $"%{filtro.Busca.Trim()}%";
            query = query.Where(p => EF.Functions.Like(p.Nome, termo));
        }
        if (filtro.PrecoMin is { } min)
            query = query.Where(p => p.Preco >= min);
        if (filtro.PrecoMax is { } max)
            query = query.Where(p => p.Preco <= max);

        query = filtro.Ordenacao switch
        {
            OrdenacaoProduto.MenorPreco => query.OrderBy(p => p.Preco).ThenBy(p => p.Id),
            OrdenacaoProduto.MaiorPreco => query.OrderByDescending(p => p.Preco).ThenBy(p => p.Id),
            OrdenacaoProduto.MaisRecentes => query.OrderByDescending(p => p.CriadoEm).ThenByDescending(p => p.Id),
            _ => query.OrderBy(p => p.Nome).ThenBy(p => p.Id),
        };

        var total = await query.CountAsync(ct);
        var itens = await query
            .Skip((filtro.Pagina - 1) * filtro.TamanhoPagina)
            .Take(filtro.TamanhoPagina)
            .ToListAsync(ct);

        return new PagedResult<Produto>(itens, filtro.Pagina, filtro.TamanhoPagina, total);
    }

    public void Adicionar(Produto produto) => db.Produtos.Add(produto);
}
