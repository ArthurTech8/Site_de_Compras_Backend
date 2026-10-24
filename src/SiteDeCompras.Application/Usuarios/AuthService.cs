using SiteDeCompras.Application.Common;
using SiteDeCompras.Domain.Usuarios;

namespace SiteDeCompras.Application.Usuarios;

public class AuthService(IUsuarioRepository usuarios, IHasherSenha hasher, IUnitOfWork uow, TimeProvider relogio)
{
    // Hash de uma senha aleatória, verificado quando o e-mail não existe: assim o login de um
    // e-mail sem conta demora o mesmo que o de uma senha errada e o tempo não revela quem tem conta.
    // Fica estático para ser gerado uma vez só, e não a cada requisição.
    private static string? _hashFalso;

    public async Task<Usuario> CadastrarAsync(CadastroRequest req, CancellationToken ct = default)
    {
        PoliticaSenha.Validar(req.Senha, req.Email, req.Nome);

        var email = Usuario.NormalizarEmail(req.Email);
        if (await usuarios.ExisteEmailAsync(email, ct))
            throw new ConflitoException("Já existe uma conta com este e-mail. Entre com ela ou use outro e-mail.");

        var usuario = new Usuario(req.Nome, req.Email, hasher.Gerar(req.Senha));
        usuarios.Adicionar(usuario);
        await uow.SalvarAsync(ct);
        return usuario;
    }

    /// <summary>Confere e-mail e senha, contando falhas e bloqueando a conta após várias seguidas.</summary>
    public async Task<Usuario> AutenticarAsync(LoginRequest req, CancellationToken ct = default)
    {
        var agora = relogio.GetUtcNow().UtcDateTime;
        var usuario = await usuarios.ObterPorEmailAsync(Usuario.NormalizarEmail(req.Email), ct);

        if (usuario is null)
        {
            hasher.Verificar(_hashFalso ??= hasher.Gerar(Guid.NewGuid().ToString()), req.Senha);
            throw new CredenciaisInvalidasException();
        }

        // Bloqueada: nem confere a senha, para que a tentativa não sirva de oráculo.
        if (usuario.EstaBloqueado(agora))
            throw new CredenciaisInvalidasException();

        var resultado = hasher.Verificar(usuario.SenhaHash, req.Senha);
        if (resultado == ResultadoVerificacaoSenha.Incorreta)
        {
            usuario.RegistrarFalhaLogin(agora);
            await uow.SalvarAsync(ct);
            throw new CredenciaisInvalidasException();
        }

        if (resultado == ResultadoVerificacaoSenha.CorretaPrecisaRegravar)
            usuario.AtualizarFormatoHash(hasher.Gerar(req.Senha));
        usuario.RegistrarLogin(agora);
        await uow.SalvarAsync(ct);
        return usuario;
    }

    public async Task<UsuarioDto> ObterAsync(Guid id, CancellationToken ct = default) =>
        UsuarioDto.De(await usuarios.ObterPorIdAsync(id, ct) ?? throw new NotFoundException("Usuário", id));

    /// <summary>
    /// A sessão vale enquanto o usuário existir, o carimbo for o mesmo do login e a conta não
    /// estiver bloqueada. Trocar a senha ou o papel derruba as sessões antigas.
    /// </summary>
    public async Task<Usuario?> ValidarSessaoAsync(Guid id, string carimbo, CancellationToken ct = default)
    {
        var usuario = await usuarios.ObterPorIdAsync(id, ct);
        if (usuario is null || usuario.CarimboSeguranca != carimbo) return null;
        return usuario.EstaBloqueado(relogio.GetUtcNow().UtcDateTime) ? null : usuario;
    }

    /// <summary>
    /// Cria o primeiro administrador a partir da configuração (user-secrets ou variável de
    /// ambiente), só se ainda não houver nenhum. Não existe rota pública que crie admins.
    /// </summary>
    /// <returns>
    /// false quando o e-mail já pertence a uma conta de cliente. Ela não é promovida, porque
    /// qualquer pessoa poderia ter se cadastrado antes com o e-mail do admin e a própria senha.
    /// </returns>
    public async Task<bool> GarantirAdminInicialAsync(string? nome, string? email, string? senha, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha)) return true;
        if (await usuarios.ExisteAdminAsync(ct)) return true;
        if (await usuarios.ExisteEmailAsync(Usuario.NormalizarEmail(email), ct)) return false;

        nome = string.IsNullOrWhiteSpace(nome) ? "Administrador" : nome;
        PoliticaSenha.Validar(senha, email, nome);
        usuarios.Adicionar(new Usuario(nome, email, hasher.Gerar(senha), PapelUsuario.Admin));
        await uow.SalvarAsync(ct);
        return true;
    }
}
