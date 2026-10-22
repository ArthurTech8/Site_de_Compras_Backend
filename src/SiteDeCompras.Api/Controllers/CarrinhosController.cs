using Microsoft.AspNetCore.Mvc;
using SiteDeCompras.Application.Carrinhos;

namespace SiteDeCompras.Api.Controllers;

[ApiController]
[Route("api/v1/carrinhos")]
[Produces("application/json")]
public class CarrinhosController(CarrinhoService service) : ControllerBase
{
    /// <summary>Cria um carrinho vazio. O cliente guarda o id retornado.</summary>
    [HttpPost]
    [ProducesResponseType<CarrinhoDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CarrinhoDto>> Criar(CancellationToken ct)
    {
        var carrinho = await service.CriarAsync(ct);
        return CreatedAtAction(nameof(Obter), new { id = carrinho.Id }, carrinho);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<CarrinhoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<CarrinhoDto> Obter(Guid id, CancellationToken ct) => service.ObterAsync(id, ct);

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct)
    {
        await service.ExcluirAsync(id, ct);
        return NoContent();
    }

    /// <summary>Adiciona um produto. Se ele já estiver no carrinho, soma a quantidade.</summary>
    [HttpPost("{id:guid}/itens")]
    [ProducesResponseType<CarrinhoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<CarrinhoDto> AdicionarItem(Guid id, AdicionarItemRequest req, CancellationToken ct) =>
        service.AdicionarItemAsync(id, req, ct);

    [HttpPut("{id:guid}/itens/{produtoId:int}")]
    [ProducesResponseType<CarrinhoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<CarrinhoDto> AlterarQuantidade(Guid id, int produtoId, AlterarQuantidadeRequest req, CancellationToken ct) =>
        service.AlterarQuantidadeAsync(id, produtoId, req, ct);

    [HttpDelete("{id:guid}/itens/{produtoId:int}")]
    [ProducesResponseType<CarrinhoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<CarrinhoDto> RemoverItem(Guid id, int produtoId, CancellationToken ct) =>
        service.RemoverItemAsync(id, produtoId, ct);

    /// <summary>Esvazia o carrinho mantendo o mesmo id.</summary>
    [HttpDelete("{id:guid}/itens")]
    [ProducesResponseType<CarrinhoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<CarrinhoDto> Limpar(Guid id, CancellationToken ct) => service.LimparAsync(id, ct);
}
