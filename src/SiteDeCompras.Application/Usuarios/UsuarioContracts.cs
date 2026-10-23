using System.ComponentModel.DataAnnotations;
using SiteDeCompras.Domain.Usuarios;

namespace SiteDeCompras.Application.Usuarios;

/// <summary>
/// O que a API devolve sobre um usuário. Hash, carimbo de segurança, falhas de login e bloqueio
/// ficam de fora de propósito.
/// </summary>
public record UsuarioDto(Guid Id, string Nome, string Email, PapelUsuario Papel)
{
    public static UsuarioDto De(Usuario u) => new(u.Id, u.Nome, u.Email, u.Papel);
}

/// <summary>
/// Não há campo de papel: toda conta criada por aqui é de cliente, mesmo que o corpo da requisição
/// traga "papel": "Admin".
/// </summary>
public record CadastroRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Nome { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; init; } = string.Empty;

    /// <summary>De 8 a 128 caracteres. O limite de cima evita gastar CPU com hashes gigantes.</summary>
    [Required, StringLength(PoliticaSenha.TamanhoMaximo, MinimumLength = PoliticaSenha.TamanhoMinimo)]
    public string Senha { get; init; } = string.Empty;
}

public record LoginRequest
{
    [Required, StringLength(254)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(PoliticaSenha.TamanhoMaximo)]
    public string Senha { get; init; } = string.Empty;
}
