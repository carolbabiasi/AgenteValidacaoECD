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
                evidence.Add(new RuleEvidence
                {
                    RecordType = line.RecordType,
                    LineNumber = line.LineNumber,
                    FieldName = line.RecordType == "0000" ? "CNPJ" : "CNPJ_EMP",
                    FoundValue = EcdFieldParser.OnlyDigits(cnpj),
                    Message = $"CNPJ inválido (dígitos verificadores) no registro {line.RecordType}.",
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
            Message = $"{evidence.Count} CNPJ(s) inválido(s).",
            Evidence = evidence,
        });
    }
}
