using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SiteDeCompras.Application.Common;
using SiteDeCompras.Domain.Carrinhos;
using SiteDeCompras.Domain.Enderecos;
using SiteDeCompras.Domain.Estoque;
using SiteDeCompras.Domain.Logistica;
using SiteDeCompras.Domain.Pedidos;
using SiteDeCompras.Domain.Produtos;
using SiteDeCompras.Domain.Usuarios;

namespace SiteDeCompras.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Carrinho> Carrinhos => Set<Carrinho>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Envio> Envios => Set<Envio>();
    public DbSet<EnderecoSalvo> EnderecosSalvos => Set<EnderecoSalvo>();
    public DbSet<OndaSeparacao> OndasSeparacao => Set<OndaSeparacao>();
    public DbSet<Manifesto> Manifestos => Set<Manifesto>();
    public DbSet<DivergenciaEstoque> DivergenciasEstoque => Set<DivergenciaEstoque>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // O SQLite não ordena nem compara decimal nativamente. Em um banco como PostgreSQL ou
        // SQL Server esta conversão deve sair e o tipo decimal(18,2) passa a ser usado.
        if (Database.IsSqlite())
            configurationBuilder.Properties<decimal>().HaveConversion<double>();

        // Todas as datas são gravadas em UTC; marca isso ao ler para o JSON sair com "Z".
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    private class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        v => v.ToUniversalTime(),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    public Task SalvarAsync(CancellationToken ct = default) => SaveChangesAsync(ct);
}
