using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.Content;

public sealed class ContentSpedNumericAmountRule : IValidationRule
{
    public const string RuleId = "ECD-CONTENT-010";
    public const string RuleVersion = "1.0.0";
    public string ImplementationKey => nameof(ContentSpedNumericAmountRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var evidence = new List<RuleEvidence>();

        foreach (var line in context.Lines)
        {
            if (line.RecordType != "I250")
            {
                continue;
            }

            var indicator = EcdFieldParser.GetField(line, 5)?.Trim().ToUpperInvariant();
            if (indicator is not ("D" or "C"))
            {
                continue;
            }

            var amountText = EcdFieldParser.GetField(line, 4);
            if (string.IsNullOrWhiteSpace(amountText))
            {
                continue;
            }

            if (!EcdFieldParser.TryParseSpedDecimal(amountText, out _))
            {
                evidence.Add(new RuleEvidence
                {
                    RecordType = "I250",
                    LineNumber = line.LineNumber,
                    FieldName = "VL_DEB_CRE",
                    FoundValue = amountText,
                    Message = "Valor monetário I250 inválido (esperado formato 0,00).",
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
                Message = "Valores monetários I250 válidos.",
            });
        }

        return Task.FromResult(new RuleEvaluationResult
        {
            RuleId = RuleId,
            RuleVersion = RuleVersion,
            Outcome = RuleOutcome.Error,
            Message = $"{evidence.Count} valor(es) I250 não numérico(s).",
            Evidence = evidence,
        });
    }
}
