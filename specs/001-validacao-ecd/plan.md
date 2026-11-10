# Implementation Plan: AgenteValidacaoECD

**Branch**: `001-validacao-ecd` | **Date**: 2026-10-29 | **Spec**: [spec.md](./spec.md)

**Input**: Agente de IA para validação da ECD (Infis), processamento determinístico + RAG
(Manual ECD) + interpretação OpenAI/SK, API REST, histórico, Angular (incremento), uso interno.

## Summary

Implementar **dois hosts ASP.NET Core (.NET 10)** — **Api.Agente** (orquestração, motor de
regras, LLM, REST, fila async) e **Api.RAG** (ingestão/indexação Manual ECD, PgVector, MCP) —
com **Clean Architecture** por serviço, **PostgreSQL único** (Neon/Supabase) para dados operacionais,
vetores e métricas de tokens, **Redis** para chat, e frontend **Angular** em fase posterior.

Decisões detalhadas: [research.md](./research.md). Modelo de dados: [data-model.md](./data-model.md).
Contratos: [contracts/](./contracts/).

**Nota briefing vs constitution**: runtime **.NET 10** (não .NET 8); persistência em **PostgreSQL** único
em vez do SQL Server do briefing (ADR-001); comunicação Agente↔RAG **somente MCP** em runtime.

## Technical Context

| Item | Valor |
|------|--------|
| **Language/Version** | C# / **.NET 10** |
| **Primary Dependencies** | ASP.NET Core, EF Core 10, **Semantic Kernel**, OpenAI SDK, **ModelContextProtocol**, PgVector.Npgsql, StackExchange.Redis |
| **Storage** | PostgreSQL único — schemas `operational`, `metrics` e `rag`+PgVector (ADR-001); Redis (sessões); filesystem (arquivos ECD) |
| **Testing** | xUnit, WebApplicationFactory, Testcontainers (opcional CI) |
| **Target Platform** | Windows/Linux servidores Infis; Docker dev |
| **Performance Goals** | Parsing streaming ≥ **50k linhas/min** em homolog (ajustável); validação MVP ECD médio ≤ **15 min** SC-001 |
| **Constraints** | Memória: não carregar ECD inteiro; LGPD; FR-023; logs sem segredos/conteúdo integral |
| **Scale/Scope** | Dezenas de usuários internos; arquivos ECD grandes esporádicos |

## Constitution Check

*GATE: passed for planning — revalidar na implementação.*

| Princípio | Plano |
|-----------|--------|
| P1 SDD | spec/plan/data-model/contracts guiam código |
| P3 Determinístico antes LLM | Pipeline: regras → consolidação → RAG → LLM |
| P4 Classificação achados | Taxonomia spec + schema JSON |
| P5 Pipeline 9 etapas | Mapeado na seção Pipeline (14 passos técnicos ⊆ P5) |
| P6 .NET 10 | `net10.0` em todos os projetos |
| P7–P9 Api.Agente / Api.RAG / MCP | Dois hosts; MCP tools [mcp-rag-tools.md](./contracts/mcp-rag-tools.md) |
| P10 Camadas | Domain sem infra/IA |
| P11 RAG proveniência | Chunks + snapshot execução |
| P12 OpenAI + JSON validado | [agent-response-schema.json](./contracts/agent-response-schema.json) |
| P13 SK tools + authz | Plugins SK; autorização app-level |
| P14 Redis memória | ADR D1 resolvido como Redis |
| P15 Consumo PG | `LlmUsageRecord` |
| P16 A2A | Phase 9: servidor A2A v0.3 + contrato SOFIA v1.0 em `Api.Agente.Host` |

**Post-design**: sem violações constitucionais. **Complexity Tracking** (justificativa 2 hosts):

| Decisão | Por quê | Alternativa rejeitada |
|---------|---------|------------------------|
| 2 deployables | Constitution exige RAG isolada + MCP | Monólito único |
| Dual DB | SQL Infis + PG constitution | SQL único com vetores |

## Project Structure

### Documentation (this feature)

```text
specs/001-validacao-ecd/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── api-agente-rest.md
│   ├── api-rag-rest.md
│   ├── mcp-rag-tools.md
│   └── agent-response-schema.json
└── tasks.md              # $speckit-tasks (próximo)
```

### Source Code (a criar na implementação)

