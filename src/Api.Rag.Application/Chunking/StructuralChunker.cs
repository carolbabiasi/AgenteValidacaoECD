using System.Text;
using System.Text.RegularExpressions;

namespace Api.Rag.Application.Chunking;

public sealed record StructuralChunkDraft(
    string HierarchyPath,
    string? SourceLocation,
    string Content);

public static partial class StructuralChunker
{
    public const int MaxChunkCharacters = 2_000;

    public static string FormatPageMarker(int pageNumber) => $"[[p. {pageNumber}]]";

    public static IReadOnlyList<StructuralChunkDraft> Chunk(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
        {
            return [];
        }

        var results = new List<StructuralChunkDraft>();
        foreach (var section in SplitSections(plainText))
        {
            var parts = SplitBody(section.Lines);
            for (var i = 0; i < parts.Count; i++)
            {
                var (body, firstPage, lastPage) = parts[i];
                var path = parts.Count == 1 ? section.Path : $"{section.Path} (parte {i + 1})";
                var location = FormatLocation(firstPage, lastPage) ?? section.HeadingLocation;
                results.Add(new StructuralChunkDraft(path, location, body));
            }
        }

        return results;
    }

    private static IEnumerable<Section> SplitSections(string plainText)
    {
        var lines = plainText.Replace("\r\n", "\n").Split('\n');
        string? chapter = null;
        string? chapterNumber = null;
        var heading = "Documento";
        string? headingLocation = null;
        int? page = null;
        var buffer = new List<(string Text, int? Page)>();

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();

            var pageMatch = PageMarker().Match(line);
            if (pageMatch.Success)
            {
                page = int.Parse(pageMatch.Groups[1].Value);
                continue;
            }

            if (TryParseHeading(line, chapterNumber, out var kind, out var title, out var location))
            {
                if (buffer.Count > 0)
                {
                    yield return new Section(BuildPath(chapter, heading), headingLocation, buffer);
                    buffer = [];
                }

                if (kind == HeadingKind.Chapter)
                {
                    chapter = title;
                    chapterNumber = ChapterHeading().Match(title).Groups[1].Value;
                    heading = title;
                }
                else
                {
                    heading = title;
                }

                headingLocation = location;
                continue;
            }

            buffer.Add((line, page));
        }

        if (buffer.Count > 0)
        {
            yield return new Section(BuildPath(chapter, heading), headingLocation, buffer);
        }
    }

    private static List<(string Body, int? FirstPage, int? LastPage)> SplitBody(List<(string Text, int? Page)> lines)
    {
        var parts = new List<(string Body, int? FirstPage, int? LastPage)>();
        var current = new StringBuilder();
        int? firstPage = null;
        int? lastPage = null;

        void Flush()
        {
            var body = current.ToString().Trim();
            if (body.Length > 0)
            {
                parts.Add((body, firstPage, lastPage));
            }

            current.Clear();
            firstPage = null;
            lastPage = null;
        }

        void Append(string text, int? page)
        {
            if (current.Length > 0)
            {
                current.Append('\n');
            }

            current.Append(text);
            firstPage ??= page;
            lastPage = page ?? lastPage;
        }

        foreach (var (text, page) in lines)
        {
            if (current.Length == 0 && string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            if (current.Length > 0 && current.Length + 1 + text.Length > MaxChunkCharacters)
            {
                Flush();
            }

            for (var offset = 0; offset < text.Length; offset += MaxChunkCharacters)
            {
                if (offset > 0)
                {
                    Flush();
                }

                Append(text.Substring(offset, Math.Min(MaxChunkCharacters, text.Length - offset)), page);
            }

            if (text.Length == 0)
            {
                Append(string.Empty, page);
            }
        }

        Flush();
        return parts;
    }

    private static bool TryParseHeading(
        string line,
        string? chapterNumber,
        out HeadingKind kind,
        out string heading,
        out string? location)
    {
        kind = HeadingKind.Section;
        location = null;
        heading = line.Trim();

        if (heading.Length == 0 || TableOfContentsEntry().IsMatch(heading))
        {
            return false;
        }

        if (line.StartsWith("## ", StringComparison.Ordinal))
        {
            heading = line[3..].Trim();
            return true;
        }

        if (ChapterHeading().IsMatch(heading))
        {
            kind = HeadingKind.Chapter;
            return true;
        }

        var registro = RegistroHeading().Match(heading);
        if (registro.Success)
        {
            if (registro.Groups["page"].Success)
            {
                location = registro.Groups["page"].Value.Trim();
            }

            return true;
        }

        var numbered = NumberedSectionHeading().Match(heading);
        return numbered.Success &&
               heading.Length <= MaxSectionHeadingLength &&
               string.Equals(numbered.Groups[1].Value, chapterNumber, StringComparison.Ordinal);
    }

    private static string BuildPath(string? chapter, string heading) =>
        chapter is null || string.Equals(chapter, heading, StringComparison.Ordinal)
            ? heading
            : $"{chapter} > {heading}";

    private static string? FormatLocation(int? firstPage, int? lastPage)
    {
        if (firstPage is null)
        {
            return null;
        }

        return lastPage is null || lastPage == firstPage
            ? $"p. {firstPage}"
            : $"p. {firstPage}–{lastPage}";
    }

    [GeneratedRegex(@"^\[\[p\. (\d+)\]\]$")]
    private static partial Regex PageMarker();

    [GeneratedRegex(@"\.{4,}\s*\d+$")]
    private static partial Regex TableOfContentsEntry();

    [GeneratedRegex(@"^Cap[ií]tulo\s+(\d+)\b", RegexOptions.IgnoreCase)]
    private static partial Regex ChapterHeading();

    [GeneratedRegex(@"^(?:Registro|REGISTRO)\s+(?=[0-9A-Z]{0,3}\d)[0-9A-Z]{4}\b(?:\s*:.*|\s+(?<page>p\.\s*\d+.*)|\s*)$")]
    private static partial Regex RegistroHeading();

    [GeneratedRegex(@"^(\d+)\.\d+(?:\.\d+)*\.?\s+[A-ZÁÉÍÓÚÂÊÔÃÕÇ]")]
    private static partial Regex NumberedSectionHeading();

    private const int MaxSectionHeadingLength = 120;

    private enum HeadingKind
    {
        Section,
        Chapter,
    }

    private sealed record Section(string Path, string? HeadingLocation, List<(string Text, int? Page)> Lines);
}
