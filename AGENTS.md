# AgenteValidacaoECD — guia para agentes

## Spec Kit (SDD)

- Constitution: `.specify/memory/constitution.md`
- Feature ativa: `specs/001-validacao-ecd/` (`tasks.md` é a fila de implementação)
- Skills Spec Kit: `.agents/skills/speckit-*`

## Skills de domínio (`skills/`)

| Skill | Quando usar |
|-------|-------------|
| `rag-pgvector` | Api.RAG, ingestão, PgVector, T032+ |
| `controle-custos-llm` | `MetricsDbContext`, `LlmUsageRecord`, T050 |
| `mcp-client-server` | Integração Agente ↔ RAG, T040+ |
| `semantic-kernel-tools` | Plugins SK, T045+ |
| `testes-agentes-ia` | xUnit, integração, gates de qualidade |
| `memoria-mongodb` | Só após ADR-002 (hoje o plano favorece Redis) |
| `sofia-a2a-contrato` | DTOs `SolicitacaoAnaliseSofia` / `ResultadoAnaliseSofia` v1.0 |
| `sofia-a2a-implementacao` | Agent Card, JSON-RPC `message/send`, Phase 9 / D9 |
| `sofia-cors-rest-a2a` | CORS `SofiaBrowser` (browser); backend SOFIA sem CORS |

## Comandos

```powershell
dotnet build AgenteValidacaoECD.slnx
dotnet test AgenteValidacaoECD.slnx
docker compose -f deploy/docker-compose.dev.yml up -d
```
