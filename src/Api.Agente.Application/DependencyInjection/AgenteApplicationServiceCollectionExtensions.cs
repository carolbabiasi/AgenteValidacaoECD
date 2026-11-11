using Api.Agente.Application.Ai;
using Api.Agente.Application.Ecd;
using Api.Agente.Application.Pipeline;
using Api.Agente.Application.Reports;
using Api.Agente.Application.Rules;
using Api.Agente.Application.Rules.Content;
using Api.Agente.Application.Rules.OfficialPva;
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
        services.AddScoped<IValidationRule, OfficialPvaVlLctoDebRule>();
        services.AddScoped<IValidationRule, OfficialPvaVlLctoCredRule>();
        services.AddScoped<IValidationRule, OfficialPvaVlLctoEscAuxiliarRule>();
        services.AddScoped<IValidationRule, OfficialPvaObrigNaoMfI200Rule>();
        services.AddScoped<IValidationRule, OfficialPvaRegistroDuplicadoI200Rule>();
        services.AddScoped<IValidationRule, OfficialPvaLcto4FormulaRule>();
        services.AddScoped<IValidationRule, OfficialPvaHistoricoObrigatorioRule>();
        services.AddScoped<IValidationRule, OfficialPvaValidacaoValorDebRule>();
        services.AddScoped<IValidationRule, OfficialPvaValidacaoValorCredRule>();
        services.AddScoped<IValidationRule, OfficialPvaValidacaoSaldoFinalRule>();
        services.AddScoped<IValidationRule, OfficialPvaValidacaoDebDifCredRule>();
        services.AddScoped<IValidationRule, OfficialPvaValidacaoSomaSaldoInicialRule>();
        services.AddScoped<IValidationRule, OfficialPvaValidacaoSomaSaldoFinalRule>();
        services.AddScoped<IValidationRule, OfficialPvaRegistroObrigatorioI350Rule>();
        services.AddScoped<IValidationRule, OfficialPvaValidacaoSaldoContaRule>();
        services.AddScoped<IValidationRule, OfficialPvaValidacaoContaResultadoRule>();
        services.AddScoped<IValidationRule, OfficialPvaDtResDuplicidadeRule>();
        services.AddScoped<IValidationRule, OfficialPvaCodCtaDtResDuplicidadeRule>();
        services.AddScoped<IValidationRule, OfficialPvaEncerramentoExercicioRule>();
        services.AddScoped<IValidationRule, OfficialPvaContaResultadoI355Rule>();
        services.AddScoped<IValidationRule, OfficialPvaDataIntervaloDoArquivoRule>();
        services.AddScoped<IValidationRule, OfficialPvaDuplicidadeContaSaldoPeriodicoRule>();
        services.AddScoped<IValidationRule, OfficialPvaCamposSaldosPeriodicosDiferenteZeroRule>();
        services.AddScoped<IValidationRule, OfficialPvaValidacaoSaldoIniDifFinRule>();
        services.AddScoped<IValidationRule, OfficialPvaValidaSldIniSomaSldIniI157Rule>();
        services.AddScoped<IValidationRule, OfficialPvaContinuidadeSaldosPeriodicosRule>();
        services.AddScoped<IValidationRule, OfficialPvaValidaMesI157Rule>();
        services.AddScoped<IValidationRule, OfficialPvaExisteI157PeriodoPosteriorRule>();
        services.AddScoped<IValidationRule, OfficialPvaNaturezaContaPaiI157Rule>();
        services.AddScoped<IValidationRule, OfficialPvaDuplicidadePeriodoSaldoPeriodicoRule>();
        services.AddScoped<IValidationRule, OfficialPvaDataMesI150Rule>();
        services.AddScoped<IValidationRule, OfficialPvaContaI155InexistenteC155Rule>();
        services.AddScoped<IValidationRule, OfficialPvaContaC155InexistenteI155Rule>();
        services.AddScoped<IValidationRule, OfficialPvaExisteI155ComI157NoC155Rule>();
        services.AddScoped<IValidationRule, OfficialPvaNaturezaContaC155Rule>();
        services.AddScoped<IValidationRule, OfficialPvaNaturezaContaI155Rule>();
        services.AddScoped<IValidationRule, OfficialPvaContaI157InexistenteC155Rule>();
        services.AddScoped<IValidationRule, OfficialPvaObrigatoriaDemonstracaoRule>();
        services.AddScoped<IValidationRule, OfficialPvaRegistroObrigatorioJ005FimExercicioRule>();
        services.AddScoped<IValidationRule, OfficialPvaEncObrigatorioRule>();
        services.AddScoped<IValidationRule, OfficialPvaPeriodoSupUmAnoRule>();
        services.AddScoped<IValidationRule, OfficialPvaSomaDasParcelasBalancoIniRule>();
        services.AddScoped<IValidationRule, OfficialPvaSomaDasParcelasBalancoFinRule>();
        services.AddScoped<IValidationRule, OfficialPvaValidaAtivoPassivoIniRule>();
        services.AddScoped<IValidationRule, OfficialPvaValidaAtivoPassivoFinRule>();
        services.AddScoped<IValidationRule, OfficialPvaBalancoSaldoIniRule>();
        services.AddScoped<IValidationRule, OfficialPvaBalancoSaldoFinRule>();
        services.AddScoped<IValidationRule, OfficialPvaSomaNiveisDreRule>();

        services.AddScoped<SofiaEcdAnalysisService>();

        return services;
    }
}
