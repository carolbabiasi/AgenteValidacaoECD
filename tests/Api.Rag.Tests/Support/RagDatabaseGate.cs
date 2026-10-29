using Api.Rag.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Rag.Tests.Support;

public static class RagDatabaseGate
{
    public static async Task<bool> TryMigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<RagDbContext>();
            await db.Database.MigrateAsync(cancellationToken);
            return await db.Database.CanConnectAsync(cancellationToken);
        }
        catch
        {
            return false;
        }
    }
}
