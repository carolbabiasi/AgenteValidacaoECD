using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.OfficialPva;

public sealed class OfficialPvaContaI155InexistenteC155Rule : IValidationRule
{
    public const string RuleId = "REGRA_CONTA_I155_INEXISTENTE_C155";
    public const string RuleVersion = "1.1.0";

    public string ImplementationKey => nameof(OfficialPvaContaI155InexistenteC155Rule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var c155 = EcdRecuperacaoC155Index.Build(context.Lines);
        if (!c155.HasRecuperacao)
        {
            return Task.FromResult(OfficialPvaI157RuleHelpers.Validated(RuleId, RuleVersion, "Sem recuperação (C155)."));
        }

        if (!EcdC155LinkagePolicy.SemMudancaPlanoContas(context.Lines))
        {
            return Task.FromResult(OfficialPvaI157RuleHelpers.Validated(RuleId, RuleVersion,
                "Mudança de plano de contas — regra não se aplica."));
        }

        var evidence = new List<RuleEvidence>();
        foreach (var balance in EcdPeriodicBalanceIndex.Build(context.Lines).Balances)
        {
            if (c155.TryGet(balance.Account) is not null)
            {
                continue;
            }

            evidence.Add(new RuleEvidence
            {
                RecordType = "I155",
                LineNumber = balance.LineNumber,
                FoundValue = balance.Account.CodConta,
                Message =
                    $"Recuperação sem mudança de plano: conta {balance.Account.CodConta} "
                    + "do I155 deve constar no C155.",
            });
        }

        return Task.FromResult(OfficialPvaI157RuleHelpers.Build(evidence, RuleId, RuleVersion,
            "Contas I155 presentes no C155 recuperado.",
            "Com recuperação e sem mudança de plano, I155 deve existir no C155."));
    }
}

public sealed class OfficialPvaContaC155InexistenteI155Rule : IValidationRule
{
    public const string RuleId = "REGRA_CONTA_C155_INEXISTENTE_I155";
    public const string RuleVersion = "1.1.0";

    public string ImplementationKey => nameof(OfficialPvaContaC155InexistenteI155Rule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var c155 = EcdRecuperacaoC155Index.Build(context.Lines);
        if (!c155.HasRecuperacao)
        {
            return Task.FromResult(OfficialPvaI157RuleHelpers.Validated(RuleId, RuleVersion, "Sem bloco C155."));
        }

        var chartC050 = EcdRecuperacaoChartIndex.Build(context.Lines);
        var i155Keys = EcdPeriodicBalanceIndex.Build(context.Lines).Balances
            .Select(b => b.Account)
            .ToHashSet();
        var evidence = new List<RuleEvidence>();

        foreach (var balance in c155.Balances)
        {
            if (!EcdC155LinkagePolicy.C155ExigeContaI155(balance, context.Lines, chartC050))
            {
                continue;
            }

            if (!i155Keys.Contains(balance.Account))
            {
                evidence.Add(new RuleEvidence
                {
                    RecordType = "C155",
                    LineNumber = balance.LineNumber,
                    FoundValue = balance.Account.CodConta,
                    Message =
                        $"C155 com saldo final ≠ 0: conta {balance.Account.CodConta} "
                        + "deve existir no I155 (mesmo COD_CTA/COD_CCUS).",
                });
                continue;
            }

            if (EcdC155LinkagePolicy.C155Cenario4ExigeI157(balance, context.Lines, chartC050) &&
                !EcdC155LinkagePolicy.ExisteI155EI157ParaConta(context.Lines, balance.Account))
            {
                evidence.Add(new RuleEvidence
                {
                    RecordType = "C155",
                    LineNumber = balance.LineNumber,
                    FoundValue = balance.Account.CodConta,
                    Message =
                        $"Mudança de plano (DT_EX ≠ 31/12): conta {balance.Account.CodConta} "
                        + "deve possuir I155 e I157 relacionados.",
                });
            }
        }

        return Task.FromResult(OfficialPvaI157RuleHelpers.Build(evidence, RuleId, RuleVersion,
            "C155 vinculado ao I155 (e I157 no cenário 4).",
            "C155 com saldo nas hipóteses do manual exige I155; cenário 4 exige I157."));
    }
}

public sealed class OfficialPvaExisteI155ComI157NoC155Rule : IValidationRule
{
    public const string RuleId = "REGRA_EXISTE_I155_COM_I157_NO_C155";
    public const string RuleVersion = "1.0.0";
    private const decimal Tolerance = 0.001m;

