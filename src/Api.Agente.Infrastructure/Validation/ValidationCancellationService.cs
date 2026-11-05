using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Exceptions;
using Api.Agente.Infrastructure.Jobs;
using Api.Agente.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Api.Agente.Infrastructure.Validation;

public sealed class ValidationCancellationService(
    OperationalDbContext db,
    IExecutionAuthorization authorization,
    ExecutionCancellationRegistry registry,
    IOptions<ValidationSubmissionOptions> options,
    ILogger<ValidationCancellationService> logger) : IValidationCancellationService
{
    public async Task<ValidationCancellationResult> CancelAsync(
        Guid executionId,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        await authorization.AuthorizeReadAsync(executionId, userId, isAdmin, cancellationToken);

        var execution = await db.ValidationExecutions
            .Include(e => e.Jobs)
            .FirstOrDefaultAsync(e => e.Id == executionId, cancellationToken)
            ?? throw new KeyNotFoundException("NOT_FOUND");

        var previousStatus = execution.Status;
        var cancellable = previousStatus == ValidationStatus.Queued ||
                          (previousStatus == ValidationStatus.Running && options.Value.AllowCancelRunning);
        if (!cancellable)
        {
            throw new ExecutionNotCancellableException(previousStatus);
        }

        var cancelledAt = DateTimeOffset.UtcNow;
        execution.Status = ValidationStatus.Cancelled;
        execution.ErrorCode = ExecutionCancelledException.Code;
        execution.ErrorMessage = $"Cancelada por {userId}.";
        execution.CompletedAt = cancelledAt;

        foreach (var job in execution.Jobs.Where(j => j.State is ValidationJobState.Pending or ValidationJobState.Processing))
        {
            job.State = ValidationJobState.Cancelled;
            job.LockedUntil = null;
        }

        await db.SaveChangesAsync(cancellationToken);

        var signalled = registry.TryCancel(executionId);
        logger.LogInformation(
            "Execução {ExecutionId} cancelada por {UserId} (status anterior {PreviousStatus}, sinal local {Signalled})",
            executionId,
            userId,
            previousStatus,
            signalled);

        return new ValidationCancellationResult(executionId, previousStatus, ValidationStatus.Cancelled, cancelledAt);
    }
}
