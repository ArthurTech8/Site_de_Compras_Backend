using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiteDeCompras.Domain.Enderecos;

namespace SiteDeCompras.Infrastructure.Persistence.Configurations;

/// <summary>Mesmas colunas e tamanhos onde quer que um endereço seja guardado.</summary>
internal static class EnderecoMapeamento
{
    public static void Mapear<T>(OwnedNavigationBuilder<T, Endereco> e) where T : class
    {
        e.Property(x => x.Destinatario).HasMaxLength(100).IsRequired();
        e.Property(x => x.Cep).HasMaxLength(8).IsRequired();
        e.Property(x => x.Logradouro).HasMaxLength(150).IsRequired();
        e.Property(x => x.Numero).HasMaxLength(20).IsRequired();
        e.Property(x => x.Complemento).HasMaxLength(100);
        e.Property(x => x.Bairro).HasMaxLength(100).IsRequired();
        e.Property(x => x.Cidade).HasMaxLength(100).IsRequired();
        e.Property(x => x.Uf).HasMaxLength(2).IsRequired();
    }
}
