using System.Security.Claims;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Exceptions;
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
            .RequireAuthorization(AuthExtensions.ValidatorPolicy)
            .WithTags("Validations");

        group.MapGet("/", ListValidationsAsync)
            .WithName("ListValidations")
            .WithSummary("Histórico paginado de validações")
            .WithDescription("Query: page, pageSize (máx. 100), from, to, status. Validator: próprias execuções; Admin: todas.");

        group.MapPost("/", SubmitValidationAsync)
            .WithName("SubmitValidation")
            .WithSummary("Upload ECD e enfileira validação")
            .WithDescription("multipart/form-data: file (obrigatório), correlationId (opcional). Resposta 202 com executionId.");

        group.MapGet("/{executionId:guid}", GetValidationStatusAsync)
            .WithName("GetValidationStatus")
            .WithSummary("Status e resumo da execução")
            .WithDescription("Inclui progress, summary, manualVersionUsed e metadados operational (não-LLM).");

        group.MapDelete("/{executionId:guid}", CancelValidationAsync)
            .WithName("CancelValidation")
            .WithSummary("Cancela execução Queued/Running")
            .WithDescription("Running só é cancelável com Validation:AllowCancelRunning=true. 409 NOT_CANCELLABLE para status finais.");

        group.MapGet("/{executionId:guid}/report", GetValidationReportAsync)
            .WithName("GetValidationReport")
            .WithSummary("Relatório JSON completo")
            .WithDescription("Envelope conforme agent-response-schema (findings, limitations, skippedValidations).");

        group.MapGet("/{executionId:guid}/report/export", ExportValidationReportAsync)
            .WithName("ExportValidationReport")
            .WithSummary("Exportação tabular dos achados")
            .WithDescription("Query format=csv|xlsx. Mesmos achados do relatório JSON.");

        group.MapPost("/{executionId:guid}/chat", PostValidationChatAsync)
            .WithName("PostValidationChat")
            .WithSummary("Chat sobre resultados da validação")
            .WithDescription("JSON: sessionId (opcional), message. Memória de sessão em Redis.");

        return group;
    }

    private static async Task<IResult> ListValidationsAsync(
        HttpContext httpContext,
        IValidationExecutionRepository executions,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] ValidationStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var (userId, isAdmin) = GetUserContext(httpContext.User);
        var result = await executions.ListHistoryAsync(
            new ValidationHistoryQuery(userId, isAdmin, page, pageSize, from, to, status),
            cancellationToken);

        return Results.Ok(new
        {
            page = result.Page,
            pageSize = result.PageSize,
            totalCount = result.TotalCount,
            items = result.Items.Select(i => new
            {
                executionId = i.ExecutionId,
                submittedAt = i.SubmittedAt,
                fileName = i.FileName,
                status = i.Status.ToString(),
                findingsCount = i.FindingsCount,
                detectedLayoutVersion = i.DetectedLayoutVersion,
            }),
        });
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

        var userId = GetUserId(httpContext.User);

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
        HttpContext httpContext,
        IExecutionAuthorization authorization,
        OperationalDbContext db,
        CancellationToken cancellationToken)
    {
        var (userId, isAdmin) = GetUserContext(httpContext.User);

        try
        {
            await authorization.AuthorizeReadAsync(executionId, userId, isAdmin, cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ExecutionAccessDeniedException)
        {
            return Forbidden();
        }

        var execution = await db.ValidationExecutions
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == executionId, cancellationToken);

        if (execution is null)
        {
            return NotFound();
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

    private static async Task<IResult> CancelValidationAsync(
        Guid executionId,
        HttpContext httpContext,
        IValidationCancellationService cancellation,
        CancellationToken cancellationToken)
    {
        var (userId, isAdmin) = GetUserContext(httpContext.User);

        try
        {
            var result = await cancellation.CancelAsync(executionId, userId, isAdmin, cancellationToken);
            return Results.Ok(new
            {
                executionId = result.ExecutionId,
                previousStatus = result.PreviousStatus.ToString(),
                status = result.Status.ToString(),
                cancelledAt = result.CancelledAt,
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ExecutionAccessDeniedException)
        {
            return Forbidden();
        }
        catch (ExecutionNotCancellableException ex)
        {
            return Results.Problem(
                title: "Execução não cancelável",
                detail: ex.Message,
                statusCode: StatusCodes.Status409Conflict);
        }
    }

    private static async Task<IResult> GetValidationReportAsync(
        Guid executionId,
        HttpContext httpContext,
        IExecutionAuthorization authorization,
        OperationalDbContext db,
        CancellationToken cancellationToken)
    {
        var (userId, isAdmin) = GetUserContext(httpContext.User);

        try
        {
            await authorization.AuthorizeReadAsync(executionId, userId, isAdmin, cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ExecutionAccessDeniedException)
        {
            return Forbidden();
        }

        var execution = await db.ValidationExecutions
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == executionId, cancellationToken);

        if (execution is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(execution.ReportJson))
        {
            return Results.Conflict(new { message = "Relatório ainda não disponível.", status = execution.Status.ToString() });
        }

        return Results.Content(execution.ReportJson, "application/json");
    }

    private static async Task<IResult> ExportValidationReportAsync(
        Guid executionId,
        HttpContext httpContext,
        IExecutionAuthorization authorization,
        IReportExportService exportService,
        OperationalDbContext db,
        [FromQuery] string format = "csv",
        CancellationToken cancellationToken = default)
    {
        var (userId, isAdmin) = GetUserContext(httpContext.User);

        try
        {
            await authorization.AuthorizeReadAsync(executionId, userId, isAdmin, cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ExecutionAccessDeniedException)
        {
            return Forbidden();
        }

        var execution = await db.ValidationExecutions
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == executionId, cancellationToken);

        if (execution is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(execution.ReportJson))
        {
            return Results.Conflict(new { message = "Relatório ainda não disponível.", status = execution.Status.ToString() });
        }

        try
        {
            var exported = exportService.Export(execution.ReportJson, format);
            var downloadName = $"ecd-report-{executionId:N}.{format.Trim().ToLowerInvariant()}";
            return Results.File(exported.Content, exported.ContentType, downloadName);
        }
        catch (ArgumentException ex)
        {
            return Results.Problem(
                title: "Formato inválido",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> PostValidationChatAsync(
        Guid executionId,
        HttpContext httpContext,
        IValidationChatService chat,
        CancellationToken cancellationToken)
    {
        var request = await httpContext.Request.ReadFromJsonAsync<ValidationChatRequest>(cancellationToken);
        if (request is null || string.IsNullOrWhiteSpace(request.Message))
        {
            return Results.Problem(
                title: "Requisição inválida",
                detail: "message obrigatória",
                statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            var response = await chat.AskAsync(executionId, request, cancellationToken);
            return Results.Ok(new
            {
                sessionId = response.SessionId,
                answer = new { text = response.Text, citations = response.Citations },
                disclaimer = response.Disclaimer,
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(title: "Chat indisponível", detail: ex.Message, statusCode: 409);
        }
    }

    private static string GetUserId(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? user.FindFirstValue("sub")
        ?? "anonymous";

    private static (string UserId, bool IsAdmin) GetUserContext(ClaimsPrincipal user) =>
        (GetUserId(user), user.IsInRole("Admin"));

    private static IResult NotFound() =>
        Results.Problem(title: "Não encontrado", detail: "NOT_FOUND", statusCode: StatusCodes.Status404NotFound);

    private static IResult Forbidden() =>
        Results.Problem(
            title: "Acesso negado",
            detail: ExecutionAccessDeniedException.Code,
            statusCode: StatusCodes.Status403Forbidden);
}
