namespace SiteDeCompras.Domain.Logistica;

/// <summary>A caixa fechada na bancada de embalagem, com o peso da balança.</summary>
public class Pacote
{
    public string Caixa { get; private set; } = string.Empty;
    public int PesoGramas { get; private set; }
    public int AlturaCm { get; private set; }
    public int LarguraCm { get; private set; }
    public int ComprimentoCm { get; private set; }

    private Pacote() { }

    public Pacote(string caixa, int pesoGramas, int alturaCm, int larguraCm, int comprimentoCm)
    {
        Caixa = caixa;
        PesoGramas = pesoGramas;
        AlturaCm = alturaCm;
        LarguraCm = larguraCm;
        ComprimentoCm = comprimentoCm;
    }
}
