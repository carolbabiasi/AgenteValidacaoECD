using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Api.Agente.Infrastructure.Persistence;

public sealed class OperationalDbContextFactory : IDesignTimeDbContextFactory<OperationalDbContext>
{
    private const string LocalDevConnectionString =
        "Host=localhost;Port=5432;Database=ecd;Username=ecd;Password=ecd_dev";

    public OperationalDbContext CreateDbContext(string[] args)
    {
        var connectionString = DesignTimeConnectionString.Resolve("Operational", LocalDevConnectionString);

        var optionsBuilder = new DbContextOptionsBuilder<OperationalDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__OperationalMigrationsHistory", "operational"));

        return new OperationalDbContext(optionsBuilder.Options);
    }
}
