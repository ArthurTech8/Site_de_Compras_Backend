using System.Text.RegularExpressions;
using SiteDeCompras.Domain.Common;

namespace SiteDeCompras.Domain.Produtos;

public class Produto
{
    /// <summary>Faixa permitida para o desconto no Pix, em percentual.</summary>
    public const decimal DescontoPixMinimo = 5m;
    public const decimal DescontoPixMaximo = 15m;

    public const int PesoPadraoGramas = 500;
    public const int MedidaPadraoCm = 10;
    public const int PesoMaximoGramas = 200_000;
    public const int MedidaMaximaCm = 300;

    private static readonly Regex FormatoSku = new(@"^[A-Z0-9][A-Z0-9-]{2,29}$", RegexOptions.Compiled);
    private static readonly Regex FormatoLocal = new(@"^[A-Z]-\d{2}-\d$", RegexOptions.Compiled);

    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string? Descricao { get; private set; }
    public decimal Preco { get; private set; }
    public string? ImagemUrl { get; private set; }
    public int Estoque { get; private set; }

    /// <summary>Desconto (%) que o produto dá no pagamento via Pix.</summary>
    public decimal DescontoPix { get; private set; } = DescontoPixMinimo;
    public bool Ativo { get; private set; } = true;

    /// <summary>Código que o leitor bipa na separação e na embalagem. Único no catálogo.</summary>
    public string Sku { get; private set; } = string.Empty;

    /// <summary>Peso e medidas do produto na embalagem do fabricante, para escolher a caixa.</summary>
    public int PesoGramas { get; private set; } = PesoPadraoGramas;
    public int AlturaCm { get; private set; } = MedidaPadraoCm;
    public int LarguraCm { get; private set; } = MedidaPadraoCm;
    public int ComprimentoCm { get; private set; } = MedidaPadraoCm;

    /// <summary>Endereço da prateleira no centro de distribuição, no formato corredor-módulo-nível (A-03-2).</summary>
    public string? LocalEstoque { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime AtualizadoEm { get; private set; }

    private Produto() { }

    public Produto(string nome, decimal preco, int estoque, string? descricao = null, string? imagemUrl = null,
        decimal descontoPix = DescontoPixMinimo)
    {
        Atualizar(nome, preco, estoque, descricao, imagemUrl, descontoPix);
        Sku = NovoSku();
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

    /// <summary>
    /// SKU, peso e medidas. Os valores nulos ficam como estão, para quem edita só o cadastro da
    /// loja não precisar mandar os dados do centro de distribuição.
    /// </summary>
    public void DefinirDadosLogisticos(string? sku, int? pesoGramas, int? alturaCm, int? larguraCm, int? comprimentoCm)
    {
        if (sku is not null)
        {
            var normalizado = NormalizarSku(sku);
            if (!FormatoSku.IsMatch(normalizado))
                throw new DomainException("O SKU deve ter de 3 a 30 letras, números ou hífens, começando por letra ou número.");
            Sku = normalizado;
        }
        if (pesoGramas is { } peso)
            PesoGramas = peso is > 0 and <= PesoMaximoGramas ? peso : throw new DomainException($"O peso deve ficar entre 1 g e {PesoMaximoGramas / 1000} kg.");

        AlturaCm = ValidarMedida(alturaCm ?? AlturaCm);
        LarguraCm = ValidarMedida(larguraCm ?? LarguraCm);
        ComprimentoCm = ValidarMedida(comprimentoCm ?? ComprimentoCm);
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>Guarda o produto em outra prateleira. Nulo tira o endereço.</summary>
    public void MoverPara(string? local)
    {
        if (string.IsNullOrWhiteSpace(local))
        {
            LocalEstoque = null;
        }
        else
        {
            var normalizado = local.Trim().ToUpperInvariant();
            if (!FormatoLocal.IsMatch(normalizado))
                throw new DomainException("O endereço deve seguir o formato corredor-módulo-nível, como A-03-2.");
            LocalEstoque = normalizado;
        }
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Resultado de uma contagem de inventário. <paramref name="disponivel"/> é o que sobra na
    /// prateleira depois de reservar as unidades já vendidas e ainda não separadas.
    /// </summary>
    public void AjustarPorInventario(int disponivel)
    {
        if (disponivel < 0)
            throw new DomainException("O estoque não pode ser negativo.");
        Estoque = disponivel;
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>Volume em cm³, para a sugestão de caixa.</summary>
    public int VolumeCm3 => AlturaCm * LarguraCm * ComprimentoCm;

    public static string NormalizarSku(string sku) => sku.Trim().ToUpperInvariant();

    private static string NovoSku() => $"PRD-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

    private static int ValidarMedida(int cm) =>
        cm is > 0 and <= MedidaMaximaCm ? cm : throw new DomainException($"As medidas devem ficar entre 1 e {MedidaMaximaCm} cm.");

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
