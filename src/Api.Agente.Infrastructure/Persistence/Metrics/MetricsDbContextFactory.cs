using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Api.Agente.Infrastructure.Persistence.Metrics;

public sealed class MetricsDbContextFactory : IDesignTimeDbContextFactory<MetricsDbContext>
{
    public MetricsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MetricsDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=ecd_rag;Username=ecd;Password=ecd_dev",
            npgsql => npgsql.MigrationsHistoryTable("__MetricsMigrationsHistory", "metrics"));

        return new MetricsDbContext(optionsBuilder.Options);
    }
}
