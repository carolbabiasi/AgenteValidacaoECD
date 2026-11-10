using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.Content;

public sealed class ContentDuplicateI200Rule : IValidationRule
{
    public const string RuleId = "ECD-CONTENT-012";
    public const string RuleVersion = "1.0.0";
    public string ImplementationKey => nameof(ContentDuplicateI200Rule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var evidence = new List<RuleEvidence>();
        var seen = new Dictionary<string, List<EcdLine>>(StringComparer.Ordinal);

        foreach (var line in context.Lines)
        {
            if (line.RecordType != "I200")
            {
                continue;
            }

            var date = EcdFieldParser.GetField(line, 2)?.Trim() ?? string.Empty;
            var numero = EcdFieldParser.GetField(line, 3)?.Trim() ?? string.Empty;
            var key = $"{date}|{numero}";

            if (!seen.TryGetValue(key, out var list))
            {
                list = [];
                seen[key] = list;
            }

            list.Add(line);
        }

        foreach (var (_, lines) in seen.Where(kv => kv.Value.Count > 1))
        {
            foreach (var line in lines)
            {
                var date = EcdFieldParser.GetField(line, 2)?.Trim() ?? string.Empty;
                var numero = EcdFieldParser.GetField(line, 3)?.Trim() ?? string.Empty;
                var historico = EcdFieldParser.GetField(line, 4)?.Trim();

                var dataFmt = EcdDisplayText.FormatSpedDate(date);
                var historicoPart = string.IsNullOrWhiteSpace(historico)
                    ? string.Empty
                    : $" — {historico}";

                evidence.Add(new RuleEvidence
                {
                    RecordType = "I200",
                    LineNumber = line.LineNumber,
                    FoundValue = $"{date}|{numero}",
                    Message = $"Lançamento duplicado na linha {line.LineNumber}: data {dataFmt}, número {numero}{historicoPart}.",
                });
            }
        }

        if (evidence.Count == 0)
        {
            return Task.FromResult(new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message = "Sem lançamentos I200 duplicados.",
            });
        }

        return Task.FromResult(new RuleEvaluationResult
        {
            RuleId = RuleId,
            RuleVersion = RuleVersion,
            Outcome = RuleOutcome.Error,
            Message = evidence.Count == 2
                ? "Dois lançamentos I200 com a mesma data e número."
                : $"{evidence.Count} lançamentos I200 repetidos (mesma data e número).",
            Evidence = evidence,
        });
    }

}
