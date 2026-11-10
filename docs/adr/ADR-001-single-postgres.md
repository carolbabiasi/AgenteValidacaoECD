# ADR-001: PostgreSQL único (Neon/Supabase)

**Status**: Accepted — 2026-11-10 (supersede a proposta original de dual database SQL Server + PostgreSQL)

## Contexto

- A constitution exige **PostgreSQL + PgVector** para vetores (AR-05) e **PostgreSQL** para
  consumo/custos de LLM (AR-06, P15). Ela **não** exige SQL Server em nenhum ponto.
- O briefing Infis citava SQL Server como padrão corporativo para os dados operacionais do Agente,
  o que levava a dois motores de banco (dual store) e, na nuvem, a duas infraestruturas pagas.
- O projeto precisa subir em ambiente **gratuito** (Vercel + Postgres managed free tier), onde não
  existe SQL Server grátis com persistência utilizável.

## Decisão

Usar **um único PostgreSQL** para toda a persistência relacional, separado por **schema**:

| Schema | Conteúdo | Serviço |
|--------|----------|---------|
| `operational` | `StoredFiles`, `ValidationExecutions`, `ValidationJobs`, `ValidationRules`, `RuleLayoutBindings`, `RuleRunResults`, `Findings` | Api.Agente |
| `metrics` | `LlmUsageRecords` (tokens, custo estimado) | Api.Agente |
| `rag` | `ManualDocuments`, `IngestionJobs`, `DocumentChunks` (`vector(1536)`) | Api.RAG |

- Provider EF Core: **Npgsql** nos três contextos (`OperationalDbContext`, `MetricsDbContext`, `RagDbContext`).
- Histórico de migrations isolado por contexto (`__OperationalMigrationsHistory`, `__MetricsMigrationsHistory`).
- A fronteira AR-01/P7 continua válida: o Agente **não** lê o schema `rag` por SQL; o acesso é
  exclusivamente via MCP. O compartilhamento de instância é de infraestrutura, não de contrato.
- Hospedagem gratuita: **Neon** ou **Supabase** (free tier, `CREATE EXTENSION vector`), com
  `SSL Mode=Require`. Dev local continua no `pgvector/pgvector:pg16` do docker-compose.

## Consequências

**Positivas**
- Um único banco para provisionar, migrar e monitorar; roda inteiro no free tier.
- Elimina 2PC/consistência eventual entre dois motores em operações que tocam execução + métricas.
- PgVector e full-text (`tsvector`) no mesmo lugar, sem réplica de vetores.

**Negativas / riscos**
- Diverge do padrão corporativo SQL Server citado no briefing — se a Infis exigir SQL Server em
  produção, o `OperationalDbContext` volta a `UseSqlServer` com migrations próprias (o modelo EF é
  provider-agnóstico; o impacto fica em migrations e tipos de coluna).
- Free tiers suspendem a instância por inatividade (cold start) e têm limite de storage/conexões —
  usar pooling e tolerar a primeira requisição lenta.
- Isolamento de carga entre RAG e operacional depende de uma instância só; se virar gargalo,
  separar em dois projetos Postgres (sem mudança de código, só connection strings).

## Alternativas consideradas

| Alternativa | Por que não |
|-------------|-------------|
| Dual store SQL Server + PostgreSQL (proposta original) | Sem free tier viável para SQL Server; duplica ops e migrations |
| Azure SQL Serverless | Free tier limitado e com pausa agressiva; custo após o período |
| SQLite para operacional | Sem concorrência adequada para fila/jobs; sem PgVector |
| Dois projetos Postgres (um por serviço) | Possível depois; hoje consome dois free tiers sem ganho |

## Referências

- `deploy/neon-postgres-sofia.md` — bootstrap Neon/Supabase + Vercel
- `deploy/scripts/Invoke-SupabaseMigrations.ps1` — aplica os três contextos
- Migration `20261105161111_InitialOperationalPostgres`
