using SiteDeCompras.Application.Common;
using SiteDeCompras.Application.Enderecos;
using SiteDeCompras.Application.Produtos;
using SiteDeCompras.Domain.Common;
using SiteDeCompras.Domain.Logistica;
using SiteDeCompras.Domain.Produtos;

namespace SiteDeCompras.Application.Logistica;

/// <summary>Bancada de embalagem: conferir os itens, escolher a caixa, pesar e gerar a etiqueta.</summary>
public class EmbalagemService(
    IEnvioRepository envios,
    IProdutoRepository produtos,
    AcompanhamentoPedidos acompanhamento,
    IUnitOfWork uow,
    TimeProvider relogio)
{
    /// <summary>Endereço fictício do centro de distribuição, impresso como remetente.</summary>
    public static readonly RemetenteDto Remetente = new(
        "Site de Compras · Centro de Distribuição",
        "Rua do Centro de Distribuição, 1000 · Galpão 3",
        $"{CalendarioLogistica.LocalCentroDistribuicao} · CEP 01000-000");

    private DateTime Agora => relogio.GetUtcNow().UtcDateTime;

    public async Task<EmbalagemDto> ObterAsync(Guid envioId, CancellationToken ct = default)
    {
        var envio = await ObterEnvioAsync(envioId, ct);
        return await MontarAsync(envio, ct);
    }

    /// <summary>O embalador bipou um produto: confere uma unidade.</summary>
    public async Task<EmbalagemDto> ConferirAsync(Guid envioId, BipagemRequest req, CancellationToken ct = default)
    {
        var envio = await ObterEnvioAsync(envioId, ct);
        var produto = await produtos.ObterPorSkuAsync(Produto.NormalizarSku(req.Codigo), ct)
            ?? throw new DomainException($"Nenhum produto tem o SKU '{req.Codigo.Trim()}'.");
        envio.Conferir(produto.Id);
        await uow.SalvarAsync(ct);
        return await MontarAsync(envio, ct);
    }

    /// <summary>Fecha a caixa. O peso da balança precisa bater com o dos produtos mais a caixa.</summary>
    public async Task<EmbalagemDto> EmbalarAsync(Guid envioId, EmbalarRequest req, CancellationToken ct = default)
    {
        var envio = await ObterEnvioAsync(envioId, ct);
        var volumes = await VolumesAsync(envio, ct);
        var caixa = Embalagem.Obter(req.Caixa, volumes);
        Embalagem.ConferirPeso(req.PesoGramas, Embalagem.PesoEsperado(volumes, caixa));

        envio.Embalar(new Pacote(caixa.Codigo, req.PesoGramas, caixa.AlturaCm, caixa.LarguraCm, caixa.ComprimentoCm), Agora);
        await acompanhamento.AtualizarAsync([envio], ct);
        await uow.SalvarAsync(ct);
        return await MontarAsync(envio, ct);
    }

    public async Task<EtiquetaDto> EtiquetarAsync(Guid envioId, CancellationToken ct = default)
    {
        var envio = await ObterEnvioAsync(envioId, ct);
        envio.Etiquetar(Agora);
        await acompanhamento.AtualizarAsync([envio], ct);
        await uow.SalvarAsync(ct);
        return Etiqueta(envio);
    }

    public async Task<EtiquetaDto> EtiquetaAsync(Guid envioId, CancellationToken ct = default)
    {
        var envio = await ObterEnvioAsync(envioId, ct);
        if (envio.CodigoRastreio is null)
            throw new DomainException("A etiqueta deste envio ainda não foi gerada.");
        return Etiqueta(envio);
    }

    private static EtiquetaDto Etiqueta(Envio e) => new(
        e.Id, e.PedidoId, e.CodigoRastreio!, e.Transportadora ?? Envio.TransportadoraPadrao, Remetente,
        EnderecoDto.De(e.Destino), PacoteDto.De(e.Pacote), e.Itens.Sum(i => i.Quantidade),
        e.Eventos.Where(ev => ev.Status == StatusEnvio.ProntoParaEnvio).Select(ev => ev.Data).DefaultIfEmpty(e.AtualizadoEm).Max(),
        e.PrazoDespacho, e.PrevisaoEntrega);

    private async Task<EmbalagemDto> MontarAsync(Envio envio, CancellationToken ct)
    {
        var porId = (await produtos.ListarPorIdsAsync(envio.Itens.Select(i => i.ProdutoId), ct)).ToDictionary(p => p.Id);
        var volumes = Volumes(envio, porId);
        var sugerida = Embalagem.Sugerir(volumes);

        var caixas = Embalagem.Caixas
            .Select(c => new CaixaDto(c.Codigo, c.Nome, c.AlturaCm, c.LarguraCm, c.ComprimentoCm, Cabe(c.Codigo, volumes), Embalagem.PesoEsperado(volumes, c)))
            .Append(Opcao(Embalagem.Avulso(volumes), volumes))
            .ToList();

        var itens = envio.Itens
            .Select(i =>
            {
                var p = porId.GetValueOrDefault(i.ProdutoId);
                return new ItemConferenciaDto(i.ProdutoId, p?.Sku ?? "", p?.Nome ?? i.NomeProduto, p?.ImagemUrl, i.Quantidade, i.QuantidadeConferida);
            })
            .ToList();

        return new EmbalagemDto(envio.Id, envio.PedidoId, envio.Status, envio.Destino.Destinatario, envio.Destino.Cidade, envio.Destino.Uf,
            itens, sugerida.Codigo, caixas, PacoteDto.De(envio.Pacote), envio.CodigoRastreio);
    }

    private static CaixaDto Opcao(Caixa c, IReadOnlyCollection<VolumeItem> volumes) =>
        new(c.Codigo, c.Nome, c.AlturaCm, c.LarguraCm, c.ComprimentoCm, true, Embalagem.PesoEsperado(volumes, c));

    private static bool Cabe(string codigo, IReadOnlyCollection<VolumeItem> volumes)
    {
        try
        {
            Embalagem.Obter(codigo, volumes);
            return true;
        }
        catch (DomainException)
        {
            return false;
        }
    }

    private async Task<IReadOnlyCollection<VolumeItem>> VolumesAsync(Envio envio, CancellationToken ct)
    {
        var porId = (await produtos.ListarPorIdsAsync(envio.Itens.Select(i => i.ProdutoId), ct)).ToDictionary(p => p.Id);
        return Volumes(envio, porId);
    }

    /// <summary>Produto apagado do catálogo entra com as medidas e o peso padrão.</summary>
    private static IReadOnlyCollection<VolumeItem> Volumes(Envio envio, IReadOnlyDictionary<int, Produto> porId) =>
        envio.Itens.Select(i => porId.GetValueOrDefault(i.ProdutoId) is { } p
                ? new VolumeItem(p.AlturaCm, p.LarguraCm, p.ComprimentoCm, p.PesoGramas, i.Quantidade)
                : new VolumeItem(Produto.MedidaPadraoCm, Produto.MedidaPadraoCm, Produto.MedidaPadraoCm, Produto.PesoPadraoGramas, i.Quantidade))
            .ToList();

    private async Task<Envio> ObterEnvioAsync(Guid id, CancellationToken ct) =>
        await envios.ObterPorIdAsync(id, ct) ?? throw new NotFoundException("Envio", id);
}
