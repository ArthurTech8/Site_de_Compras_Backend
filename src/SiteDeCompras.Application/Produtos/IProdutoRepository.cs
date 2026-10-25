using SiteDeCompras.Application.Common;
using SiteDeCompras.Domain.Produtos;

namespace SiteDeCompras.Application.Produtos;

public interface IProdutoRepository
{
    Task<Produto?> ObterPorIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Produto>> ListarPorIdsAsync(IEnumerable<int> ids, CancellationToken ct = default);
    Task<PagedResult<Produto>> ListarAsync(ProdutoFiltro filtro, CancellationToken ct = default);
    Task<Produto?> ObterPorSkuAsync(string sku, CancellationToken ct = default);

    /// <summary>Os produtos ativos, para a tela de estoque do centro de distribuição.</summary>
    Task<IReadOnlyList<Produto>> ListarAtivosAsync(CancellationToken ct = default);
    void Adicionar(Produto produto);
}
