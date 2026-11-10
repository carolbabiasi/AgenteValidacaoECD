using Api.Agente.Application.Ai;
using Api.Agente.Application.Ecd;
using Api.Agente.Application.Pipeline;
using Api.Agente.Application.Reports;
using Api.Agente.Application.Rules;
using Api.Agente.Application.Rules.Content;
using Api.Agente.Application.Rules.Samples;
using Api.Agente.Application.Sofia;
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
        services.AddScoped<IValidationRule, ContentInvalidCnpjRule>();
        services.AddScoped<IValidationRule, ContentSpedDateRule>();
        services.AddScoped<IValidationRule, ContentI010RequiredRule>();
        services.AddScoped<IValidationRule, ContentBalancedI200Rule>();
        services.AddScoped<IValidationRule, ContentI250AccountExistsRule>();
        services.AddScoped<IValidationRule, ContentDuplicateAccountRule>();
        services.AddScoped<IValidationRule, ContentAccountNatureRule>();
        services.AddScoped<IValidationRule, ContentAccountParentExistsRule>();
        services.AddScoped<IValidationRule, ContentBlockIRecordOrderRule>();
        services.AddScoped<IValidationRule, ContentSpedNumericAmountRule>();
        services.AddScoped<IValidationRule, ContentI990And9999CountersRule>();
        services.AddScoped<IValidationRule, ContentDuplicateI200Rule>();

        services.AddScoped<SofiaEcdAnalysisService>();

        return services;
    }
}
