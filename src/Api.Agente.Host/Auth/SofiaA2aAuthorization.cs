using System.Security.Claims;
namespace Api.Agente.Host.Auth;

public static class SofiaA2aAuthorization
{
    public const string ServiceKeyHeader = "X-Sofia-Service-Key";

    public static bool IsAuthorized(HttpContext context, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var configuredKey = configuration["Sofia:A2aServiceKey"];
        if (!string.IsNullOrWhiteSpace(configuredKey))
        {
            var header = context.Request.Headers[ServiceKeyHeader].ToString();
            if (string.Equals(header, configuredKey, StringComparison.Ordinal))
            {
                return true;
            }
        }

        if (context.User.Identity?.IsAuthenticated == true)
        {
            var roles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToHashSet(StringComparer.Ordinal);
            if (roles.Contains("Validator") || roles.Contains("Admin"))
            {
                return true;
            }
        }

        return environment.IsDevelopment() && string.IsNullOrWhiteSpace(configuredKey);
    }
}
