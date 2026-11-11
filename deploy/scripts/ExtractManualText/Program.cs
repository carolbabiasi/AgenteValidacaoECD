using System.Text;
using Api.Rag.Infrastructure.Extractors;

var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
var pdfPath = args.ElementAtOrDefault(0)
    ?? Path.Combine(repoRoot, "docs", "Manual_de_Orientação_da_ECD_Leiaute_9_janeiro_2026.pdf");
var outPath = args.ElementAtOrDefault(1)
    ?? Path.Combine(repoRoot, "specs", "002-regras-oficiais-pva-ecd", "artifacts", "manual-jan2026-extract.txt");

if (!File.Exists(pdfPath))
{
    Console.Error.WriteLine($"PDF não encontrado: {pdfPath}");
    return 1;
}

Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);

await using var stream = File.OpenRead(pdfPath);
var extractor = new PdfPigManualTextExtractor();
var extracted = await extractor.ExtractAsync(stream, Path.GetFileName(pdfPath), "application/pdf");
await File.WriteAllTextAsync(outPath, extracted.PlainText, Encoding.UTF8);

Console.WriteLine($"Extraído {extracted.PlainText.Length:N0} caracteres → {outPath}");
return 0;
