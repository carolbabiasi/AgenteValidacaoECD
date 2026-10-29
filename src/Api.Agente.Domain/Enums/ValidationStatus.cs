namespace Api.Agente.Domain.Enums;

public enum ValidationStatus
{
    Queued = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4,
}
