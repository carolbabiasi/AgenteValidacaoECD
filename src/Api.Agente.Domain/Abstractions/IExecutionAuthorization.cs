namespace Api.Agente.Domain.Abstractions;

public interface IExecutionAuthorization
{
    Task AuthorizeAgentOperationsAsync(Guid executionId, CancellationToken cancellationToken = default);

    Task AuthorizeReadAsync(
        Guid executionId,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);
}
