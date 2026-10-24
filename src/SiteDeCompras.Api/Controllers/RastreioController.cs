using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SiteDeCompras.Application.Logistica;

namespace SiteDeCompras.Api.Controllers;

[ApiController]
[Route("api/v1/rastreio")]
[Produces("application/json")]
public class RastreioController(LogisticaService service) : ControllerBase
{
    /// <summary>
    /// Rastreio aberto, sem login, para quem tem o código. Mostra só a cidade de destino, a
    /// previsão e os eventos; nada de nome, rua ou itens.
    /// </summary>
    [HttpGet("{codigo}")]
    [ProducesResponseType<RastreioPublicoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<RastreioPublicoDto> Obter([StringLength(20)] string codigo, CancellationToken ct) =>
        service.RastreioPublicoAsync(codigo, ct);
}
