using SiteDeCompras.Application.Common;
using SiteDeCompras.Application.Produtos;
using SiteDeCompras.Domain.Common;
using SiteDeCompras.Domain.Estoque;
using SiteDeCompras.Domain.Logistica;
using SiteDeCompras.Domain.Produtos;

namespace SiteDeCompras.Application.Logistica;

/// <summary>Separação em ondas: juntar envios, montar a rota de coleta e conferir cada bipe.</summary>
public class SeparacaoService(
    IOndaRepository ondas,
    IEnvioRepository envios,
    IProdutoRepository produtos,
    IDivergenciaRepository divergencias,
    AcompanhamentoPedidos acompanhamento,
    IUnitOfWork uow,
    TimeProvider relogio)
{
    private DateTime Agora => relogio.GetUtcNow().UtcDateTime;

    public async Task<IReadOnlyList<OndaResumoDto>> ListarAsync(CancellationToken ct = default)
    {
        var lista = new List<OndaResumoDto>();
        foreach (var onda in await ondas.ListarRecentesAsync(concluidas: 10, ct))
        {
            var daOnda = await envios.ListarDaOndaAsync(onda.Id, ct);
            var itens = daOnda.SelectMany(e => e.Itens).ToList();
            lista.Add(new OndaResumoDto(onda.Id, onda.Status, onda.CriadaEm, onda.ConcluidaEm, daOnda.Count,
                itens.Sum(i => i.Quantidade), itens.Sum(i => i.QuantidadeSeparada)));
        }
        return lista;
    }

    public async Task<OndaDto> ObterAsync(int id, CancellationToken ct = default)
    {
        var onda = await ObterOndaAsync(id, ct);
        return await MontarAsync(onda, await envios.ListarDaOndaAsync(id, ct), ct);
    }

    /// <summary>
    /// Junta os envios mais urgentes da fila numa onda nova. Envios com produto em divergência
    /// esperam o inventário, para o separador não voltar a uma prateleira vazia.
    /// </summary>
    public async Task<OndaDto> GerarAsync(GerarOndaRequest req, CancellationToken ct = default)
    {
        var bloqueados = (await divergencias.ListarAbertasAsync(ct)).Select(d => d.ProdutoId).Distinct().ToList();
        var candidatos = await envios.ListarParaOndaAsync(req.Envios, bloqueados, ct);
        if (candidatos.Count == 0)
            throw new DomainException(bloqueados.Count == 0
                ? "Não há envios aguardando separação."
                : "Não há envios para separar agora. Os que têm produto em falta esperam o inventário.");

        var agora = Agora;
        var onda = new OndaSeparacao(agora);
        ondas.Adicionar(onda);
        await uow.SalvarAsync(ct); // gera o número da onda

        foreach (var envio in candidatos)
            envio.IniciarSeparacao(onda.Id, agora);
        await acompanhamento.AtualizarAsync(candidatos, ct);
        await uow.SalvarAsync(ct);
        return await MontarAsync(onda, candidatos, ct);
    }

    /// <summary>O separador bipou um produto: a unidade vai para o envio mais urgente que ainda precisa dela.</summary>
    public async Task<ResultadoOndaDto> BiparAsync(int id, BipagemRequest req, CancellationToken ct = default)
    {
        var onda = await ObterAbertaAsync(id, ct);
        var produto = await produtos.ObterPorSkuAsync(Produto.NormalizarSku(req.Codigo), ct)
            ?? throw new DomainException($"Nenhum produto tem o SKU '{req.Codigo.Trim()}'.");
        var daOnda = await envios.ListarDaOndaAsync(id, ct);

        var alvo = daOnda
            .Where(e => e.Status == StatusEnvio.EmSeparacao && e.TemPendenteDe(produto.Id))
            .OrderBy(e => e.PrazoDespacho).ThenBy(e => e.CriadoEm)
            .FirstOrDefault();
        if (alvo is null)
            throw new DomainException(daOnda.Any(e => e.Itens.Any(i => i.ProdutoId == produto.Id))
                ? $"Todas as unidades de '{produto.Nome}' desta onda já foram separadas."
                : $"'{produto.Nome}' não faz parte desta onda.");

        alvo.Separar(produto.Id, Agora);
        if (alvo.Status == StatusEnvio.Separado)
            await acompanhamento.AtualizarAsync([alvo], ct);
        await uow.SalvarAsync(ct);

        var itens = daOnda.SelectMany(e => e.Itens).Where(i => i.ProdutoId == produto.Id).ToList();
        var mensagem = alvo.Status == StatusEnvio.Separado
            ? $"Envio do pedido #{NumeroCurto(alvo.PedidoId)} separado. Leve para a embalagem."
            : $"{produto.Nome}: {itens.Sum(i => i.QuantidadeSeparada)} de {itens.Sum(i => i.Quantidade)}.";
        return new ResultadoOndaDto(await MontarAsync(onda, daOnda, ct), mensagem);
    }

    /// <summary>
    /// O produto não está na prateleira. Os envios que dependem dele voltam para a fila e o
    /// estoque ganha uma divergência para o inventário.
    /// </summary>
    public async Task<ResultadoOndaDto> FaltaAsync(int id, FaltaRequest req, CancellationToken ct = default)
    {
        var onda = await ObterAbertaAsync(id, ct);
        var produto = await produtos.ObterPorIdAsync(req.ProdutoId, ct) ?? throw new NotFoundException("Produto", req.ProdutoId);
        var daOnda = await envios.ListarDaOndaAsync(id, ct);

        var afetados = daOnda.Where(e => e.Status == StatusEnvio.EmSeparacao && e.TemPendenteDe(produto.Id)).ToList();
        if (afetados.Count == 0)
            throw new DomainException($"Nenhum envio desta onda ainda espera '{produto.Nome}'.");

        var agora = Agora;
        var faltando = afetados.SelectMany(e => e.Itens)
            .Where(i => i.ProdutoId == produto.Id)
            .Sum(i => i.Quantidade - i.QuantidadeSeparada);
        foreach (var envio in afetados)
            envio.RegistrarFalta(produto.Nome, agora);
        divergencias.Adicionar(new DivergenciaEstoque(produto.Id, onda.Id, faltando, agora));
        await acompanhamento.AtualizarAsync(afetados, ct);
        await uow.SalvarAsync(ct);

        var restantes = daOnda.Where(e => e.OndaId == onda.Id).ToList();
        var mensagem = afetados.Count == 1
            ? $"1 envio voltou para a fila. A falta de '{produto.Nome}' foi registrada para o inventário."
            : $"{afetados.Count} envios voltaram para a fila. A falta de '{produto.Nome}' foi registrada para o inventário.";
        return new ResultadoOndaDto(await MontarAsync(onda, restantes, ct), mensagem);
    }

    public async Task<OndaDto> ConcluirAsync(int id, CancellationToken ct = default)
    {
        var onda = await ObterOndaAsync(id, ct);
        var daOnda = await envios.ListarDaOndaAsync(id, ct);
        onda.Concluir(daOnda, Agora);
        await uow.SalvarAsync(ct);
        return await MontarAsync(onda, daOnda, ct);
    }

    /// <summary>A rota de coleta segue o endereço das prateleiras; produto sem endereço fica por último.</summary>
    private async Task<OndaDto> MontarAsync(OndaSeparacao onda, IReadOnlyCollection<Envio> daOnda, CancellationToken ct)
    {
        var itens = daOnda.SelectMany(e => e.Itens).ToList();
        var porId = (await produtos.ListarPorIdsAsync(itens.Select(i => i.ProdutoId), ct)).ToDictionary(p => p.Id);

        var coleta = itens
            .GroupBy(i => i.ProdutoId)
            .Select(g =>
            {
                var p = porId.GetValueOrDefault(g.Key);
                return new ItemColetaDto(g.Key, p?.Sku ?? "", p?.Nome ?? g.First().NomeProduto, p?.ImagemUrl, p?.LocalEstoque,
                    g.Sum(i => i.Quantidade), g.Sum(i => i.QuantidadeSeparada));
            })
            .OrderBy(c => c.Local is null).ThenBy(c => c.Local).ThenBy(c => c.Nome)
            .ToList();

        var lista = daOnda
            .OrderBy(e => e.PrazoDespacho).ThenBy(e => e.CriadoEm)
            .Select(e => new EnvioDaOndaDto(e.Id, e.PedidoId, e.Status, e.Destino.Destinatario,
                e.Itens.Sum(i => i.Quantidade), e.Itens.Sum(i => i.QuantidadeSeparada)))
            .ToList();
        return new OndaDto(onda.Id, onda.Status, onda.CriadaEm, onda.ConcluidaEm, coleta, lista);
    }

    private async Task<OndaSeparacao> ObterOndaAsync(int id, CancellationToken ct) =>
        await ondas.ObterPorIdAsync(id, ct) ?? throw new NotFoundException("Onda", id);

    private async Task<OndaSeparacao> ObterAbertaAsync(int id, CancellationToken ct)
    {
        var onda = await ObterOndaAsync(id, ct);
        return onda.Status == StatusOnda.Aberta ? onda : throw new DomainException("A onda já foi concluída.");
    }

    internal static string NumeroCurto(Guid pedidoId) => pedidoId.ToString("N")[..8].ToUpperInvariant();
}
