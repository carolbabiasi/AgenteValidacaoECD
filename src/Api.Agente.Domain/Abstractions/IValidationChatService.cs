namespace Api.Agente.Domain.Abstractions;

public sealed record ValidationChatRequest(Guid? SessionId, string Message);

public sealed record ValidationChatResponse(
    Guid SessionId,
    string Text,
    IReadOnlyList<object> Citations,
    string Disclaimer);

public interface IValidationChatService
{
    Task<ValidationChatResponse> AskAsync(
        Guid executionId,
        ValidationChatRequest request,
        CancellationToken cancellationToken = default);
}
