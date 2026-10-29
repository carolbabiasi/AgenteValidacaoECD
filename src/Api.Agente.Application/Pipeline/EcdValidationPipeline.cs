using System.Text.Json;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Entities;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;
using Api.Agente.Application.Reports;

namespace Api.Agente.Application.Pipeline;

public sealed class EcdValidationPipeline(
    IValidationExecutionRepository executions,
    IFileStorage fileStorage,
    IEcdFileReader fileReader,
    ILayoutDetector layoutDetector,
    IRuleEngine ruleEngine,
    ValidationReportBuilder reportBuilder,
    IRagContextBuilder ragContextBuilder,
    IAgentAnalysisService agentAnalysis) : IValidationPipeline
{
    public async Task RunAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var execution = await executions.GetWithFileAsync(executionId, cancellationToken);
        if (execution?.OriginalFile is null)
        {
            return;
        }

        await executions.UpdateProgressAsync(
            executionId,
            ValidationStatus.Running,
            "LayoutDetection",
            10,
            e => e.StartedAt ??= DateTimeOffset.UtcNow,
            cancellationToken);

        await using var stream = await fileStorage.OpenReadAsync(execution.OriginalFile.StoragePath, cancellationToken);
        var lines = await fileReader.ReadAllLinesAsync(stream, cancellationToken);

        var layout = layoutDetector.Detect(lines);
        var hasOpening = lines.Any(l => l.RecordType == "0000");

        await executions.UpdateProgressAsync(
            executionId,
            ValidationStatus.Running,
            "StructuralValidation",
            40,
            e =>
            {
                e.DetectedLayoutVersion = layout.IsIndeterminate ? null : layout.LayoutVersion;
                e.DetectedPeriodStart = layout.PeriodStart;
                e.DetectedPeriodEnd = layout.PeriodEnd;
                e.RecordsAnalyzedCount = lines.Count;
            },
            cancellationToken);

        var context = new EcdEvaluationContext
        {
            ExecutionId = executionId,
            LayoutVersion = layout.LayoutVersion,
            Lines = lines,
            HasOpeningBlock = hasOpening,
        };

        var structural = await ruleEngine.RunStructuralAsync(context, cancellationToken);
        var accounting = await ruleEngine.RunAccountingAsync(context, cancellationToken);
        var allResults = structural.Concat(accounting).ToList();

        await executions.UpdateProgressAsync(
            executionId,
            ValidationStatus.Running,
            "Consolidation",
            80,
            cancellationToken: cancellationToken);

        var (ruleRuns, findings) = MapResults(allResults, executionId);
        execution.RulesExecutedCount = allResults.Count;
        execution.FindingsCount = findings.Count;

        var deterministicReportJson = reportBuilder.Build(execution, allResults, findings);

        await executions.SaveRuleResultsAndFindingsAsync(executionId, ruleRuns, findings, cancellationToken);

        await executions.UpdateProgressAsync(
            executionId,
            ValidationStatus.Running,
            "RagRetrieval",
            85,
            cancellationToken: cancellationToken);

        var ragContext = await ragContextBuilder.BuildAsync(execution, findings, cancellationToken);

        await executions.UpdateProgressAsync(
            executionId,
            ValidationStatus.Running,
            "AgentAnalysis",
            92,
            cancellationToken: cancellationToken);

        var analysis = await agentAnalysis.EnrichReportAsync(
            execution,
            deterministicReportJson,
            ragContext,
            cancellationToken);

        await executions.SetReportAsync(executionId, analysis.ReportJson, cancellationToken);

        await executions.UpdateProgressAsync(
            executionId,
            ValidationStatus.Completed,
            "Completed",
            100,
            e =>
            {
                e.CompletedAt = DateTimeOffset.UtcNow;
                e.ReportJson = analysis.ReportJson;
                e.RulesExecutedCount = allResults.Count;
                e.FindingsCount = findings.Count;
                e.ManualVersionUsed = analysis.ManualVersionUsed;
                e.RagSnapshotJson = analysis.RagSnapshotJson;
                e.LlmResponseSchemaVersion = "1.0.0";
            },
            cancellationToken);
    }

    private static (List<RuleRunResult> RuleRuns, List<Finding> Findings) MapResults(
        IReadOnlyList<RuleEvaluationResult> results,
        Guid executionId)
    {
        var ruleRuns = new List<RuleRunResult>();
        var findings = new List<Finding>();

        foreach (var result in results)
        {
            var runId = Guid.NewGuid();
            ruleRuns.Add(new RuleRunResult
            {
                Id = runId,
                ExecutionId = executionId,
                RuleId = result.RuleId,
                RuleVersion = result.RuleVersion,
                Outcome = result.Outcome,
                Message = result.Message,
                EvidenceJson = JsonSerializer.Serialize(result.Evidence),
            });

            if (result.Outcome is not (RuleOutcome.Error or RuleOutcome.Alert or RuleOutcome.ProbableInconsistency))
            {
                continue;
            }

            var first = result.Evidence.FirstOrDefault();
            findings.Add(new Finding
            {
                Id = Guid.NewGuid(),
                ExecutionId = executionId,
                Category = result.Outcome == RuleOutcome.Error
                    ? FindingCategory.StructuralError
                    : FindingCategory.BusinessInconsistency,
                Severity = result.Outcome == RuleOutcome.Error ? FindingSeverity.High : FindingSeverity.Medium,
                PrimaryRecordType = first?.RecordType,
                LineNumber = first?.LineNumber,
                Summary = result.Message,
                Explanation = first?.Message,
                SourceRuleRunIdsJson = JsonSerializer.Serialize(new[] { result.RuleId }),
            });
        }

        return (ruleRuns, findings);
    }
}
