# Research: AgenteValidacaoECD — Stack, RAG, IA e persistência

**Feature**: `001-validacao-ecd`  
**Date**: 2026-10-29  
**Inputs**: [spec.md](./spec.md), constitution v1.0.0, briefing técnico Infis

## Decisions Summary

| Tópico | Decisão | Rationale | Alternativas consideradas |
|--------|---------|-----------|---------------------------|
| Runtime | **.NET 10** (`net10.0`) | Constitution P6; briefing cita .NET 8 — **supersedido** | .NET 8 LTS (rejeitado: viola constitution) |
| Topologia | **2 hosts**: **Api.Agente** + **Api.RAG** | Constitution P7–P9; fronteira MCP e dados | Monólito único (rejeitado: viola P7); microserviços adicionais (rejeitado: YAGNI) |
| Agente↔RAG | **MCP** (cliente no Agente, servidor na RAG) | Constitution AR-02 | REST direto Agente→RAG (rejeitado); acesso SQL à RAG (proibido AR-01) |
| Orquestração IA | **Semantic Kernel** + **OpenAI** | Constitution P8, P12 | MAF (AgenteFinanceiro usa MAF; ECD spec/constitution cita SK) |
| LLM | **OpenAI** (`gpt-4o` / `gpt-4o-mini` configurável) | Maturidade, structured outputs, política Infis provável | Azure OpenAI (válido se Infis exigir — mesma API surface); modelos locais (custo ops alto V1) |
| Embeddings | **OpenAI** `text-embedding-3-small` (dim **1536**) | Qualidade/custo; alinhamento dimensão PgVector | `text-embedding-3-large`; modelos open-source self-hosted (ADR futuro) |
| Vetores | **PostgreSQL 16+** + **PgVector** (Api.RAG) | Constitution AR-05; não armazenar vetores no SQL Server | SQL Server vector preview; Azure AI Search; Qdrant (ops extra) |
| Dados operacionais | **SQL Server** (Api.Agente) | Padrão Infis no briefing; execuções, regras, arquivos meta, auditoria | PostgreSQL único (menos ops, mas diverge padrão corporativo) |
| Consumo tokens/custos | **PostgreSQL** (schema dedicado no Agente ou DB `AgenteValidacaoECD_Metrics`) | Constitution P15 | Tabela no SQL Server (possível, mas constitution cita PostgreSQL — **preferir PG metrics**) |
| Memória conversacional | **Redis** (TTL, isolamento por sessão) | Constitution D1; simplicidade TTL vs Mongo | MongoDB (documentos ricos; mais ops); in-memory (não produção) |
| Auth interna V1 | **JWT** emitido por IdP corporativo (**Microsoft Entra ID**) ou API Key interna homolog | Spec D4; uso interno Infis | Auth custom só usuário/senha (rejeitado: auditoria fraca) |
| Processamento async | **Fila persistida** (tabela `ValidationJobs` SQL Server) + **BackgroundService** worker in-process | Resiliência sem introduzir broker na V1 | Hangfire (dashboard útil P2); RabbitMQ (complexidade prematura) |
| Armazenamento ECD | **Filesystem** (share/local) + metadados SQL Server; hash SHA-256 | Preservar original FR-003; arquivos grandes | Blob Azure (recomendado produção — ADR quando subir cloud) |
| Parser ECD | **Interface `IEcdFileReader`** + implementação **streaming linha a linha**; adapter para pacote interno Infis (**D3**) | Memória controlada; não inventar layout | Carregar arquivo inteiro (rejeitado); LLM parse (proibido P3) |
| Busca RAG | **Híbrida**: similaridade vetorial (PgVector) + **full-text** PostgreSQL (`tsvector`) | Melhor recall em termos de registro/código SPED | Só vetorial (risco miss em códigos exatos) |
| Chunking manual | **Estrutural primeiro** (capítulo/seção), depois tamanho alvo **~800–1200 tokens** com overlap **10–15%** | Spec FR-029/030; D7 avaliado em piloto | Fixo 512 tokens (sem preservar estrutura) |
| Dedup ingestão | Hash **conteúdo normalizado** por versão manual + `chunk_key` estável | FR-034 | Sem dedup (rejeitado) |
| Frontend | **Angular 19+** (projeto separado `web/`) | Briefing; consome Api.Agente | Blazor (fora briefing); só Swagger V1 (spec permite MVP API-first) |
| Observabilidade | **OpenTelemetry** + Serilog structured | Logs correlacionados `executionId` | Apenas console |
| Testes | **xUnit** + **WebApplicationFactory** (integração API/MCP mock) | Constitution DoD | NUnit (equivalente) |

