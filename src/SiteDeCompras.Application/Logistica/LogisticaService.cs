using SiteDeCompras.Application.Common;
using SiteDeCompras.Application.Pedidos;
using SiteDeCompras.Domain.Logistica;

namespace SiteDeCompras.Application.Logistica;

public class LogisticaService(
    IEnvioRepository envios,
    IPedidoRepository pedidos,
    IDivergenciaRepository divergencias,
    AcompanhamentoPedidos acompanhamento,
    IUnitOfWork uow,
    TimeProvider relogio)
{
    private DateTime Agora => relogio.GetUtcNow().UtcDateTime;

    public async Task<PagedResult<EnvioResumoDto>> ListarAsync(EnvioFiltro filtro, CancellationToken ct = default)
    {
        var agora = Agora;
        var pagina = await envios.ListarAsync(filtro, agora, ct);
        return new PagedResult<EnvioResumoDto>(
            pagina.Itens.Select(e => EnvioResumoDto.De(e, agora)).ToList(), pagina.Pagina, pagina.TamanhoPagina, pagina.TotalItens);
    }

    public async Task<EnvioDto> ObterAsync(Guid id, CancellationToken ct = default) =>
        EnvioDto.De(await envios.ObterPorIdAsync(id, ct) ?? throw new NotFoundException("Envio", id), Agora, comInternos: true);

    /// <summary>
    /// Leva o envio à próxima etapa do transporte e atualiza o status do pedido junto, na mesma
    /// transação.
    /// </summary>
    public async Task<EnvioDto> AvancarAsync(Guid id, CancellationToken ct = default)
    {
        var agora = Agora;
        var envio = await envios.ObterPorIdAsync(id, ct) ?? throw new NotFoundException("Envio", id);
        envio.Avancar(agora);
        await acompanhamento.AtualizarAsync([envio], ct);
        await uow.SalvarAsync(ct);
        return EnvioDto.De(envio, agora, comInternos: true);
    }

    public async Task<PainelLogisticaDto> PainelAsync(CancellationToken ct = default)
    {
        var agora = Agora;
        var (inicioDoDia, fimDoDia) = CalendarioLogistica.DiaEmUtc(agora);
        var porStatus = await envios.ContarPorStatusAsync(ct);
        var completo = Enum.GetValues<StatusEnvio>().ToDictionary(s => s, s => porStatus.GetValueOrDefault(s));
        var atrasados = await envios.ContarNoCentroComPrazoAteAsync(agora, ct);

        return new PainelLogisticaDto(
            completo,
            atrasados,
            await envios.ContarNoCentroComPrazoAteAsync(fimDoDia, ct) - atrasados,
            await envios.ContarEntreguesEntreAsync(inicioDoDia, fimDoDia, ct),
            (await divergencias.ListarAbertasAsync(ct)).Count);
    }

    /// <summary>Envios do pedido, com a mesma regra de quem pode ver o pedido.</summary>
    public async Task<IReadOnlyList<EnvioDto>> RastreioDoPedidoAsync(
        Guid pedidoId, Guid? usuarioId, bool podeVerTodos, CancellationToken ct = default)
    {
        var pedido = await pedidos.ObterPorIdAsync(pedidoId, ct);
        if (pedido is null || !pedido.PodeSerVistoPor(usuarioId, podeVerTodos))
            throw new NotFoundException("Pedido", pedidoId);

        var agora = Agora;
        return (await envios.ListarDoPedidoAsync(pedidoId, ct)).Select(e => EnvioDto.De(e, agora)).ToList();
    }

    public async Task<RastreioPublicoDto> RastreioPublicoAsync(string codigo, CancellationToken ct = default)
    {
        var normalizado = codigo.Trim().ToUpperInvariant();
        var envio = await envios.ObterPorCodigoAsync(normalizado, ct) ?? throw new NotFoundException("Rastreio", normalizado);
        return RastreioPublicoDto.De(envio);
    }
}
