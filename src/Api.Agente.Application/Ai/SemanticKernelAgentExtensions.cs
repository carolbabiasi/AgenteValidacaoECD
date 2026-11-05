using Api.Agente.Application.Ai;
using Api.Agente.Application.Chat;
using Api.Agente.Application.Rag;
using Api.Agente.Domain.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Agente.Application.Ai;

public static class SemanticKernelAgentExtensions
{
    public static IServiceCollection AddAgenteAi(this IServiceCollection services)
    {
        services.AddSingleton<IAgentResponseSchemaValidator, AgentResponseSchemaValidator>();
        services.AddScoped<IRagContextBuilder, RagContextBuilder>();
        services.AddScoped<IAgentAnalysisService, AgentAnalysisService>();
        services.AddScoped<IValidationChatService, ValidationChatService>();
        return services;
    }
}
