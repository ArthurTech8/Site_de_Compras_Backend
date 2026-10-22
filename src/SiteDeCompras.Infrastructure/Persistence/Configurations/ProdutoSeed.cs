namespace SiteDeCompras.Infrastructure.Persistence.Configurations;

/// <summary>
/// Catálogo inicial, o mesmo de senac-26-CarrinhoCompras/data.js. As imagens ficam no frontend.
/// </summary>
internal static class ProdutoSeed
{
    private static readonly DateTime Data = new(2026, 10, 22, 0, 0, 0, DateTimeKind.Utc);

    public static readonly object[] Produtos =
    [
        Novo(1, "Geladeira", 2300.00m, "imagens/Geladeira.webp"),
        Novo(2, "Sofá 3 lugares", 2459.00m, "imagens/sofa3lugares.webp"),
        Novo(3, "Celular Xiaomi", 2189.90m, "imagens/celularxaiomi.webp"),
        Novo(4, "Notebook", 4589.00m, "imagens/notebook.webp"),
        Novo(5, "Caneca", 59.00m, "imagens/caneca.webp"),
        Novo(6, "Playstation 4", 2229.89m, "imagens/ps4.webp"),
    ];

    private static object Novo(int id, string nome, decimal preco, string imagemUrl) => new
    {
        Id = id,
        Nome = nome,
        Descricao = (string?)null,
        Preco = preco,
        ImagemUrl = imagemUrl,
        Estoque = 10,
        Ativo = true,
        CriadoEm = Data,
        AtualizadoEm = Data,
    };
}
