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
            .RequireAuthorization(RagAuthExtensions.RagAdminPolicy)
            .WithTags("Ingestions");

        ingestions.MapPost("/", SubmitIngestionAsync)
            .WithName("SubmitIngestion")
            .WithSummary("Inicia ingestão de nova versão do Manual ECD")
            .WithDescription("multipart: file, manualVersion, layoutFamily, publicationDate, documentKind, documentCode, activateOnSuccess.");

        ingestions.MapGet("/{ingestionJobId:guid}", GetIngestionStatusAsync)
            .WithName("GetIngestionStatus")
            .WithSummary("Status do job de ingestão")
            .WithDescription("Contadores, erros e fase do pipeline de chunking/embeddings.");

        var manuals = app.MapGroup("/api/v1/manuals")
            .RequireAuthorization(RagAuthExtensions.RagReaderPolicy)
            .WithTags("Manuals");

        manuals.MapGet("/", ListManualsAsync)
            .WithName("ListManuals")
            .WithSummary("Lista versões de manual indexadas")
            .WithDescription("Filtros opcionais layoutFamily, status e documentKind.");

        manuals.MapPatch("/{manualDocumentId:guid}/status", PatchManualStatusAsync)
            .RequireAuthorization(RagAuthExtensions.RagAdminPolicy)
            .WithName("PatchManualStatus")
            .WithSummary("Ativa ou arquiva versão do manual")
            .WithDescription("Não altera regras determinísticas do Agente (FR-039).");

        manuals.MapGet("/{manualDocumentId:guid}/chunks/{chunkId:guid}", GetChunkAdminAsync)
            .WithName("GetChunkAdmin")
            .WithSummary("Consulta admin de chunk")
            .WithDescription("Metadados e conteúdo textual do chunk para auditoria.");

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
        var documentCode = form["documentCode"].FirstOrDefault();
        if (!TryParseDocumentKind(form["documentKind"].FirstOrDefault(), out var documentKind))
        {
            return InvalidDocumentKind();
        }

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
                new IngestionSubmissionRequest(
                    file.FileName,
                    file.ContentType,
                    manualVersion ?? string.Empty,
                    publicationDate,
                    layoutFamily ?? string.Empty,
                    activateOnSuccess,
                    documentKind ?? DocumentKind.Manual,
                    string.IsNullOrWhiteSpace(documentCode) ? null : documentCode),
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
        [FromQuery] string? documentKind,
        IRagRetrievalService retrieval,
        CancellationToken cancellationToken)
    {
        if (!TryParseDocumentKind(documentKind, out var kind))
        {
            return InvalidDocumentKind();
        }

        var activeOnly = string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase);
        var versions = await retrieval.ListManualVersionsAsync(layoutFamily, activeOnly, kind, cancellationToken);
        return Results.Ok(new
        {
            items = versions.Select(v => new
            {
                manualVersion = v.ManualVersion,
                publicationDate = v.PublicationDate,
                status = v.Status,
                layoutFamily = v.LayoutFamily,
                documentKind = v.DocumentKind.ToString(),
                documentCode = v.DocumentCode,
            }),
        });
    }

    internal static bool TryParseDocumentKind(string? raw, out DocumentKind? kind)
    {
        kind = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return true;
        }

        if (Enum.TryParse<DocumentKind>(raw.Trim(), ignoreCase: true, out var parsed) &&
            Enum.IsDefined(parsed))
        {
            kind = parsed;
            return true;
        }

        return false;
    }

    private static IResult InvalidDocumentKind() =>
        Results.Problem(
            title: "documentKind inválido",
            detail: "Use Manual, Faq ou Norm.",
            statusCode: StatusCodes.Status400BadRequest);

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
                manual.DocumentCode,
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
            documentKind = chunk.ManualDocument?.DocumentKind.ToString(),
            documentCode = chunk.ManualDocument?.DocumentCode,
            hierarchyPath = chunk.HierarchyPath,
            sourceLocation = chunk.SourceLocation,
            contentHash = chunk.ContentHash,
            content = chunk.Content,
        });
    }

    private sealed record ManualStatusPatchRequest(string Status);
}
