using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.Content;

public sealed class ContentInvalidCnpjRule : IValidationRule
{
    public const string RuleId = "ECD-CONTENT-001";
    public const string RuleVersion = "1.0.0";
    public string ImplementationKey => nameof(ContentInvalidCnpjRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var evidence = new List<RuleEvidence>();

        foreach (var line in context.Lines)
        {
            string? cnpj = line.RecordType switch
            {
                "0000" => EcdFieldParser.GetField(line, 6),
                "I030" => EcdFieldParser.GetField(line, 7),
                _ => null,
            };

            if (string.IsNullOrWhiteSpace(cnpj))
            {
                continue;
            }

            if (!BrazilianCnpj.IsValid(cnpj))
            {
                var digits = EcdFieldParser.OnlyDigits(cnpj);
                var label = line.RecordType == "0000" ? "abertura (0000)" : "identificação (I030)";
                evidence.Add(new RuleEvidence
                {
                    RecordType = line.RecordType,
                    LineNumber = line.LineNumber,
                    FieldName = line.RecordType == "0000" ? "CNPJ" : "CNPJ_EMP",
                    FoundValue = digits,
                    Message =
                        $"Na linha {line.LineNumber}, {label}: o CNPJ {EcdDisplayText.FormatCnpj(digits)} é inválido (dígitos verificadores).",
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
                Message = "CNPJs verificados sem inconsistência.",
            });
        }

        return Task.FromResult(new RuleEvaluationResult
        {
            RuleId = RuleId,
            RuleVersion = RuleVersion,
            Outcome = RuleOutcome.Error,
            Message = evidence.Count == 1
                ? "CNPJ inválido no cadastro da escrituração."
                : $"{evidence.Count} CNPJs inválidos no cadastro da escrituração.",
            Evidence = evidence,
        });
    }
}
