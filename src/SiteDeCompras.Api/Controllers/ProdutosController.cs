using Microsoft.AspNetCore.Mvc;
using SiteDeCompras.Application.Common;
using SiteDeCompras.Application.Produtos;

namespace SiteDeCompras.Api.Controllers;

[ApiController]
[Route("api/v1/produtos")]
[Produces("application/json")]
public class ProdutosController(ProdutoService service) : ControllerBase
{
    /// <summary>Lista os produtos ativos com busca, filtro de preço, ordenação e paginação.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<ProdutoDto>>(StatusCodes.Status200OK)]
    public Task<PagedResult<ProdutoDto>> Listar([FromQuery] ProdutoFiltro filtro, CancellationToken ct) =>
        service.ListarAsync(filtro, ct);

    [HttpGet("{id:int}")]
    [ProducesResponseType<ProdutoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ProdutoDto> Obter(int id, CancellationToken ct) => service.ObterAsync(id, ct);

    [HttpPost]
    [ProducesResponseType<ProdutoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProdutoDto>> Criar(SalvarProdutoRequest req, CancellationToken ct)
    {
        var produto = await service.CriarAsync(req, ct);
        return CreatedAtAction(nameof(Obter), new { id = produto.Id }, produto);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<ProdutoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ProdutoDto> Atualizar(int id, SalvarProdutoRequest req, CancellationToken ct) =>
        service.AtualizarAsync(id, req, ct);

    /// <summary>Desativa o produto (exclusão lógica).</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remover(int id, CancellationToken ct)
    {
        await service.RemoverAsync(id, ct);
        return NoContent();
    }
}
