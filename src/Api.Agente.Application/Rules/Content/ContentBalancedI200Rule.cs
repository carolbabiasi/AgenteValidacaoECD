using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.Content;

public sealed class ContentBalancedI200Rule : IValidationRule
{
    public const string RuleId = "ECD-CONTENT-004";
    public const string RuleVersion = "1.0.0";
    public string ImplementationKey => nameof(ContentBalancedI200Rule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var evidence = new List<RuleEvidence>();
        var lines = context.Lines;

        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].RecordType != "I200")
            {
                continue;
            }

            decimal debits = 0;
            decimal credits = 0;
            var i200 = lines[i];

            for (var j = i + 1; j < lines.Count; j++)
            {
                var rt = lines[j].RecordType;
                if (rt is "I200" or "I990")
                {
                    break;
                }

                if (rt != "I250")
                {
                    continue;
                }

                var amountText = EcdFieldParser.GetField(lines[j], 4);
                var indicator = EcdFieldParser.GetField(lines[j], 5)?.Trim().ToUpperInvariant();
                if (!EcdFieldParser.TryParseSpedDecimal(amountText, out var amount))
                {
                    continue;
                }

                if (indicator == "D")
                {
                    debits += amount;
                }
                else if (indicator == "C")
                {
                    credits += amount;
                }
            }

            if (debits == credits)
            {
                continue;
            }

            evidence.Add(new RuleEvidence
            {
                RecordType = "I200",
                LineNumber = i200.LineNumber,
                ExpectedValue = credits.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                FoundValue = debits.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                Message =
                    $"Na linha {i200.LineNumber}, lançamento I200 com partidas desbalanceadas: "
                    + $"débitos {EcdDisplayText.FormatMoney(debits)}, créditos {EcdDisplayText.FormatMoney(credits)}.",
            });
        }

        if (evidence.Count == 0)
        {
            return Task.FromResult(new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message = "Partidas dos lançamentos I200 balanceadas.",
            });
        }

        return Task.FromResult(new RuleEvaluationResult
        {
            RuleId = RuleId,
            RuleVersion = RuleVersion,
            Outcome = RuleOutcome.Error,
            Message = evidence.Count == 1
                ? "Lançamento contábil (I200) com débitos e créditos diferentes."
                : $"{evidence.Count} lançamentos I200 com débitos e créditos diferentes.",
            Evidence = evidence,
        });
    }
}
