namespace SiteDeCompras.Domain.Estoque;

/// <summary>
/// O separador foi à prateleira e o produto não estava lá. Fica aberta até alguém contar o
/// produto no inventário; enquanto isso, os envios com esse produto não entram em novas ondas.
/// </summary>
public class DivergenciaEstoque
{
    public int Id { get; private set; }
    public int ProdutoId { get; private set; }
    public int? OndaId { get; private set; }

    /// <summary>Unidades que o separador não achou.</summary>
    public int Quantidade { get; private set; }
    public DateTime CriadaEm { get; private set; }
    public DateTime? ResolvidaEm { get; private set; }

    public bool Aberta => ResolvidaEm is null;

    private DivergenciaEstoque() { }

    public DivergenciaEstoque(int produtoId, int? ondaId, int quantidade, DateTime agoraUtc)
    {
        ProdutoId = produtoId;
        OndaId = ondaId;
        Quantidade = quantidade;
        CriadaEm = agoraUtc;
    }

    public void Resolver(DateTime agoraUtc) => ResolvidaEm ??= agoraUtc;
}
