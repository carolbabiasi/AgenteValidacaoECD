using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.OfficialPva;

public sealed class OfficialPvaObrigatoriaDemonstracaoRule : IValidationRule
{
    public const string RuleId = "REGRA_OBRIGATORIA_DEMONSTRACAO";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaObrigatoriaDemonstracaoRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var demos = EcdDemonstracoesIndex.Build(context.Lines);
        if (demos.Blocos.Count == 0)
        {
            return Task.FromResult(OfficialPvaDemonstracaoRuleHelpers.Validated(
                RuleId,
                RuleVersion,
                "Nenhum registro J005 informado."));
        }

        var evidence = new List<RuleEvidence>();
        foreach (var bloco in demos.Blocos)
        {
            if (!bloco.HasJ100)
            {
                evidence.Add(new RuleEvidence
                {
                    RecordType = "J005",
                    LineNumber = bloco.Header.LineNumber,
                    Message =
                        $"J005 linha {bloco.Header.LineNumber}: falta registro J100 (Balanço Patrimonial).",
                });
            }

            if (!bloco.HasJ150)
            {
                evidence.Add(new RuleEvidence
                {
                    RecordType = "J005",
                    LineNumber = bloco.Header.LineNumber,
                    Message =
                        $"J005 linha {bloco.Header.LineNumber}: falta registro J150 (DRE).",
                });
            }
        }

        return Task.FromResult(OfficialPvaDemonstracaoRuleHelpers.Build(
            evidence,
            RuleId,
            RuleVersion,
            "J100 e J150 informados para cada J005.",
            "Cada J005 deve possuir registros J100 e J150."));
    }
}

public sealed class OfficialPvaRegistroObrigatorioJ005FimExercicioRule : IValidationRule
{
    public const string RuleId = "REGRA_REGISTRO_OBRIGATORIO_J005_FIM_EXERCICIO";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaRegistroObrigatorioJ005FimExercicioRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var periodo = EcdArquivoIndex.TryGetPeriodo0000(context.Lines);
        var dtExSocial = EcdArquivoIndex.TryGetDtExSocial(context.Lines);
        var indEsc = OfficialPvaEscrituracao.GetIndEsc(context.Lines);

        if (periodo is null || dtExSocial is null)
        {
            return Task.FromResult(OfficialPvaDemonstracaoRuleHelpers.Validated(
                RuleId,
                RuleVersion,
                "Sem período 0000 ou DT_EX_SOCIAL (I030) para avaliar J005 de fim de exercício."));
        }

        if (indEsc is not ("G" or "R" or "B"))
        {
            return Task.FromResult(OfficialPvaDemonstracaoRuleHelpers.Validated(
                RuleId,
                RuleVersion,
                "Regra aplica-se a escriturações G, R ou B."));
        }

        if (!EcdArquivoIndex.DataNoPeriodoArquivo(dtExSocial.Value, periodo))
        {
            return Task.FromResult(OfficialPvaDemonstracaoRuleHelpers.Validated(
                RuleId,
                RuleVersion,
                "DT_EX_SOCIAL fora do período do arquivo; condição não aplicável."));
        }

        var demos = EcdDemonstracoesIndex.Build(context.Lines);
        var blocoFimEx = demos.Blocos.FirstOrDefault(b => b.Header.DtFin == dtExSocial.Value);
        var evidence = new List<RuleEvidence>();

        if (blocoFimEx is null)
        {
            evidence.Add(new RuleEvidence
            {
                RecordType = "I030",
                LineNumber = context.Lines.First(l => l.RecordType == "I030").LineNumber,
                ExpectedValue = dtExSocial.Value.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture),
                Message =
                    $"Encerramento em {dtExSocial.Value:dd/MM/yyyy}: falta J005 com DT_FIN igual à DT_EX_SOCIAL.",
            });
        }
        else
        {
            if (!blocoFimEx.HasJ100)
            {
                evidence.Add(new RuleEvidence
                {
                    RecordType = "J005",
                    LineNumber = blocoFimEx.Header.LineNumber,
                    Message = "Falta registro J100 no bloco de demonstrações do fim do exercício.",
                });
            }

            if (!blocoFimEx.HasJ150)
            {
                evidence.Add(new RuleEvidence
                {
                    RecordType = "J005",
                    LineNumber = blocoFimEx.Header.LineNumber,
                    Message = "Falta registro J150 no bloco de demonstrações do fim do exercício.",
                });
            }
        }

        return Task.FromResult(OfficialPvaDemonstracaoRuleHelpers.Build(
            evidence,
            RuleId,
            RuleVersion,
            "J005/J100/J150 presentes na DT_EX_SOCIAL.",
            "Com encerramento no período (G/R/B), deve existir J005 com DT_FIN = DT_EX_SOCIAL, J100 e J150."));
    }
}

