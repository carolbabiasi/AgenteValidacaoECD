using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Api.Rag.Infrastructure.Persistence;

public sealed class RagDbContextFactory : IDesignTimeDbContextFactory<RagDbContext>
{
    private const string LocalDevConnectionString =
        "Host=localhost;Port=5432;Database=ecd;Username=ecd;Password=ecd_dev";

    public RagDbContext CreateDbContext(string[] args)
    {
        // Os scripts de deploy exportam ConnectionStrings__Rag; sem ela, usa o
        // Postgres do docker-compose de desenvolvimento.
        var fromEnvironment = Environment.GetEnvironmentVariable("ConnectionStrings__Rag");
        var connectionString = string.IsNullOrWhiteSpace(fromEnvironment)
            ? LocalDevConnectionString
            : fromEnvironment;

        var optionsBuilder = new DbContextOptionsBuilder<RagDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql =>
            {
                npgsql.MigrationsHistoryTable("__RagMigrationsHistory", "rag");
                npgsql.UseVector();
            });

        return new RagDbContext(optionsBuilder.Options);
    }
}
