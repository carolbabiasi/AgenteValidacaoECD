using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Enums;
using Api.Agente.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Agente.Infrastructure.Security;

public sealed class ExecutionAuthorizationService(OperationalDbContext db) : IExecutionAuthorization
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
}
