# AgenteValidacaoECD

Agente de IA para validação da Escrituração Contábil Digital (ECD) — Infis Consultoria.

Dois serviços ASP.NET Core (.NET 10):

| Serviço | Papel | URL dev |
|---------|-------|---------|
| **Api.Agente** | Upload ECD, pipeline determinístico, explicação LLM, histórico/export, chat | http://localhost:5100 |
| **Api.RAG** | Ingestão do Manual ECD, chunks + PgVector, busca híbrida, servidor MCP | http://localhost:5201 |

A Api.Agente consulta a Api.RAG **somente via MCP** (constitution P7).

## Estrutura

```text
src/
  Api.Agente.{Host,Application,Domain,Infrastructure}
  Api.Rag.{Host,Application,Domain,Infrastructure}
tests/
  Api.Agente.Tests                   unitários + integração in-memory do host Agente
  Api.Rag.Tests                      unitários + integração (exige PostgreSQL)
  AgenteValidacaoECD.IntegrationTests pipeline + explicação com RAG MCP fake
deploy/                              docker-compose dev, script de JWT dev
docs/adr/                            ADR-001..005
fixtures/                            arquivos sintéticos de smoke
specs/001-validacao-ecd/             spec, plan, tasks, contracts, quickstart, evidence
```

## Build e testes

```powershell
dotnet build AgenteValidacaoECD.slnx
dotnet test AgenteValidacaoECD.slnx
```

**NuGet**: `nuget.config` na raiz usa apenas nuget.org. Para feed Infis, configure credenciais localmente.

## Execução local

```powershell
docker compose -f deploy/docker-compose.dev.yml up -d
dotnet run --project src/Api.Rag.Host
dotnet run --project src/Api.Agente.Host
```

Criação do database, migrações, tokens de homologação e roteiro de smoke:
[quickstart](specs/001-validacao-ecd/quickstart.md) e [deploy/README.md](deploy/README.md).

OpenAI é opcional em dev: sem `OpenAI:ApiKey` os hosts usam modo determinístico.

## Endpoints principais (Api.Agente)

| Método | Rota | Uso |
|--------|------|-----|
| POST | `/api/v1/validations` | Upload ECD (`.txt`) e enfileira |
| GET | `/api/v1/validations` | Histórico paginado |
| GET | `/api/v1/validations/{id}` | Status e resumo |
| DELETE | `/api/v1/validations/{id}` | Cancela `Queued`/`Running` |
| GET | `/api/v1/validations/{id}/report` | Relatório JSON |
| GET | `/api/v1/validations/{id}/report/export?format=csv\|xlsx` | Export tabular |
| POST | `/api/v1/validations/{id}/chat` | Perguntas sobre o resultado |

Contratos completos em [`specs/001-validacao-ecd/contracts/`](specs/001-validacao-ecd/contracts/).

## Observabilidade

- Logs Serilog estruturados com `ExecutionId`.
- Traces OpenTelemetry: `ecd.validation.run` e `ecd.phase.*` (source `Api.Agente.Pipeline`), mais ASP.NET Core e HttpClient.
  Configure `OpenTelemetry:OtlpEndpoint` ou `OpenTelemetry:ConsoleExporter=true`.

## Documentação SDD

- Constitution: `.specify/memory/constitution.md`
- Feature: `specs/001-validacao-ecd/` (spec, plan, tasks)
- Guia para agentes: [AGENTS.md](AGENTS.md)
