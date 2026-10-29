namespace Api.Agente.Domain.Exceptions;

public sealed class ExecutionAccessDeniedException : Exception
{
    public const string Code = "FORBIDDEN";

    public ExecutionAccessDeniedException()
        : base(Code)
    {
    }
}
