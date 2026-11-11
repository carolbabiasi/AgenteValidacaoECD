using Api.Agente.Domain.Ecd;

namespace Api.Agente.Application.Ecd;

internal static class EcdC155LinkagePolicy
{
    private static readonly HashSet<string> NaturezasPatrimonioResultado = new(StringComparer.Ordinal)
    {
        "01", "02", "03", "04",
    };

    private static readonly HashSet<string> NaturezasPatrimonio = new(StringComparer.Ordinal)
    {
        "01", "02", "03",
    };

    private const decimal Tolerance = 0.001m;

    public static bool SemMudancaPlanoContas(IReadOnlyList<EcdLine> lines) =>
        string.Equals(
            EcdArquivoIndex.TryGetIndMudancaPlanoContas(lines) ?? "0",
            "0",
            StringComparison.Ordinal);

    public static bool ComMudancaPlanoContas(IReadOnlyList<EcdLine> lines) =>
        string.Equals(
            EcdArquivoIndex.TryGetIndMudancaPlanoContas(lines),
            "1",
            StringComparison.Ordinal);

    public static bool IsEncerramento31Dezembro(IReadOnlyList<EcdLine> lines)
    {
        var dtEx = EcdArquivoIndex.TryGetDtExSocial(lines);
        return dtEx is { Month: 12, Day: 31 };
    }

    public static bool IsEncerramentoDiferente31Dezembro(IReadOnlyList<EcdLine> lines)
    {
        var dtEx = EcdArquivoIndex.TryGetDtExSocial(lines);
        return dtEx is not null && (dtEx.Value.Month != 12 || dtEx.Value.Day != 31);
    }

    /// <summary>
    /// C155 com VL_SLD_FIN ≠ 0 exige vínculo com I155 conforme cenários 1–4 do manual (p. 97).
    /// </summary>
    public static bool C155ExigeContaI155(
        EcdRecuperacaoBalanceLine c155,
        IReadOnlyList<EcdLine> lines,
        EcdRecuperacaoChartIndex chartC050)
    {
        var signedFin = EcdSignedAmount.FromDebitCredit(c155.SaldoFinal, c155.IndicadorSaldoFinal);
        if (Math.Abs(signedFin) <= Tolerance)
        {
            return false;
        }

        var natureza = chartC050.TryGetNatureza(c155.Account.CodConta);
        if (natureza is null || !NaturezasPatrimonioResultado.Contains(natureza))
        {
            return false;
        }

        if (IsEncerramento31Dezembro(lines))
        {
            return natureza is "04" or "01" or "02" or "03";
        }

        if (SemMudancaPlanoContas(lines) && IsEncerramentoDiferente31Dezembro(lines))
        {
            return true;
        }

        if (ComMudancaPlanoContas(lines) && IsEncerramentoDiferente31Dezembro(lines))
        {
            return true;
        }

        return false;
    }

    public static bool C155Cenario4ExigeI157(
        EcdRecuperacaoBalanceLine c155,
        IReadOnlyList<EcdLine> lines,
        EcdRecuperacaoChartIndex chartC050) =>
        ComMudancaPlanoContas(lines) &&
        IsEncerramentoDiferente31Dezembro(lines) &&
        C155ExigeContaI155(c155, lines, chartC050);

    public static bool ExisteI155EI157ParaConta(
        IReadOnlyList<EcdLine> lines,
        EcdAccountKey account)
    {
        var i155 = EcdPeriodicBalanceIndex.Build(lines).Balances
            .Select(b => b.Account)
            .ToHashSet();

        if (!i155.Contains(account))
        {
            return false;
        }

        return EcdPeriodicTransferIndex.Build(lines).Transfers
            .Any(t =>
                t.OldAccount.CodConta == account.CodConta &&
                t.OldAccount.CodCcus == account.CodCcus &&
                t.ParentAccount.CodConta == account.CodConta &&
                t.ParentAccount.CodCcus == account.CodCcus);
    }

    public static bool NaturezaPatrimonio(string? natureza) =>
        natureza is not null && NaturezasPatrimonio.Contains(natureza);
}
