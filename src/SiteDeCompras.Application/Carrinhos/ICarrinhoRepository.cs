using SiteDeCompras.Domain.Carrinhos;

namespace SiteDeCompras.Application.Carrinhos;

public interface ICarrinhoRepository
{
    Task<Carrinho?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    void Adicionar(Carrinho carrinho);
    void Remover(Carrinho carrinho);
}
