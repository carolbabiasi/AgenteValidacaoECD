namespace Api.Rag.Domain.Abstractions;

public interface IManualIngestionPipeline
{
    Task RunAsync(Guid ingestionJobId, CancellationToken cancellationToken = default);
}
