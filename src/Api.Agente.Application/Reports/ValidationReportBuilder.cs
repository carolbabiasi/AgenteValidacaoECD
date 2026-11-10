using System.Text.Json;
using System.Text.Json.Serialization;
using Api.Agente.Domain.Entities;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;

namespace Api.Agente.Application.Reports;

public sealed class ValidationReportBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    private static readonly object[] PendingNormativeCatalog =
    [
        new { ruleId = "ECD-V1-MATRIZ", reason = "Matriz de leiautes V1 e catálogo normativo RFB ainda não implementado." },
    ];

    public string Build(
        ValidationExecution execution,
        IReadOnlyList<RuleEvaluationResult> ruleResults,
        IReadOnlyList<Finding> findings)
    {
        var skippedFromRules = ruleResults
            .Where(r => r.Outcome is RuleOutcome.NotVerified or RuleOutcome.NotApplicable)
            .Select(r => new { ruleId = r.RuleId, reason = r.Message })
            .ToList();

        var skipped = skippedFromRules.Concat(PendingNormativeCatalog).ToList();

        var countsBySeverity = findings
            .GroupBy(f => f.Severity.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var hasNormativeGaps = skipped.Count > 0;
        var requiresHumanReview = findings.Any(f => f.Severity is FindingSeverity.Critical or FindingSeverity.High)
            || hasNormativeGaps;

        var payload = new
        {
            schemaVersion = "1.0.0",
            executionId = execution.Id,
            processingStatus = hasNormativeGaps ? "CompletedWithGaps" : "Completed",
            summary = new
            {
                narrative =
                    "Relatório determinístico — regras ativas no catálogo; não constitui parecer de conformidade plena.",
                countsBySeverity,
            },
            findings = findings.Select(f => new
            {
                findingId = f.Id.ToString("N"),
                category = MapCategory(f.Category),
                severity = f.Severity.ToString(),
                title = f.Summary,
                description = f.Explanation,
                recordRefs = f.LineNumber is null && f.PrimaryRecordType is null
                    ? Array.Empty<object>()
                    : new[]
                    {
                        new
                        {
                            recordType = f.PrimaryRecordType,
                            lineNumber = f.LineNumber,
                        },
                    },
                ruleId = ExtractRuleId(f.SourceRuleRunIdsJson),
            }),
            limitations = new[]
            {
                "Regras SAMPLE (ECD-SAMPLE-*) não são normativas oficiais.",
                "Validação de conteúdo parcial (ECD-CONTENT-001..012); demais verificações da matriz V1 pendentes.",
            },
            skippedValidations = skipped,
            requiresHumanReview = requiresHumanReview,
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static string MapCategory(FindingCategory category) => category switch
    {
        FindingCategory.StructuralError => "StructuralError",
        FindingCategory.BusinessInconsistency => "BusinessInconsistency",
        FindingCategory.AiIndication => "AiIndication",
        FindingCategory.MissingEvidence => "MissingEvidence",
        _ => "TechnicalFailure",
    };

    private static string? ExtractRuleId(string sourceRuleRunIdsJson)
    {
        try
        {
            var ids = JsonSerializer.Deserialize<string[]>(sourceRuleRunIdsJson);
            return ids?.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }
}
