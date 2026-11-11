using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.OfficialPva;

public sealed class OfficialPvaHistoricoObrigatorioRule : IValidationRule
{
    public const string RuleId = "REGRA_HISTORICO_OBRIGATORIO";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaHistoricoObrigatorioRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var evidence = new List<RuleEvidence>();

        foreach (var line in context.Lines)
        {
            if (line.RecordType != "I250")
            {
                continue;
            }

            var codHist = EcdFieldParser.GetField(line, 7)?.Trim();
            var hist = EcdFieldParser.GetField(line, 8)?.Trim();

            if (!string.IsNullOrWhiteSpace(codHist) || !string.IsNullOrWhiteSpace(hist))
            {
                continue;
            }

            evidence.Add(new RuleEvidence
            {
                RecordType = "I250",
                LineNumber = line.LineNumber,
                Message =
                    $"Na linha {line.LineNumber}, partida I250 sem HIST (campo 08) nem COD_HIST_PAD (campo 07).",
            });
        }

        if (evidence.Count == 0)
        {
            return Task.FromResult(new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message = "Todas as partidas I250 possuem histórico ou código padronizado.",
            });
        }

        return Task.FromResult(new RuleEvaluationResult
        {
            RuleId = RuleId,
            RuleVersion = RuleVersion,
            Outcome = RuleOutcome.Error,
            Message =
                "Cada partida I250 deve ter HIST (campo 08) ou COD_HIST_PAD (campo 07) preenchido.",
            Evidence = evidence,
        });
    }
}
