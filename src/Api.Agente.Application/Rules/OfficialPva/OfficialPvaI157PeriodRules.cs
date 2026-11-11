using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.OfficialPva;

public sealed class OfficialPvaValidaSldIniSomaSldIniI157Rule : IValidationRule
{
    public const string RuleId = "REGRA_VALIDA_SLD_INI_SOMA_SLD_INI_I157";
    public const string RuleVersion = "1.0.0";
    private const decimal Tolerance = 0.001m;

    public string ImplementationKey => nameof(OfficialPvaValidaSldIniSomaSldIniI157Rule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        if (OfficialPvaEscrituracao.UsesMoedaFuncional(context.Lines))
        {
            return Task.FromResult(OfficialPvaI157RuleHelpers.SkipMf(RuleId, RuleVersion));
        }

        var balances = EcdPeriodicBalanceIndex.Build(context.Lines);
        var transfers = EcdPeriodicTransferIndex.Build(context.Lines);
        var byParent = transfers.Transfers.GroupBy(t => t.ParentI155LineNumber);
        var evidence = new List<RuleEvidence>();

        foreach (var group in byParent)
        {
            var parent = balances.Balances.FirstOrDefault(b => b.LineNumber == group.Key);
            if (parent is null)
            {
                continue;
            }

            var expected = EcdSignedAmount.FromDebitCredit(parent.SaldoInicial, parent.IndicadorSaldoInicial);
            var actual = group.Sum(t =>
                EcdSignedAmount.FromDebitCredit(t.SaldoInicial, t.IndicadorSaldoInicial));

            if (Math.Abs(expected - actual) <= Tolerance)
            {
                continue;
            }

            evidence.Add(new RuleEvidence
            {
                RecordType = "I155",
                LineNumber = parent.LineNumber,
                ExpectedValue = expected.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                FoundValue = actual.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                Message =
                    $"I155 linha {parent.LineNumber}: VL_SLD_INI deve igualar a soma dos VL_SLD_INI dos I157 filhos.",
            });
        }

        return Task.FromResult(OfficialPvaI157RuleHelpers.Build(evidence, RuleId, RuleVersion,
            "VL_SLD_INI do I155 confere com a soma dos I157 filhos.",
            "VL_SLD_INI (I155) deve igualar a soma dos VL_SLD_INI (I157)."));
    }
}

public sealed class OfficialPvaContinuidadeSaldosPeriodicosRule : IValidationRule
{
    public const string RuleId = "REGRA_CONTINUIDADE_SALDOS_PERIODICOS";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaContinuidadeSaldosPeriodicosRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var periodo = EcdArquivoIndex.TryGetPeriodo0000(context.Lines);
        var index = EcdPeriodicBalanceIndex.Build(context.Lines);
        if (periodo is null || index.Periods.Count == 0)
        {
            return Task.FromResult(OfficialPvaI157RuleHelpers.Validated(RuleId, RuleVersion, "Sem I150 para avaliar continuidade."));
        }

        var evidence = new List<RuleEvidence>();
        foreach (var (year, month) in EcdArquivoIndex.EnumerateMesesNoArquivo(periodo))
        {
            var firstDay = new DateOnly(year, month, 1);
            var lastDay = firstDay.AddMonths(1).AddDays(-1);
            var period = index.Periods.FirstOrDefault(p => p.DtIni <= lastDay && p.DtFin >= firstDay);
            if (period is null)
            {
                evidence.Add(new RuleEvidence
                {
                    RecordType = "0000",
                    LineNumber = context.Lines.First(l => l.RecordType == "0000").LineNumber,
                    Message = $"Mês {month:00}/{year}: não há período I150 cobrindo o mês do arquivo.",
                });
                continue;
            }

            if (!index.GetBalancesForPeriod(period).Any())
            {
                evidence.Add(new RuleEvidence
                {
                    RecordType = "I150",
                    LineNumber = period.HeaderLineNumber,
                    Message =
                        $"Mês {month:00}/{year}: período I150 linha {period.HeaderLineNumber} sem registros I155.",
                });
            }
        }

        return Task.FromResult(OfficialPvaI157RuleHelpers.Build(evidence, RuleId, RuleVersion,
            "Todos os meses do arquivo possuem saldos periódicos I155.",
            "Cada mês do intervalo 0000 deve ter I150/I155 correspondente."));
    }
}

