# Quickstart: Validação local (homologação)

**Pré-requisito**: implementação conforme [plan.md](./plan.md) — este guia valida a feature
end-to-end após `$speckit-implement`.

## Pré-requisitos

- .NET **10** SDK
- Docker Desktop (SQL Server, PostgreSQL+PgVector, Redis)
- Node.js LTS (Angular — fase P3)
- OpenAI API key (user-secrets / env)
- Arquivo ECD de teste **sem dados reais de cliente** (fixture interna)

## Subir dependências (exemplo Docker Compose)

```powershell
docker compose -f deploy/docker-compose.dev.yml up -d
```

Serviços esperados:

| Serviço | Porta | Uso |
|---------|-------|-----|
| SQL Server | 1433 | Api.Agente operacional |
| PostgreSQL | 5432 | Api.RAG + métricas |
| Redis | 6379 | memória chat |

## Configuração

```powershell
cd src/Api.Agente.Host
dotnet user-secrets set "OpenAI:ApiKey" "<key>"
dotnet user-secrets set "ConnectionStrings:Operational" "Server=localhost,1433;..."
dotnet user-secrets set "ConnectionStrings:Metrics" "Host=localhost;Database=ecd_metrics;..."
dotnet user-secrets set "Mcp:Rag:BaseUrl" "http://localhost:5201"
```

Api.RAG:

```powershell
cd src/Api.Rag.Host
dotnet user-secrets set "ConnectionStrings:Rag" "Host=localhost;Database=ecd_rag;..."
```

## Migrações

```powershell
dotnet ef database update --project src/Api.Agente.Infrastructure
dotnet ef database update --project src/Api.Rag.Infrastructure
```

## Executar hosts

```powershell
# Terminal 1
dotnet run --project src/Api.Rag.Host --urls http://localhost:5201

# Terminal 2
dotnet run --project src/Api.Agente.Host --urls http://localhost:5100
```

## Swagger / OpenAPI (homolog)

Com `ASPNETCORE_ENVIRONMENT=Development`:

| Host | UI | JSON |
|------|-----|------|
| Api.Agente | http://localhost:5100/swagger | http://localhost:5100/openapi/v1.json |
| Api.RAG | http://localhost:5201/swagger | http://localhost:5201/openapi/v1.json |

Na UI, informe **Bearer JWT** (homolog: token emitido com issuer `AgenteValidacaoECD-Dev` / roles do contrato).

Para expor a UI fora de Development, defina `"OpenApi": { "ExposeUi": true }` em `appsettings` (evitar em produção pública).

## Smoke — ingestão manual (admin)

```powershell
curl -X POST "http://localhost:5201/api/v1/ingestions" `
  -H "Authorization: Bearer <token>" `
  -F "file=@./fixtures/manual-ecd-sample.pdf" `
  -F "manualVersion=ECD-Manual-DEV-1" `
  -F "layoutFamily=ECD" `
  -F "activateOnSuccess=true"
```

Aguardar `GET /api/v1/ingestions/{id}` → `Completed`.

## Smoke — validação ECD

```powershell
curl -X POST "http://localhost:5100/api/v1/validations" `
  -H "Authorization: Bearer <token>" `
  -F "file=@./fixtures/ecd-sample.txt"
```

Poll:

```powershell
curl "http://localhost:5100/api/v1/validations/{executionId}" -H "Authorization: Bearer <token>"
```

Relatório:

```powershell
curl "http://localhost:5100/api/v1/validations/{executionId}/report" -H "Authorization: Bearer <token>"
```

## Resultados esperados (aceite smoke)

1. `executionId` retornado; status evolui até `Completed` ou `CompletedWithGaps`.
2. Relatório contém `rulesExecuted`, `findings[]`, `skippedValidations` quando aplicável.
3. `manualVersionUsed` e `ragReferences` presentes **ou** `limitations` explícitas (SC-007).
4. Arquivo original consultável via hash/path auditável.
5. Nenhum achado **Erro** sem `ruleId` associado a regra determinística (SC-003 amostra).

## Testes automatizados

```powershell
dotnet test AgenteValidacaoECD.sln
```

Prioridade: testes de regras (`tests/Api.Agente.Tests/Rules/`), integração pipeline com RAG mock MCP.

## Troubleshooting

| Sintoma | Ação |
|---------|------|
| RAG vazio | Verificar ingestão e MCP URL |
| LLM invalid JSON | Ver logs validação schema; retry policy D8 |
| Layout desconhecido | Esperado até Matriz Leiautes V1; regras `NotVerified` |

## Referências

- [spec.md](./spec.md) — FR/SC
- [contracts/](./contracts/) — REST/MCP/JSON schema
- [data-model.md](./data-model.md) — entidades
