namespace SiteDeCompras.Domain.Logistica;

/// <summary>
/// Prazos de despacho e de entrega, como no Mercado Livre: pago até o horário de corte em dia
/// útil, sai no mesmo dia; depois disso, no próximo dia útil. O transporte leva um número de dias
/// úteis que depende da região do CEP.
/// </summary>
public static class CalendarioLogistica
{
    /// <summary>Horário de Brasília. O Brasil não tem horário de verão desde 2019.</summary>
    private static readonly TimeSpan FusoBrasilia = TimeSpan.FromHours(-3);

    public static readonly TimeOnly HorarioCorte = new(12, 0);
    public static readonly TimeOnly HorarioDespacho = new(18, 0);

    public const string LocalCentroDistribuicao = "São Paulo, SP";

    /// <summary>Até quando o pacote precisa sair do centro de distribuição.</summary>
    public static DateTime PrazoDespacho(DateTime pagoEmUtc)
    {
        var local = new DateTimeOffset(pagoEmUtc, TimeSpan.Zero).ToOffset(FusoBrasilia);
        var dia = DateOnly.FromDateTime(local.DateTime);
        if (!DiaUtil(dia) || TimeOnly.FromDateTime(local.DateTime) >= HorarioCorte)
            dia = ProximoDiaUtil(dia);
        return ParaUtc(dia, HorarioDespacho);
    }

    /// <summary>Previsão de entrega: o prazo de despacho mais os dias úteis de transporte da região.</summary>
    public static DateTime PrevisaoEntrega(DateTime prazoDespachoUtc, string cep)
    {
        var dia = DateOnly.FromDateTime(new DateTimeOffset(prazoDespachoUtc, TimeSpan.Zero).ToOffset(FusoBrasilia).DateTime);
        for (var i = 0; i < DiasUteisDeTransporte(cep); i++)
            dia = ProximoDiaUtil(dia);
        return ParaUtc(dia, HorarioDespacho);
    }

    /// <summary>
    /// Pelo primeiro dígito do CEP, que no Brasil indica a região. Tabela fictícia até existirem
    /// transportadoras com tabela própria.
    /// </summary>
    public static int DiasUteisDeTransporte(string cep) => cep.FirstOrDefault() switch
    {
        '0' => 1, // Grande São Paulo
        '1' => 2, // interior de SP
        '2' => 3, // RJ e ES
        '3' => 3, // MG
        '4' => 5, // BA e SE
        '5' => 6, // PE, AL, PB, RN
        '6' => 8, // CE, PI, MA e Norte
        '7' => 5, // Centro-Oeste, TO e RO
        '8' => 3, // PR e SC
        '9' => 4, // RS
        _ => 8,
    };

    /// <summary>Início e fim, em UTC, do dia de Brasília que contém o instante.</summary>
    public static (DateTime Inicio, DateTime Fim) DiaEmUtc(DateTime instanteUtc)
    {
        var dia = DateOnly.FromDateTime(new DateTimeOffset(instanteUtc, TimeSpan.Zero).ToOffset(FusoBrasilia).DateTime);
        return (ParaUtc(dia, TimeOnly.MinValue), ParaUtc(dia.AddDays(1), TimeOnly.MinValue));
    }

    private static bool DiaUtil(DateOnly dia) => dia.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);

    private static DateOnly ProximoDiaUtil(DateOnly dia)
    {
        do dia = dia.AddDays(1);
        while (!DiaUtil(dia));
        return dia;
    }

    private static DateTime ParaUtc(DateOnly dia, TimeOnly hora) =>
        new DateTimeOffset(dia.ToDateTime(hora), FusoBrasilia).UtcDateTime;
}
