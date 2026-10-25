using SiteDeCompras.Domain.Logistica;

namespace SiteDeCompras.Application.Logistica;

public interface IOndaRepository
{
    Task<OndaSeparacao?> ObterPorIdAsync(int id, CancellationToken ct = default);

    /// <summary>As abertas e as últimas concluídas, das mais novas para as mais antigas.</summary>
    Task<IReadOnlyList<OndaSeparacao>> ListarRecentesAsync(int concluidas, CancellationToken ct = default);
    void Adicionar(OndaSeparacao onda);
}

public interface IManifestoRepository
{
    Task<Manifesto?> ObterPorIdAsync(int id, CancellationToken ct = default);
    Task<Manifesto?> ObterAbertoAsync(string transportadora, CancellationToken ct = default);

    /// <summary>Os abertos e fechados, mais os últimos coletados.</summary>
    Task<IReadOnlyList<Manifesto>> ListarRecentesAsync(int coletados, CancellationToken ct = default);

    /// <summary>Quantidade de pacotes e peso total de cada manifesto.</summary>
    Task<IReadOnlyDictionary<int, (int Volumes, int PesoGramas)>> TotaisAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default);
    void Adicionar(Manifesto manifesto);
}

public interface IDivergenciaRepository
{
    Task<IReadOnlyList<Domain.Estoque.DivergenciaEstoque>> ListarAbertasAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Domain.Estoque.DivergenciaEstoque>> ListarAbertasDoProdutoAsync(int produtoId, CancellationToken ct = default);
    void Adicionar(Domain.Estoque.DivergenciaEstoque divergencia);
}
