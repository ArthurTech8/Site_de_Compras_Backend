using SiteDeCompras.Domain.Common;

namespace SiteDeCompras.Domain.Logistica;

/// <summary>
/// Um produto do pacote, com o que já foi tirado da prateleira (separação) e o que o embalador
/// já conferiu na bancada.
/// </summary>
public class ItemEnvio
{
    public int Id { get; private set; }
    public Guid EnvioId { get; private set; }
    public int ProdutoId { get; private set; }
    public string NomeProduto { get; private set; } = string.Empty;
    public int Quantidade { get; private set; }
    public int QuantidadeSeparada { get; private set; }
    public int QuantidadeConferida { get; private set; }

    public bool Separado => QuantidadeSeparada == Quantidade;
    public bool Conferido => QuantidadeConferida == Quantidade;

    private ItemEnvio() { }

    internal ItemEnvio(Guid envioId, int produtoId, string nomeProduto, int quantidade)
    {
        EnvioId = envioId;
        ProdutoId = produtoId;
        NomeProduto = nomeProduto;
        Quantidade = quantidade;
    }

    internal void Separar()
    {
        if (Separado)
            throw new DomainException($"Todas as unidades de '{NomeProduto}' já foram separadas.");
        QuantidadeSeparada++;
    }

    internal void Conferir()
    {
        if (Conferido)
            throw new DomainException($"Todas as unidades de '{NomeProduto}' já foram conferidas.");
        QuantidadeConferida++;
    }

    /// <summary>As unidades voltam para a prateleira quando a separação é interrompida.</summary>
    internal void DevolverAPrateleira() => QuantidadeSeparada = 0;
}
