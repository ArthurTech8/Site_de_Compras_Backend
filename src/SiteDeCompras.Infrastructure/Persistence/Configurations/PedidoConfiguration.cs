using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiteDeCompras.Domain.Pedidos;

namespace SiteDeCompras.Infrastructure.Persistence.Configurations;

public class PedidoConfiguration : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> builder)
    {
        builder.ToTable("Pedidos");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(p => p.CriadoEm);
        builder.HasIndex(p => p.UsuarioId);
        builder.HasOne<Domain.Usuarios.Usuario>()
            .WithMany()
            .HasForeignKey(p => p.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // A condição de pagamento fica nas colunas do próprio pedido (Pagamento_Total etc.).
        builder.OwnsOne(p => p.Pagamento, pagamento =>
        {
            pagamento.Property(c => c.Forma).HasConversion<string>().HasMaxLength(20);
            pagamento.Property(c => c.Subtotal).HasPrecision(18, 2);
            pagamento.Property(c => c.DescontoPercentual).HasPrecision(5, 2);
            pagamento.Property(c => c.Desconto).HasPrecision(18, 2);
            pagamento.Property(c => c.TaxaJurosMensal).HasPrecision(9, 6);
            pagamento.Property(c => c.Juros).HasPrecision(18, 2);
            pagamento.Property(c => c.Total).HasPrecision(18, 2);
            pagamento.Property(c => c.ValorParcela).HasPrecision(18, 2);
        });
        builder.Navigation(p => p.Pagamento).IsRequired();

        // Opcional só porque os pedidos de antes da logística não têm endereço.
        builder.OwnsOne(p => p.EnderecoEntrega, EnderecoMapeamento.Mapear);

        builder.HasMany(p => p.Itens)
            .WithOne()
            .HasForeignKey(i => i.PedidoId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Itens)
            .HasField("_itens")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class ItemPedidoConfiguration : IEntityTypeConfiguration<ItemPedido>
{
    public void Configure(EntityTypeBuilder<ItemPedido> builder)
    {
        builder.ToTable("ItensPedido");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.NomeProduto).HasMaxLength(150).IsRequired();
        builder.Property(i => i.ImagemUrl).HasMaxLength(500);
        builder.Property(i => i.PrecoUnitario).HasPrecision(18, 2);
        builder.Ignore(i => i.Subtotal);
        builder.HasOne<Domain.Produtos.Produto>()
            .WithMany()
            .HasForeignKey(i => i.ProdutoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