public sealed class OfficialPvaValidaMesI157Rule : IValidationRule
{
    public const string RuleId = "REGRA_VALIDA_MES_I157";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaValidaMesI157Rule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var periodo = EcdArquivoIndex.TryGetPeriodo0000(context.Lines);
        var transfers = EcdPeriodicTransferIndex.Build(context.Lines);
        if (periodo is null || !transfers.HasAnyTransfer)
        {
            return Task.FromResult(OfficialPvaI157RuleHelpers.Validated(RuleId, RuleVersion, "Sem I157 para validar mês."));
        }

        var evidence = new List<RuleEvidence>();
        foreach (var period in transfers.Transfers
                     .GroupBy(t => t.Period.HeaderLineNumber)
                     .Select(g => g.First().Period))
        {
            if (EcdArquivoIndex.MesmoMesAno(period.DtIni, periodo.DtIni))
            {
                continue;
            }

            evidence.Add(new RuleEvidence
            {
                RecordType = "I150",
                LineNumber = period.HeaderLineNumber,
                FoundValue = period.DtIni.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture),
                Message =
                    $"Com I157 no arquivo, DT_INI do I150 deve ter o mesmo mês/ano que DT_INI do 0000.",
            });
        }

        return Task.FromResult(OfficialPvaI157RuleHelpers.Build(evidence, RuleId, RuleVersion,
            "Mês do I150 (com I157) coincide com mês inicial do arquivo.",
            "Com I157, o mês de DT_INI (I150) deve coincidir com DT_INI (0000)."));
    }
}

public sealed class OfficialPvaExisteI157PeriodoPosteriorRule : IValidationRule
{
    public const string RuleId = "REGRA_EXISTE_I157_PERIODO_POSTERIOR";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaExisteI157PeriodoPosteriorRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var periodo = EcdArquivoIndex.TryGetPeriodo0000(context.Lines);
        var transfers = EcdPeriodicTransferIndex.Build(context.Lines);
        if (periodo is null || !transfers.HasAnyTransfer)
        {
            return Task.FromResult(OfficialPvaI157RuleHelpers.Validated(RuleId, RuleVersion, "Sem I157 para validar período."));
        }

        var firstPeriod = transfers.Transfers.MinBy(t => t.Period.HeaderLineNumber)!.Period;
        if (firstPeriod.DtIni == periodo.DtIni)
        {
            return Task.FromResult(OfficialPvaI157RuleHelpers.Validated(RuleId, RuleVersion,
                "Primeiro I150 com I157 inicia na DT_INI do arquivo."));
        }

        return Task.FromResult(new RuleEvaluationResult
        {
            RuleId = RuleId,
            RuleVersion = RuleVersion,
            Outcome = RuleOutcome.Error,
            Message =
                "Com I157 informado, o primeiro I150 deve ter DT_INI igual à DT_INI do registro 0000.",
            Evidence =
            [
                new RuleEvidence
                {
                    RecordType = "I150",
                    LineNumber = firstPeriod.HeaderLineNumber,
                    ExpectedValue = periodo.DtIni.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture),
                    FoundValue = firstPeriod.DtIni.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture),
                    Message = "DT_INI do primeiro período com I157 diverge do 0000.",
                },
            ],
        });
    }
}

public sealed class OfficialPvaNaturezaContaPaiI157Rule : IValidationRule
{
    public const string RuleId = "REGRA_NATUREZA_CONTA_PAI_I157";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaNaturezaContaPaiI157Rule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var transfers = EcdPeriodicTransferIndex.Build(context.Lines);
        var c155 = EcdRecuperacaoC155Index.Build(context.Lines);
        if (!transfers.HasAnyTransfer)
        {
            return Task.FromResult(OfficialPvaI157RuleHelpers.Validated(RuleId, RuleVersion, "Sem I157 para validar natureza."));
        }

        var chart = EcdChartOfAccountsIndex.Parse(context.Lines).ToDictionary(a => a.CodConta, StringComparer.Ordinal);
        var evidence = new List<RuleEvidence>();

        foreach (var transfer in transfers.Transfers)
        {
            if (!chart.TryGetValue(transfer.ParentAccount.CodConta, out var parentAccount))
            {
                continue;
            }

            var c155Line = c155.TryGet(transfer.OldAccount);
            if (c155Line is null)
            {
                evidence.Add(new RuleEvidence
                {
                    RecordType = "I157",
                    LineNumber = transfer.LineNumber,
                    FoundValue = transfer.OldAccount.CodConta,
                    Message =
                        $"Conta {transfer.OldAccount.CodConta} (I157) sem registro C155 correspondente.",
                });
                continue;
            }

            if (!chart.TryGetValue(transfer.OldAccount.CodConta, out var oldAccount))
            {
                continue;
            }

            if (SameNaturezaGrupo(parentAccount.Natureza, oldAccount.Natureza))
            {
                continue;
            }

            evidence.Add(new RuleEvidence
            {
                RecordType = "I157",
                LineNumber = transfer.LineNumber,
                Message =
                    $"Natureza da conta I157 ({oldAccount.Natureza}) incompatível com I155/C155 ({parentAccount.Natureza}).",
            });
        }

