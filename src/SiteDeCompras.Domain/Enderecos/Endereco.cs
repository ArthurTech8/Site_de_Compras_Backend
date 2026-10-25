using SiteDeCompras.Domain.Common;

namespace SiteDeCompras.Domain.Enderecos;

/// <summary>
/// Endereço de entrega. É um valor: o pedido e o envio guardam uma cópia, então editar ou apagar
/// um endereço salvo na conta não muda para onde um pedido já feito vai.
/// </summary>
public class Endereco
{
    public static readonly IReadOnlySet<string> Ufs = new HashSet<string>
    {
        "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG", "PA",
        "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO",
    };

    public string Destinatario { get; private set; } = string.Empty;

    /// <summary>Só os 8 dígitos, sem hífen.</summary>
    public string Cep { get; private set; } = string.Empty;
    public string Logradouro { get; private set; } = string.Empty;
    public string Numero { get; private set; } = string.Empty;
    public string? Complemento { get; private set; }
    public string Bairro { get; private set; } = string.Empty;
    public string Cidade { get; private set; } = string.Empty;
    public string Uf { get; private set; } = string.Empty;

    private Endereco() { }

    public Endereco(
        string destinatario, string cep, string logradouro, string numero, string? complemento,
        string bairro, string cidade, string uf)
    {
        Destinatario = Obrigatorio(destinatario, "O nome de quem recebe");
        Cep = NormalizarCep(cep) ?? throw new DomainException("O CEP precisa ter 8 dígitos.");
        Logradouro = Obrigatorio(logradouro, "A rua");
        Numero = Obrigatorio(numero, "O número");
        Complemento = string.IsNullOrWhiteSpace(complemento) ? null : complemento.Trim();
        Bairro = Obrigatorio(bairro, "O bairro");
        Cidade = Obrigatorio(cidade, "A cidade");
        Uf = uf?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!Ufs.Contains(Uf))
            throw new DomainException("A UF não existe.");
    }

    /// <summary>Aceita "01310-100" ou "01310100"; devolve nulo se não sobrarem 8 dígitos.</summary>
    public static string? NormalizarCep(string? cep)
    {
        var digitos = new string((cep ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
        return digitos.Length == 8 ? digitos : null;
    }

    public Endereco Copiar() => (Endereco)MemberwiseClone();

    private static string Obrigatorio(string? valor, string campo) =>
        string.IsNullOrWhiteSpace(valor) ? throw new DomainException($"{campo} é obrigatório.") : valor.Trim();
}
