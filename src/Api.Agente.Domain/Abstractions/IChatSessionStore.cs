namespace Api.Agente.Domain.Abstractions;

public sealed record ChatMessage(string Role, string Content, DateTimeOffset At);

public interface IChatSessionStore
{
    Task<IReadOnlyList<ChatMessage>> GetHistoryAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task AppendAsync(Guid sessionId, ChatMessage message, CancellationToken cancellationToken = default);
}
