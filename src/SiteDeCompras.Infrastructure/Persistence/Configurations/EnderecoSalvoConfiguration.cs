using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiteDeCompras.Domain.Enderecos;
using SiteDeCompras.Domain.Usuarios;

namespace SiteDeCompras.Infrastructure.Persistence.Configurations;

public class EnderecoSalvoConfiguration : IEntityTypeConfiguration<EnderecoSalvo>
{
    public void Configure(EntityTypeBuilder<EnderecoSalvo> builder)
    {
        builder.ToTable("EnderecosSalvos");
        builder.HasKey(e => e.Id);
        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(e => e.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.UsuarioId);
        builder.OwnsOne(e => e.Endereco, EnderecoMapeamento.Mapear);
        builder.Navigation(e => e.Endereco).IsRequired();
    }
}
