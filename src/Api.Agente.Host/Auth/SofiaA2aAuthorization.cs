using System.Security.Cryptography;
using System.Text;

namespace Api.Agente.Host.Auth;

public static class SofiaA2aAuthorization
{
    public static bool IsIntegrationConfigured(IConfiguration configuration)
    {
        return !string.IsNullOrWhiteSpace(ResolveSharedToken(configuration));
    }

    public static bool TryAuthorizeA2a(HttpContext context, IConfiguration configuration, out IResult? failure)
    {
        failure = null;

        if (!IsIntegrationConfigured(configuration))
        {
            failure = ServiceUnavailable("Integração A2A indisponível (token não configurado).");
            return false;
        }

        if (!TryExtractBearerToken(context.Request, out var presented))
        {
            failure = Unauthorized(context, "Cabeçalho Authorization Bearer obrigatório.");
            return false;
        }

        var expected = ResolveSharedToken(configuration)!;
        if (!FixedTimeEquals(presented, expected))
        {
            failure = Unauthorized(context, "Bearer inválido.");
            return false;
        }

        return true;
    }

    public static string? ResolveSharedToken(IConfiguration configuration) =>
        configuration["SOFIA_A2A_TOKEN"]
        ?? configuration["Sofia:A2aServiceKey"];

    private static bool TryExtractBearerToken(HttpRequest request, out string token)
    {
        token = string.Empty;
        if (!request.Headers.TryGetValue("Authorization", out var headerValues))
        {
            return false;
        }

        var header = headerValues.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.Ordinal))
        {
            return false;
        }

        token = header["Bearer ".Length..];
        return !string.IsNullOrEmpty(token);
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);
        return aBytes.Length == bBytes.Length && CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }

    private static IResult Unauthorized(HttpContext context, string message)
    {
        context.Response.Headers.WWWAuthenticate = "Bearer";
        return Results.Json(
            new { error = message },
            statusCode: StatusCodes.Status401Unauthorized);
    }

    private static IResult ServiceUnavailable(string message) =>
        Results.Json(
            new { error = message },
            statusCode: StatusCodes.Status503ServiceUnavailable);
}