    public string ImplementationKey => nameof(OfficialPvaExisteI155ComI157NoC155Rule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var transfers = EcdPeriodicTransferIndex.Build(context.Lines);
        if (!transfers.HasAnyTransfer)
        {
            return Task.FromResult(OfficialPvaI157RuleHelpers.Validated(RuleId, RuleVersion, "Sem I157."));
        }

        var c155 = EcdRecuperacaoC155Index.Build(context.Lines);
        var evidence = new List<RuleEvidence>();

        foreach (var transfer in transfers.Transfers)
        {
            var i157Signed = EcdSignedAmount.FromDebitCredit(transfer.SaldoInicial, transfer.IndicadorSaldoInicial);
            if (Math.Abs(i157Signed) <= Tolerance)
            {
                continue;
            }

            if (!c155.HasRecuperacao)
            {
                evidence.Add(new RuleEvidence
                {
                    RecordType = "I157",
                    LineNumber = transfer.LineNumber,
                    FoundValue = transfer.OldAccount.CodConta,
                    Message =
                        $"I157 com saldo inicial ≠ 0 ({transfer.OldAccount.CodConta}) exige C155 correspondente.",
                });
                continue;
            }

            var recuperado = c155.TryGet(transfer.OldAccount);
            if (recuperado is null)
            {
                evidence.Add(new RuleEvidence
                {
                    RecordType = "I157",
                    LineNumber = transfer.LineNumber,
                    FoundValue = transfer.OldAccount.CodConta,
                    Message =
                        $"I157 conta {transfer.OldAccount.CodConta}: falta C155 com mesmo COD_CTA/COD_CCUS.",
                });
                continue;
            }

            var c155Signed = EcdSignedAmount.FromDebitCredit(
                recuperado.SaldoFinal,
                recuperado.IndicadorSaldoFinal);
            if (Math.Abs(c155Signed) <= Tolerance)
            {
                evidence.Add(new RuleEvidence
                {
                    RecordType = "C155",
                    LineNumber = recuperado.LineNumber,
                    FoundValue = transfer.OldAccount.CodConta,
                    Message =
                        $"I157 com saldo em {transfer.OldAccount.CodConta} exige C155 com saldo final ≠ 0.",
                });
            }
        }

        return Task.FromResult(OfficialPvaI157RuleHelpers.Build(evidence, RuleId, RuleVersion,
            "I157 com saldo vinculado ao C155 recuperado.",
            "I157 com saldo inicial ≠ 0 exige C155 com saldo final ≠ 0 na mesma conta."));
    }
}

public sealed class OfficialPvaNaturezaContaC155Rule : IValidationRule
{
    public const string RuleId = "REGRA_NATUREZA_CONTA_C155";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaNaturezaContaC155Rule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var c155 = EcdRecuperacaoC155Index.Build(context.Lines);
        if (!c155.HasRecuperacao)
        {
            return Task.FromResult(OfficialPvaI157RuleHelpers.Validated(RuleId, RuleVersion, "Sem C155."));
        }

        var chartC050 = EcdRecuperacaoChartIndex.Build(context.Lines);
        var chartI050 = EcdChartOfAccountsIndex.Parse(context.Lines)
            .ToDictionary(a => a.CodConta, a => a.Natureza, StringComparer.Ordinal);
        var i155ByConta = EcdPeriodicBalanceIndex.Build(context.Lines).Balances
            .GroupBy(b => b.Account.CodConta, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        var evidence = new List<RuleEvidence>();
        foreach (var balance in c155.Balances)
        {
            if (!i155ByConta.ContainsKey(balance.Account.CodConta))
            {
                continue;
            }

            var natC050 = chartC050.TryGetNatureza(balance.Account.CodConta);
            var natI050 = chartI050.GetValueOrDefault(balance.Account.CodConta);
            if (string.IsNullOrWhiteSpace(natC050) || string.IsNullOrWhiteSpace(natI050))
            {
                continue;
            }

            if (string.Equals(natC050, natI050, StringComparison.Ordinal))
            {
                continue;
            }

            evidence.Add(new RuleEvidence
            {
                RecordType = "C155",
                LineNumber = balance.LineNumber,
                FoundValue = balance.Account.CodConta,
                Message =
                    $"Natureza C050 ({natC050}) difere da I050 ({natI050}) para conta {balance.Account.CodConta}.",
            });
        }

        return Task.FromResult(OfficialPvaI157RuleHelpers.Build(evidence, RuleId, RuleVersion,
            "Natureza C155 alinhada ao I050.",
            "C050.COD_NAT deve coincidir com I050.COD_NAT para a mesma conta no C155/I155."));
    }
}

public sealed class OfficialPvaContaI157InexistenteC155Rule : IValidationRule
{
    public const string RuleId = "REGRA_CONTA_I157_INEXISTENTE";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaContaI157InexistenteC155Rule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        if (!EcdC155LinkagePolicy.ComMudancaPlanoContas(context.Lines))
        {
            return Task.FromResult(OfficialPvaI157RuleHelpers.Validated(RuleId, RuleVersion,
                "Sem mudança de plano — regra não se aplica."));
        }

        var c155 = EcdRecuperacaoC155Index.Build(context.Lines);
        if (!c155.HasRecuperacao)
        {
            return Task.FromResult(OfficialPvaI157RuleHelpers.Validated(RuleId, RuleVersion, "Sem C155."));
        }

        var evidence = new List<RuleEvidence>();
        foreach (var transfer in EcdPeriodicTransferIndex.Build(context.Lines).Transfers)
        {
            if (c155.TryGet(transfer.OldAccount) is not null)
            {
                continue;
            }

            evidence.Add(new RuleEvidence
            {
                RecordType = "I157",
                LineNumber = transfer.LineNumber,
                FoundValue = transfer.OldAccount.CodConta,
                Message =
                    $"Mudança de plano: conta anterior {transfer.OldAccount.CodConta} "
                    + "do I157 deve existir no C155.",
            });
        }

        return Task.FromResult(OfficialPvaI157RuleHelpers.Build(evidence, RuleId, RuleVersion,
            "Contas I157 presentes no C155.",
            "Com mudança de plano e recuperação, I157 deve constar no C155."));
    }
}
