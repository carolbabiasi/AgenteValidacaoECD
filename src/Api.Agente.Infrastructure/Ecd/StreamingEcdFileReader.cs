using System.Text;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;

namespace Api.Agente.Infrastructure.Ecd;

public sealed class StreamingEcdFileReader : IEcdFileReader
{
    public async Task<IReadOnlyList<EcdLine>> ReadAllLinesAsync(
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var lines = new List<EcdLine>();
        using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

        var lineNumber = 0;
        while (true)
        {
            var raw = await reader.ReadLineAsync(cancellationToken);
            if (raw is null)
            {
                break;
            }

            lineNumber++;
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            lines.Add(new EcdLine(lineNumber, raw, ParseRecordType(raw)));
        }

        return lines;
    }

    public static string? ParseRecordType(string raw)
    {
        if (!raw.StartsWith('|'))
        {
            return null;
        }

        var end = raw.IndexOf('|', 1);
        if (end <= 1)
        {
            return null;
        }

        return raw[1..end];
    }
}
