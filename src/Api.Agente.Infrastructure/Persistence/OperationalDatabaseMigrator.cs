using Api.Agente.Infrastructure.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Api.Agente.Infrastructure.Persistence;

/// <summary>
/// Aplica migrações pendentes do schema operational (ex.: Neon em produção/Vercel).
/// </summary>
public sealed class OperationalDatabaseMigrator(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<OperationalDatabaseMigrator> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (PlatformHostingProbe.UseEphemeralPersistence(configuration, environment))
        {
            return;
        }

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<OperationalDbContext>();
            await db.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Migrações operational aplicadas.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao aplicar migrações operational.");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
