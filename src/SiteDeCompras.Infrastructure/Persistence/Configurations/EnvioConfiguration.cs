using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiteDeCompras.Domain.Estoque;
using SiteDeCompras.Domain.Logistica;
using SiteDeCompras.Domain.Pedidos;
using SiteDeCompras.Domain.Produtos;

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

        builder.HasMany(e => e.Itens)
            .WithOne()
            .HasForeignKey(i => i.EnvioId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(e => e.Itens)
            .HasField("_itens")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsOne(e => e.Pacote, p =>
        {
            p.Property(x => x.Caixa).HasColumnName("PacoteCaixa").HasMaxLength(10);
            p.Property(x => x.PesoGramas).HasColumnName("PacotePesoGramas");
            p.Property(x => x.AlturaCm).HasColumnName("PacoteAlturaCm");
            p.Property(x => x.LarguraCm).HasColumnName("PacoteLarguraCm");
            p.Property(x => x.ComprimentoCm).HasColumnName("PacoteComprimentoCm");
        });
        builder.Property(e => e.Transportadora).HasMaxLength(60);

        builder.HasOne<OndaSeparacao>()
            .WithMany()
            .HasForeignKey(e => e.OndaId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<Manifesto>()
            .WithMany()
            .HasForeignKey(e => e.ManifestoId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class ItemEnvioConfiguration : IEntityTypeConfiguration<ItemEnvio>
{
    public void Configure(EntityTypeBuilder<ItemEnvio> builder)
    {
        builder.ToTable("ItensEnvio");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.NomeProduto).HasMaxLength(150).IsRequired();
        builder.HasIndex(i => i.ProdutoId);
        builder.Ignore(i => i.Separado);
        builder.Ignore(i => i.Conferido);
    }
}

public class OndaSeparacaoConfiguration : IEntityTypeConfiguration<OndaSeparacao>
{
    public void Configure(EntityTypeBuilder<OndaSeparacao> builder)
    {
        builder.ToTable("OndasSeparacao");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(o => o.Status);
    }
}

public class ManifestoConfiguration : IEntityTypeConfiguration<Manifesto>
{
    public void Configure(EntityTypeBuilder<Manifesto> builder)
    {
        builder.ToTable("Manifestos");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Transportadora).HasMaxLength(60).IsRequired();
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(m => m.Status);
    }
}

public class DivergenciaEstoqueConfiguration : IEntityTypeConfiguration<DivergenciaEstoque>
{
    public void Configure(EntityTypeBuilder<DivergenciaEstoque> builder)
    {
        builder.ToTable("DivergenciasEstoque");
        builder.HasKey(d => d.Id);
        builder.Ignore(d => d.Aberta);
        builder.HasIndex(d => new { d.ProdutoId, d.ResolvidaEm });
        builder.HasOne<Produto>().WithMany().HasForeignKey(d => d.ProdutoId).OnDelete(DeleteBehavior.Restrict);
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
