---
description: "Tarefas de implementação — AgenteValidacaoECD (001-validacao-ecd)"
---

# Tasks: AgenteValidacaoECD — Validação ECD

**Input**: [spec.md](./spec.md), [plan.md](./plan.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/), [quickstart.md](./quickstart.md)

**Prerequisites**: Constitution v1.0.0; plano aprovado.

**Tests**: Obrigatórios (spec §8, SC-001..SC-007, matriz T-01..T-22).

## Formato

`- [ ] Tnnn [P?] [USn?] Descrição` — **Depende de** / **Arquivos** / **Conclusão** na descrição quando relevante.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Solution .NET 10, repos, CI local.

- [x] T001 Criar `AgenteValidacaoECD.sln` e projetos `net10.0`: `src/Api.Agente.Host`, `src/Api.Agente.Application`, `src/Api.Agente.Domain`, `src/Api.Agente.Infrastructure`, `src/Api.Rag.Host`, `src/Api.Rag.Application`, `src/Api.Rag.Domain`, `src/Api.Rag.Infrastructure`, `tests/Api.Agente.Tests`, `tests/Api.Rag.Tests`, `tests/AgenteValidacaoECD.IntegrationTests`. **Conclusão**: `dotnet build AgenteValidacaoECD.sln` OK.

- [x] T002 Configurar referências: Application→Domain; Infrastructure→Domain+Application; Host→Application+Infrastructure; Tests conforme plano. **Depende de**: T001.

- [x] T003 [P] Adicionar `src/Directory.Build.props` (nullable, implicit usings, analyzers). **Depende de**: T001.

- [x] T004 [P] Criar `.gitignore`, `.editorconfig`, `nuget.config` raiz; ignorar secrets e `storage/`. **Depende de**: T001.

- [x] T005 [P] Criar `deploy/docker-compose.dev.yml` (SQL Server, PostgreSQL+pgvector, Redis) e README deploy. **Depende de**: T001.

- [x] T006 Configurar URLs dev: Agente `http://localhost:5100`, RAG `http://localhost:5201` em `launchSettings.json`. **Depende de**: T001.

- [x] T007 [P] Documentar ADR-001..005 esqueleto em `docs/adr/` (títulos + status Proposed). **Depende de**: T001.

---

## Phase 2: Foundational (Blocking)

**Purpose**: Infra transversal antes das user stories.

- [x] T008 Implementar `OperationalDbContext` (SQL Server) em `src/Api.Agente.Infrastructure/Persistence/OperationalDbContext.cs` com entidades base `ValidationExecution`, `StoredFile`, `ValidationJob`. **Depende de**: T002. **Conclusão**: migration inicial aplicável.

- [x] T009 [P] Implementar `MetricsDbContext` (PostgreSQL) + `LlmUsageRecord` em `src/Api.Agente.Infrastructure/Persistence/Metrics/`. **Depende de**: T002.

- [x] T010 [P] Implementar `RagDbContext` (Npgsql+PgVector) em `src/Api.Rag.Infrastructure/Persistence/RagDbContext.cs`. **Depende de**: T002.

- [x] T011 Configurar Serilog + enricher `ExecutionId` em ambos Hosts (`Program.cs`). **Depende de**: T002.

- [x] T012 [P] Middleware global de exceções e ProblemDetails em `src/Api.Agente.Host/Middleware/` e RAG equivalente. **Depende de**: T002.

- [x] T013 [P] Health checks SQL/Redis/MCP em Agente; SQL/PG em RAG. **Depende de**: T008, T010.

- [x] T014 Implementar `IFileStorage` filesystem em `src/Api.Agente.Infrastructure/Storage/FileSystemEcdStorage.cs` (path imutável, SHA-256). **Depende de**: T008.

- [x] T015 Stub auth JWT + policy `Validator`/`Admin` em `src/Api.Agente.Host/Auth/` (homolog; D4). **Depende de**: T002.

- [x] T016 Registrar DI extensions: `AddAgenteApplication`, `AddAgenteInfrastructure`, `AddRagApplication`, `AddRagInfrastructure`. **Depende de**: T008–T014.

**Checkpoint**: Foundation ready.

---

## Phase 3: User Story 1 — Validar ECD e relatório (P1) MVP

**Goal**: Upload → fila → pipeline determinístico → relatório JSON (sem IA completa).

**Independent Test**: POST `/api/v1/validations` + GET report (T-01..T-07, T-11, T-14).

### Tests US1

- [x] T017 [P] [US1] Testes integração upload rejeição inválida em `tests/Api.Agente.Tests/Integration/ValidationSubmitTests.cs`. **Depende de**: T016.

- [x] T018 [P] [US1] Testes parser/fixture ECD anonimizado em `tests/Api.Agente.Tests/Unit/EcdLineReaderTests.cs`. **Depende de**: T022.

### Implementation US1

- [x] T019 [P] [US1] Domínio: enums `ValidationStatus`, `RuleOutcome`, entidades `RuleEvidence` em `src/Api.Agente.Domain/`. **Depende de**: T002.

