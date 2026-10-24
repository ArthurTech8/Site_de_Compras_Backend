namespace SiteDeCompras.Domain.Enderecos;

/// <summary>Endereço guardado na conta do usuário para escolher no checkout.</summary>
public class EnderecoSalvo
{
    /// <summary>Endereços por conta, para a lista do checkout não virar um depósito.</summary>
    public const int MaximoPorUsuario = 10;

    public int Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public Endereco Endereco { get; private set; } = null!;
    public DateTime CriadoEm { get; private set; }

    private EnderecoSalvo() { }

    public EnderecoSalvo(Guid usuarioId, Endereco endereco, DateTime agoraUtc)
    {
        UsuarioId = usuarioId;
        Endereco = endereco;
        CriadoEm = agoraUtc;
    }
}
