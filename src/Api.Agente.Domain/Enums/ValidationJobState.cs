namespace Api.Agente.Domain.Enums;

public enum ValidationJobState
{
    Pending = 0,
    Processing = 1,
    Done = 2,
    DeadLetter = 3,
}
