using SiteDeCompras.Domain.Common;

namespace SiteDeCompras.Domain.Usuarios;

/// <summary>
/// Conta de acesso à loja. A senha nunca passa por aqui em texto puro: a entidade só guarda o hash
/// gerado pela camada de aplicação.
/// </summary>
public class Usuario
{
    /// <summary>Falhas seguidas de login que bloqueiam a conta temporariamente.</summary>
    public const int MaximoFalhasLogin = 5;
    public static readonly TimeSpan DuracaoBloqueio = TimeSpan.FromMinutes(15);

    public Guid Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;

    /// <summary>E-mail como o usuário digitou, para exibição.</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>E-mail em minúsculas e sem espaços. É a chave única de login.</summary>
    public string EmailNormalizado { get; private set; } = string.Empty;

    public string SenhaHash { get; private set; } = string.Empty;
    public PapelUsuario Papel { get; private set; }

    /// <summary>
    /// Muda sempre que a senha ou o papel mudam. As sessões abertas guardam o carimbo da hora do
    /// login e deixam de valer quando ele não bate mais.
    /// </summary>
    public string CarimboSeguranca { get; private set; } = string.Empty;

    public int FalhasLogin { get; private set; }
    public DateTime? BloqueadoAte { get; private set; }
    public DateTime? UltimoLoginEm { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime AtualizadoEm { get; private set; }

    private Usuario() { }

    public Usuario(string nome, string email, string senhaHash, PapelUsuario papel = PapelUsuario.Cliente)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("O nome é obrigatório.");
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("O e-mail é obrigatório.");
        if (string.IsNullOrWhiteSpace(senhaHash))
            throw new DomainException("A senha é obrigatória.");

        Id = Guid.NewGuid();
        Nome = nome.Trim();
        Email = email.Trim();
        EmailNormalizado = NormalizarEmail(email);
        SenhaHash = senhaHash;
        Papel = papel;
        CarimboSeguranca = NovoCarimbo();
        CriadoEm = AtualizadoEm = DateTime.UtcNow;
    }

    public static string NormalizarEmail(string email) => email.Trim().ToLowerInvariant();

    public bool EstaBloqueado(DateTime agora) => BloqueadoAte > agora;

    /// <summary>Conta uma senha errada e bloqueia a conta ao atingir o limite.</summary>
    public void RegistrarFalhaLogin(DateTime agora)
    {
        FalhasLogin++;
        if (FalhasLogin >= MaximoFalhasLogin)
        {
            BloqueadoAte = agora.Add(DuracaoBloqueio);
            FalhasLogin = 0;
        }
        AtualizadoEm = agora;
    }

    public void RegistrarLogin(DateTime agora)
    {
        FalhasLogin = 0;
        BloqueadoAte = null;
        UltimoLoginEm = agora;
        AtualizadoEm = agora;
    }

    /// <summary>Troca o hash da senha e derruba as sessões abertas.</summary>
    public void AlterarSenhaHash(string senhaHash)
    {
        if (string.IsNullOrWhiteSpace(senhaHash))
            throw new DomainException("A senha é obrigatória.");
        SenhaHash = senhaHash;
        CarimboSeguranca = NovoCarimbo();
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Atualiza só o hash, sem derrubar sessões: usado quando o algoritmo ou o custo do hash mudam
    /// e a senha correta é regravada no formato novo.
    /// </summary>
    public void AtualizarFormatoHash(string senhaHash) => SenhaHash = senhaHash;

    public void DefinirPapel(PapelUsuario papel)
    {
        if (Papel == papel) return;
        Papel = papel;
        CarimboSeguranca = NovoCarimbo();
        AtualizadoEm = DateTime.UtcNow;
    }

    private static string NovoCarimbo() => Guid.NewGuid().ToString("N");
}
