namespace Api.Agente.Application.Ecd;

public static class EcdSignedAmount
{
    public static decimal FromDebitCredit(decimal amount, string? indicadorDc)
    {
        var ind = indicadorDc?.Trim().ToUpperInvariant();
        return ind switch
        {
            "D" => amount,
            "C" => -amount,
            _ => amount,
        };
    }

    public static decimal NetDebitMinusCredit(decimal debits, decimal credits) => debits - credits;

    public static string? InvertIndicadorDc(string? indicadorDc) =>
        indicadorDc?.Trim().ToUpperInvariant() switch
        {
            "D" => "C",
            "C" => "D",
            _ => indicadorDc?.Trim().ToUpperInvariant(),
        };

    /// <summary>D/C invertido para partidas de encerramento (I250 sob I200 com IND_LCTO=E).</summary>
    public static decimal FromEncerramentoLancamento(decimal amount, string? indicadorDc) =>
        -FromDebitCredit(amount, indicadorDc);

    /// <summary>VL_CTA (I355) com IND_DC invertido para comparação com Σ lançamentos tipo E.</summary>
    public static decimal FromEncerramentoSaldoConta(decimal amount, string? indicadorDc) =>
        -FromDebitCredit(amount, InvertIndicadorDc(indicadorDc));
}
