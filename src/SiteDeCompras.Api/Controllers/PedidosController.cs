using Microsoft.AspNetCore.Mvc;
using SiteDeCompras.Api.Seguranca;
using SiteDeCompras.Application.Logistica;
using SiteDeCompras.Application.Pedidos;

namespace SiteDeCompras.Api.Controllers;

[ApiController]
[Route("api/v1/pedidos")]
[Produces("application/json")]
public class PedidosController(PedidoService service, LogisticaService logistica) : ControllerBase
{
    /// <summary>
    /// Finaliza o carrinho com a forma de pagamento escolhida. O pagamento é simulado e aprovado
    /// na hora; o carrinho é esvaziado e o estoque baixado. Com login, o pedido fica no nome do usuário.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<PedidoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PedidoDto>> Fechar(FecharPedidoRequest req, CancellationToken ct)
    {
        var pedido = await service.FecharAsync(req, User.UsuarioId(), ct);
        return CreatedAtAction(nameof(Obter), new { id = pedido.Id }, pedido);
    }

    /// <summary>Pedido com dono só aparece para o dono ou para um admin; para os outros é 404.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<PedidoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<PedidoDto> Obter(Guid id, CancellationToken ct) => service.ObterAsync(id, User.UsuarioId(), User.EhAdmin(), ct);

    /// <summary>Os envios do pedido com o histórico de cada um. Mesma regra de acesso do pedido.</summary>
    [HttpGet("{id:guid}/rastreio")]
    [ProducesResponseType<IReadOnlyList<EnvioDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IReadOnlyList<EnvioDto>> Rastreio(Guid id, CancellationToken ct) =>
        logistica.RastreioDoPedidoAsync(id, User.UsuarioId(), User.EhAdmin(), ct);
}
