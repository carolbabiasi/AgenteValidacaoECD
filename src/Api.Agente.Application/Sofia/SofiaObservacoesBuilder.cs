using System.Text.Json;
using System.Text.RegularExpressions;
using Api.Agente.Domain.Entities;

namespace Api.Agente.Application.Sofia;

public static partial class SofiaObservacoesBuilder
{
    public static List<string> Build(ValidationExecution execution, string? reportJson)
    {
        var observacoes = new List<string>();

        if (!string.IsNullOrWhiteSpace(execution.DetectedLayoutVersion))
        {
            observacoes.Add($"Leiaute detectado: {execution.DetectedLayoutVersion}");
        }

        if (!string.IsNullOrWhiteSpace(execution.ManualVersionUsed))
        {
            observacoes.Add(FormatManual(execution.ManualVersionUsed));
        }

        var ragLabel = TryGetShortRagLabel(reportJson);
        if (ragLabel is not null)
        {
            observacoes.Add($"Referência no manual: {ragLabel}");
        }

        return observacoes;
    }

    private static string FormatManual(string manualVersion)
    {
        var match = ManualVersionPattern().Match(manualVersion);
        if (!match.Success)
        {
            return $"Manual de referência: {manualVersion}";
        }

        var layout = match.Groups["layout"].Value;
        var period = match.Groups["period"].Value;
        return $"Manual de referência: ECD Leiaute {layout} (versão {period})";
    }

    private static string? TryGetShortRagLabel(string? reportJson)
    {
        if (string.IsNullOrWhiteSpace(reportJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(reportJson);
            if (!document.RootElement.TryGetProperty("findings", out var findings))
            {
                return null;
            }

            foreach (var finding in findings.EnumerateArray())
            {
                if (!finding.TryGetProperty("ragReferences", out var references))
                {
                    continue;
                }

                foreach (var reference in references.EnumerateArray())
                {
                    var path = reference.TryGetProperty("hierarchyPath", out var hp)
                        ? hp.GetString()
                        : null;

                    if (string.IsNullOrWhiteSpace(path))
                    {
                        continue;
                    }

                    return ShortenHierarchyPath(path);
                }
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private static string ShortenHierarchyPath(string path)
    {
        var trimmed = path.Trim();
        var lastSegment = trimmed.Contains('>', StringComparison.Ordinal)
            ? trimmed.Split('>', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)[^1]
            : trimmed;

        if (lastSegment.Length > 96)
        {
            return lastSegment[..93] + "...";
        }

        return lastSegment;
    }

    [GeneratedRegex(@"^ECD-L(?<layout>\d+)-(?<period>\d{4}-\d{2})$", RegexOptions.CultureInvariant)]
    private static partial Regex ManualVersionPattern();
}
