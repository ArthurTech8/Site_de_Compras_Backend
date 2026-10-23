using SiteDeCompras.Application.Common;

namespace SiteDeCompras.Application.Usuarios;

/// <summary>
/// Regras de senha no estilo do NIST SP 800-63B: tamanho mínimo, sem exigir símbolos ou maiúsculas,
/// e recusando as senhas mais usadas em vazamentos.
/// </summary>
public static class PoliticaSenha
{
    public const int TamanhoMinimo = 8;
    public const int TamanhoMaximo = 128;

    // As mais frequentes nas listas de vazamentos, incluindo as comuns no Brasil.
    private static readonly HashSet<string> Comuns = new(StringComparer.OrdinalIgnoreCase)
    {
        "12345678", "123456789", "1234567890", "87654321", "11111111", "00000000", "12341234",
        "password", "password1", "password123", "passw0rd", "qwertyui", "qwerty123", "iloveyou",
        "abc12345", "abcd1234", "admin123", "administrador", "senha123", "senha1234", "minhasenha",
        "mudar123", "brasil123", "flamengo", "corinthians", "palmeiras", "santos123", "gremio123",
        "vasco123", "saopaulo", "12qwaszx", "1q2w3e4r", "q1w2e3r4", "asdfghjk", "zaq12wsx",
        "lojaonline", "loja1234", "welcome1", "sunshine", "princess", "football", "baseball",
    };

    public static void Validar(string senha, string email, string nome)
    {
        if (senha.Length is < TamanhoMinimo or > TamanhoMaximo)
            throw new ValidacaoException("Senha", $"A senha deve ter de {TamanhoMinimo} a {TamanhoMaximo} caracteres.");
        if (senha.Distinct().Count() < 4)
            throw new ValidacaoException("Senha", "A senha repete caracteres demais. Use pelo menos 4 diferentes.");
        if (Comuns.Contains(senha))
            throw new ValidacaoException("Senha", "Essa senha é muito comum e aparece em listas de vazamentos. Escolha outra.");

        // Partes do nome, o nome sem espaços e o usuário do e-mail ("maria", "silva", "mariasilva").
        var pessoais = nome.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Append(nome.Replace(" ", ""))
            .Append(email.Split('@')[0].Trim());
        if (pessoais.Any(trecho => trecho.Length >= 4 && senha.Contains(trecho, StringComparison.OrdinalIgnoreCase)))
            throw new ValidacaoException("Senha", "A senha não pode conter o seu nome ou o seu e-mail.");
    }
}
