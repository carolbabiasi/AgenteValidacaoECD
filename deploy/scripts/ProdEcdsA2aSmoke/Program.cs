using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
var baseUrl = args.ElementAtOrDefault(0) ?? "https://infis-agente-validacao-ecd.vercel.app";
var ecdDir = args.ElementAtOrDefault(1) ?? Path.Combine(repoRoot, "ecds-teste");
var token = Environment.GetEnvironmentVariable("SOFIA_A2A_TOKEN");
if (string.IsNullOrWhiteSpace(token))
{
    var envFile = Path.Combine(repoRoot, "deploy", "local", "supabase.env");
    if (File.Exists(envFile))
    {
        foreach (var line in File.ReadLines(envFile))
        {
            if (line.StartsWith("SOFIA_A2A_TOKEN=", StringComparison.Ordinal))
            {
                token = line["SOFIA_A2A_TOKEN=".Length..].Trim();
                break;
            }
        }
    }
}

if (string.IsNullOrWhiteSpace(token))
{
    Console.Error.WriteLine("Defina SOFIA_A2A_TOKEN ou deploy/local/supabase.env");
    return 1;
}

if (!Directory.Exists(ecdDir))
{
    Console.Error.WriteLine($"Pasta não encontrada: {ecdDir}");
    return 1;
}

using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

var results = new List<JsonObject>();
foreach (var file in Directory.GetFiles(ecdDir, "*.txt").OrderBy(Path.GetFileName))
{
    var name = Path.GetFileName(file);
    var sw = System.Diagnostics.Stopwatch.StartNew();
    var row = new JsonObject
    {
        ["arquivo"] = name,
        ["kb"] = Math.Round(new FileInfo(file).Length / 1024.0, 1),
    };

    try
    {
        var texto = await File.ReadAllTextAsync(file, Encoding.UTF8);
        var solicitacao = new JsonObject
        {
            ["versaoContrato"] = "1.0",
            ["idExecucao"] = Guid.NewGuid().ToString(),
            ["idSolicitacao"] = Guid.NewGuid().ToString(),
            ["usuario"] = new JsonObject { ["nome"] = "Teste produção ecds-teste" },
            ["pergunta"] = $"Validar {name}",
            ["documentos"] = new JsonArray
            {
                new JsonObject
                {
                    ["idDocumento"] = Path.GetFileNameWithoutExtension(name),
                    ["nome"] = name,
                    ["texto"] = texto,
                },
            },
        };

        var envelope = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = $"prod-{Path.GetFileNameWithoutExtension(name)}",
            ["method"] = "message/send",
            ["params"] = new JsonObject
            {
                ["message"] = new JsonObject
                {
                    ["role"] = "user",
                    ["messageId"] = Guid.NewGuid().ToString(),
                    ["parts"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["kind"] = "text",
                            ["text"] = solicitacao.ToJsonString(),
                        },
                    },
                },
            },
        };

        var content = new StringContent(envelope.ToJsonString(), Encoding.UTF8, "application/json");
        var response = await http.PostAsync($"{baseUrl.TrimEnd('/')}/a2a", content);
        var body = await response.Content.ReadAsStringAsync();
        sw.Stop();
        row["ms"] = sw.ElapsedMilliseconds;
        row["http"] = (int)response.StatusCode;

        using var doc = JsonDocument.Parse(body);
        if (doc.RootElement.TryGetProperty("error", out var err))
        {
            row["ok"] = false;
            row["erro"] = err.GetProperty("message").GetString();
        }
        else if (doc.RootElement.GetProperty("result").GetProperty("status").GetProperty("state").GetString() == "completed")
        {
            var resultadoText = doc.RootElement.GetProperty("result").GetProperty("artifacts")[0]
                .GetProperty("parts")[0].GetProperty("text").GetString()!;
            using var resultado = JsonDocument.Parse(resultadoText);
            var root = resultado.RootElement;
            row["ok"] = true;
            row["status"] = root.GetProperty("status").GetString();
            var achados = root.GetProperty("achados");
            row["qtdAchados"] = achados.GetArrayLength();

            var severidades = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var lista = new JsonArray();
            var i = 0;
            foreach (var a in achados.EnumerateArray())
            {
                var sev = a.GetProperty("severidade").GetString() ?? "?";
                severidades[sev] = severidades.GetValueOrDefault(sev) + 1;
                if (i < 12)
                {
                    var desc = a.GetProperty("descricao").GetString() ?? "";
                    if (desc.Length > 160)
                    {
                        desc = desc[..160] + "...";
                    }

                    lista.Add(new JsonObject
                    {
                        ["idAchado"] = a.GetProperty("idAchado").GetString(),
                        ["severidade"] = sev,
                        ["descricao"] = desc,
                    });
                }

                i++;
            }

            row["severidades"] = string.Join(", ", severidades.Select(kv => $"{kv.Key}={kv.Value}"));
            row["achados"] = lista;

            var analise = root.GetProperty("analise").GetString() ?? "";
            if (analise.Length > 500)
            {
                analise = analise[..500] + "...";
            }

            row["analise"] = analise;

            if (root.TryGetProperty("idValidacaoInterna", out var idVal) && idVal.ValueKind == JsonValueKind.String)
            {
                row["executionId"] = idVal.GetString();
            }

            if (root.TryGetProperty("observacoes", out var observacoes) && observacoes.ValueKind == JsonValueKind.Array)
            {
                var obsList = new JsonArray();
                foreach (var o in observacoes.EnumerateArray())
                {
                    obsList.Add(o.GetString());
                }

                row["observacoes"] = obsList;
            }

            if (root.TryGetProperty("erro", out var erroNeg) && erroNeg.ValueKind == JsonValueKind.Object)
            {
                row["erroNegocio"] = erroNeg.GetProperty("codigo").GetString();
            }
        }
        else
        {
            row["ok"] = false;
            row["erro"] = "Resposta A2A inesperada";
            row["raw"] = body.Length > 600 ? body[..600] : body;
        }
    }
    catch (Exception ex)
    {
        sw.Stop();
        row["ms"] = sw.ElapsedMilliseconds;
        row["ok"] = false;
        row["erro"] = ex.Message;
    }

    results.Add(row);
    Console.Error.WriteLine($"{name}: ok={row["ok"]} achados={row["qtdAchados"]?.ToString() ?? "-"} ms={row["ms"]}");
}

var outPath = Path.Combine(repoRoot, "deploy", "local", "prod-ecds-teste-result.json");
await File.WriteAllTextAsync(
    outPath,
    JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }),
    Encoding.UTF8);
Console.WriteLine(outPath);
return 0;
