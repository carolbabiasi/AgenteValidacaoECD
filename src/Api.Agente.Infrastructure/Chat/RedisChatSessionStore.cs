using System.Text.Json;
using Api.Agente.Domain.Abstractions;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace Api.Agente.Infrastructure.Chat;

public sealed class RedisChatSessionStore(IConnectionMultiplexer redis, IConfiguration configuration) : IChatSessionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private string KeyPrefix => configuration["Chat:RedisKeyPrefix"] ?? "ecd:chat:";

    public async Task<IReadOnlyList<ChatMessage>> GetHistoryAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var db = redis.GetDatabase();
        var values = await db.ListRangeAsync(KeyPrefix + sessionId.ToString("N"));
        var messages = values
            .Select(v => JsonSerializer.Deserialize<ChatMessage>(v.ToString(), JsonOptions))
            .Where(m => m is not null)
            .Cast<ChatMessage>()
            .ToList();

        return messages;
    }

    public async Task AppendAsync(Guid sessionId, ChatMessage message, CancellationToken cancellationToken = default)
    {
        var db = redis.GetDatabase();
        var payload = JsonSerializer.Serialize(message, JsonOptions);
        await db.ListRightPushAsync(KeyPrefix + sessionId.ToString("N"), payload);
        await db.KeyExpireAsync(KeyPrefix + sessionId.ToString("N"), TimeSpan.FromDays(7));
    }
}
