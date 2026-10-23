namespace SiteDeCompras.Application.Usuarios;

public enum ResultadoVerificacaoSenha
{
    Incorreta,
    Correta,

    /// <summary>A senha confere, mas o hash usa um formato ou custo antigo e deve ser regravado.</summary>
    CorretaPrecisaRegravar
}

/// <summary>
/// Hash de senha lento e com sal (PBKDF2, Argon2...). Nunca um hash rápido como SHA-256 ou MD5.
/// </summary>
public interface IHasherSenha
{
    string Gerar(string senha);
    ResultadoVerificacaoSenha Verificar(string hash, string senha);
}
