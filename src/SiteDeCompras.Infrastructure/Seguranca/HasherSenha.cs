using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SiteDeCompras.Application.Usuarios;

namespace SiteDeCompras.Infrastructure.Seguranca;

/// <summary>
/// Usa o hasher do ASP.NET Core Identity: PBKDF2-HMAC-SHA512 com sal aleatório de 128 bits por
/// senha e comparação em tempo constante. As iterações seguem a recomendação da OWASP para
/// PBKDF2-HMAC-SHA512 (210.000). Hashes gravados com menos iterações são regravados no próximo login.
/// </summary>
public class HasherSenha : IHasherSenha
{
    public const int Iteracoes = 210_000;

    private static readonly object Dono = new();
    private readonly PasswordHasher<object> _hasher = new(
        Options.Create(new PasswordHasherOptions
        {
            CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3,
            IterationCount = Iteracoes,
        }));

    public string Gerar(string senha) => _hasher.HashPassword(Dono, senha);

    public ResultadoVerificacaoSenha Verificar(string hash, string senha)
    {
        try
        {
            return _hasher.VerifyHashedPassword(Dono, hash, senha) switch
            {
                PasswordVerificationResult.Success => ResultadoVerificacaoSenha.Correta,
                PasswordVerificationResult.SuccessRehashNeeded => ResultadoVerificacaoSenha.CorretaPrecisaRegravar,
                _ => ResultadoVerificacaoSenha.Incorreta,
            };
        }
        catch (FormatException)
        {
            return ResultadoVerificacaoSenha.Incorreta;
        }
    }
}
