namespace Api.Agente.Domain.Exceptions;

public sealed class ExecutionCancelledException(Guid executionId)
    : OperationCanceledException($"Execução {executionId} cancelada.")
{
    public const string Code = "EXECUTION_CANCELLED";

    public Guid ExecutionId { get; } = executionId;
}
