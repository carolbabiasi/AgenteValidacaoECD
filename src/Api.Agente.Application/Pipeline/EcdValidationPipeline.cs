using System.Diagnostics;
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
        using var run = EcdPipelineTelemetry.StartRun(executionId);
        try
        {
            await RunPhasesAsync(executionId, run, cancellationToken);
        }
        catch (Exception ex)
        {
            EcdPipelineTelemetry.RecordFailure(run, ex);
            throw;
        }
    }

    private async Task RunPhasesAsync(Guid executionId, Activity? run, CancellationToken cancellationToken)
    {
        var execution = await executions.GetWithFileAsync(executionId, cancellationToken);
        if (execution?.OriginalFile is null)
        {
            run?.SetTag("ecd.skipped", "execution_or_file_not_found");
            return;
        }

        var (lines, layout) = await RunPhaseAsync("LayoutDetection", executionId, async phase =>
        {
            await executions.UpdateProgressAsync(
                executionId,
                ValidationStatus.Running,
                "LayoutDetection",
                10,
                e => e.StartedAt ??= DateTimeOffset.UtcNow,
                cancellationToken);

            await using var stream = await fileStorage.OpenReadAsync(execution.OriginalFile.StoragePath, cancellationToken);
            var readLines = await fileReader.ReadAllLinesAsync(stream, cancellationToken);
            var detected = layoutDetector.Detect(readLines);

            phase?.SetTag("ecd.records_analyzed", readLines.Count);
            phase?.SetTag("ecd.layout_version", detected.IsIndeterminate ? "indeterminate" : detected.LayoutVersion);
            return (readLines, detected);
        });

        var allResults = await RunPhaseAsync("StructuralValidation", executionId, async phase =>
        {
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
                HasOpeningBlock = lines.Any(l => l.RecordType == "0000"),
            };

            var structural = await ruleEngine.RunStructuralAsync(context, cancellationToken);
            var accounting = await ruleEngine.RunAccountingAsync(context, cancellationToken);
            var results = structural.Concat(accounting).ToList();

            phase?.SetTag("ecd.rules_executed", results.Count);
            return results;
        });

        var (findings, deterministicReportJson) = await RunPhaseAsync("Consolidation", executionId, async phase =>
        {
            await executions.UpdateProgressAsync(
                executionId,
                ValidationStatus.Running,
                "Consolidation",
                80,
                cancellationToken: cancellationToken);

            var (ruleRuns, mappedFindings) = MapResults(allResults, executionId);
            execution.RulesExecutedCount = allResults.Count;
            execution.FindingsCount = mappedFindings.Count;

            var reportJson = reportBuilder.Build(execution, allResults, mappedFindings);
            await executions.SaveRuleResultsAndFindingsAsync(executionId, ruleRuns, mappedFindings, cancellationToken);

            phase?.SetTag("ecd.findings_count", mappedFindings.Count);
            return (mappedFindings, reportJson);
        });

        var ragContext = await RunPhaseAsync("RagRetrieval", executionId, async _ =>
        {
            await executions.UpdateProgressAsync(
                executionId,
                ValidationStatus.Running,
                "RagRetrieval",
                85,
                cancellationToken: cancellationToken);

            return await ragContextBuilder.BuildAsync(execution, findings, cancellationToken);
        });

        var analysis = await RunPhaseAsync("AgentAnalysis", executionId, async phase =>
        {
            await executions.UpdateProgressAsync(
                executionId,
                ValidationStatus.Running,
                "AgentAnalysis",
                92,
                cancellationToken: cancellationToken);

            var result = await agentAnalysis.EnrichReportAsync(
                execution,
                deterministicReportJson,
                ragContext,
                cancellationToken);

            phase?.SetTag("ecd.manual_version", result.ManualVersionUsed);
            return result;
        });

        await RunPhaseAsync("Persistence", executionId, async _ =>
        {
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

            return true;
        });

        run?.SetTag("ecd.findings_count", findings.Count);
        run?.SetTag("ecd.status", ValidationStatus.Completed.ToString());
    }

    private static async Task<T> RunPhaseAsync<T>(string phase, Guid executionId, Func<Activity?, Task<T>> body)
    {
        using var activity = EcdPipelineTelemetry.StartPhase(phase, executionId);
        try
        {
            return await body(activity);
        }
        catch (Exception ex)
        {
            EcdPipelineTelemetry.RecordFailure(activity, ex);
            throw;
        }
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
