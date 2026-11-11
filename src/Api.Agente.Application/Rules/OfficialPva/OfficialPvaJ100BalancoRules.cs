using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.OfficialPva;

public sealed class OfficialPvaSomaDasParcelasBalancoIniRule : IValidationRule
{
    public const string RuleId = "REGRA_SOMA_DAS_PARCELAS_BALANCO_INI";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaSomaDasParcelasBalancoIniRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(EvaluateSoma(context, saldoInicial: true, RuleId, RuleVersion));

    internal static RuleEvaluationResult EvaluateSoma(
        EcdEvaluationContext context,
        bool saldoInicial,
        string ruleId,
        string ruleVersion)
    {
        var demos = EcdDemonstracoesIndex.Build(context.Lines);
        var evidence = new List<RuleEvidence>();
        foreach (var bloco in demos.Blocos)
        {
            OfficialPvaDemonstracaoBalancoHelpers.ValidateSomaTotalizadores(
                bloco.BalancoPatrimonial,
                saldoInicial,
                "J100",
                evidence);
        }

        var campo = saldoInicial ? "VL_CTA_INI" : "VL_CTA_FIN";
        return OfficialPvaDemonstracaoRuleHelpers.Build(
            evidence,
            ruleId,
            ruleVersion,
            $"Totalizadores J100 conferem com a soma dos filhos ({campo}).",
            $"{campo} do totalizador J100 difere da soma dos filhos.");
    }
}

public sealed class OfficialPvaSomaDasParcelasBalancoFinRule : IValidationRule
{
    public const string RuleId = "REGRA_SOMA_DAS_PARCELAS_BALANCO_FIN";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaSomaDasParcelasBalancoFinRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(OfficialPvaSomaDasParcelasBalancoIniRule.EvaluateSoma(
            context,
            saldoInicial: false,
            RuleId,
            RuleVersion));
}

public sealed class OfficialPvaValidaAtivoPassivoIniRule : IValidationRule
{
    public const string RuleId = "REGRA_VALIDA_ATIVO_PASSIVO_INI";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaValidaAtivoPassivoIniRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(EvaluateAtivoPassivo(context, saldoInicial: true, RuleId, RuleVersion));

    internal static RuleEvaluationResult EvaluateAtivoPassivo(
        EcdEvaluationContext context,
        bool saldoInicial,
        string ruleId,
        string ruleVersion)
    {
        var demos = EcdDemonstracoesIndex.Build(context.Lines);
        var evidence = new List<RuleEvidence>();
        foreach (var bloco in demos.Blocos)
        {
            OfficialPvaDemonstracaoBalancoHelpers.ValidateAtivoPassivoNivel1(
                bloco.BalancoPatrimonial,
                saldoInicial,
                evidence);
        }

        var campo = saldoInicial ? "VL_CTA_INI" : "VL_CTA_FIN";
        return OfficialPvaDemonstracaoRuleHelpers.Build(
            evidence,
            ruleId,
            ruleVersion,
            $"Ativo e Passivo nível 1 conferem ({campo}).",
            $"Ativo e Passivo nível 1 divergem em {campo}.");
    }
}

public sealed class OfficialPvaValidaAtivoPassivoFinRule : IValidationRule
{
    public const string RuleId = "REGRA_VALIDA_ATIVO_PASSIVO_FIN";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaValidaAtivoPassivoFinRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(OfficialPvaValidaAtivoPassivoIniRule.EvaluateAtivoPassivo(
            context,
            saldoInicial: false,
            RuleId,
            RuleVersion));
}

public sealed class OfficialPvaBalancoSaldoIniRule : IValidationRule
{
    public const string RuleId = "REGRA_BALANCO_SALDO_INI";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaBalancoSaldoIniRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(EvaluateDetalhe(context, saldoInicial: true, RuleId, RuleVersion));

    internal static RuleEvaluationResult EvaluateDetalhe(
        EcdEvaluationContext context,
        bool saldoInicial,
        string ruleId,
        string ruleVersion)
    {
        var demos = EcdDemonstracoesIndex.Build(context.Lines);
        var evidence = new List<RuleEvidence>();
        foreach (var bloco in demos.Blocos)
        {
            OfficialPvaDemonstracaoBalancoHelpers.ValidateBalancoDetalheAtivoPassivo(
                bloco.BalancoPatrimonial,
                saldoInicial,
                evidence);
        }

        var campo = saldoInicial ? "saldo inicial" : "saldo final";
        return OfficialPvaDemonstracaoRuleHelpers.Build(
            evidence,
            ruleId,
            ruleVersion,
            $"Soma dos detalhes Ativo e Passivo conferem ({campo}).",
            $"Soma dos detalhes Ativo e Passivo divergem ({campo}).");
    }
}

public sealed class OfficialPvaBalancoSaldoFinRule : IValidationRule
{
    public const string RuleId = "REGRA_BALANCO_SALDO_FIN";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaBalancoSaldoFinRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(OfficialPvaBalancoSaldoIniRule.EvaluateDetalhe(
            context,
            saldoInicial: false,
            RuleId,
            RuleVersion));
}
