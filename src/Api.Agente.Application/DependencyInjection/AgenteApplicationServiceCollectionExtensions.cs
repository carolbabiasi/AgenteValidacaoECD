using Api.Agente.Application.Ai;
using Api.Agente.Application.Ecd;
using Api.Agente.Application.Pipeline;
using Api.Agente.Application.Reports;
using Api.Agente.Application.Rules;
using Api.Agente.Application.Rules.Samples;
using Api.Agente.Domain.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Agente.Application.DependencyInjection;

public static class AgenteApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddAgenteApplication(this IServiceCollection services)
    {
        services.AddSingleton<ValidationReportBuilder>();
        services.AddSingleton<IReportExportService, ReportExportService>();
        services.AddScoped<ILayoutDetector, BasicLayoutDetector>();
        services.AddScoped<IRuleEngine, RuleEngine>();
        services.AddScoped<IValidationPipeline, EcdValidationPipeline>();
        services.AddAgenteAi();

        services.AddScoped<IValidationRule, SampleMalformedLineRule>();
        services.AddScoped<IValidationRule, SampleMissingOpeningBlockRule>();

        return services;
    }
}
