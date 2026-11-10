using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.Content;

public sealed class ContentI990And9999CountersRule : IValidationRule
{
    public const string RuleId = "ECD-CONTENT-011";
    public const string RuleVersion = "1.0.1";
    public string ImplementationKey => nameof(ContentI990And9999CountersRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var evidence = new List<RuleEvidence>();

        var i001Starts = new List<int>();
        for (var i = 0; i < context.Lines.Count; i++)
        {
            if (context.Lines[i].RecordType == "I001")
            {
                i001Starts.Add(i);
            }
        }

        if (i001Starts.Count > 0)
        {
            var blockStart = i001Starts[0];
            for (var i = blockStart; i < context.Lines.Count; i++)
            {
                var line = context.Lines[i];
                if (line.RecordType == "I001" && i != blockStart)
                {
                    blockStart = i;
                }

                if (line.RecordType != "I990")
                {
                    continue;
                }

                var declaredText = EcdFieldParser.GetField(line, 2);
                if (!int.TryParse(declaredText, out var declared) || declared == 0)
                {
                    continue;
                }

                // QTD_LIN do I990 de fechamento inclui I001..registros intermediários e I990 preparatório (|0|),
                // mas não inclui a própria linha I990 que declara o contador.
                var blockCount = 0;
                for (var j = blockStart; j < i; j++)
                {
                    var rt = context.Lines[j].RecordType;
                    if (rt is not null && rt.StartsWith('I'))
                    {
                        blockCount++;
                    }
                }

                if (declared != blockCount)
                {
                    evidence.Add(new RuleEvidence
                    {
                        RecordType = "I990",
                        LineNumber = line.LineNumber,
                        FieldName = "QTD_LIN",
                        ExpectedValue = blockCount.ToString(),
                        FoundValue = declared.ToString(),
                        Message = $"I990 declara {declared} registros no Bloco I, contagem efetiva={blockCount}.",
                    });
                }
            }
        }

        var closing9999 = context.Lines.LastOrDefault(l => l.RecordType == "9999");
        if (closing9999 is not null)
        {
            var declaredTotalText = EcdFieldParser.GetField(closing9999, 2);
            if (int.TryParse(declaredTotalText, out var declaredTotal) && declaredTotal != context.Lines.Count)
            {
                evidence.Add(new RuleEvidence
                {
                    RecordType = "9999",
                    LineNumber = closing9999.LineNumber,
                    FieldName = "QTD_LIN_ARQ",
                    ExpectedValue = context.Lines.Count.ToString(),
                    FoundValue = declaredTotal.ToString(),
                    Message = "Contador 9999 não confere com o total de linhas do arquivo.",
                });
            }
        }

        var i990Lines = context.Lines.Where(l => l.RecordType == "I990").ToList();
        if (i990Lines.Count > 1)
        {
            var nonZeroCloses = i990Lines
                .Select(l => (Line: l, Qty: int.TryParse(EcdFieldParser.GetField(l, 2), out var q) ? q : 0))
                .Where(x => x.Qty > 0)
                .ToList();

            if (nonZeroCloses.Count > 1)
            {
                foreach (var extra in nonZeroCloses.Skip(1))
                {
                    evidence.Add(new RuleEvidence
                    {
                        RecordType = "I990",
                        LineNumber = extra.Line.LineNumber,
                        FoundValue = extra.Qty.ToString(),
                        Message =
                            $"Na linha {extra.Line.LineNumber}: há outro registro I990 com contador {extra.Qty}; só deve existir um encerramento do Bloco I com quantidade positiva.",
                    });
                }
            }
        }

        if (evidence.Count == 0)
        {
            return Task.FromResult(new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message = "Contadores I990/9999 consistentes.",
            });
        }

        return Task.FromResult(new RuleEvaluationResult
        {
            RuleId = RuleId,
            RuleVersion = RuleVersion,
            Outcome = RuleOutcome.Error,
            Message = evidence.Count == 1
                ? "Contador de bloco ou de arquivo não confere."
                : "Contadores do Bloco I (I990) e do arquivo (9999) não conferem.",
            Evidence = evidence,
        });
    }
}
