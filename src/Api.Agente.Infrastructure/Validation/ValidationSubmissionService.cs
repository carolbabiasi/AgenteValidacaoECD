using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Entities;
using Api.Agente.Domain.Enums;
using Api.Agente.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Api.Agente.Infrastructure.Validation;

public sealed class ValidationSubmissionOptions
{
    public const string SectionName = "Validation";

    public long MaxFileBytes { get; set; } = 100 * 1024 * 1024;

    public int DuplicateWindowMinutes { get; set; } = 5;

    public bool AllowCancelRunning { get; set; } = true;
}

public sealed class DuplicateSubmissionException(string sha256) : Exception($"Submissão duplicada para hash {sha256}.");

public sealed class InvalidValidationFileException(string message) : Exception(message);

public sealed class ValidationSubmissionService(
    OperationalDbContext db,
    IFileStorage fileStorage,
    IOptions<ValidationSubmissionOptions> options) : IValidationSubmissionService
{
    public async Task<ValidationSubmissionResult> SubmitAsync(
        Stream fileContent,
        string fileName,
        string contentType,
        string userId,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        ValidateFile(fileName, fileContent, options.Value.MaxFileBytes);

        var descriptor = await fileStorage.SaveAsync(fileContent, fileName, contentType, cancellationToken);

        var windowStart = DateTimeOffset.UtcNow.AddMinutes(-options.Value.DuplicateWindowMinutes);
        var duplicate = await db.ValidationExecutions
            .AsNoTracking()
            .Include(e => e.OriginalFile)
            .AnyAsync(
                e => e.SubmittedByUserId == userId &&
                     e.OriginalFile!.Sha256 == descriptor.Sha256 &&
                     e.SubmittedAt >= windowStart &&
                     e.Status != ValidationStatus.Cancelled &&
                     e.Status != ValidationStatus.Failed,
                cancellationToken);

        if (duplicate)
        {
            throw new DuplicateSubmissionException(descriptor.Sha256);
        }

        var executionId = Guid.NewGuid();
        var storedFile = new StoredFile
        {
            Id = descriptor.FileId,
            Sha256 = descriptor.Sha256,
            OriginalFileName = fileName,
            ContentType = contentType,
            SizeBytes = descriptor.SizeBytes,
            StoragePath = descriptor.StoragePath,
            UploadedAt = DateTimeOffset.UtcNow,
        };

        var execution = new ValidationExecution
        {
            Id = executionId,
            CorrelationId = correlationId ?? executionId.ToString("N"),
            Status = ValidationStatus.Queued,
            SubmittedByUserId = userId,
            OriginalFileId = storedFile.Id,
            SubmittedAt = DateTimeOffset.UtcNow,
        };

        var job = new ValidationJob
        {
            Id = Guid.NewGuid(),
            ExecutionId = executionId,
            Attempt = 1,
            State = ValidationJobState.Pending,
        };

        db.StoredFiles.Add(storedFile);
        db.ValidationExecutions.Add(execution);
        db.ValidationJobs.Add(job);
        await db.SaveChangesAsync(cancellationToken);

        return new ValidationSubmissionResult(
            executionId,
            ValidationStatus.Queued,
            $"/api/v1/validations/{executionId}");
    }

    private static void ValidateFile(string fileName, Stream content, long maxBytes)
    {
        if (!fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidValidationFileException("INVALID_FILE: apenas arquivos .txt são aceitos no MVP.");
        }

        if (content.CanSeek && content.Length > maxBytes)
        {
            throw new InvalidValidationFileException("INVALID_FILE: arquivo excede tamanho máximo.");
        }
    }
}
