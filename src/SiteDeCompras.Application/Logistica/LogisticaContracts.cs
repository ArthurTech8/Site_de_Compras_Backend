using System.ComponentModel.DataAnnotations;
using SiteDeCompras.Application.Enderecos;
using SiteDeCompras.Domain.Logistica;

namespace SiteDeCompras.Application.Logistica;

public record EventoRastreioDto(DateTime Data, StatusEnvio Status, string Descricao, string Local, bool Publico)
{
    public static EventoRastreioDto De(EventoRastreio e) => new(e.Data, e.Status, e.Descricao, e.Local, e.Publico);
}

/// <summary>O envio completo, para o dono do pedido e para a equipe da loja.</summary>
public record EnvioDto(
    Guid Id,
    Guid PedidoId,
    string? CodigoRastreio,
    StatusEnvio Status,
    EnderecoDto Destino,
    DateTime CriadoEm,
    DateTime AtualizadoEm,
    DateTime PrazoDespacho,
    DateTime PrevisaoEntrega,
    bool Atrasado,
    IReadOnlyList<EventoRastreioDto> Eventos)
{
    /// <param name="comInternos">Inclui os eventos que só a equipe vê, como a falta de um item na prateleira.</param>
    public static EnvioDto De(Envio e, DateTime agoraUtc, bool comInternos = false) => new(
        e.Id, e.PedidoId, e.CodigoRastreio, e.Status, EnderecoDto.De(e.Destino), e.CriadoEm, e.AtualizadoEm,
        e.PrazoDespacho, e.PrevisaoEntrega, e.Atrasado(agoraUtc),
        e.Eventos.Where(ev => ev.Publico || comInternos).OrderBy(ev => ev.Data).ThenBy(ev => ev.Id).Select(EventoRastreioDto.De).ToList());
}

/// <summary>Uma linha da fila de envios do back-office.</summary>
public record EnvioResumoDto(
    Guid Id,
    Guid PedidoId,
    string? CodigoRastreio,
    StatusEnvio Status,
    string Destinatario,
    string Cidade,
    string Uf,
    DateTime CriadoEm,
    DateTime AtualizadoEm,
    DateTime PrazoDespacho,
    DateTime PrevisaoEntrega,
    bool Atrasado)
{
    public static EnvioResumoDto De(Envio e, DateTime agoraUtc) => new(
        e.Id, e.PedidoId, e.CodigoRastreio, e.Status, e.Destino.Destinatario, e.Destino.Cidade, e.Destino.Uf,
        e.CriadoEm, e.AtualizadoEm, e.PrazoDespacho, e.PrevisaoEntrega, e.Atrasado(agoraUtc));
}

/// <summary>
/// O rastreio aberto, para quem só tem o código: sem nome, rua, itens ou valores. Só a cidade de
/// destino, a previsão e os eventos.
/// </summary>
public record RastreioPublicoDto(
    string CodigoRastreio,
    StatusEnvio Status,
    string CidadeDestino,
    string UfDestino,
    DateTime PrevisaoEntrega,
    IReadOnlyList<EventoRastreioDto> Eventos)
{
    public static RastreioPublicoDto De(Envio e) => new(
        e.CodigoRastreio!, e.Status, e.Destino.Cidade, e.Destino.Uf, e.PrevisaoEntrega,
        e.Eventos.Where(ev => ev.Publico).OrderBy(ev => ev.Data).ThenBy(ev => ev.Id).Select(EventoRastreioDto.De).ToList());
}

public record PainelLogisticaDto(
    IReadOnlyDictionary<StatusEnvio, int> PorStatus,
    int Atrasados,
    int DespachoVenceHoje,
    int EntreguesHoje,
    int DivergenciasAbertas);

public class EnvioFiltro
{
    public StatusEnvio? Status { get; init; }

    /// <summary>Só os que passaram do prazo de despacho e ainda estão no centro de distribuição.</summary>
    public bool Atrasados { get; init; }

    /// <summary>Código de rastreio, início do número do pedido, nome de quem recebe ou cidade.</summary>
    [MaxLength(100)]
    public string? Busca { get; init; }

    [Range(1, int.MaxValue)]
    public int Pagina { get; init; } = 1;

    [Range(1, 100)]
    public int TamanhoPagina { get; init; } = 20;
}
