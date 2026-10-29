using Api.Agente.Domain.Abstractions;
using Api.Agente.Infrastructure.Chat;
using Api.Agente.Infrastructure.Ecd;
using Api.Agente.Infrastructure.Jobs;
using Api.Agente.Infrastructure.Mcp;
using Api.Agente.Infrastructure.Metrics;
using Api.Agente.Infrastructure.Persistence;
using Api.Agente.Infrastructure.Persistence.Metrics;
using Api.Agente.Infrastructure.Security;
using Api.Agente.Infrastructure.Storage;
using Api.Agente.Infrastructure.Validation;
using StackExchange.Redis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Api.Agente.Infrastructure.DependencyInjection;

public static class AgenteInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddAgenteInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment = null)
    {
        services.Configure<FileSystemEcdStorageOptions>(
            configuration.GetSection(FileSystemEcdStorageOptions.SectionName));
        services.Configure<ValidationSubmissionOptions>(
            configuration.GetSection(ValidationSubmissionOptions.SectionName));
        services.Configure<RagMcpOptions>(configuration.GetSection(RagMcpOptions.SectionName));

        var useInMemory = configuration.GetValue<bool>("Testing:UseInMemoryDatabase") ||
                          environment?.IsEnvironment("Testing") == true;
        var inMemoryName = configuration["Testing:InMemoryDatabaseName"] ?? "AgenteOperationalTests";

        services.AddDbContext<OperationalDbContext>(options =>
        {
            if (useInMemory)
            {
                options.UseInMemoryDatabase(inMemoryName);
                return;
            }

            var connectionString = configuration.GetConnectionString("Operational")
                ?? throw new InvalidOperationException("Connection string 'Operational' não configurada.");
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsHistoryTable("__OperationalMigrationsHistory", "operational"));
        });

        services.AddDbContext<MetricsDbContext>(options =>
        {
            if (useInMemory)
            {
                options.UseInMemoryDatabase(inMemoryName + "_metrics");
                return;
            }

            var connectionString = configuration.GetConnectionString("Metrics")
                ?? throw new InvalidOperationException("Connection string 'Metrics' não configurada.");
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__MetricsMigrationsHistory", "metrics"));
        });

        services.AddSingleton<IFileStorage, FileSystemEcdStorage>();
        services.AddScoped<IEcdFileReader, StreamingEcdFileReader>();
        services.AddScoped<IRuleCatalogRepository, RuleCatalogRepository>();
        services.AddScoped<IValidationExecutionRepository, ValidationExecutionRepository>();
        services.AddScoped<IValidationSubmissionService, ValidationSubmissionService>();

        services.AddSingleton<RagServiceJwtTokenFactory>();
        services.AddHttpClient(nameof(RagMcpClient));
        services.AddScoped<IRagMcpClient, RagMcpClient>();
        services.AddScoped<IExecutionAuthorization, ExecutionAuthorizationService>();
        services.AddScoped<ILlmUsageRecorder, LlmUsageRecorder>();

        if (useInMemory)
        {
            services.AddSingleton<IChatSessionStore, InMemoryChatSessionStore>();
        }
        else
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis") ?? "localhost:6379"));
            services.AddSingleton<IChatSessionStore, RedisChatSessionStore>();
        }

        services.AddHostedService<ValidationJobProcessor>();
        services.AddHostedService<ValidationCatalogSeeder>();

        return services;
    }
}