## OpenAI — justificativa de provedor pago

| Critério | OpenAI | Alternativa local (ex.: Ollama) |
|----------|--------|----------------------------------|
| Structured output | Nativo JSON schema / response format | Variável por modelo |
| Embeddings qualidade PT | Forte em `text-embedding-3-*` | Depende do modelo |
| Custo ops Infis V1 | Baixo (API) | GPU/infra dedicada |
| LGPD / dados contábeis | Exige DPA + política FR-023 | Dados on-prem |

**Decisão**: OpenAI na V1 com **configuração** para migrar endpoint (Azure OpenAI) sem alterar motor de regras.

## SQL Server + PostgreSQL — dual store

| Store | Conteúdo | Serviço |
|-------|----------|---------|
| SQL Server | Usuários (refs Entra), execuções, resultados, regras versionadas, jobs, paths arquivos, auditoria | Api.Agente |
| PostgreSQL (PgVector) | Chunks, embeddings, jobs ingestão, versões manual | Api.RAG |
| PostgreSQL (metrics) | Token usage, custo estimado | Api.Agente |

**Rationale**: Atende briefing Infis (SQL Server operacional) e constitution (vetores + consumo em PG). **Não** replicar vetores no SQL Server.

## MCP — ferramentas RAG (superfície mínima V1)

| Tool MCP | Propósito |
|----------|-----------|
| `rag.search_manual` | Busca híbrida por query + filtros (`manualVersion`, `layoutVersion`) |
| `rag.get_chunk` | Recupera chunk por id com metadados de citação |
| `rag.list_manual_versions` | Lista versões disponíveis / ativa por família de leiaute |

Transporte: HTTP/SSE conforme SDK **ModelContextProtocol** para ASP.NET Core (avaliar pacote oficial na implementação).

## Motor de regras — padrão

| Elemento | Decisão |
|----------|---------|
| Modelo | **Regras como plugins** implementando `IValidationRule` + catálogo em SQL |
| Seleção | Por **versão de leiaute** detectada + tabela `RuleLayoutBinding` |
| Evidência | Objeto `RuleEvidence` (registro, linha, campo, valor encontrado/esperado) |
| Outcome | Enum alinhado à taxonomia spec (Erro, Alerta, …) |
| Versionamento | Semver regra + migração de catálogo; testes fixture por regra |

**Não** codificar regras oficiais ECD no plano — apenas mecanismo + exemplos **DEP-xxx** até Matriz de Leiautes V1.

## Riscos técnicos remanescentes

| ID | Item | Mitigação no plano |
|----|------|-------------------|
| R-D2 | Leiautes ECD V1 indefinidos | Feature flags por layout; regras só após matriz |
| R-D3 | Pacote parsing Infis | Interface + adapter stub até ADR |
| R-D7 | Chunk/topK finais | Piloto ingestão + métricas recall em homologação |

## Referências

- [Semantic Kernel](https://learn.microsoft.com/en-us/semantic-kernel/overview/)
- [Pgvector](https://github.com/pgvector/pgvector)
- [Model Context Protocol](https://modelcontextprotocol.io/)
