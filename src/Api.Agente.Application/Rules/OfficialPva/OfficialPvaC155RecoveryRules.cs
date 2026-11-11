using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.OfficialPva;

public sealed class OfficialPvaContaI155InexistenteC155Rule : IValidationRule
{
    public const string RuleId = "REGRA_CONTA_I155_INEXISTENTE_C155";
    public const string RuleVersion = "1.0.0";
    private const decimal Tolerance = 0.001m;

    public string ImplementationKey => nameof(OfficialPvaContaI155InexistenteC155Rule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var c155 = EcdRecuperacaoC155Index.Build(context.Lines);
        if (!c155.HasRecuperacao)
        {
            return Task.FromResult(OfficialPvaI157RuleHelpers.Validated(RuleId, RuleVersion, "Sem C155."));
        }

        var i155Keys = EcdPeriodicBalanceIndex.Build(context.Lines).Balances
            .Select(b => b.Account)
            .ToHashSet();
        var evidence = new List<RuleEvidence>();

        foreach (var balance in c155.Balances)
        {
            var signedFin = EcdSignedAmount.FromDebitCredit(balance.SaldoFinal, balance.IndicadorSaldoFinal);
            if (Math.Abs(signedFin) <= Tolerance)
            {
                continue;
            }

            if (i155Keys.Contains(balance.Account))
            {
                continue;
            }

            evidence.Add(new RuleEvidence
            {
                RecordType = "C155",
                LineNumber = balance.LineNumber,
                FoundValue = balance.Account.CodConta,
                Message =
                    $"C155 com saldo final ≠ 0: conta {balance.Account.CodConta} "
                    + "deve existir no I155 (mesmo COD_CTA/COD_CCUS).",
            });
        }

        return Task.FromResult(OfficialPvaI157RuleHelpers.Build(evidence, RuleId, RuleVersion,
            "Contas C155 com saldo encontradas no I155.",
            "Conta/centro de custos do C155 com saldo deve existir no I155."));
    }
}

public sealed class OfficialPvaContaC155InexistenteI155Rule : IValidationRule
{
    public const string RuleId = "REGRA_CONTA_C155_INEXISTENTE_I155";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaContaC155InexistenteI155Rule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var c155 = EcdRecuperacaoC155Index.Build(context.Lines);
        if (!c155.HasRecuperacao)
        {
            return Task.FromResult(OfficialPvaI157RuleHelpers.Validated(RuleId, RuleVersion, "Sem bloco C155."));
        }

        var c155Keys = c155.Balances.Select(b => b.Account).ToHashSet();
        var evidence = new List<RuleEvidence>();

        foreach (var balance in EcdPeriodicBalanceIndex.Build(context.Lines).Balances)
        {
            if (c155Keys.Contains(balance.Account))
            {
                continue;
            }

            evidence.Add(new RuleEvidence
            {
                RecordType = "I155",
                LineNumber = balance.LineNumber,
                FoundValue = balance.Account.CodConta,
                Message =
                    $"Com recuperação (C155), conta {balance.Account.CodConta} do I155 "
                    + "deve constar no C155.",
            });
        }

        return Task.FromResult(OfficialPvaI157RuleHelpers.Build(evidence, RuleId, RuleVersion,
            "Contas I155 presentes no C155 recuperado.",
            "Com C155, cada conta do I155 deve constar no detalhe recuperado."));
    }
}
