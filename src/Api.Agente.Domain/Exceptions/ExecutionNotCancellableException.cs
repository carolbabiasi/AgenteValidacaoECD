using Api.Agente.Domain.Enums;

namespace Api.Agente.Domain.Exceptions;

public sealed class ExecutionNotCancellableException(ValidationStatus status)
    : Exception($"{Code}: execução em status {status} não pode ser cancelada.")
{
    public const string Code = "NOT_CANCELLABLE";

    public ValidationStatus Status { get; } = status;
}