        return Task.FromResult(OfficialPvaI157RuleHelpers.Build(evidence, RuleId, RuleVersion,
            "Naturezas I157/C155 compatíveis com conta I155.",
            "I157/C155 devem ter natureza compatível com a conta I155 (01–03 ou 04)."));
    }

    private static bool SameNaturezaGrupo(string? parent, string? child)
    {
        static bool IsResultado(string? n) => n == "04";
        return IsResultado(parent) == IsResultado(child);
    }
}

public sealed class OfficialPvaDuplicidadePeriodoSaldoPeriodicoRule : IValidationRule
{
    public const string RuleId = "REGRA_DUPLICIDADE_PERIODO_SALDO_PERIODICO";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaDuplicidadePeriodoSaldoPeriodicoRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var index = EcdPeriodicBalanceIndex.Build(context.Lines);
        var byKey = new Dictionary<(DateOnly Ini, DateOnly Fin), List<EcdPeriodicPeriod>>();

        foreach (var period in index.Periods)
        {
            var key = (period.DtIni, period.DtFin);
            if (!byKey.TryGetValue(key, out var list))
            {
                list = [];
                byKey[key] = list;
            }

            list.Add(period);
        }

        var evidence = new List<RuleEvidence>();
        foreach (var (_, periods) in byKey.Where(kv => kv.Value.Count > 1))
        {
            foreach (var period in periods)
            {
                evidence.Add(new RuleEvidence
                {
                    RecordType = "I150",
                    LineNumber = period.HeaderLineNumber,
                    FoundValue = $"{period.DtIni:dd/MM/yyyy}-{period.DtFin:dd/MM/yyyy}",
                    Message = "Período I150 duplicado (DT_INI + DT_FIN).",
                });
            }
        }

        return Task.FromResult(OfficialPvaI157RuleHelpers.Build(evidence, RuleId, RuleVersion,
            "Períodos I150 únicos.",
            "Chave DT_INI + DT_FIN do I150 deve ser única."));
    }
}

public sealed class OfficialPvaDataMesI150Rule : IValidationRule
{
    public const string RuleId = "REGRA_DATA_MES";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaDataMesI150Rule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var index = EcdPeriodicBalanceIndex.Build(context.Lines);
        var evidence = new List<RuleEvidence>();

        foreach (var period in index.Periods)
        {
            if (EcdArquivoIndex.MesmoMesAno(period.DtIni, period.DtFin))
            {
                continue;
            }

            evidence.Add(new RuleEvidence
            {
                RecordType = "I150",
                LineNumber = period.HeaderLineNumber,
                Message =
                    $"DT_INI e DT_FIN do período devem estar no mesmo mês (linha {period.HeaderLineNumber}).",
            });
        }

        return Task.FromResult(OfficialPvaI157RuleHelpers.Build(evidence, RuleId, RuleVersion,
            "DT_INI e DT_FIN de cada I150 no mesmo mês.",
            "DT_INI e DT_FIN (I150) devem pertencer ao mesmo mês."));
    }
}

internal static class OfficialPvaI157RuleHelpers
{
    public static RuleEvaluationResult Validated(string ruleId, string version, string message) => new()
    {
        RuleId = ruleId,
        RuleVersion = version,
        Outcome = RuleOutcome.Validated,
        Message = message,
    };

    public static RuleEvaluationResult SkipMf(string ruleId, string version) => new()
    {
        RuleId = ruleId,
        RuleVersion = version,
        Outcome = RuleOutcome.Validated,
        Message = "IDENT_MF=S; usar variante MF.",
    };

    public static RuleEvaluationResult Build(
        List<RuleEvidence> evidence,
        string ruleId,
        string version,
        string okMessage,
        string errorMessage) =>
        evidence.Count == 0
            ? Validated(ruleId, version, okMessage)
            : new RuleEvaluationResult
            {
                RuleId = ruleId,
                RuleVersion = version,
                Outcome = RuleOutcome.Error,
                Message = errorMessage,
                Evidence = evidence,
            };
}
