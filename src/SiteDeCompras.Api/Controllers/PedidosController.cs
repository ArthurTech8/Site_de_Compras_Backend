using Microsoft.AspNetCore.Mvc;
using SiteDeCompras.Application.Pedidos;

namespace SiteDeCompras.Api.Controllers;

[ApiController]
[Route("api/v1/pedidos")]
[Produces("application/json")]
public class PedidosController(PedidoService service) : ControllerBase
{
    /// <summary>
    /// Finaliza o carrinho com a forma de pagamento escolhida. O pagamento é simulado e aprovado
    /// na hora; o carrinho é esvaziado e o estoque baixado.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<PedidoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PedidoDto>> Fechar(FecharPedidoRequest req, CancellationToken ct)
    {
        var pedido = await service.FecharAsync(req, ct);
        return CreatedAtAction(nameof(Obter), new { id = pedido.Id }, pedido);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<PedidoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<PedidoDto> Obter(Guid id, CancellationToken ct) => service.ObterAsync(id, ct);
}