- [x] T020 [P] [US1] Interfaces `IEcdFileReader`, `ILayoutDetector`, `IRuleEngine`, `IValidationPipeline` em `src/Api.Agente.Domain/Abstractions/`. **Depende de**: T019.

- [x] T021 [US1] Implementar `StreamingEcdFileReader` linha a linha em `src/Api.Agente.Infrastructure/Ecd/StreamingEcdFileReader.cs`. **Depende de**: T020.

- [x] T022 [US1] Implementar `BasicLayoutDetector` (metadados conhecidos; indeterminado explícito) em `src/Api.Agente.Application/Ecd/BasicLayoutDetector.cs`. **Depende de**: T021.

- [x] T023 [P] [US1] Catálogo EF: `ValidationRule`, `RuleLayoutBinding`, `RuleRunResult`, `Finding` migrations. **Depende de**: T008, T019.

- [x] T024 [US1] `RuleEngine` + registro DI plugins em `src/Api.Agente.Application/Rules/RuleEngine.cs`. **Depende de**: T020, T023.

- [x] T025 [P] [US1] Duas regras **SAMPLE** (malformed line, missing opening block) em `src/Api.Agente.Application/Rules/Samples/`. **Depende de**: T024.

- [x] T026 [US1] Testes unitários regras SAMPLE em `tests/Api.Agente.Tests/Rules/SampleRulesTests.cs`. **Depende de**: T025.

- [x] T027 [US1] `ValidationJobProcessor` BackgroundService + fila SQL em `src/Api.Agente.Infrastructure/Jobs/`. **Depende de**: T008, T024.

- [x] T028 [US1] `EcdValidationPipeline` (passos 1–9 determinísticos) em `src/Api.Agente.Application/Pipeline/EcdValidationPipeline.cs`. **Depende de**: T021–T027.

- [x] T029 [US1] Endpoints REST: POST/GET validations conforme [api-agente-rest.md](./contracts/api-agente-rest.md) em `src/Api.Agente.Host/Endpoints/ValidationEndpoints.cs`. **Depende de**: T028, T015.

- [x] T030 [US1] Montagem relatório JSON + campos pendências (`skippedValidations`) em `src/Api.Agente.Application/Reports/ValidationReportBuilder.cs`. **Depende de**: T028.

- [x] T031 [US1] Teste integração happy path + violação SAMPLE em `tests/AgenteValidacaoECD.IntegrationTests/ValidationPipelineTests.cs`. **Depende de**: T029, T030.

**Checkpoint**: US1 MVP demonstrável via API.

---

## Phase 4: User Story 4 — Base RAG Manual ECD (P2)

**Goal**: Ingestão, indexação, MCP search (Agente ainda não consome — prepara US2).

**Independent Test**: POST ingestão + MCP `rag.search_manual` (T-19, T-20).

### Tests US4

- [x] T032 [P] [US4] Testes dedup chunk key em `tests/Api.Rag.Tests/Unit/ChunkDeduplicationTests.cs`. **Depende de**: T037.

### Implementation US4

- [x] T033 [P] [US4] Entidades domínio `ManualDocument`, `IngestionJob`, `DocumentChunk` em `src/Api.Rag.Domain/`. **Depende de**: T010.

- [x] T034 [US4] `ManualIngestionPipeline` (10 passos plano) em `src/Api.Rag.Application/Ingestion/ManualIngestionPipeline.cs`. **Depende de**: T033.

- [x] T035 [P] [US4] Extrator PDF/texto stub + interface `IManualTextExtractor` em `src/Api.Rag.Infrastructure/Extractors/`. **Depende de**: T033.

- [x] T036 [US4] Chunking estrutural + hash dedup em `src/Api.Rag.Application/Chunking/StructuralChunker.cs`. **Depende de**: T034.

- [x] T037 [US4] Serviço embeddings OpenAI + upsert PgVector em `src/Api.Rag.Infrastructure/Embeddings/OpenAiEmbeddingService.cs`. **Depende de**: T010, T036.

- [x] T038 [US4] Busca híbrida (vector + tsvector) em `src/Api.Rag.Application/Retrieval/HybridManualSearch.cs`. **Depende de**: T037.

- [x] T039 [US4] REST admin ingestão/status/manuals em `src/Api.Rag.Host/Endpoints/` conforme [api-rag-rest.md](./contracts/api-rag-rest.md). **Depende de**: T034.

- [x] T040 [US4] Servidor MCP tools [mcp-rag-tools.md](./contracts/mcp-rag-tools.md) em `src/Api.Rag.Host/Mcp/RagMcpServerExtensions.cs`. **Depende de**: T038.

- [x] T041 [US4] Teste integração ingestão→search em `tests/Api.Rag.Tests/Integration/IngestionSearchTests.cs`. **Depende de**: T039, T040.

**Checkpoint**: RAG operacional; MCP exposto.

---

## Phase 5: User Story 2 — Agente IA explica achados (P1)

**Goal**: SK + MCP RAG + schema JSON validado + snapshot manual na execução.

**Independent Test**: chat/explicação pós-validação (T-09, T-10, SC-004, SC-007).

### Tests US2

