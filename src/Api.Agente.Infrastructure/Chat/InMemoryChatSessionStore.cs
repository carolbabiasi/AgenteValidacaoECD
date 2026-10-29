using System.Collections.Concurrent;
using Api.Agente.Domain.Abstractions;

namespace Api.Agente.Infrastructure.Chat;

public sealed class InMemoryChatSessionStore : IChatSessionStore
{
    private readonly ConcurrentDictionary<Guid, List<ChatMessage>> _sessions = new();

    public Task<IReadOnlyList<ChatMessage>> GetHistoryAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        _sessions.TryGetValue(sessionId, out var list);
        IReadOnlyList<ChatMessage> history = list ?? [];
        return Task.FromResult(history);
    }

    public Task AppendAsync(Guid sessionId, ChatMessage message, CancellationToken cancellationToken = default)
    {
        var list = _sessions.GetOrAdd(sessionId, _ => []);
        lock (list)
        {
            list.Add(message);
        }

        return Task.CompletedTask;
    }
}
