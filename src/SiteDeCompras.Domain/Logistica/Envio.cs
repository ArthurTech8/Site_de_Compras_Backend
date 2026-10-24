using System.Security.Cryptography;
using SiteDeCompras.Domain.Common;
using SiteDeCompras.Domain.Enderecos;
using SiteDeCompras.Domain.Pedidos;

namespace SiteDeCompras.Domain.Logistica;

/// <summary>
/// O pacote que sai do centro de distribuição até o cliente. Fica separado do pedido, como no
/// Mercado Envios: hoje cada pedido gera um envio, mas um pedido de vários vendedores poderá
/// gerar vários.
/// </summary>
public class Envio
{
    private readonly List<EventoRastreio> _eventos = [];

    public Guid Id { get; private set; }
    public Guid PedidoId { get; private set; }

    /// <summary>Gerado junto com a etiqueta, em <see cref="StatusEnvio.ProntoParaEnvio"/>.</summary>
    public string? CodigoRastreio { get; private set; }
    public StatusEnvio Status { get; private set; }
    public Endereco Destino { get; private set; } = null!;
    public DateTime CriadoEm { get; private set; }
    public DateTime AtualizadoEm { get; private set; }

    /// <summary>Até quando o pacote precisa ser coletado pela transportadora.</summary>
    public DateTime PrazoDespacho { get; private set; }
    public DateTime PrevisaoEntrega { get; private set; }
    public IReadOnlyCollection<EventoRastreio> Eventos => _eventos.AsReadOnly();

    private Envio() { }

    /// <summary>Abre o envio de um pedido pago, na fila de separação.</summary>
    public static Envio Criar(Pedido pedido, DateTime agoraUtc)
    {
        if (pedido.EnderecoEntrega is null)
            throw new DomainException("O pedido não tem endereço de entrega.");

        var envio = new Envio
        {
            Id = Guid.NewGuid(),
            PedidoId = pedido.Id,
            Status = StatusEnvio.AguardandoSeparacao,
            Destino = pedido.EnderecoEntrega.Copiar(),
            CriadoEm = agoraUtc,
            AtualizadoEm = agoraUtc,
            PrazoDespacho = CalendarioLogistica.PrazoDespacho(agoraUtc),
        };
        envio.PrevisaoEntrega = CalendarioLogistica.PrevisaoEntrega(envio.PrazoDespacho, envio.Destino.Cep);
        envio.RegistrarEvento(agoraUtc);
        return envio;
    }

    public bool Atrasado(DateTime agoraUtc) => Status.NoCentroDeDistribuicao() && agoraUtc > PrazoDespacho;

    /// <summary>
    /// Leva o envio à próxima etapa. Enquanto não existem as telas de separação, embalagem e
    /// expedição, é assim que o operador move o pacote.
    /// </summary>
    public void Avancar(DateTime agoraUtc)
    {
        if (Status == StatusEnvio.Entregue)
            throw new DomainException("O envio já foi entregue.");

        Status += 1;
        if (Status == StatusEnvio.ProntoParaEnvio)
            CodigoRastreio = NovoCodigoRastreio();
        AtualizadoEm = agoraUtc;
        RegistrarEvento(agoraUtc);
    }

    private void RegistrarEvento(DateTime agoraUtc)
    {
        var destino = $"{Destino.Cidade}, {Destino.Uf}";
        var (descricao, local) = Status switch
        {
            StatusEnvio.AguardandoSeparacao => ("Pagamento aprovado. O pedido entrou na fila do centro de distribuição.", CalendarioLogistica.LocalCentroDistribuicao),
            StatusEnvio.EmSeparacao => ("Estamos separando os seus produtos.", CalendarioLogistica.LocalCentroDistribuicao),
            StatusEnvio.Separado => ("Produtos separados, indo para a embalagem.", CalendarioLogistica.LocalCentroDistribuicao),
            StatusEnvio.Embalado => ("Pacote embalado.", CalendarioLogistica.LocalCentroDistribuicao),
            StatusEnvio.ProntoParaEnvio => ("Etiqueta gerada. O pacote aguarda a coleta da transportadora.", CalendarioLogistica.LocalCentroDistribuicao),
            StatusEnvio.Expedido => ("Pacote coletado pela transportadora.", CalendarioLogistica.LocalCentroDistribuicao),
            StatusEnvio.EmTransito => ($"Em trânsito para {destino}.", CalendarioLogistica.LocalCentroDistribuicao),
            StatusEnvio.SaiuParaEntrega => ("Saiu para entrega.", destino),
            StatusEnvio.Entregue => ("Pacote entregue.", destino),
            _ => throw new ArgumentOutOfRangeException(nameof(Status)),
        };
        _eventos.Add(new EventoRastreio(Id, agoraUtc, Status, descricao, local));
    }

    /// <summary>"SC" + 9 dígitos + "BR", no formato dos objetos dos Correios. Único por índice no banco.</summary>
    private static string NovoCodigoRastreio() => $"SC{RandomNumberGenerator.GetInt32(0, 1_000_000_000):D9}BR";
}
