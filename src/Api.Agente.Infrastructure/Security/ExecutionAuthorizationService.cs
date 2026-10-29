using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Exceptions;
using Api.Agente.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Api.Agente.Infrastructure.Security;

public sealed class ExecutionAuthorizationService(
    OperationalDbContext db,
    ILogger<ExecutionAuthorizationService> logger) : IExecutionAuthorization
{
    public async Task AuthorizeAgentOperationsAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var execution = await db.ValidationExecutions
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == executionId, cancellationToken);

        if (execution is null)
        {
            throw new KeyNotFoundException("NOT_FOUND");
        }

        if (execution.Status is ValidationStatus.Failed or ValidationStatus.Cancelled)
        {
            throw new InvalidOperationException("EXECUTION_NOT_AUTHORIZED");
        }
    }

    public async Task AuthorizeReadAsync(
        Guid executionId,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var execution = await db.ValidationExecutions
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == executionId, cancellationToken);

        if (execution is null)
        {
            throw new KeyNotFoundException("NOT_FOUND");
        }

        if (isAdmin || string.Equals(execution.SubmittedByUserId, userId, StringComparison.Ordinal))
        {
            return;
        }

        logger.LogWarning(
            "Acesso negado à execução {ExecutionId}: usuário {UserId}, proprietário {OwnerId}",
            executionId,
            userId,
            execution.SubmittedByUserId);

        throw new ExecutionAccessDeniedException();
    }
}
