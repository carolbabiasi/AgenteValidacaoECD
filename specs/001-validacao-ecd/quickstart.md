# Quickstart: Validação local (homologação)

Guia de validação end-to-end da feature conforme [plan.md](./plan.md). Todos os comandos
partem da **raiz do repositório** em PowerShell. Evidências da última execução:
[evidence/smoke.md](./evidence/smoke.md).

## Pré-requisitos

- .NET **10** SDK (`dotnet --version` ≥ 10.0)
- Docker Desktop **em execução** (SQL Server, PostgreSQL+PgVector, Redis)
- `dotnet-ef` (`dotnet tool install --global dotnet-ef`)
- OpenAI API key **opcional**: sem chave, Api.Agente gera o relatório em modo determinístico
  e Api.RAG usa embeddings determinísticos (adequado para smoke, não para avaliar recall)
- Node.js LTS apenas para a UI Angular (Phase 7, ainda não implementada)

Fixtures sintéticas, sem dados de cliente, ficam em [`fixtures/`](../../fixtures/README.md).

## 1. Subir dependências

```powershell
docker compose -f deploy/docker-compose.dev.yml up -d
```

| Serviço | Porta | Uso |
|---------|-------|-----|
| SQL Server | 1433 | Api.Agente operacional (`ecd_operational`) |
| PostgreSQL + pgvector | 5432 | Api.RAG (`ecd_rag`) e métricas LLM da Api.Agente |
| Redis | 6379 | memória de chat |

Crie o database operacional uma única vez (ajuste o nome do container conforme `docker ps`):

```powershell
docker exec deploy-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "Ecd_Dev_Passw0rd!" -C `
  -Q "IF DB_ID('ecd_operational') IS NULL CREATE DATABASE ecd_operational;"
