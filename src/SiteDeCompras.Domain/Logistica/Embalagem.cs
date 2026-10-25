using SiteDeCompras.Domain.Common;

namespace SiteDeCompras.Domain.Logistica;

/// <summary>Uma caixa do estoque de embalagens. Medidas internas, em cm.</summary>
public record Caixa(string Codigo, string Nome, int AlturaCm, int LarguraCm, int ComprimentoCm, int PesoGramas)
{
    public int VolumeCm3 => AlturaCm * LarguraCm * ComprimentoCm;
}

/// <summary>O que a embalagem precisa saber de cada produto: medidas, peso e quantidade.</summary>
public record VolumeItem(int AlturaCm, int LarguraCm, int ComprimentoCm, int PesoGramas, int Quantidade);

/// <summary>
/// Escolha da caixa e conferência do peso, como na bancada de embalagem da Amazon: o sistema
/// sugere a menor caixa onde tudo cabe, e a balança precisa bater com o peso esperado.
/// </summary>
public static class Embalagem
{
    /// <summary>Folga para o enchimento (papel, plástico-bolha) além do volume dos produtos.</summary>
    public const decimal FolgaVolume = 1.15m;

    /// <summary>Diferença aceita entre a balança e o peso calculado.</summary>
    public const decimal ToleranciaPeso = 0.20m;

    public const string CodigoAvulso = "AVULSO";

    public static IReadOnlyList<Caixa> Caixas { get; } =
    [
        new("P", "Caixa P", 10, 15, 20, 150),
        new("M", "Caixa M", 15, 20, 30, 250),
        new("G", "Caixa G", 25, 30, 40, 450),
        new("GG", "Caixa GG", 40, 40, 60, 800),
    ];

    /// <summary>Produto grande demais para as caixas: vai na embalagem do fabricante, só com a etiqueta.</summary>
    public static Caixa Avulso(IReadOnlyCollection<VolumeItem> itens)
    {
        // As maiores medidas de cada produto, com as unidades empilhadas pela menor dimensão.
        var medidas = itens.Select(i => Ordenar(i)).ToList();
        var maior = medidas.Max(m => m[2]);
        var meio = medidas.Max(m => m[1]);
        var empilhado = itens.Zip(medidas, (i, m) => m[0] * i.Quantidade).Sum();
        return new Caixa(CodigoAvulso, "Volume avulso (embalagem do fabricante)", Math.Min(empilhado, 999), meio, maior, 0);
    }

    public static Caixa Sugerir(IReadOnlyCollection<VolumeItem> itens)
    {
        if (itens.Count == 0)
            throw new DomainException("O envio não tem itens para embalar.");

        var volume = itens.Sum(i => (decimal)i.AlturaCm * i.LarguraCm * i.ComprimentoCm * i.Quantidade) * FolgaVolume;
        return Caixas.FirstOrDefault(c => c.VolumeCm3 >= volume && itens.All(i => Cabe(i, c))) ?? Avulso(itens);
    }

    public static Caixa Obter(string codigo, IReadOnlyCollection<VolumeItem> itens)
    {
        var normalizado = codigo.Trim().ToUpperInvariant();
        if (normalizado == CodigoAvulso)
            return Avulso(itens);

        var caixa = Caixas.FirstOrDefault(c => c.Codigo == normalizado)
            ?? throw new DomainException($"A caixa '{codigo}' não existe.");
        var volume = itens.Sum(i => (decimal)i.AlturaCm * i.LarguraCm * i.ComprimentoCm * i.Quantidade);
        if (!itens.All(i => Cabe(i, caixa)) || volume > caixa.VolumeCm3)
            throw new DomainException($"Os produtos não cabem na {caixa.Nome}.");
        return caixa;
    }

    public static int PesoEsperado(IReadOnlyCollection<VolumeItem> itens, Caixa caixa) =>
        itens.Sum(i => i.PesoGramas * i.Quantidade) + caixa.PesoGramas;

    /// <summary>Peso fora da tolerância costuma ser item trocado ou faltando na caixa.</summary>
    public static void ConferirPeso(int pesoBalancaGramas, int pesoEsperadoGramas)
    {
        if (pesoBalancaGramas <= 0)
            throw new DomainException("Informe o peso da balança.");
        var diferenca = Math.Abs(pesoBalancaGramas - pesoEsperadoGramas) / (decimal)pesoEsperadoGramas;
        if (diferenca > ToleranciaPeso)
            throw new DomainException(
                $"O peso da balança ({pesoBalancaGramas / 1000m:0.###} kg) está longe do esperado ({pesoEsperadoGramas / 1000m:0.###} kg). Confira os itens da caixa.");
    }

    private static bool Cabe(VolumeItem item, Caixa caixa)
    {
        var p = Ordenar(item);
        var c = new[] { caixa.AlturaCm, caixa.LarguraCm, caixa.ComprimentoCm }.Order().ToArray();
        return p[0] <= c[0] && p[1] <= c[1] && p[2] <= c[2];
    }

    private static int[] Ordenar(VolumeItem i) => new[] { i.AlturaCm, i.LarguraCm, i.ComprimentoCm }.Order().ToArray();
}
