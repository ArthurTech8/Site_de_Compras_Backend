using SiteDeCompras.Domain.Common;

namespace SiteDeCompras.Domain.Produtos;

public class Produto
{
    /// <summary>Faixa permitida para o desconto no Pix, em percentual.</summary>
    public const decimal DescontoPixMinimo = 5m;
    public const decimal DescontoPixMaximo = 15m;

    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string? Descricao { get; private set; }
    public decimal Preco { get; private set; }
    public string? ImagemUrl { get; private set; }
    public int Estoque { get; private set; }

    /// <summary>Desconto (%) que o produto dá no pagamento via Pix.</summary>
    public decimal DescontoPix { get; private set; } = DescontoPixMinimo;
    public bool Ativo { get; private set; } = true;
    public DateTime CriadoEm { get; private set; }
    public DateTime AtualizadoEm { get; private set; }

    private Produto() { }

    public Produto(string nome, decimal preco, int estoque, string? descricao = null, string? imagemUrl = null,
        decimal descontoPix = DescontoPixMinimo)
    {
        Atualizar(nome, preco, estoque, descricao, imagemUrl, descontoPix);
        CriadoEm = AtualizadoEm;
    }

    public void Atualizar(string nome, decimal preco, int estoque, string? descricao, string? imagemUrl,
        decimal descontoPix = DescontoPixMinimo)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("O nome do produto é obrigatório.");
        if (preco <= 0)
            throw new DomainException("O preço do produto deve ser maior que zero.");
        if (estoque < 0)
            throw new DomainException("O estoque não pode ser negativo.");
        if (descontoPix is < DescontoPixMinimo or > DescontoPixMaximo)
            throw new DomainException($"O desconto no Pix deve ficar entre {DescontoPixMinimo}% e {DescontoPixMaximo}%.");
        if (!ImagemUrlValida(imagemUrl))
            throw new DomainException("A imagem deve ser um caminho do site (ex.: imagens/foto.webp) ou um endereço http(s).");

        Nome = nome.Trim();
        Preco = preco;
        Estoque = estoque;
        Descricao = descricao?.Trim();
        ImagemUrl = imagemUrl?.Trim();
        DescontoPix = descontoPix;
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Aceita caminho relativo ou URL http(s). Recusa javascript:, data: e "//outro-site", que o
    /// navegador trataria como outro protocolo ou outro domínio.
    /// </summary>
    public static bool ImagemUrlValida(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        url = url.Trim();
        if (url.StartsWith("//") || url.StartsWith(@"\")) return false;
        if (Uri.TryCreate(url, UriKind.Absolute, out var absoluta) && !url.StartsWith('/'))
            return absoluta.Scheme is "http" or "https";
        return !url.Contains(':');
    }

    /// <summary>Retira do estoque a quantidade vendida.</summary>
    public void BaixarEstoque(int quantidade)
    {
        if (!Ativo)
            throw new DomainException($"O produto '{Nome}' não está disponível.");
        if (quantidade > Estoque)
            throw new DomainException($"Estoque insuficiente para '{Nome}'. Disponível: {Estoque}.");
        Estoque -= quantidade;
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
