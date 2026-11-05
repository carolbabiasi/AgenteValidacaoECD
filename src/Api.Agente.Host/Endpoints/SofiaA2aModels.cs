using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api.Agente.Host.Endpoints;

internal sealed class JsonRpcRequest
{
    [JsonPropertyName("jsonrpc")]
    public string? JsonRpc { get; set; }

    [JsonPropertyName("id")]
    public JsonElement? Id { get; set; }

    [JsonPropertyName("method")]
    public string? Method { get; set; }

    [JsonPropertyName("params")]
    public JsonRpcParams? Params { get; set; }
}

internal sealed class JsonRpcParams
{
    [JsonPropertyName("message")]
    public A2aMessage? Message { get; set; }
}

internal sealed class A2aMessage
{
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("messageId")]
    public string? MessageId { get; set; }

    [JsonPropertyName("parts")]
    public List<A2aPart>? Parts { get; set; }
}

internal sealed class A2aPart
{
    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

internal static class SofiaA2aJson
{
    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
