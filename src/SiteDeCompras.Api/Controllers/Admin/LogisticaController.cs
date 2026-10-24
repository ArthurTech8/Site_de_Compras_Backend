using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteDeCompras.Api.Seguranca;
using SiteDeCompras.Application.Common;
using SiteDeCompras.Application.Logistica;

namespace SiteDeCompras.Api.Controllers.Admin;

/// <summary>Back-office do centro de distribuição.</summary>
[ApiController]
[Authorize(Policy = Politicas.ConsultarLogistica)]
[Route("api/v1/admin/logistica")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class LogisticaController(LogisticaService service) : ControllerBase
{
    /// <summary>Quantos envios há em cada etapa, quantos passaram do prazo de despacho e as entregas de hoje.</summary>
    [HttpGet("painel")]
    [ProducesResponseType<PainelLogisticaDto>(StatusCodes.Status200OK)]
    public Task<PainelLogisticaDto> Painel(CancellationToken ct) => service.PainelAsync(ct);

    /// <summary>Fila de envios, do menor prazo de despacho para o maior.</summary>
    [HttpGet("envios")]
    [ProducesResponseType<PagedResult<EnvioResumoDto>>(StatusCodes.Status200OK)]
    public Task<PagedResult<EnvioResumoDto>> Listar([FromQuery] EnvioFiltro filtro, CancellationToken ct) =>
        service.ListarAsync(filtro, ct);

    [HttpGet("envios/{id:guid}")]
    [ProducesResponseType<EnvioDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<EnvioDto> Obter(Guid id, CancellationToken ct) => service.ObterAsync(id, ct);

    /// <summary>
    /// Leva o envio para a próxima etapa. Atalho até existirem as telas de separação, embalagem e
    /// expedição; o pedido acompanha o envio.
    /// </summary>
    [HttpPost("envios/{id:guid}/avancar")]
    [Authorize(Policy = Politicas.OperarLogistica)]
    [ProducesResponseType<EnvioDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<EnvioDto> Avancar(Guid id, CancellationToken ct) => service.AvancarAsync(id, ct);
}
