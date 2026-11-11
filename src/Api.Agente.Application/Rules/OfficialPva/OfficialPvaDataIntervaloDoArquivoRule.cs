using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.OfficialPva;

/// <summary>
/// Manual — REGRA_DATA_INTERVALO_DO_ARQUIVO: datas em I150, I200 e I350 dentro de 0000.DT_INI..DT_FIN.
/// </summary>
public sealed class OfficialPvaDataIntervaloDoArquivoRule : IValidationRule
{
    public const string RuleId = "REGRA_DATA_INTERVALO_DO_ARQUIVO";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaDataIntervaloDoArquivoRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var periodo = EcdArquivoIndex.TryGetPeriodo0000(context.Lines);
        if (periodo is null)
        {
            return Task.FromResult(new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message = "Sem 0000 válido; intervalo do arquivo indisponível.",
            });
        }

        var evidence = new List<RuleEvidence>();
        CollectI150(context.Lines, periodo, evidence);
        CollectI200(context.Lines, periodo, evidence);
        CollectI350(context.Lines, periodo, evidence);

        return Task.FromResult(evidence.Count == 0
            ? new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message = "Datas de I150, I200 e I350 estão no intervalo do arquivo (0000).",
            }
            : new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Error,
                Message =
                    "Datas devem estar entre DT_INI e DT_FIN do registro 0000.",
                Evidence = evidence,
            });
    }

    private static void CollectI150(
        IReadOnlyList<EcdLine> lines,
        EcdArquivoPeriodo periodo,
        List<RuleEvidence> evidence)
    {
        foreach (var line in lines.Where(l => l.RecordType == "I150"))
        {
            CheckDate(line, 2, "DT_INI", periodo, evidence);
            CheckDate(line, 3, "DT_FIN", periodo, evidence);
        }
    }

    private static void CollectI200(
        IReadOnlyList<EcdLine> lines,
        EcdArquivoPeriodo periodo,
        List<RuleEvidence> evidence)
    {
        var journal = EcdJournalIndex.Build(lines);
        foreach (var header in journal.Headers)
        {
            if (EcdArquivoIndex.DataNoPeriodoArquivo(header.DataLancamento, periodo))
            {
                continue;
            }

            evidence.Add(new RuleEvidence
            {
                RecordType = "I200",
                LineNumber = header.LineNumber,
                FoundValue = header.DataLancamento.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture),
                Message =
                    $"Na linha {header.LineNumber}, DT_LCTO {header.DataLancamento:dd/MM/yyyy} "
                    + "está fora do intervalo do arquivo.",
            });
        }
    }

    private static void CollectI350(
        IReadOnlyList<EcdLine> lines,
        EcdArquivoPeriodo periodo,
        List<RuleEvidence> evidence)
    {
        foreach (var apuracao in EcdClosingIndex.Build(lines).Apuracoes)
        {
            if (EcdArquivoIndex.DataNoPeriodoArquivo(apuracao.DtRes, periodo))
            {
                continue;
            }

            evidence.Add(new RuleEvidence
            {
                RecordType = "I350",
                LineNumber = apuracao.LineNumber,
                FoundValue = apuracao.DtRes.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture),
                Message =
                    $"Na linha {apuracao.LineNumber}, DT_RES {apuracao.DtRes:dd/MM/yyyy} "
                    + "está fora do intervalo do arquivo.",
            });
        }
    }

    private static void CheckDate(
        EcdLine line,
        int field,
        string label,
        EcdArquivoPeriodo periodo,
        List<RuleEvidence> evidence)
    {
        if (!EcdFieldParser.TryParseSpedDate(EcdFieldParser.GetField(line, field), out var data))
        {
            return;
        }

        if (EcdArquivoIndex.DataNoPeriodoArquivo(data, periodo))
        {
            return;
        }

        evidence.Add(new RuleEvidence
        {
            RecordType = line.RecordType,
            LineNumber = line.LineNumber,
            FoundValue = data.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture),
            Message =
                $"Na linha {line.LineNumber}, {label} {data:dd/MM/yyyy} está fora do intervalo do arquivo.",
        });
    }
}
