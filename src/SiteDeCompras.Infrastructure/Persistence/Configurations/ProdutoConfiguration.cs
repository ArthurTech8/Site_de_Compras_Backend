using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiteDeCompras.Domain.Produtos;

namespace SiteDeCompras.Infrastructure.Persistence.Configurations;

public class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> builder)
    {
        builder.ToTable("Produtos");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Nome).HasMaxLength(150).IsRequired();
        builder.Property(p => p.Descricao).HasMaxLength(2000);
        builder.Property(p => p.Preco).HasPrecision(18, 2);
        builder.Property(p => p.ImagemUrl).HasMaxLength(500);
        builder.HasIndex(p => p.Nome);
        builder.HasIndex(p => p.Ativo);

        builder.HasData(ProdutoSeed.Produtos);
    }
}
