using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.OfficialPva;

public sealed class OfficialPvaCamposSaldosPeriodicosDiferenteZeroRule : IValidationRule
{
    public const string RuleId = "REGRA_CAMPOS_SALDOS_PERIODICOS_DIFERENTE_ZERO";
    public const string RuleVersion = "1.0.0";

    private const decimal Tolerance = 0.001m;

    public string ImplementationKey => nameof(OfficialPvaCamposSaldosPeriodicosDiferenteZeroRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        if (OfficialPvaEscrituracao.UsesMoedaFuncional(context.Lines))
        {
            return Task.FromResult(SkipMf());
        }

        var i157AfterI155 = BuildI155WithI157ChildLines(context.Lines);
        var index = EcdPeriodicBalanceIndex.Build(context.Lines);
        var evidence = new List<RuleEvidence>();

        foreach (var balance in index.Balances)
        {
            if (i157AfterI155.Contains(balance.LineNumber))
            {
                continue;
            }

            if (HasAnyNonZeroBalance(balance))
            {
                continue;
            }

            evidence.Add(new RuleEvidence
            {
                RecordType = "I155",
                LineNumber = balance.LineNumber,
                Message =
                    $"Na linha {balance.LineNumber}, conta {balance.Account.CodConta}: "
                    + "sem I157 filho, ao menos um entre VL_SLD_INI, VL_DEB, VL_CRED e VL_SLD_FIN deve ser ≠ 0.",
            });
        }

        return Task.FromResult(evidence.Count == 0
            ? new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message = "Saldos periódicos I155 com movimento ou saldo informado.",
            }
            : new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Alert,
                Message =
                    "Sem I157 filho, I155 deve ter ao menos um valor de saldo/movimento diferente de zero.",
                Evidence = evidence,
            });
    }

    private static bool HasAnyNonZeroBalance(EcdPeriodicBalanceLine balance) =>
        Math.Abs(balance.SaldoInicial) > Tolerance ||
        Math.Abs(balance.TotalDebitos) > Tolerance ||
        Math.Abs(balance.TotalCreditos) > Tolerance ||
        Math.Abs(balance.SaldoFinal) > Tolerance;

    private static HashSet<int> BuildI155WithI157ChildLines(IReadOnlyList<EcdLine> lines)
    {
        var withChild = new HashSet<int>();
        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].RecordType != "I155")
            {
                continue;
            }

            for (var j = i + 1; j < lines.Count; j++)
            {
                var next = lines[j].RecordType;
                if (next is "I155" or "I150" or "I200" or "I350" or "I990")
                {
                    break;
                }

                if (next == "I157")
                {
                    withChild.Add(lines[i].LineNumber);
                    break;
                }
            }
        }

        return withChild;
    }

    private static RuleEvaluationResult SkipMf() => new()
    {
        RuleId = RuleId,
        RuleVersion = RuleVersion,
        Outcome = RuleOutcome.Validated,
        Message = "IDENT_MF=S; usar REGRA_CAMPOS_SALDOS_PERIODICOS_DIFERENTE_ZERO_MF.",
    };
}
