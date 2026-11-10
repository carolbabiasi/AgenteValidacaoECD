namespace Api.Agente.Infrastructure.Persistence;

/// <summary>
/// Resolve a connection string usada pelas factories de design time (dotnet ef).
/// Prioriza a variável de ambiente <c>ConnectionStrings__{name}</c>, como fazem os
/// scripts de deploy, e só então cai no Postgres do docker-compose de desenvolvimento.
/// </summary>
internal static class DesignTimeConnectionString
{
    public static string Resolve(string name, string localDevFallback)
    {
        var fromEnvironment = Environment.GetEnvironmentVariable($"ConnectionStrings__{name}");

        return string.IsNullOrWhiteSpace(fromEnvironment) ? localDevFallback : fromEnvironment;
    }
}
