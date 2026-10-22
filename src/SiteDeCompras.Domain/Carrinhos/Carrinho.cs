using SiteDeCompras.Domain.Common;
using SiteDeCompras.Domain.Produtos;

namespace SiteDeCompras.Domain.Carrinhos;

public class Carrinho
{
    private readonly List<ItemCarrinho> _itens = [];

    public Guid Id { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime AtualizadoEm { get; private set; }
    public IReadOnlyCollection<ItemCarrinho> Itens => _itens.AsReadOnly();

    public decimal Total => _itens.Sum(i => i.Subtotal);
    public int QuantidadeTotal => _itens.Sum(i => i.Quantidade);

    public Carrinho()
    {
        Id = Guid.NewGuid();
        CriadoEm = AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Adiciona o produto ao carrinho. Se ele já estiver no carrinho, soma a quantidade.
    /// </summary>
    public void AdicionarItem(Produto produto, int quantidade)
    {
        if (!produto.Ativo)
            throw new DomainException($"O produto '{produto.Nome}' não está disponível.");

        var item = _itens.FirstOrDefault(i => i.ProdutoId == produto.Id);
        var novaQuantidade = (item?.Quantidade ?? 0) + quantidade;
        GarantirEstoque(produto, novaQuantidade);

        if (item is null)
            _itens.Add(new ItemCarrinho(Id, produto.Id, produto.Nome, produto.ImagemUrl, produto.Preco, quantidade));
        else
            item.DefinirQuantidade(novaQuantidade);

        AtualizadoEm = DateTime.UtcNow;
    }

    public void AlterarQuantidade(Produto produto, int quantidade)
    {
        var item = ObterItem(produto.Id);
        GarantirEstoque(produto, quantidade);
        item.DefinirQuantidade(quantidade);
        AtualizadoEm = DateTime.UtcNow;
    }

    public void RemoverItem(int produtoId)
    {
        _itens.Remove(ObterItem(produtoId));
        AtualizadoEm = DateTime.UtcNow;
    }

    public void Limpar()
    {
        _itens.Clear();
        AtualizadoEm = DateTime.UtcNow;
    }

    public bool ContemProduto(int produtoId) => _itens.Any(i => i.ProdutoId == produtoId);

    private ItemCarrinho ObterItem(int produtoId) =>
        _itens.FirstOrDefault(i => i.ProdutoId == produtoId)
        ?? throw new DomainException("O produto não está no carrinho.");

    private static void GarantirEstoque(Produto produto, int quantidade)
    {
        if (quantidade > produto.Estoque)
            throw new DomainException($"Estoque insuficiente para '{produto.Nome}'. Disponível: {produto.Estoque}.");
    }
}
