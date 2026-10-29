using Api.Rag.Domain.Abstractions;
using Api.Rag.Infrastructure.Embeddings;
using Api.Rag.Infrastructure.Extractors;
using Api.Rag.Infrastructure.Jobs;
using Api.Rag.Infrastructure.Persistence;
using Api.Rag.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Api.Rag.Infrastructure.DependencyInjection;

public static class RagInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddRagInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment = null)
    {
        services.AddDbContext<RagDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("Rag")
                ?? throw new InvalidOperationException("Connection string 'Rag' não configurada.");
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__RagMigrationsHistory", "rag");
                npgsql.UseVector();
            });
        });

        services.AddScoped<IRagIngestionRepository, RagIngestionRepository>();
        services.AddSingleton<IRagManualBlobStore, FileSystemRagManualBlobStore>();

        services.AddSingleton<PlainTextManualTextExtractor>();
        services.AddSingleton<StubPdfManualTextExtractor>();
        services.AddSingleton<IManualTextExtractor>(sp =>
        {
            IEnumerable<IManualTextExtractor> extractors =
            [
                sp.GetRequiredService<PlainTextManualTextExtractor>(),
                sp.GetRequiredService<StubPdfManualTextExtractor>(),
            ];
            return new CompositeManualTextExtractor(extractors);
        });

        RegisterEmbeddingService(services, configuration, environment);

        if (environment is null || !environment.IsEnvironment("Testing"))
        {
            services.AddHostedService<IngestionJobProcessor>();
        }

        return services;
    }

    private static void RegisterEmbeddingService(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment)
    {
        var apiKey = configuration["OpenAI:ApiKey"];
        var useOpenAi = !string.IsNullOrWhiteSpace(apiKey) &&
                        environment is not null &&
                        !environment.IsEnvironment("Testing");

        if (useOpenAi)
        {
            services.AddHttpClient<IEmbeddingService, OpenAiEmbeddingService>(client =>
            {
                client.BaseAddress = new Uri(configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1/");
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            });
        }
        else
        {
            services.AddSingleton<IEmbeddingService, DeterministicEmbeddingService>();
        }
    }
}
