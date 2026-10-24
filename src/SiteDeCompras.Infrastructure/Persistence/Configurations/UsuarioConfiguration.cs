using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiteDeCompras.Domain.Usuarios;

namespace SiteDeCompras.Infrastructure.Persistence.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();
        builder.Property(u => u.Nome).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(254).IsRequired();

        // A unicidade vale sobre o e-mail normalizado: "Ana@Loja.com" e "ana@loja.com " são a mesma conta.
        builder.Property(u => u.EmailNormalizado).HasMaxLength(254).IsRequired();
        builder.HasIndex(u => u.EmailNormalizado).IsUnique();

        // Formato do ASP.NET Core Identity v3 em Base64: versão, algoritmo, iterações, sal e hash.
        builder.Property(u => u.SenhaHash).HasMaxLength(512).IsRequired();

        // Gravado como texto para que novos papéis (ex.: Atendimento) entrem sem migrar dados.
        builder.Property(u => u.Papel).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.HasIndex(u => u.Papel);

        builder.Property(u => u.CarimboSeguranca).HasMaxLength(64).IsRequired();
        builder.Property(u => u.CarimboSeguranca).IsConcurrencyToken();
    }
}
