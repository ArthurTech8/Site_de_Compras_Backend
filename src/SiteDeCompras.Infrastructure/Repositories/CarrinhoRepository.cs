using Microsoft.EntityFrameworkCore;
using SiteDeCompras.Application.Carrinhos;
using SiteDeCompras.Domain.Carrinhos;
using SiteDeCompras.Infrastructure.Persistence;

namespace SiteDeCompras.Infrastructure.Repositories;

public class CarrinhoRepository(AppDbContext db) : ICarrinhoRepository
{
    public Task<Carrinho?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        db.Carrinhos.Include(c => c.Itens).FirstOrDefaultAsync(c => c.Id == id, ct);

    public void Adicionar(Carrinho carrinho) => db.Carrinhos.Add(carrinho);

    public void Remover(Carrinho carrinho) => db.Carrinhos.Remove(carrinho);
}
