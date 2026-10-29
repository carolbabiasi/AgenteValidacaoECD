namespace Api.Agente.Domain.Abstractions;

public interface IValidationPipeline
{
    Task RunAsync(Guid executionId, CancellationToken cancellationToken = default);
}
