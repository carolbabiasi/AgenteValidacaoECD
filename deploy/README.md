# Ambiente de desenvolvimento

```powershell
docker compose -f deploy/docker-compose.dev.yml up -d
```

| Serviço | Porta | Uso |
|---------|-------|-----|
| SQL Server | 1433 | Api.Agente operacional |
| PostgreSQL (pgvector) | 5432 | Api.RAG + métricas LLM |
| Redis | 6379 | Sessões de chat |

Senhas apenas para **dev local** — não usar em produção.
