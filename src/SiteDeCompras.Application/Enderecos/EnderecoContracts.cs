using System.ComponentModel.DataAnnotations;
using SiteDeCompras.Domain.Enderecos;

namespace SiteDeCompras.Application.Enderecos;

public record EnderecoDto(
    string Destinatario,
    string Cep,
    string Logradouro,
    string Numero,
    string? Complemento,
    string Bairro,
    string Cidade,
    string Uf)
{
    public static EnderecoDto De(Endereco e) =>
        new(e.Destinatario, e.Cep, e.Logradouro, e.Numero, e.Complemento, e.Bairro, e.Cidade, e.Uf);
}

public record EnderecoSalvoDto(int Id, EnderecoDto Endereco, DateTime CriadoEm)
{
    public static EnderecoSalvoDto De(EnderecoSalvo e) => new(e.Id, EnderecoDto.De(e.Endereco), e.CriadoEm);
}

public record EnderecoRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Destinatario { get; init; } = string.Empty;

    /// <summary>Com ou sem hífen: "01310-100" ou "01310100".</summary>
    [Required, RegularExpression(@"^\d{5}-?\d{3}$", ErrorMessage = "O CEP precisa ter 8 dígitos.")]
    public string Cep { get; init; } = string.Empty;

    [Required, StringLength(150)]
    public string Logradouro { get; init; } = string.Empty;

    [Required, StringLength(20)]
    public string Numero { get; init; } = string.Empty;

    [StringLength(100)]
    public string? Complemento { get; init; }

    [Required, StringLength(100)]
    public string Bairro { get; init; } = string.Empty;

    [Required, StringLength(100)]
    public string Cidade { get; init; } = string.Empty;

    [Required, StringLength(2, MinimumLength = 2)]
    public string Uf { get; init; } = string.Empty;

    public Endereco ParaDominio() => new(Destinatario, Cep, Logradouro, Numero, Complemento, Bairro, Cidade, Uf);
}
