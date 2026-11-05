using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Api.Agente.Infrastructure.Hosting;

internal static class PlatformHostingProbe
{
    public static bool IsVercel =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VERCEL"));

    public static bool UseEphemeralPersistence(IConfiguration configuration, IHostEnvironment? environment)
    {
        if (configuration.GetValue<bool>("Testing:UseInMemoryDatabase") ||
            environment?.IsEnvironment("Testing") == true)
        {
            return true;
        }

        if (!IsVercel)
        {
            return false;
        }

        var operational = configuration.GetConnectionString("Operational");
        var metrics = configuration.GetConnectionString("Metrics");
        return !HasRemoteConnectionString(operational) || !HasRemoteConnectionString(metrics);
    }

    private static bool HasRemoteConnectionString(string? connectionString) =>
        !string.IsNullOrWhiteSpace(connectionString) &&
        !connectionString.Contains("localhost", StringComparison.OrdinalIgnoreCase) &&
        !connectionString.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase);
}
