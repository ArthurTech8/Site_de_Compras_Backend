namespace SiteDeCompras.Application.Common;

public interface IUnitOfWork
{
    Task SalvarAsync(CancellationToken ct = default);
}
