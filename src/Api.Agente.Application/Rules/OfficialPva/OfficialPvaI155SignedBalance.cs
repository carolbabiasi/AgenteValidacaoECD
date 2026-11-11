using Api.Agente.Application.Ecd;

namespace Api.Agente.Application.Rules.OfficialPva;

internal static class OfficialPvaI155SignedBalance
{
    public static decimal ComputeExpectedFinal(
        decimal saldoInicial,
        string? indIni,
        decimal debitos,
        decimal creditos) =>
        EcdSignedAmount.FromDebitCredit(saldoInicial, indIni) + debitos - creditos;

    public static bool MatchesDeclared(
        decimal expectedSigned,
        decimal saldoFinal,
        string? indFin,
        decimal tolerance = 0.001m)
    {
        var declaredSigned = EcdSignedAmount.FromDebitCredit(saldoFinal, indFin);
        return Math.Abs(expectedSigned - declaredSigned) <= tolerance;
    }
}