```text
AgenteValidacaoECD.sln

src/
├── Api.Agente.Host/                 # REST, auth, Swagger
├── Api.Agente.Application/          # casos de uso, pipeline, SK agent
├── Api.Agente.Domain/               # entidades, IValidationRule, evidências
├── Api.Agente.Infrastructure/       # EF Npgsql, Redis, MCP client, OpenAI, files
├── Api.Rag.Host/                    # REST admin + MCP server
├── Api.Rag.Application/             # ingestão, chunking, embeddings
├── Api.Rag.Domain/
└── Api.Rag.Infrastructure/          # EF Npgsql + PgVector, extratores documento

web/
└── agente-validacao-ecd-ui/         # Angular (P2/P3)

tests/
├── Api.Agente.Tests/
├── Api.Rag.Tests/
└── AgenteValidacaoECD.IntegrationTests/

deploy/
└── docker-compose.dev.yml
```

**Structure Decision**: **Modular monolith split** em dois hosts (não microsserviços adicionais).
Cada host com **Domain → Application → Infrastructure → Host(API)**. Domínio **sem**
referência a SK, OpenAI, EF, MCP.

## Component Design

### Api.Agente.Host

| Responsabilidade | Detalhe |
|------------------|---------|
| REST | [api-agente-rest.md](./contracts/api-agente-rest.md) |
| AuthN/Z | JWT Entra + policies (`Validator`, `Admin`) |
| Upload | Limite tamanho; antivírus opcional P2 |
| Swagger | OpenAPI v1 |
| Health | `/health`, `/health/ready` (SQL, Redis, MCP RAG) |

### Api.Agente.Application — Pipeline (14 passos)

Orquestrador `EcdValidationPipeline` (interface testável):

| # | Fase | Componente | Async |
|---|------|------------|-------|
| 1 | Recebimento | `SubmitValidationHandler` grava arquivo + execução | sync |
| 2 | Integridade formato | `IFileGuard` | sync |
| 3 | Registro DB | `ValidationExecution` `Queued` | sync |
| 4 | Leiaute/período | `ILayoutDetector` | worker |
| 5 | Leitura | `IEcdFileReader` **stream linha a linha** | worker |
| 6 | Interpretação | `IEcdRecordParser` → grafo/blocos | worker |
| 7 | Estrutural | `IRuleEngine.RunStructuralAsync` | worker |
| 8 | Contábil | `IRuleEngine.RunAccountingAsync` | worker |
| 9 | Consolidação | `IResultAggregator` → `RuleRunResult`, `Finding` | worker |
| 10 | RAG | `IRagContextBuilder` via **MCP** `rag.search_manual` | worker |
| 11 | LLM | `IAgentAnalysisService` (SK + schema JSON) | worker |
| 12 | Persistência | relatório + snapshot RAG | worker |
| 13 | API | status `Completed*` | — |
| 14 | Status final | métricas operacionais + audit | worker |

**Worker**: `ValidationJobProcessor` (BackgroundService) lê fila SQL com lock otimista.

**Resiliência**:

- Retry transiente LLM/RAG (política D8 — 2 retries backoff).
- Cancelamento: token por execução.
- Duplicidade: hash SHA256 + usuário + janela 5 min → 409.
- Falha parcial: `CompletedWithGaps` + `skippedValidations`.

**Original**: `StoredFile` imutável; parser **read-only**.

### Motor determinístico (`Api.Agente.Domain` + Application)

```text
IRuleEngine
  → ResolveRules(layoutVersion) via RuleLayoutBinding
  → For each IValidationRule: Evaluate(EcdEvaluationContext)
  → RuleResult(Outcome, Evidence)
```

| Concern | Decisão |
|---------|---------|
| Organização | Uma classe por regra (`EcdStr001MalformedLineRule`, …) registrada DI |
| Contexto | `EcdEvaluationContext` expõe índices por registro, bloco, hierarquia |
| Evidência | `RuleEvidence` serializado JSON |
| Versionamento | Tabela `ValidationRule` + semver; código referencia `ImplementationKey` |
| Testes | Fixture por regra + golden files ECD anonimizados |
| Novas regras | Novo plugin + row catálogo **sem** alterar engine |
| Não verificado | Layout desconhecido, DEP pendente, exceção parser |

**Dependência normativa**: catálogo inicial vazio até **Matriz de Leiautes V1** (D2). Plano
implementa **mecanismo + 2–3 regras exemplo** marcadas `SAMPLE` não normativas.

### Api.Agente — Semantic Kernel

| Peça | Função |
|------|--------|
| `Kernel` | DI scoped por execução |
| Plugins | `ValidationExplainPlugin` (achados já consolidados) |
| MCP | Import tools RAG como SK functions |
| Structured output | Prompt + validação [agent-response-schema.json](./contracts/agent-response-schema.json) |
| Autorização | SK não dispara MCP/LLM sem `IExecutionAuthorization` |

