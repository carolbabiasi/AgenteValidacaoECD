using Api.Agente.Domain.Enums;

namespace Api.Agente.Domain.Entities;

public sealed class ValidationJob
{
    public Guid Id { get; set; }

    public Guid ExecutionId { get; set; }

    public ValidationExecution? Execution { get; set; }

    public int Attempt { get; set; }

    public DateTimeOffset? LockedUntil { get; set; }

    public ValidationJobState State { get; set; }
}
