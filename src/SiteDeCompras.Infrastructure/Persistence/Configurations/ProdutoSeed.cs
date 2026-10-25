namespace SiteDeCompras.Infrastructure.Persistence.Configurations;

/// <summary>
/// Catálogo inicial, o mesmo de senac-26-CarrinhoCompras/data.js. As imagens ficam no frontend.
/// O desconto no Pix de cada produto (5% a 15%) é só um exemplo para o checkout. SKU, peso, medidas
/// (na embalagem do fabricante) e prateleira são exemplos para o centro de distribuição.
/// </summary>
internal static class ProdutoSeed
{
    private static readonly DateTime Data = new(2026, 10, 22, 0, 0, 0, DateTimeKind.Utc);

    public static readonly object[] Produtos =
    [
        Novo(1, "Geladeira", 2300.00m, "imagens/Geladeira.webp", 10m, "GEL-0001", 68000, 175, 65, 72, "D-01-1"),
        Novo(2, "Sofá 3 lugares", 2459.00m, "imagens/sofa3lugares.webp", 8m, "SOF-0002", 52000, 90, 95, 210, "D-02-1"),
        Novo(3, "Celular Xiaomi", 2189.90m, "imagens/celularxaiomi.webp", 12m, "CEL-0003", 420, 6, 10, 18, "A-01-2"),
        Novo(4, "Notebook", 4589.00m, "imagens/notebook.webp", 15m, "NTB-0004", 2600, 7, 30, 42, "A-02-1"),
        Novo(5, "Caneca", 59.00m, "imagens/caneca.webp", 5m, "CAN-0005", 450, 11, 11, 13, "A-03-2"),
        Novo(6, "Playstation 4", 2229.89m, "imagens/ps4.webp", 7m, "PS4-0006", 3400, 11, 33, 40, "B-11-1"),
    ];

    private static object Novo(int id, string nome, decimal preco, string imagemUrl, decimal descontoPix,
        string sku, int pesoGramas, int alturaCm, int larguraCm, int comprimentoCm, string local) => new
    {
        Id = id,
        Nome = nome,
        Descricao = (string?)null,
        Preco = preco,
        ImagemUrl = imagemUrl,
        Estoque = 10,
        DescontoPix = descontoPix,
        Sku = sku,
        PesoGramas = pesoGramas,
        AlturaCm = alturaCm,
        LarguraCm = larguraCm,
        ComprimentoCm = comprimentoCm,
        LocalEstoque = local,
        Ativo = true,
        CriadoEm = Data,
        AtualizadoEm = Data,
    };
}
