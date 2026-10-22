using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiteDeCompras.Domain.Carrinhos;

namespace SiteDeCompras.Infrastructure.Persistence.Configurations;

public class CarrinhoConfiguration : IEntityTypeConfiguration<Carrinho>
{
    public void Configure(EntityTypeBuilder<Carrinho> builder)
    {
        builder.ToTable("Carrinhos");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Ignore(c => c.Total);
        builder.Ignore(c => c.QuantidadeTotal);

        builder.HasMany(c => c.Itens)
            .WithOne()
            .HasForeignKey(i => i.CarrinhoId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(c => c.Itens)
            .HasField("_itens")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class ItemCarrinhoConfiguration : IEntityTypeConfiguration<ItemCarrinho>
{
    public void Configure(EntityTypeBuilder<ItemCarrinho> builder)
    {
        builder.ToTable("ItensCarrinho");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.NomeProduto).HasMaxLength(150).IsRequired();
        builder.Property(i => i.ImagemUrl).HasMaxLength(500);
        builder.Property(i => i.PrecoUnitario).HasPrecision(18, 2);
        builder.Ignore(i => i.Subtotal);
        builder.HasIndex(i => new { i.CarrinhoId, i.ProdutoId }).IsUnique();
        builder.HasOne<Domain.Produtos.Produto>()
            .WithMany()
            .HasForeignKey(i => i.ProdutoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
