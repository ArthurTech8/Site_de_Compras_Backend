using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiteDeCompras.Domain.Logistica;
using SiteDeCompras.Domain.Pedidos;

namespace SiteDeCompras.Infrastructure.Persistence.Configurations;

public class EnvioConfiguration : IEntityTypeConfiguration<Envio>
{
    public void Configure(EntityTypeBuilder<Envio> builder)
    {
        builder.ToTable("Envios");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.PrazoDespacho);
        builder.Property(e => e.CodigoRastreio).HasMaxLength(13);
        builder.HasIndex(e => e.CodigoRastreio).IsUnique();

        builder.HasOne<Pedido>()
            .WithMany()
            .HasForeignKey(e => e.PedidoId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => e.PedidoId);

        builder.OwnsOne(e => e.Destino, EnderecoMapeamento.Mapear);
        builder.Navigation(e => e.Destino).IsRequired();

        builder.HasMany(e => e.Eventos)
            .WithOne()
            .HasForeignKey(ev => ev.EnvioId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(e => e.Eventos)
            .HasField("_eventos")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class EventoRastreioConfiguration : IEntityTypeConfiguration<EventoRastreio>
{
    public void Configure(EntityTypeBuilder<EventoRastreio> builder)
    {
        builder.ToTable("EventosRastreio");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.Descricao).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Local).HasMaxLength(120).IsRequired();
    }
}
