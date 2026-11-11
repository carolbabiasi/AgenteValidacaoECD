using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.OfficialPva;

/// <summary>
/// Manual ECD Leiaute 9 — REGRA_REGISTRO_DUPLICADO (registro I200): NUM_LCTO (campo 02) único no arquivo.
/// </summary>
public sealed class OfficialPvaRegistroDuplicadoI200Rule : IValidationRule
{
    public const string RuleId = "REGRA_REGISTRO_DUPLICADO";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaRegistroDuplicadoI200Rule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var journal = EcdJournalIndex.Build(context.Lines);
        var byNumero = new Dictionary<string, List<EcdJournalHeader>>(StringComparer.Ordinal);

        foreach (var header in journal.Headers)
        {
            if (string.IsNullOrWhiteSpace(header.NumeroLancamento))
            {
                continue;
            }

            if (!byNumero.TryGetValue(header.NumeroLancamento, out var list))
            {
                list = [];
                byNumero[header.NumeroLancamento] = list;
            }

            list.Add(header);
        }

        var evidence = new List<RuleEvidence>();
        foreach (var (numero, headers) in byNumero.Where(kv => kv.Value.Count > 1))
        {
            foreach (var header in headers)
            {
                evidence.Add(new RuleEvidence
                {
                    RecordType = "I200",
                    LineNumber = header.LineNumber,
                    FoundValue = numero,
                    Message =
                        $"Na linha {header.LineNumber}, o número do lançamento NUM_LCTO ({numero}) "
                        + "está duplicado no arquivo (chave do registro I200).",
                });
            }
        }

        if (evidence.Count == 0)
        {
            return Task.FromResult(new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message = "Não há NUM_LCTO duplicado em registros I200.",
            });
        }

        return Task.FromResult(new RuleEvaluationResult
        {
            RuleId = RuleId,
            RuleVersion = RuleVersion,
            Outcome = RuleOutcome.Error,
            Message =
                "O número do lançamento NUM_LCTO (campo 02 do I200) deve ser único no arquivo.",
            Evidence = evidence,
        });
    }
}
