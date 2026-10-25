using SiteDeCompras.Application.Common;
using SiteDeCompras.Domain.Common;
using SiteDeCompras.Domain.Logistica;

namespace SiteDeCompras.Application.Logistica;

/// <summary>Doca de expedição: o manifesto de coleta de cada transportadora.</summary>
public class ExpedicaoService(
    IManifestoRepository manifestos,
    IEnvioRepository envios,
    AcompanhamentoPedidos acompanhamento,
    IUnitOfWork uow,
    TimeProvider relogio)
{
    private DateTime Agora => relogio.GetUtcNow().UtcDateTime;

    public async Task<ExpedicaoDto> ListarAsync(CancellationToken ct = default)
    {
        var lista = await manifestos.ListarRecentesAsync(coletados: 10, ct);
        var totais = await manifestos.TotaisAsync(lista.Select(m => m.Id).ToList(), ct);
        var resumos = lista
            .Select(m =>
            {
                var (volumes, peso) = totais.GetValueOrDefault(m.Id);
                return new ManifestoResumoDto(m.Id, m.Transportadora, m.Status, m.CriadoEm, m.FechadoEm, m.ColetadoEm, volumes, peso);
            })
            .ToList();
        return new ExpedicaoDto(resumos, await envios.ContarProntosSemManifestoAsync(Envio.TransportadoraPadrao, ct));
    }

    /// <summary>Abre o manifesto do dia. Cada transportadora tem um aberto por vez.</summary>
    public async Task<ManifestoDto> CriarAsync(CancellationToken ct = default)
    {
        var transportadora = Envio.TransportadoraPadrao;
        if (await manifestos.ObterAbertoAsync(transportadora, ct) is { } aberto)
            throw new DomainException($"Já existe um manifesto aberto para a {transportadora}: o #{aberto.Id}.");

        var manifesto = new Manifesto(transportadora, Agora);
        manifestos.Adicionar(manifesto);
        await uow.SalvarAsync(ct);
        return await MontarAsync(manifesto, ct);
    }

    public async Task<ManifestoDto> ObterAsync(int id, CancellationToken ct = default) =>
        await MontarAsync(await ObterManifestoAsync(id, ct), ct);

    /// <summary>O expedidor bipou a etiqueta ao colocar o pacote na doca.</summary>
    public async Task<ManifestoDto> BiparAsync(int id, BipagemRequest req, CancellationToken ct = default)
    {
        var manifesto = await ObterManifestoAsync(id, ct);
        var codigo = req.Codigo.Trim().ToUpperInvariant();
        var envio = await envios.ObterParaAlterarPorCodigoAsync(codigo, ct)
            ?? throw new DomainException($"Nenhuma etiqueta tem o código '{codigo}'.");
        envio.IncluirNoManifesto(manifesto);
        await uow.SalvarAsync(ct);
        return await MontarAsync(manifesto, ct);
    }

    public async Task<ManifestoDto> RetirarAsync(int id, Guid envioId, CancellationToken ct = default)
    {
        var manifesto = await ObterManifestoAsync(id, ct);
        var envio = await envios.ObterPorIdAsync(envioId, ct) ?? throw new NotFoundException("Envio", envioId);
        envio.RetirarDoManifesto(manifesto);
        await uow.SalvarAsync(ct);
        return await MontarAsync(manifesto, ct);
    }

    public async Task<ManifestoDto> FecharAsync(int id, CancellationToken ct = default)
    {
        var manifesto = await ObterManifestoAsync(id, ct);
        manifesto.Fechar((await envios.ListarDoManifestoAsync(id, ct)).Count, Agora);
        await uow.SalvarAsync(ct);
        return await MontarAsync(manifesto, ct);
    }

    /// <summary>A transportadora levou os pacotes: todos são expedidos e os pedidos passam a "Enviado".</summary>
    public async Task<ManifestoDto> ColetarAsync(int id, CancellationToken ct = default)
    {
        var manifesto = await ObterManifestoAsync(id, ct);
        var doManifesto = await envios.ListarDoManifestoAsync(id, ct);
        manifesto.Coletar(doManifesto, Agora);
        await acompanhamento.AtualizarAsync(doManifesto, ct);
        await uow.SalvarAsync(ct);
        return await MontarAsync(manifesto, ct);
    }

    private async Task<ManifestoDto> MontarAsync(Manifesto m, CancellationToken ct)
    {
        var pacotes = (await envios.ListarDoManifestoAsync(m.Id, ct))
            .Select(e => new PacoteManifestoDto(e.Id, e.PedidoId, e.CodigoRastreio!, e.Destino.Destinatario, e.Destino.Cidade, e.Destino.Uf,
                e.Pacote?.Caixa, e.Pacote?.PesoGramas))
            .ToList();
        var fora = await envios.ContarProntosSemManifestoAsync(m.Transportadora, ct);
        return new ManifestoDto(m.Id, m.Transportadora, m.Status, m.CriadoEm, m.FechadoEm, m.ColetadoEm, pacotes, fora);
    }

    private async Task<Manifesto> ObterManifestoAsync(int id, CancellationToken ct) =>
        await manifestos.ObterPorIdAsync(id, ct) ?? throw new NotFoundException("Manifesto", id);
}
