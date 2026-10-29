namespace Api.Rag.Application.Chunking;

public sealed record StructuralChunkDraft(
    string HierarchyPath,
    string? SourceLocation,
    string Content);

public static class StructuralChunker
{
    private const int MaxChunkCharacters = 2_000;

    public static IReadOnlyList<StructuralChunkDraft> Chunk(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
        {
            return [];
        }

        var results = new List<StructuralChunkDraft>();
        foreach (var section in SplitSections(plainText))
        {
            var path = section.Path;
            var body = section.Body.Trim();
            if (body.Length == 0)
            {
                continue;
            }

            if (body.Length <= MaxChunkCharacters)
            {
                results.Add(new StructuralChunkDraft(path, section.Location, body));
                continue;
            }

            var offset = 0;
            var part = 1;
            while (offset < body.Length)
            {
                var length = Math.Min(MaxChunkCharacters, body.Length - offset);
                var slice = body.Substring(offset, length);
                results.Add(new StructuralChunkDraft($"{path} (parte {part})", section.Location, slice));
                offset += length;
                part++;
            }
        }

        return results;
    }

    private static IEnumerable<(string Path, string? Location, string Body)> SplitSections(string plainText)
    {
        var lines = plainText.Replace("\r\n", "\n").Split('\n');
        var currentPath = "Documento";
        string? currentLocation = null;
        var buffer = new List<string>();

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();
            if (TryParseHeading(line, out var heading, out var location))
            {
                if (buffer.Count > 0)
                {
                    yield return (currentPath, currentLocation, string.Join('\n', buffer));
                    buffer.Clear();
                }

                currentPath = heading;
                currentLocation = location ?? currentLocation;
                continue;
            }

            buffer.Add(line);
        }

        if (buffer.Count > 0)
        {
            yield return (currentPath, currentLocation, string.Join('\n', buffer));
        }
    }

    private static bool TryParseHeading(string line, out string heading, out string? location)
    {
        location = null;
        heading = line;

        if (line.StartsWith("## ", StringComparison.Ordinal))
        {
            heading = line[3..].Trim();
            return true;
        }

        if (line.StartsWith("Capítulo ", StringComparison.OrdinalIgnoreCase) ||
            line.StartsWith("Capitulo ", StringComparison.OrdinalIgnoreCase))
        {
            heading = line.Trim();
            return true;
        }

        if (line.StartsWith("Registro ", StringComparison.OrdinalIgnoreCase))
        {
            heading = line.Trim();
            var pageIdx = line.IndexOf("p.", StringComparison.OrdinalIgnoreCase);
            if (pageIdx >= 0)
            {
                location = line[pageIdx..].Trim();
            }

            return true;
        }

        return false;
    }
}
