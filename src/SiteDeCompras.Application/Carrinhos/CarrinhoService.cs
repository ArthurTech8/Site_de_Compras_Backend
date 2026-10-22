using SiteDeCompras.Application.Common;
using SiteDeCompras.Application.Produtos;
using SiteDeCompras.Domain.Carrinhos;
using SiteDeCompras.Domain.Produtos;

namespace SiteDeCompras.Application.Carrinhos;

public class CarrinhoService(ICarrinhoRepository carrinhos, IProdutoRepository produtos, IUnitOfWork uow)
{
    public async Task<CarrinhoDto> CriarAsync(CancellationToken ct = default)
    {
        var carrinho = new Carrinho();
        carrinhos.Adicionar(carrinho);
        await uow.SalvarAsync(ct);
        return CarrinhoDto.De(carrinho);
    }

    public async Task<CarrinhoDto> ObterAsync(Guid id, CancellationToken ct = default) =>
        CarrinhoDto.De(await ObterCarrinhoAsync(id, ct));

    public async Task<CarrinhoDto> AdicionarItemAsync(Guid id, AdicionarItemRequest req, CancellationToken ct = default)
    {
        var carrinho = await ObterCarrinhoAsync(id, ct);
        var produto = await ObterProdutoAsync(req.ProdutoId, ct);
        carrinho.AdicionarItem(produto, req.Quantidade);
        await uow.SalvarAsync(ct);
        return CarrinhoDto.De(carrinho);
    }

    public async Task<CarrinhoDto> AlterarQuantidadeAsync(Guid id, int produtoId, AlterarQuantidadeRequest req, CancellationToken ct = default)
    {
        var carrinho = await ObterCarrinhoComItemAsync(id, produtoId, ct);
        var produto = await ObterProdutoAsync(produtoId, ct);
        carrinho.AlterarQuantidade(produto, req.Quantidade);
        await uow.SalvarAsync(ct);
        return CarrinhoDto.De(carrinho);
    }

    public async Task<CarrinhoDto> RemoverItemAsync(Guid id, int produtoId, CancellationToken ct = default)
    {
        var carrinho = await ObterCarrinhoComItemAsync(id, produtoId, ct);
        carrinho.RemoverItem(produtoId);
        await uow.SalvarAsync(ct);
        return CarrinhoDto.De(carrinho);
    }

    public async Task<CarrinhoDto> LimparAsync(Guid id, CancellationToken ct = default)
    {
        var carrinho = await ObterCarrinhoAsync(id, ct);
        carrinho.Limpar();
        await uow.SalvarAsync(ct);
        return CarrinhoDto.De(carrinho);
    }

    public async Task ExcluirAsync(Guid id, CancellationToken ct = default)
    {
        carrinhos.Remover(await ObterCarrinhoAsync(id, ct));
        await uow.SalvarAsync(ct);
    }

    private async Task<Carrinho> ObterCarrinhoAsync(Guid id, CancellationToken ct) =>
        await carrinhos.ObterPorIdAsync(id, ct) ?? throw new NotFoundException("Carrinho", id);

    private async Task<Carrinho> ObterCarrinhoComItemAsync(Guid id, int produtoId, CancellationToken ct)
    {
        var carrinho = await ObterCarrinhoAsync(id, ct);
        return carrinho.ContemProduto(produtoId) ? carrinho : throw new NotFoundException("Item do produto", produtoId);
    }

    private async Task<Produto> ObterProdutoAsync(int id, CancellationToken ct) =>
        await produtos.ObterPorIdAsync(id, ct) ?? throw new NotFoundException("Produto", id);
}
