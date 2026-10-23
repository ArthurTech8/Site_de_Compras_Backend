namespace SiteDeCompras.Infrastructure.Persistence.Configurations;

/// <summary>
/// Catálogo inicial, o mesmo de senac-26-CarrinhoCompras/data.js. As imagens ficam no frontend.
/// O desconto no Pix de cada produto (5% a 15%) é só um exemplo para o checkout.
/// </summary>
internal static class ProdutoSeed
{
    private static readonly DateTime Data = new(2026, 10, 22, 0, 0, 0, DateTimeKind.Utc);

    public static readonly object[] Produtos =
    [
        Novo(1, "Geladeira", 2300.00m, "imagens/Geladeira.webp", 10m),
        Novo(2, "Sofá 3 lugares", 2459.00m, "imagens/sofa3lugares.webp", 8m),
        Novo(3, "Celular Xiaomi", 2189.90m, "imagens/celularxaiomi.webp", 12m),
        Novo(4, "Notebook", 4589.00m, "imagens/notebook.webp", 15m),
        Novo(5, "Caneca", 59.00m, "imagens/caneca.webp", 5m),
        Novo(6, "Playstation 4", 2229.89m, "imagens/ps4.webp", 7m),
    ];

    private static object Novo(int id, string nome, decimal preco, string imagemUrl, decimal descontoPix) => new
    {
        Id = id,
        Nome = nome,
        Descricao = (string?)null,
        Preco = preco,
        ImagemUrl = imagemUrl,
        Estoque = 10,
        DescontoPix = descontoPix,
        Ativo = true,
        CriadoEm = Data,
        AtualizadoEm = Data,
    };
}
