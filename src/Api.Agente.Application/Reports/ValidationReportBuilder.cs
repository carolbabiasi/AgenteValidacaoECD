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

    public string Build(
        ValidationExecution execution,
        IReadOnlyList<RuleEvaluationResult> ruleResults,
        IReadOnlyList<Finding> findings)
    {
        var skipped = ruleResults
            .Where(r => r.Outcome is RuleOutcome.NotVerified or RuleOutcome.NotApplicable)
            .Select(r => new { ruleId = r.RuleId, reason = r.Message })
            .ToList();

        var countsBySeverity = findings
            .GroupBy(f => f.Severity.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var payload = new
        {
            schemaVersion = "1.0.0",
            executionId = execution.Id,
            processingStatus = skipped.Any(r => ruleResults.Any(x => x.Outcome == RuleOutcome.NotVerified))
                ? "CompletedWithGaps"
                : "Completed",
            summary = new
            {
                narrative = "Relatório determinístico (MVP US1) — regras SAMPLE; não constitui parecer de conformidade.",
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
                "Análise LLM e contexto RAG não executados neste MVP.",
                "Regras SAMPLE não são normativas oficiais.",
            },
            skippedValidations = skipped,
            requiresHumanReview = findings.Any(f => f.Severity is FindingSeverity.Critical or FindingSeverity.High),
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
