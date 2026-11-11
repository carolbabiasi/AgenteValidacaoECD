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
}
