using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Api.Agente.Infrastructure.Persistence.Metrics;

public sealed class MetricsDbContextFactory : IDesignTimeDbContextFactory<MetricsDbContext>
{
    private const string LocalDevConnectionString =
        "Host=localhost;Port=5432;Database=ecd;Username=ecd;Password=ecd_dev";

    public MetricsDbContext CreateDbContext(string[] args)
    {
        var connectionString = DesignTimeConnectionString.Resolve("Metrics", LocalDevConnectionString);

        var optionsBuilder = new DbContextOptionsBuilder<MetricsDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__MetricsMigrationsHistory", "metrics"));

        return new MetricsDbContext(optionsBuilder.Options);
    }
}
