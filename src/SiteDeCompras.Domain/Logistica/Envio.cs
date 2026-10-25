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
    /// <summary>Transportadora única até a fase de transporte cadastrar as outras.</summary>
    public const string TransportadoraPadrao = "Correio Simulado";

    private readonly List<EventoRastreio> _eventos = [];
    private readonly List<ItemEnvio> _itens = [];

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

    /// <summary>A onda de separação em que o envio está, ou a última em que foi separado.</summary>
    public int? OndaId { get; private set; }

    /// <summary>Caixa e peso, a partir de <see cref="StatusEnvio.Embalado"/>.</summary>
    public Pacote? Pacote { get; private set; }
    public string? Transportadora { get; private set; }
    public int? ManifestoId { get; private set; }

    public IReadOnlyCollection<EventoRastreio> Eventos => _eventos.AsReadOnly();
    public IReadOnlyCollection<ItemEnvio> Itens => _itens.AsReadOnly();

    private Envio() { }

    /// <summary>Abre o envio de um pedido pago, na fila de separação, com os itens do pedido.</summary>
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
        foreach (var item in pedido.Itens)
            envio._itens.Add(new ItemEnvio(envio.Id, item.ProdutoId, item.NomeProduto, item.Quantidade));
        envio.RegistrarEvento(agoraUtc);
        return envio;
    }

    public bool Atrasado(DateTime agoraUtc) => Status.NoCentroDeDistribuicao() && agoraUtc > PrazoDespacho;

    public bool TemPendenteDe(int produtoId) => _itens.Any(i => i.ProdutoId == produtoId && !i.Separado);

    // ---------- Separação ----------

    public void IniciarSeparacao(int ondaId, DateTime agoraUtc)
    {
        Exigir(StatusEnvio.AguardandoSeparacao, "entrar numa onda de separação");
        OndaId = ondaId;
        MudarPara(StatusEnvio.EmSeparacao, agoraUtc);
    }

    /// <summary>Uma unidade do produto saiu da prateleira. Com a última, o envio fica separado.</summary>
    public void Separar(int produtoId, DateTime agoraUtc)
    {
        Exigir(StatusEnvio.EmSeparacao, "separar itens");
        var item = _itens.FirstOrDefault(i => i.ProdutoId == produtoId && !i.Separado)
            ?? throw new DomainException("Este envio não tem mais unidades desse produto para separar.");
        item.Separar();
        AtualizadoEm = agoraUtc;
        if (_itens.All(i => i.Separado))
            MudarPara(StatusEnvio.Separado, agoraUtc);
    }

    /// <summary>
    /// O produto não estava na prateleira. O que já foi separado volta, e o envio sai da onda e
    /// volta para a fila. O cliente não vê esse evento.
    /// </summary>
    public void RegistrarFalta(string nomeProduto, DateTime agoraUtc)
    {
        Exigir(StatusEnvio.EmSeparacao, "registrar falta");
        foreach (var item in _itens)
            item.DevolverAPrateleira();
        OndaId = null;
        Status = StatusEnvio.AguardandoSeparacao;
        AtualizadoEm = agoraUtc;
        _eventos.Add(new EventoRastreio(Id, agoraUtc, Status,
            $"Falta de '{nomeProduto}' na prateleira. O envio voltou para a fila de separação.",
            CalendarioLogistica.LocalCentroDistribuicao, publico: false));
    }

    // ---------- Embalagem ----------

    /// <summary>O embalador bipou uma unidade do produto na bancada.</summary>
    public void Conferir(int produtoId)
    {
        Exigir(StatusEnvio.Separado, "conferir itens");
        var item = _itens.FirstOrDefault(i => i.ProdutoId == produtoId)
            ?? throw new DomainException("Este produto não faz parte do envio.");
        item.Conferir();
    }

    public void Embalar(Pacote pacote, DateTime agoraUtc)
    {
        Exigir(StatusEnvio.Separado, "ser embalado");
        var faltam = _itens.Sum(i => i.Quantidade - i.QuantidadeConferida);
        if (faltam > 0)
            throw new DomainException(faltam == 1
                ? "Falta conferir 1 unidade antes de fechar a caixa."
                : $"Faltam conferir {faltam} unidades antes de fechar a caixa.");
        Pacote = pacote;
        MudarPara(StatusEnvio.Embalado, agoraUtc);
    }

    /// <summary>Gera o código de rastreio e deixa o pacote pronto para a doca.</summary>
    public void Etiquetar(DateTime agoraUtc)
    {
        Exigir(StatusEnvio.Embalado, "receber a etiqueta");
        Transportadora = TransportadoraPadrao;
        CodigoRastreio = NovoCodigoRastreio();
        MudarPara(StatusEnvio.ProntoParaEnvio, agoraUtc);
    }

    // ---------- Expedição ----------

    public void IncluirNoManifesto(Manifesto manifesto)
    {
        manifesto.GarantirAberto();
        Exigir(StatusEnvio.ProntoParaEnvio, "entrar num manifesto");
        if (ManifestoId == manifesto.Id)
            throw new DomainException("Este pacote já está neste manifesto.");
        if (ManifestoId is not null)
            throw new DomainException("Este pacote já está em outro manifesto.");
        if (Transportadora != manifesto.Transportadora)
            throw new DomainException($"Este pacote é da {Transportadora}, não da {manifesto.Transportadora}.");
        ManifestoId = manifesto.Id;
    }

    public void RetirarDoManifesto(Manifesto manifesto)
    {
        manifesto.GarantirAberto();
        if (ManifestoId != manifesto.Id)
            throw new DomainException("Este pacote não está neste manifesto.");
        ManifestoId = null;
    }

    internal void Expedir(DateTime agoraUtc)
    {
        Exigir(StatusEnvio.ProntoParaEnvio, "ser expedido");
        MudarPara(StatusEnvio.Expedido, agoraUtc);
    }

    // ---------- Transporte ----------

    /// <summary>
    /// Leva o envio à próxima etapa do transporte. Dentro do centro de distribuição quem move o
    /// envio são as telas de separação, embalagem e expedição; até existir o simulador de
    /// transporte, é assim que o operador anda com o pacote depois da coleta.
    /// </summary>
    public void Avancar(DateTime agoraUtc)
    {
        if (Status == StatusEnvio.Entregue)
            throw new DomainException("O envio já foi entregue.");
        if (Status.NoCentroDeDistribuicao())
            throw new DomainException("No centro de distribuição o envio anda pelas telas de separação, embalagem e expedição.");
        MudarPara(Status + 1, agoraUtc);
    }

    private void Exigir(StatusEnvio esperado, string acao)
    {
        if (Status != esperado)
            throw new DomainException($"O envio está {Status.Nome()} e não pode {acao}.");
    }

    private void MudarPara(StatusEnvio status, DateTime agoraUtc)
    {
        Status = status;
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
            StatusEnvio.Expedido => ($"Pacote coletado pela transportadora {Transportadora ?? TransportadoraPadrao}.", CalendarioLogistica.LocalCentroDistribuicao),
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
