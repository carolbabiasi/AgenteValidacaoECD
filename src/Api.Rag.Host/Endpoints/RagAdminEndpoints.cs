using Api.Rag.Domain.Abstractions;
using Api.Rag.Domain.Enums;
using Api.Rag.Host.Auth;
using Api.Rag.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Rag.Host.Endpoints;

public static class RagAdminEndpoints
{
    public static RouteGroupBuilder MapRagAdminEndpoints(this WebApplication app)
    {
        var ingestions = app.MapGroup("/api/v1/ingestions")
            .RequireAuthorization(RagAuthExtensions.RagAdminPolicy);

        ingestions.MapPost("/", SubmitIngestionAsync);
        ingestions.MapGet("/{ingestionJobId:guid}", GetIngestionStatusAsync);

        var manuals = app.MapGroup("/api/v1/manuals")
            .RequireAuthorization(RagAuthExtensions.RagReaderPolicy);

        manuals.MapGet("/", ListManualsAsync);
        manuals.MapPatch("/{manualDocumentId:guid}/status", PatchManualStatusAsync)
            .RequireAuthorization(RagAuthExtensions.RagAdminPolicy);
        manuals.MapGet("/{manualDocumentId:guid}/chunks/{chunkId:guid}", GetChunkAdminAsync);

        return ingestions;
    }

    private static async Task<IResult> SubmitIngestionAsync(
        HttpContext httpContext,
        IIngestionSubmissionService submission,
        CancellationToken cancellationToken)
    {
        var form = await httpContext.Request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file");
        if (file is null || file.Length == 0)
        {
            return Results.Problem(
                title: "Arquivo inválido",
                detail: "INVALID_FILE: envie multipart com part 'file'.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var manualVersion = form["manualVersion"].FirstOrDefault();
        var layoutFamily = form["layoutFamily"].FirstOrDefault();
        var publicationDateRaw = form["publicationDate"].FirstOrDefault();
        var activateOnSuccess = bool.TryParse(form["activateOnSuccess"].FirstOrDefault(), out var activate) && activate;

        DateOnly? publicationDate = null;
        if (!string.IsNullOrWhiteSpace(publicationDateRaw) &&
            DateOnly.TryParse(publicationDateRaw, out var parsedDate))
        {
            publicationDate = parsedDate;
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await submission.SubmitAsync(
                stream,
                file.FileName,
                file.ContentType,
                manualVersion ?? string.Empty,
                publicationDate,
                layoutFamily ?? string.Empty,
                activateOnSuccess,
                cancellationToken);

            return Results.Accepted(result.StatusUrl, new
            {
                ingestionJobId = result.IngestionJobId,
                statusUrl = result.StatusUrl,
            });
        }
        catch (ArgumentException ex)
        {
            return Results.Problem(title: "Requisição inválida", detail: ex.Message, statusCode: 400);
        }
        catch (NotSupportedException ex)
        {
            return Results.Problem(title: "Formato não suportado", detail: ex.Message, statusCode: 415);
        }
    }

    private static async Task<IResult> GetIngestionStatusAsync(
        Guid ingestionJobId,
        RagDbContext db,
        CancellationToken cancellationToken)
    {
        var job = await db.IngestionJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == ingestionJobId, cancellationToken);

        if (job is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(new
        {
            ingestionJobId = job.Id,
            manualDocumentId = job.ManualDocumentId,
            status = job.Status.ToString(),
            chunksCreated = job.ChunksCreated,
            chunksSkipped = job.ChunksSkipped,
            errorLog = job.ErrorLog,
            startedAt = job.StartedAt,
            completedAt = job.CompletedAt,
        });
    }

    private static async Task<IResult> ListManualsAsync(
        [FromQuery] string? layoutFamily,
        [FromQuery] string? status,
        IRagRetrievalService retrieval,
        CancellationToken cancellationToken)
    {
        var activeOnly = string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase);
        var versions = await retrieval.ListManualVersionsAsync(layoutFamily, activeOnly, cancellationToken);
        return Results.Ok(new { items = versions });
    }

    private static async Task<IResult> PatchManualStatusAsync(
        Guid manualDocumentId,
        ManualStatusPatchRequest request,
        IRagIngestionRepository repository,
        CancellationToken cancellationToken)
    {
        var manual = await repository.GetManualAsync(manualDocumentId, cancellationToken);
        if (manual is null)
        {
            return Results.NotFound();
        }

        if (!Enum.TryParse<ManualDocumentStatus>(request.Status, true, out var newStatus))
        {
            return Results.Problem(
                title: "Status inválido",
                detail: "Use Draft, Active ou Archived.",
                statusCode: 400);
        }

        if (newStatus == ManualDocumentStatus.Active)
        {
            await repository.ArchiveActiveManualsExceptAsync(
                manual.LayoutFamily,
                manual.Id,
                cancellationToken);
        }

        manual.Status = newStatus;
        await repository.SaveChangesAsync(cancellationToken);

        return Results.Ok(new
        {
            manualDocumentId = manual.Id,
            status = manual.Status.ToString(),
        });
    }

    private static async Task<IResult> GetChunkAdminAsync(
        Guid manualDocumentId,
        Guid chunkId,
        IRagIngestionRepository repository,
        CancellationToken cancellationToken)
    {
        var chunk = await repository.GetChunkWithManualAsync(chunkId, cancellationToken);
        if (chunk is null || chunk.ManualDocumentId != manualDocumentId)
        {
            return Results.NotFound();
        }

        return Results.Ok(new
        {
            chunkId = chunk.Id,
            manualDocumentId = chunk.ManualDocumentId,
            manualVersion = chunk.ManualDocument?.ManualVersion,
            hierarchyPath = chunk.HierarchyPath,
            sourceLocation = chunk.SourceLocation,
            contentHash = chunk.ContentHash,
            content = chunk.Content,
        });
    }

    private sealed record ManualStatusPatchRequest(string Status);
}
