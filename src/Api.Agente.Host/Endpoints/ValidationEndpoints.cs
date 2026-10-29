using System.Security.Claims;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Enums;
using Api.Agente.Host.Auth;
using Api.Agente.Infrastructure.Persistence;
using Api.Agente.Infrastructure.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Agente.Host.Endpoints;

public static class ValidationEndpoints
{
    public static RouteGroupBuilder MapValidationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/validations")
            .RequireAuthorization(AuthExtensions.ValidatorPolicy);

        group.MapPost("/", SubmitValidationAsync);
        group.MapGet("/{executionId:guid}", GetValidationStatusAsync);
        group.MapGet("/{executionId:guid}/report", GetValidationReportAsync);

        return group;
    }

    private static async Task<IResult> SubmitValidationAsync(
        HttpContext httpContext,
        IValidationSubmissionService submission,
        CancellationToken cancellationToken)
    {
        var form = await httpContext.Request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file");
        var correlationId = form["correlationId"].FirstOrDefault();

        if (file is null || file.Length == 0)
        {
            return Results.Problem(
                title: "Arquivo inválido",
                detail: "INVALID_FILE: envie multipart com part 'file'.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.User.FindFirstValue("sub")
            ?? "anonymous";

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await submission.SubmitAsync(
                stream,
                file.FileName,
                file.ContentType,
                userId,
                correlationId,
                cancellationToken);

            return Results.Accepted(result.StatusUrl, new
            {
                executionId = result.ExecutionId,
                status = result.Status.ToString(),
                statusUrl = result.StatusUrl,
            });
        }
        catch (InvalidValidationFileException ex)
        {
            return Results.Problem(
                title: "Arquivo inválido",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
        catch (DuplicateSubmissionException)
        {
            return Results.Problem(
                title: "Submissão duplicada",
                detail: "DUPLICATE_SUBMISSION",
                statusCode: StatusCodes.Status409Conflict);
        }
    }

    private static async Task<IResult> GetValidationStatusAsync(
        Guid executionId,
        OperationalDbContext db,
        CancellationToken cancellationToken)
    {
        var execution = await db.ValidationExecutions
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == executionId, cancellationToken);

        if (execution is null)
        {
            return Results.Problem(
                title: "Não encontrado",
                detail: "NOT_FOUND",
                statusCode: StatusCodes.Status404NotFound);
        }

        var pendingMandatory = await db.RuleRunResults
            .AsNoTracking()
            .CountAsync(
                r => r.ExecutionId == executionId && r.Outcome == RuleOutcome.NotVerified,
                cancellationToken);

        var durationMs = execution.CompletedAt.HasValue && execution.StartedAt.HasValue
            ? (int?)(execution.CompletedAt.Value - execution.StartedAt.Value).TotalMilliseconds
            : null;

        return Results.Ok(new
        {
            executionId = execution.Id,
            status = execution.Status.ToString(),
            submittedAt = execution.SubmittedAt,
            detectedLayoutVersion = execution.DetectedLayoutVersion,
            detectedPeriod = execution.DetectedPeriodStart is null
                ? null
                : new
                {
                    start = execution.DetectedPeriodStart,
                    end = execution.DetectedPeriodEnd,
                },
            progress = execution.ProgressPhase is null
                ? null
                : new { phase = execution.ProgressPhase, percent = execution.ProgressPercent },
            summary = new
            {
                recordsAnalyzed = execution.RecordsAnalyzedCount,
                rulesExecuted = execution.RulesExecutedCount,
                findingsCount = execution.FindingsCount,
                pendingMandatoryRules = pendingMandatory,
            },
            manualVersionUsed = execution.ManualVersionUsed,
            operational = new
            {
                correlationId = execution.CorrelationId,
                durationMs,
                llmUsageSummary = (object?)null,
            },
        });
    }

    private static async Task<IResult> GetValidationReportAsync(
        Guid executionId,
        OperationalDbContext db,
        CancellationToken cancellationToken)
    {
        var execution = await db.ValidationExecutions
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == executionId, cancellationToken);

        if (execution is null)
        {
            return Results.Problem(
                title: "Não encontrado",
                detail: "NOT_FOUND",
                statusCode: StatusCodes.Status404NotFound);
        }

        if (string.IsNullOrWhiteSpace(execution.ReportJson))
        {
            return Results.Conflict(new { message = "Relatório ainda não disponível.", status = execution.Status.ToString() });
        }

        return Results.Content(execution.ReportJson, "application/json");
    }
}
