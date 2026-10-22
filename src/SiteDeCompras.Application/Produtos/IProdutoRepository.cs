using SiteDeCompras.Application.Common;
using SiteDeCompras.Domain.Produtos;

namespace SiteDeCompras.Application.Produtos;

public interface IProdutoRepository
{
    Task<Produto?> ObterPorIdAsync(int id, CancellationToken ct = default);
    Task<PagedResult<Produto>> ListarAsync(ProdutoFiltro filtro, CancellationToken ct = default);
    void Adicionar(Produto produto);
}
