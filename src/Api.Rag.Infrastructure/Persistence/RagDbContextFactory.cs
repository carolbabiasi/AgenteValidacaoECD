using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Api.Rag.Infrastructure.Persistence;

public sealed class RagDbContextFactory : IDesignTimeDbContextFactory<RagDbContext>
{
    public RagDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<RagDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=ecd_rag;Username=ecd;Password=ecd_dev",
            npgsql =>
            {
                npgsql.MigrationsHistoryTable("__RagMigrationsHistory", "rag");
                npgsql.UseVector();
            });

        return new RagDbContext(optionsBuilder.Options);
    }
}