public sealed class OfficialPvaEncObrigatorioRule : IValidationRule
{
    public const string RuleId = "REGRA_ENC_OBRIGATORIO";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaEncObrigatorioRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var periodo = EcdArquivoIndex.TryGetPeriodo0000(context.Lines);
        if (periodo is null)
        {
            return Task.FromResult(OfficialPvaDemonstracaoRuleHelpers.Validated(
                RuleId,
                RuleVersion,
                "Sem período 0000 para avaliar REGRA_ENC_OBRIGATORIO."));
        }

        var closing = EcdClosingIndex.Build(context.Lines);
        var demos = EcdDemonstracoesIndex.Build(context.Lines);
        var evidence = new List<RuleEvidence>();

        foreach (var bloco in demos.Blocos)
        {
            if (!string.Equals(bloco.Header.IdDem, "1", StringComparison.Ordinal))
            {
                continue;
            }

            if (bloco.Header.DtIni < periodo.DtIni || bloco.Header.DtFin > periodo.DtFin)
            {
                continue;
            }

            if (!bloco.HasJ100 || !bloco.HasJ210)
            {
                continue;
            }

            var hasI350 = closing.Apuracoes.Any(a => a.DtRes == bloco.Header.DtFin);
            if (hasI350)
            {
                continue;
            }

            evidence.Add(new RuleEvidence
            {
                RecordType = "J005",
                LineNumber = bloco.Header.LineNumber,
                ExpectedValue = bloco.Header.DtFin.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture),
                Message =
                    $"ID_DEM=1 com J100 e J210: falta I350 com DT_RES = {bloco.Header.DtFin:dd/MM/yyyy}.",
            });
        }

        return Task.FromResult(OfficialPvaDemonstracaoRuleHelpers.Build(
            evidence,
            RuleId,
            RuleVersion,
            "I350 alinhado à DT_FIN do J005 quando aplicável.",
            "Com J005 ID_DEM=1, J100 e J210 no período do arquivo, deve existir I350 com DT_RES = J005.DT_FIN."));
    }
}

public sealed class OfficialPvaPeriodoSupUmAnoRule : IValidationRule
{
    public const string RuleId = "REGRA_PERIODO_SUP_UM_ANO";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaPeriodoSupUmAnoRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var demos = EcdDemonstracoesIndex.Build(context.Lines);
        if (demos.Blocos.Count == 0)
        {
            return Task.FromResult(OfficialPvaDemonstracaoRuleHelpers.Validated(
                RuleId,
                RuleVersion,
                "Nenhum registro J005 informado."));
        }

        var evidence = new List<RuleEvidence>();
        foreach (var bloco in demos.Blocos)
        {
            var dias = bloco.Header.DtFin.DayNumber - bloco.Header.DtIni.DayNumber;
            if (dias > 365)
            {
                evidence.Add(new RuleEvidence
                {
                    RecordType = "J005",
                    LineNumber = bloco.Header.LineNumber,
                    Message =
                        $"Período das demonstrações ({bloco.Header.DtIni:dd/MM/yyyy} a {bloco.Header.DtFin:dd/MM/yyyy}) "
                        + "é superior a um ano.",
                });
            }
        }

        return Task.FromResult(OfficialPvaDemonstracaoRuleHelpers.BuildAlert(
            evidence,
            RuleId,
            RuleVersion,
            "Períodos de demonstrações dentro de um ano.",
            "Há J005 com período das demonstrações superior a um ano."));
    }
}
