# Evidência — Smoke quickstart (T062)

**Data**: 2026-11-05  
**Roteiro**: [quickstart.md](../quickstart.md) automatizado em `deploy/scripts/Invoke-Smoke.ps1`  
**Resultado**: **16/16 checks OK** (execução `9b225d7b`), após corrigir 4 defeitos encontrados na primeira rodada.

## Ambiente

| Item | Valor |
|------|-------|
| SO | Windows 10.0.26200, Windows PowerShell 5.1 |
| .NET SDK | 10.0.403 |
| Docker | Engine 29.8.2; `deploy/docker-compose.dev.yml` (SQL Server 2022, pgvector/pg16, Redis 7) |
| Migrações | `OperationalDbContext`, `MetricsDbContext`, `RagDbContext` aplicadas com `dotnet ef database update` |
| Hosts | `dotnet run` (Development); Api.Agente com `OpenTelemetry__ConsoleExporter=true` |
| OpenAI | **sem chave**: relatório determinístico e embeddings determinísticos |
| Health | `/health/ready` = `Healthy` nos dois hosts |

## Checks

| # | Check | Resultado |
|---|-------|-----------|
| 1 | RAG `POST /ingestions` (`fixtures/manual-ecd-sample.md`) | 202 |
| 2 | Ingestão concluída | `Completed`, 3 chunks |
| 3 | Agente `POST /validations` (`fixtures/ecd-sample.txt`) | 202 |
| 4 | Execução concluída | `Completed`, 2 regras, 0 achados, ~2,2 s |
| 5 | Relatório JSON com `findings`/`limitations`/`skippedValidations` | 200 |
| 6 | SC-007: `manualVersionUsed` ou `limitations` | `ECD-Manual-SMOKE-9b225d7b` |
| 7 | RAG consultado via MCP (sem `RAG_UNAVAILABLE`) | OK |
| 8 | Export CSV | 200 |
| 9 | Histórico paginado contém a execução | 200 |
| 10 | RBAC: outro usuário lê a execução | 403 |
| 11 | Reenvio do mesmo hash | 409 `DUPLICATE_SUBMISSION` |
| 12 | `DELETE` em execução `Completed` | 409 `NOT_CANCELLABLE` |
| 13 | `DELETE` logo após submit | `Queued` → `Cancelled` |
| 14 | Reenvio do arquivo cancelado | 202 |
| 15 | `fixtures/ecd-sample-missing-opening.txt` | `Completed`, 1 achado `High` |
| 16 | Chat pós-validação | 200 com disclaimer |

## Tracing (T059)

Spans exportados no console para cada execução processada, todos com `ecd.execution_id`:

```text
ecd.validation.run
  ecd.phase.LayoutDetection
  ecd.phase.StructuralValidation
  ecd.phase.Consolidation
  ecd.phase.RagRetrieval      (inclui spans HttpClient da chamada MCP)
  ecd.phase.AgentAnalysis
  ecd.phase.Persistence
```

3 execuções processadas resultaram em 3 × 7 spans. A execução cancelada em `Queued` não gerou spans, porque não chegou ao worker.

## Defeitos encontrados e corrigidos durante o smoke

Nenhum desses defeitos aparecia em `dotnet test`. O Agente testava contra um MCP fake, e os testes de integração do RAG saíam cedo sem PostgreSQL.

| # | Defeito | Sintoma | Correção |
|---|---------|---------|----------|
| 1 | Tools MCP do RAG recebiam um objeto `input`; contrato e cliente usam argumentos planos | Toda execução real registrava `RAG_UNAVAILABLE` | `RagMcpTools` com parâmetros planos; `McpContractTests` cobre schema e chamada |
| 2 | Busca híbrida enviava parâmetros nulos sem tipo | `42P08 could not determine data type of parameter` | `NpgsqlDbType.Text` em `layoutFamily`/`manualVersion` |
| 3 | Score lido na coluna 8 (a query só tem índices 0–7) e `DataReader` aberto durante a segunda consulta | `IndexOutOfRange` / `NpgsqlOperationInProgressException` | `GetDouble(7)` e reader fechado antes de carregar manuais |
| 4 | `manualVersion` de teste com 37 caracteres (coluna `varchar(32)`) e factories paralelas congelando o bootstrap logger | Testes RAG falhavam com PostgreSQL ativo | Versões curtas; `RagTestsCollection` compartilha a factory |

Também foi corrigido o `.gitignore`: uploads ECD em `src/Api.Agente.Host/storage/` não estavam ignorados.

## Testes automatizados (mesmo ambiente)

| Projeto | Resultado |
|---------|-----------|
| `Api.Agente.Tests` | 23/23 |
| `AgenteValidacaoECD.IntegrationTests` | 4/4 |
| `Api.Rag.Tests` (com PostgreSQL) | 9/9 |

## Não coberto por este smoke

- LLM real e `LlmUsageRecord` (sem `OpenAI:ApiKey`), portanto SC-004 e P15 não foram exercitados.
- Cancelamento de execução `Running` ponta a ponta. Coberto por teste unitário (`ExecutionCancellationTests`), mas o fixture termina em ~2 s.
- UI Angular (T055–T058).
- `detectedLayoutVersion` sai `LECD` (campo 2 do `0000`) porque o detector não tem fonte normativa de versão de leiaute (D2). Ver [dod-review.md](./dod-review.md).
