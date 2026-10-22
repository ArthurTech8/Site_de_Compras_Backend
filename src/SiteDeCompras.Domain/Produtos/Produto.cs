using SiteDeCompras.Domain.Common;

namespace SiteDeCompras.Domain.Produtos;

public class Produto
{
    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string? Descricao { get; private set; }
    public decimal Preco { get; private set; }
    public string? ImagemUrl { get; private set; }
    public int Estoque { get; private set; }
    public bool Ativo { get; private set; } = true;
    public DateTime CriadoEm { get; private set; }
    public DateTime AtualizadoEm { get; private set; }

    private Produto() { }

    public Produto(string nome, decimal preco, int estoque, string? descricao = null, string? imagemUrl = null)
    {
        Atualizar(nome, preco, estoque, descricao, imagemUrl);
        CriadoEm = AtualizadoEm;
    }

    public void Atualizar(string nome, decimal preco, int estoque, string? descricao, string? imagemUrl)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("O nome do produto é obrigatório.");
        if (preco <= 0)
            throw new DomainException("O preço do produto deve ser maior que zero.");
        if (estoque < 0)
            throw new DomainException("O estoque não pode ser negativo.");

        Nome = nome.Trim();
        Preco = preco;
        Estoque = estoque;
        Descricao = descricao?.Trim();
        ImagemUrl = imagemUrl?.Trim();
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Exclusão lógica: o produto some do catálogo mas continua referenciável por pedidos antigos.
    /// </summary>
    public void Desativar()
    {
        Ativo = false;
        AtualizadoEm = DateTime.UtcNow;
    }
}
