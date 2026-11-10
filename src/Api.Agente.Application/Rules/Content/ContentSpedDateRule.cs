using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.Content;

public sealed class ContentSpedDateRule : IValidationRule
{
    public const string RuleId = "ECD-CONTENT-002";
    public const string RuleVersion = "1.0.0";
    public string ImplementationKey => nameof(ContentSpedDateRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var evidence = new List<RuleEvidence>();

        foreach (var line in context.Lines)
        {
            switch (line.RecordType)
            {
                case "0000":
                    ValidateField(line, 3, "DT_INI", evidence);
                    ValidateField(line, 4, "DT_FIN", evidence);
                    break;
                case "I030":
                    ValidateField(line, 2, "DT_INI_ESC", evidence);
                    break;
                case "I150":
                    ValidateField(line, 2, "DT_INI", evidence);
                    ValidateField(line, 3, "DT_FIN", evidence);
                    break;
                case "I200":
                    ValidateField(line, 2, "DT_LCTO", evidence);
                    break;
            }
        }

        if (evidence.Count == 0)
        {
            return Task.FromResult(new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message = "Datas SPED (ddMMyyyy) verificadas.",
            });
        }

        return Task.FromResult(new RuleEvaluationResult
        {
            RuleId = RuleId,
            RuleVersion = RuleVersion,
            Outcome = RuleOutcome.Error,
            Message = $"{evidence.Count} data(s) inválida(s).",
            Evidence = evidence,
        });
    }

    private static void ValidateField(EcdLine line, int index, string fieldName, List<RuleEvidence> evidence)
    {
        var value = EcdFieldParser.GetField(line, index);
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (EcdFieldParser.TryParseSpedDate(value, out _))
        {
            return;
        }

        evidence.Add(new RuleEvidence
        {
            RecordType = line.RecordType,
            LineNumber = line.LineNumber,
            FieldName = fieldName,
            FoundValue = value,
            Message = $"Data inválida (esperado ddMMyyyy) em {line.RecordType}/{fieldName}.",
        });
    }
}
