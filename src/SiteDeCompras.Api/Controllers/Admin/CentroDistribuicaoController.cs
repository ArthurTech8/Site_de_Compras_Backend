using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteDeCompras.Api.Seguranca;
using SiteDeCompras.Application.Logistica;

namespace SiteDeCompras.Api.Controllers.Admin;

/// <summary>
/// Operação dentro do centro de distribuição: estoque, separação em ondas, embalagem e expedição.
/// A equipe consulta; só o admin e o operador do CD mexem.
/// </summary>
[ApiController]
[Authorize(Policy = Politicas.ConsultarLogistica)]
[Route("api/v1/admin/logistica")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class CentroDistribuicaoController(
    EstoqueService estoque,
    SeparacaoService separacao,
    EmbalagemService embalagem,
    ExpedicaoService expedicao) : ControllerBase
{
    // ---------- Estoque ----------

    /// <summary>Onde cada produto está, quanto há para vender, quanto espera a separação e as divergências.</summary>
    [HttpGet("estoque")]
    [ProducesResponseType<IReadOnlyList<PosicaoEstoqueDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<PosicaoEstoqueDto>> Estoque(CancellationToken ct) => estoque.ListarAsync(ct);

    /// <summary>Guarda o produto em outra prateleira.</summary>
    [HttpPut("estoque/{produtoId:int}/local")]
    [Authorize(Policy = Politicas.OperarCentroDistribuicao)]
    [ProducesResponseType<PosicaoEstoqueDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<PosicaoEstoqueDto> Mover(int produtoId, MoverProdutoRequest req, CancellationToken ct) =>
        estoque.MoverAsync(produtoId, req, ct);

    /// <summary>Registra a contagem da prateleira e resolve a divergência do produto, se a contagem cobrir os pedidos.</summary>
    [HttpPost("estoque/{produtoId:int}/inventario")]
    [Authorize(Policy = Politicas.OperarCentroDistribuicao)]
    [ProducesResponseType<InventarioDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<InventarioDto> Inventariar(int produtoId, InventarioRequest req, CancellationToken ct) =>
        estoque.InventariarAsync(produtoId, req, ct);

    // ---------- Separação ----------

    /// <summary>As ondas abertas e as últimas concluídas.</summary>
    [HttpGet("ondas")]
    [ProducesResponseType<IReadOnlyList<OndaResumoDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<OndaResumoDto>> Ondas(CancellationToken ct) => separacao.ListarAsync(ct);

    /// <summary>Junta os envios mais urgentes da fila numa onda e monta a rota de coleta.</summary>
    [HttpPost("ondas")]
    [Authorize(Policy = Politicas.OperarCentroDistribuicao)]
    [ProducesResponseType<OndaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OndaDto>> GerarOnda(GerarOndaRequest? req, CancellationToken ct)
    {
        var onda = await separacao.GerarAsync(req ?? new GerarOndaRequest(), ct);
        return CreatedAtAction(nameof(Onda), new { id = onda.Id }, onda);
    }

    [HttpGet("ondas/{id:int}")]
    [ProducesResponseType<OndaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<OndaDto> Onda(int id, CancellationToken ct) => separacao.ObterAsync(id, ct);

    /// <summary>Bipe de um SKU na coleta.</summary>
    [HttpPost("ondas/{id:int}/bipar")]
    [Authorize(Policy = Politicas.OperarCentroDistribuicao)]
    [ProducesResponseType<ResultadoOndaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<ResultadoOndaDto> Bipar(int id, BipagemRequest req, CancellationToken ct) => separacao.BiparAsync(id, req, ct);

    /// <summary>O produto não estava na prateleira: os envios dele voltam para a fila.</summary>
    [HttpPost("ondas/{id:int}/falta")]
    [Authorize(Policy = Politicas.OperarCentroDistribuicao)]
    [ProducesResponseType<ResultadoOndaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<ResultadoOndaDto> Falta(int id, FaltaRequest req, CancellationToken ct) => separacao.FaltaAsync(id, req, ct);

    [HttpPost("ondas/{id:int}/concluir")]
    [Authorize(Policy = Politicas.OperarCentroDistribuicao)]
    [ProducesResponseType<OndaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<OndaDto> Concluir(int id, CancellationToken ct) => separacao.ConcluirAsync(id, ct);

    // ---------- Embalagem ----------

    /// <summary>Itens a conferir, caixa sugerida e pesos esperados do envio.</summary>
    [HttpGet("envios/{id:guid}/embalagem")]
    [ProducesResponseType<EmbalagemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<EmbalagemDto> Embalagem(Guid id, CancellationToken ct) => embalagem.ObterAsync(id, ct);

    /// <summary>Bipe de um SKU na bancada: confere uma unidade.</summary>
    [HttpPost("envios/{id:guid}/conferir")]
    [Authorize(Policy = Politicas.OperarCentroDistribuicao)]
    [ProducesResponseType<EmbalagemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<EmbalagemDto> Conferir(Guid id, BipagemRequest req, CancellationToken ct) => embalagem.ConferirAsync(id, req, ct);

    /// <summary>Fecha a caixa com o peso da balança.</summary>
    [HttpPost("envios/{id:guid}/embalar")]
    [Authorize(Policy = Politicas.OperarCentroDistribuicao)]
    [ProducesResponseType<EmbalagemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<EmbalagemDto> Embalar(Guid id, EmbalarRequest req, CancellationToken ct) => embalagem.EmbalarAsync(id, req, ct);

    /// <summary>Gera o código de rastreio e a etiqueta.</summary>
    [HttpPost("envios/{id:guid}/etiqueta")]
    [Authorize(Policy = Politicas.OperarCentroDistribuicao)]
    [ProducesResponseType<EtiquetaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<EtiquetaDto> Etiquetar(Guid id, CancellationToken ct) => embalagem.EtiquetarAsync(id, ct);

    /// <summary>Os dados da etiqueta para imprimir de novo.</summary>
    [HttpGet("envios/{id:guid}/etiqueta")]
    [ProducesResponseType<EtiquetaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<EtiquetaDto> Etiqueta(Guid id, CancellationToken ct) => embalagem.EtiquetaAsync(id, ct);

    // ---------- Expedição ----------

    /// <summary>Manifestos na doca, os últimos coletados e quantos pacotes prontos ainda estão fora de manifesto.</summary>
    [HttpGet("manifestos")]
    [ProducesResponseType<ExpedicaoDto>(StatusCodes.Status200OK)]
    public Task<ExpedicaoDto> Manifestos(CancellationToken ct) => expedicao.ListarAsync(ct);

    [HttpPost("manifestos")]
    [Authorize(Policy = Politicas.OperarCentroDistribuicao)]
    [ProducesResponseType<ManifestoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ManifestoDto>> AbrirManifesto(CancellationToken ct)
    {
        var manifesto = await expedicao.CriarAsync(ct);
        return CreatedAtAction(nameof(Manifesto), new { id = manifesto.Id }, manifesto);
    }

    [HttpGet("manifestos/{id:int}")]
    [ProducesResponseType<ManifestoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ManifestoDto> Manifesto(int id, CancellationToken ct) => expedicao.ObterAsync(id, ct);

    /// <summary>Bipe da etiqueta na doca: o pacote entra no manifesto.</summary>
    [HttpPost("manifestos/{id:int}/pacotes")]
    [Authorize(Policy = Politicas.OperarCentroDistribuicao)]
    [ProducesResponseType<ManifestoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<ManifestoDto> IncluirPacote(int id, BipagemRequest req, CancellationToken ct) => expedicao.BiparAsync(id, req, ct);

    [HttpDelete("manifestos/{id:int}/pacotes/{envioId:guid}")]
    [Authorize(Policy = Politicas.OperarCentroDistribuicao)]
    [ProducesResponseType<ManifestoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<ManifestoDto> RetirarPacote(int id, Guid envioId, CancellationToken ct) => expedicao.RetirarAsync(id, envioId, ct);

    [HttpPost("manifestos/{id:int}/fechar")]
    [Authorize(Policy = Politicas.OperarCentroDistribuicao)]
    [ProducesResponseType<ManifestoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<ManifestoDto> Fechar(int id, CancellationToken ct) => expedicao.FecharAsync(id, ct);

    /// <summary>A transportadora levou os pacotes: todos são expedidos.</summary>
    [HttpPost("manifestos/{id:int}/coletar")]
    [Authorize(Policy = Politicas.OperarCentroDistribuicao)]
    [ProducesResponseType<ManifestoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<ManifestoDto> Coletar(int id, CancellationToken ct) => expedicao.ColetarAsync(id, ct);
}
