# Data Model: AgenteValidacaoECD

**Feature**: `001-validacao-ecd`  
**Date**: 2026-10-29  
**Stores**: SQL Server (operacional — Api.Agente), PostgreSQL (RAG + métricas)

## Api.Agente — SQL Server (operacional)

### ValidationExecution

| Campo | Tipo | Notas |
|-------|------|-------|
| Id | `uniqueidentifier` PK | `executionId` público |
| CorrelationId | `nvarchar(64)` | tracing |
| Status | enum | `Queued`, `Running`, `Completed`, `Failed`, `Cancelled` |
| SubmittedByUserId | `nvarchar(128)` | ref Entra |
| OrganizationId | `nvarchar(64)` | nullable |
| OriginalFileId | FK → `StoredFile` | |
| DetectedLayoutVersion | `nvarchar(32)` | nullable |
| DetectedPeriodStart/End | `date` | nullable |
| RecordsAnalyzedCount | `int` | |
| RulesExecutedCount | `int` | |
| FindingsCount | `int` | |
| ManualVersionUsed | `nvarchar(64)` | snapshot RAG FR-035 |
| RagSnapshotJson | `nvarchar(max)` | refs recuperadas |
| LlmResponseSchemaVersion | `nvarchar(16)` | |
| StartedAt / CompletedAt | `datetimeoffset` | |
| ErrorCode / ErrorMessage | nullable | falha técnica |

**Regras**: Não marcar `Completed` como “sem pendências” se existir regra obrigatória `NotVerified` (SC-006).

### StoredFile

| Campo | Tipo | Notas |
|-------|------|-------|
| Id | PK | |
| Sha256 | `char(64)` | integridade |
| OriginalFileName | `nvarchar(512)` | |
| ContentType | `nvarchar(128)` | |
| SizeBytes | `bigint` | |
| StoragePath | `nvarchar(1024)` | imutável após gravação |
| UploadedAt | `datetimeoffset` | |

### ValidationJob (fila async)

| Campo | Tipo | Notas |
|-------|------|-------|
| Id | PK | |
| ExecutionId | FK | |
| Attempt | `int` | idempotência |
| LockedUntil | `datetimeoffset` | worker lock |
| State | enum | `Pending`, `Processing`, `Done`, `DeadLetter` |

### ValidationRule (catálogo)

| Campo | Tipo | Notas |
|-------|------|-------|
| Id | `nvarchar(32)` PK | ex.: `ECD-STR-001` |
| Version | semver | |
| Name, Description | | |
| Category | enum | Structural, Accounting, … |
| SeverityDefault | enum | |
| OfficialReference | `nvarchar(512)` | DEP/link manual |
| ValidFrom / ValidTo | `date` | |
| ImplementationKey | `nvarchar(128)` | DI plugin |
| IsActive | `bit` | |

### RuleLayoutBinding

| RuleId + LayoutVersion | PK composta | regra aplicável ao leiaute |

### RuleRunResult

| Campo | Tipo | Notas |
|-------|------|-------|
| Id | PK | |
| ExecutionId | FK | |
| RuleId, RuleVersion | | |
| Outcome | enum | taxonomia spec |
| EvidenceJson | `nvarchar(max)` | registros, linhas, valores |
| Message | `nvarchar(max)` | |

### Finding (visão consolidada relatório)

| Campo | Tipo | Notas |
|-------|------|-------|
| Id | PK | |
| ExecutionId | FK | |
| Category / Severity | enum | |
| PrimaryRecordType | `nvarchar(10)` | ex. SPED reg |
| LineNumber | `int` nullable | |
| Summary | | |
| Explanation | nullable | IA |
| SourceRuleRunIds | JSON | rastreio |

### AuditLog

| Campo | Tipo | Notas |
|-------|------|-------|
| Id | PK | |
| ExecutionId | nullable | |
| UserId | | |
| Action | `nvarchar(64)` | |
| PayloadJson | | |
| CreatedAt | | |

### User (projeção mínima)

| Campo | Tipo | Notas |
|-------|------|-------|
| Id | PK | Entra object id |
| DisplayName | | |
| Roles | JSON / tabela filha | |

## Api.Agente — PostgreSQL (métricas LLM)

### LlmUsageRecord

| Campo | Tipo | Notas |
|-------|------|-------|
| Id | PK | |
| ExecutionId | UUID | |
| Provider | `text` | OpenAI |
| Model | `text` | |
| PromptTokens / CompletionTokens | `int` | nullable se desconhecido |
| EmbeddingTokens | `int` nullable | |
| EstimatedCost | `decimal` | |
| PriceTableRef | `text` | D11 |
| IsEstimate | `bool` | P15 |
| CreatedAt | `timestamptz` | |

## Api.RAG — PostgreSQL (PgVector)

### ManualDocument

| Campo | Tipo | Notas |
|-------|------|-------|
| Id | PK | |
| SourceUri / FileName | | |
| ManualVersion | `varchar(32)` | versão do documento dentro da série |
| PublicationDate | `date` nullable | |
| LayoutFamily | `varchar(32)` | filtro recuperação |
| DocumentKind | enum | `Manual` (padrão), `Faq`, `Norm` — FR-032 |
| DocumentCode | `varchar(64)` | série do documento (ex.: `MANUAL`, `FAQ-RFB`, `IN-RFB-2003-2021`); padrão `MANUAL` |
| Status | enum | `Draft`, `Active`, `Archived`; no máximo um `Active` por `(LayoutFamily, DocumentCode)` |
| CreatedAt | | |

### IngestionJob

| Campo | Tipo | Notas |
|-------|------|-------|
| Id | PK | |
| ManualDocumentId | FK | |
| Status | enum | FR-036 |
| ErrorLog | `text` nullable | |
| StartedAt / CompletedAt | | |

### DocumentChunk

| Campo | Tipo | Notas |
|-------|------|-------|
| Id | PK | |
| ManualDocumentId | FK | |
| ChunkKey | `varchar(128)` | dedup FR-034 |
| ContentHash | `char(64)` | |
| HierarchyPath | `text` | capítulo/seção/registro |
| SourceLocation | `text` | página/offset se houver |
| Content | `text` | |
| Embedding | `vector(1536)` | |
| Tsv | `tsvector` | busca híbrida |

**Índices**: HNSW/IVFFlat em embedding; GIN em tsv; unique `(ManualDocumentId, ChunkKey)`.

## Redis (memória conversacional)

| Key pattern | Valor | TTL |
|-------------|-------|-----|
| `conv:{orgId}:{userId}:{sessionId}` | lista mensagens serializada | configurável (ex. 7d) |

Separado de chunks oficiais (P14).

## Relacionamentos (resumo)

```text
StoredFile 1──1 ValidationExecution
ValidationExecution 1──* RuleRunResult
ValidationExecution 1──* Finding
ValidationRule *──* LayoutVersion (via RuleLayoutBinding)
ManualDocument 1──* DocumentChunk
ManualDocument 1──* IngestionJob
ValidationExecution ──(snapshot)── ManualDocument version + chunk refs
```

## State — ValidationExecution

```text
Queued → Running → Completed
                 → Failed
Queued → Cancelled
Running → Cancelled (DELETE com Validation:AllowCancelRunning=true)
Running → Failed (timeout/erro)
```

Reprocessamento: nova `ValidationExecution` ou novo `Attempt` em job — **não** sobrescrever relatório auditado sem versão.
