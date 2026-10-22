namespace SiteDeCompras.Application.Common;

public record PagedResult<T>(IReadOnlyList<T> Itens, int Pagina, int TamanhoPagina, int TotalItens)
{
    public int TotalPaginas => (int)Math.Ceiling(TotalItens / (double)TamanhoPagina);
}
