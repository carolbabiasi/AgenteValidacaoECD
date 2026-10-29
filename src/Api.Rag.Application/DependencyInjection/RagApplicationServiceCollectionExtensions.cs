using Api.Rag.Application.Ingestion;
using Api.Rag.Application.Retrieval;
using Api.Rag.Domain.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Rag.Application.DependencyInjection;

public static class RagApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddRagApplication(this IServiceCollection services)
    {
        services.AddScoped<IManualIngestionPipeline, ManualIngestionPipeline>();
        services.AddScoped<IIngestionSubmissionService, IngestionSubmissionService>();
        services.AddScoped<IRagRetrievalService, HybridManualSearch>();
        return services;
    }
}