Memória chat: Redis via `IChatSessionStore` separado de RAG.

### Api.Rag — Ingestão (10 passos spec §6)

`ManualIngestionPipeline`:

1. Receber arquivo manual  
2. Criar `ManualDocument` + `IngestionJob`  
3. Extrair texto (`IManualTextExtractor`: `PdfPigManualTextExtractor` para PDF, texto puro para `.txt`/`.md`).
   No PDF, cada página recebe o marcador `[[p. N]]` e linhas de cabeçalho/rodapé repetidas
   (em ≥ metade das páginas, dígitos normalizados) são removidas.  
4. Mapear outline (heurístico): `Capítulo N` → capítulo; `N.x.` com prefixo igual ao capítulo
   corrente → seção; `REGISTRO XXXX:` / `Registro XXXX:` → registro; `## ` → seção (Markdown);
   linhas de sumário (`.....  64`) ignoradas.  
5. Chunking estrutural ([research.md](./research.md)): um trecho por seção/registro; seções
   acima de **2.000 caracteres** são quebradas em fronteira de linha (`(parte n)`), sem overlap.
   `SourceLocation` = `p. X` ou `p. X–Y`. `ChunkKey` ≤ 128 caracteres (caminho truncado + hash).
   Parâmetros sujeitos a revisão no piloto (D7 / R-D7).  
6. Embeddings OpenAI batch (`text-embedding-3-small`, 1536 dims, lote de 16, retry em 429/5xx)  
7. Upsert `DocumentChunk` + vetor + `tsvector`  
8. Validação amostral (contagem, dim embedding)  
9. Ativar versão se solicitado  
10. Registrar histórico; versões antigas `Archived` opcional  

**Atualização**: nova versão não altera regras (FR-039). Análises em andamento usam snapshot
de versão resolvida no início do passo RAG.

**Recuperação**: híbrida vetor + FTS; filtros `layoutVersion` / `manualVersion`.

### Api.Rag — MCP Server

Expor tools [mcp-rag-tools.md](./contracts/mcp-rag-tools.md) no host RAG.

### Frontend Angular (`web/`)

| Módulo | Função |
|--------|--------|
| `upload` | envio ECD |
| `executions` | lista/status |
| `findings` | filtros severidade/registro |
| `admin/manuals` | ingestão/status (role admin) |

Consome apenas **Api.Agente** REST; **não** acessa RAG diretamente.

## Cross-Cutting

| Tema | Abordagem |
|------|-----------|
| Logs | Serilog + `ExecutionId` enricher |
| Métricas | OpenTelemetry traces (pipeline phases) |
| EF Core | Npgsql nos três contextos (operational, metrics, rag) |
| Segurança | HTTPS, secrets, RBAC, auditoria `AuditLog` |
| EF migrations | separadas por contexto |

## Implementation Phases (incremental)

| Fase | Entrega | Spec |
|------|---------|------|
| **0** | Solution skeleton, Docker dev, CI `dotnet test` | — |
| **1** | Api.Agente: upload, fila, parser stream stub, execução sem regras | US1 parcial |
| **2** | Motor regras + catálogo SQL + 2 regras SAMPLE + testes | FR-007–011 |
| **3** | Relatório JSON/export + histórico | FR-016–019, US3 |
| **4** | Api.RAG ingestão + PGvector + MCP search | US4, FR-028–036 |
| **5** | SK + schema LLM + snapshot RAG | US2, FR-012–015, SC-007 |
| **6** | Auth Entra, audit, consumo tokens PG | FR-020–025 |
| **7** | Angular MVP | US5 |
| **8** | Hardening volume, cancelamento, A2A spike D9 | — |

## ADRs a produzir na implementação

| ADR | Tópico |
|-----|--------|
| ADR-001 | PostgreSQL único (Neon/Supabase) — Accepted |
| ADR-002 | Redis vs Mongo (decisão Redis) |
| ADR-003 | Parser ECD — pacote Infis vs nativo (D3) |
| ADR-004 | OpenAI vs Azure OpenAI produção |
| ADR-005 | Armazenamento arquivo (filesystem vs blob) |

## Risks & Mitigations

Ver [research.md](./research.md) R-D2, R-D3, R-D7 e [spec.md](./spec.md) Risks.

## Validation

- Smoke: [quickstart.md](./quickstart.md)
- Matriz spec T-01..T-22 mapeada em `$speckit-tasks`
- Constitution DoD DOD-01..12 no checklist de PR

---

**Next command**: `$speckit-tasks` — decompor fases acima em tarefas ordenadas para `$speckit-implement`.