- [x] T042 [P] [US2] Testes validação schema [agent-response-schema.json](./contracts/agent-response-schema.json) em `tests/Api.Agente.Tests/Unit/AgentResponseSchemaValidatorTests.cs`. **Depende de**: T046.

### Implementation US2

- [x] T043 [US2] Cliente MCP RAG em `src/Api.Agente.Infrastructure/Mcp/RagMcpClient.cs` (sem acesso SQL RAG). **Depende de**: T040, T016.

- [x] T044 [US2] `RagContextBuilder` filtra por leiaute/versão manual em `src/Api.Agente.Application/Rag/RagContextBuilder.cs`. **Depende de**: T043, T022.

- [x] T045 [US2] Configurar Semantic Kernel + plugins explicação em `src/Api.Agente.Application/Ai/SemanticKernelAgentExtensions.cs`. **Depende de**: T016.

- [x] T046 [US2] `AgentAnalysisService` structured output + retry limitado em `src/Api.Agente.Application/Ai/AgentAnalysisService.cs`. **Depende de**: T045, T044, T030.

- [x] T047 [US2] Persistir `ManualVersionUsed`, `RagSnapshotJson` na execução. **Depende de**: T046, T008.

- [x] T048 [US2] Integrar passos 10–11 no `EcdValidationPipeline`. **Depende de**: T046, T028.

- [x] T049 [US2] Endpoint POST `/validations/{id}/chat` + Redis `IChatSessionStore` em `src/Api.Agente.Infrastructure/Chat/RedisChatSessionStore.cs`. **Depende de**: T045, T005.

- [x] T050 [US2] Registrar `LlmUsageRecord` (PostgreSQL metrics) por chamada. **Depende de**: T009, T046.

- [x] T051 [US2] Testes integração MCP mock + distinção erro/alerta em `tests/AgenteValidacaoECD.IntegrationTests/AgentExplanationTests.cs`. **Depende de**: T048, T049.

**Checkpoint**: US1 + US2 P1 completos com RAG.

---

## Phase 6: User Story 3 — Histórico e exportação (P2)

**Goal**: Listagem, export CSV/XLSX, RBAC.

**Independent Test**: T-12, T-13, FR-017–018.

- [ ] T052 [P] [US3] GET `/validations` paginado em `ValidationEndpoints.cs`. **Depende de**: T029.

- [ ] T053 [US3] Export tabular em `src/Api.Agente.Application/Reports/ReportExportService.cs`. **Depende de**: T030.

- [ ] T054 [US3] Endpoint GET `.../report/export` + testes autorização em `tests/Api.Agente.Tests/Integration/ReportExportTests.cs`. **Depende de**: T052, T053.

**Checkpoint**: US3 completo.

---

## Phase 7: User Story 5 — Interface Angular (P3)

**Goal**: UI upload, status, findings, admin ingestão (via Agente proxy ou RAG com CORS — definir gateway).

- [ ] T055 Criar app Angular `web/agente-validacao-ecd-ui` (standalone, routing). **Depende de**: T029.

- [ ] T056 [P] [US5] Módulo upload + polling status consumindo API Agente. **Depende de**: T055.

- [ ] T057 [P] [US5] Módulo findings (filtros severidade/registro). **Depende de**: T055, T030.

- [ ] T058 [US5] Tela admin ingestão (chama Api.RAG com token admin). **Depende de**: T039, T055.

**Checkpoint**: US5 demo UI.

---

## Phase 8: Polish & Cross-Cutting

- [ ] T059 [P] OpenTelemetry tracing fases pipeline em `src/Api.Agente.Application/Pipeline/`. **Depende de**: T028.

- [ ] T060 Cancelamento execução DELETE + duplicate hash (409). **Depende de**: T029.

- [ ] T061 [P] README raiz + alinhar [quickstart.md](./quickstart.md) com paths reais. **Depende de**: T001.

- [ ] T062 Executar smoke quickstart e registrar evidências em `specs/001-validacao-ecd/evidence/smoke.md`. **Depende de**: T051, T041.

- [ ] T063 Revisão constitution checklist DOD-01..12 para release interna. **Depende de**: T062.

---

## Dependencies & Execution Order

```text
Phase 1 → Phase 2 → Phase 3 (US1 MVP)
                 → Phase 4 (US4 RAG) → Phase 5 (US2 IA)
                 → Phase 6 (US3)
                 → Phase 7 (US5 UI)
                 → Phase 8
```

**US2** depende funcionalmente de **US4** (MCP). **US1** independe de IA/RAG.

### Parallel examples

- T003, T004, T005, T007 após T001
- T017, T018 após infra US1
- T033, T035 paralelo; T056, T057 após T055

## MVP scope

**MVP mínimo**: Phase 1 + 2 + 3 (T001–T031) — validação determinística e relatório API.

**MVP produto P1 spec**: + Phase 4 + 5 (T032–T051).

## Implementation strategy

1. Entregar T001–T031 e validar com fixtures.
2. RAG (T032–T041) antes de IA.
3. IA + chat (T042–T051).
4. Histórico/export e Angular.
5. Polish + smoke T062.

**Next command**: `$speckit-implement` — executar tarefas **uma a uma** na ordem acima.
