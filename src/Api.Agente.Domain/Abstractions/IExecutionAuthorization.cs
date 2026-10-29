namespace Api.Agente.Domain.Abstractions;

public interface IExecutionAuthorization
{
    Task AuthorizeAgentOperationsAsync(Guid executionId, CancellationToken cancellationToken = default);
}
