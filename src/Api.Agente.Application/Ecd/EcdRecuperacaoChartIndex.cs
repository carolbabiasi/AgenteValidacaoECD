using Api.Agente.Domain.Ecd;

namespace Api.Agente.Application.Ecd;

public sealed class EcdRecuperacaoChartIndex
{
    private readonly Dictionary<string, string> _natureByConta;

    private EcdRecuperacaoChartIndex(Dictionary<string, string> natureByConta) =>
        _natureByConta = natureByConta;

    public string? TryGetNatureza(string codConta) =>
        string.IsNullOrWhiteSpace(codConta)
            ? null
            : _natureByConta.GetValueOrDefault(codConta.Trim());

    public static EcdRecuperacaoChartIndex Build(IReadOnlyList<EcdLine> lines)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in lines)
        {
            if (line.RecordType != "C050")
            {
                continue;
            }

            var cod = EcdFieldParser.GetField(line, 6)?.Trim();
            if (string.IsNullOrWhiteSpace(cod))
            {
                continue;
            }

            var natureza = EcdFieldParser.GetField(line, 3)?.Trim();
            if (!string.IsNullOrWhiteSpace(natureza))
            {
                map[cod] = natureza;
            }
        }

        return new EcdRecuperacaoChartIndex(map);
    }
}