```

## 2. Configuração

As connection strings de dev já estão em `src/*/appsettings.json` e apontam para o compose.
Só configure segredos se quiser LLM/embeddings reais:

```powershell
dotnet user-secrets --project src/Api.Agente.Host set "OpenAI:ApiKey" "<key>"
dotnet user-secrets --project src/Api.Rag.Host set "OpenAI:ApiKey" "<key>"
```

| Chave | Host | Padrão dev | Uso |
|-------|------|-----------|-----|
| `ConnectionStrings:Operational` | Agente | SQL Server `ecd_operational` | execuções, regras, achados |
| `ConnectionStrings:Metrics` | Agente | PostgreSQL `ecd_rag` | `LlmUsageRecord` (schema `metrics`) |
| `ConnectionStrings:Redis` | Agente | `localhost:6379` | chat |
| `Rag:BaseUrl` / `Rag:McpPath` | Agente | `http://localhost:5201` / `/mcp` | cliente MCP |
| `Validation:DuplicateWindowMinutes` | Agente | `5` | janela de `DUPLICATE_SUBMISSION` |
| `Validation:AllowCancelRunning` | Agente | `true` | DELETE em execução `Running` |
| `OpenTelemetry:OtlpEndpoint` | Agente | vazio | exporta traces OTLP (ex.: `http://localhost:4317`) |
| `OpenTelemetry:ConsoleExporter` | Agente | `false` | imprime spans no console |
| `ConnectionStrings:Rag` | RAG | PostgreSQL `ecd_rag` | manuais, chunks, vetores |
| `OpenAI:ChatModel` / `OpenAI:EmbeddingModel` | ambos | `gpt-4o-mini` / `text-embedding-3-small` | modelos |

## 3. Migrações

```powershell
dotnet ef database update --project src/Api.Agente.Infrastructure --startup-project src/Api.Agente.Host --context OperationalDbContext
dotnet ef database update --project src/Api.Agente.Infrastructure --startup-project src/Api.Agente.Host --context MetricsDbContext
dotnet ef database update --project src/Api.Rag.Infrastructure --startup-project src/Api.Rag.Host --context RagDbContext
```

## 4. Executar hosts

```powershell
# Terminal 1
dotnet run --project src/Api.Rag.Host

# Terminal 2
dotnet run --project src/Api.Agente.Host
```

Os perfis de `launchSettings.json` usam `Development` e as portas abaixo.

| Host | Base | Health | OpenAPI UI |
|------|------|--------|-----------|
| Api.Agente | http://localhost:5100 | `/health/ready` | http://localhost:5100/swagger |
| Api.RAG | http://localhost:5201 | `/health/ready` | http://localhost:5201/swagger |

Para expor a UI fora de Development, defina `"OpenApi": { "ExposeUi": true }` (evitar em produção pública).

## 5. Tokens de homologação

```powershell
$agenteToken = ./deploy/scripts/New-DevJwt.ps1 -Service Agente -UserId smoke-user
$ragToken    = ./deploy/scripts/New-DevJwt.ps1 -Service Rag -Roles RagAdmin
```

Roles: Api.Agente aceita `Validator` e `Admin`; Api.RAG aceita `RagAdmin` e `RagReader`.
Chaves são **DEV-ONLY** (ver `appsettings.json`).

> No Windows PowerShell, `curl` é alias de `Invoke-WebRequest`; use `curl.exe`.

## 6. Smoke — ingestão manual (admin RAG)

```powershell
curl.exe -s -X POST "http://localhost:5201/api/v1/ingestions" `
  -H "Authorization: Bearer $ragToken" `
  -F "file=@fixtures/manual-ecd-sample.md" `
  -F "manualVersion=ECD-Manual-DEV-1" `
  -F "layoutFamily=ECD" `
  -F "activateOnSuccess=true"

curl.exe -s "http://localhost:5201/api/v1/ingestions/{ingestionJobId}" -H "Authorization: Bearer $ragToken"
```

Aguardar `Completed`. Para o corpus oficial completo (manual, FAQ, IN e ITG), com host em
`http://localhost:5201` e `OpenAI:ApiKey` em Development:

```powershell
./deploy/scripts/Invoke-RagIngestDocs.ps1
```

Ingestão unitária: use `documentKind` (`Manual`, `Faq`, `Norm`) e `documentCode` (série, ex. `FAQ-RFB`).
Os trechos do PDF citam página (`p. 64–65`).

## 7. Smoke — validação ECD

```powershell
curl.exe -s -X POST "http://localhost:5100/api/v1/validations" `
  -H "Authorization: Bearer $agenteToken" `
  -F "file=@fixtures/ecd-sample.txt"

curl.exe -s "http://localhost:5100/api/v1/validations/{executionId}" -H "Authorization: Bearer $agenteToken"
curl.exe -s "http://localhost:5100/api/v1/validations/{executionId}/report" -H "Authorization: Bearer $agenteToken"
curl.exe -s -o report.csv "http://localhost:5100/api/v1/validations/{executionId}/report/export?format=csv" -H "Authorization: Bearer $agenteToken"
curl.exe -s "http://localhost:5100/api/v1/validations?page=1&pageSize=20" -H "Authorization: Bearer $agenteToken"
```

Reenviar o mesmo arquivo em até 5 minutos retorna `409 DUPLICATE_SUBMISSION`.

## 8. Smoke — cancelamento

```powershell
curl.exe -s -X DELETE "http://localhost:5100/api/v1/validations/{executionId}" -H "Authorization: Bearer $agenteToken"
```

`Queued`/`Running` → `200` com `status: Cancelled`; execução finalizada → `409 NOT_CANCELLABLE`.

## Resultados esperados (aceite smoke)

1. `executionId` retornado; status evolui até `Completed`. Validações não realizadas aparecem
   em `skippedValidations` do relatório (não há status `CompletedWithGaps` na V1).
2. Relatório contém `findings[]`, `limitations[]` e `skippedValidations[]`; o status traz `summary.rulesExecuted`.
3. `manualVersionUsed` e referências RAG presentes **ou** `limitations` explícitas (SC-007).
4. Arquivo original auditável por SHA-256 em `src/Api.Agente.Host/storage/ecd-uploads` (`Storage:RootPath`
   relativo ao content root; ignorado pelo git) e registrado em `StoredFile`.
5. Nenhum achado **Erro** sem regra determinística associada (SC-003 amostra).
6. Spans `ecd.validation.run` e `ecd.phase.*` com tag `ecd.execution_id` (com `ConsoleExporter=true` ou OTLP).

## Testes automatizados

```powershell
dotnet test AgenteValidacaoECD.slnx
```

Prioridade: regras (`tests/Api.Agente.Tests/Rules/`), pipeline com RAG MCP fake
(`tests/AgenteValidacaoECD.IntegrationTests/`). Os testes de integração de `tests/Api.Rag.Tests`
saem sem asserção quando o PostgreSQL não está acessível: rode-os com o compose ativo.

## Troubleshooting

| Sintoma | Ação |
|---------|------|
| `failed to connect to the docker API` | Iniciar Docker Desktop antes do compose |
| `Cannot open database "ecd_operational"` | Executar o passo de criação do database (seção 1) |
| `401` nas APIs | Token do serviço errado (Agente ≠ RAG) ou expirado; gere de novo |
| RAG vazio / `limitations` com RAG indisponível | Verificar ingestão `Completed` e `Rag:BaseUrl` |
| LLM invalid JSON | Ver logs de validação de schema; retry limitado (D8) |
| Layout desconhecido | Esperado até Matriz de Leiautes V1; regras `NotVerified` |

## Referências

- [spec.md](./spec.md) — FR/SC
- [contracts/](./contracts/) — REST/MCP/JSON schema
- [data-model.md](./data-model.md) — entidades
- [deploy/README.md](../../deploy/README.md) — ambiente dev
