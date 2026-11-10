# Ambiente de desenvolvimento

```powershell
docker compose -f deploy/docker-compose.dev.yml up -d
```

| Serviço | Porta | Uso |
|---------|-------|-----|
| PostgreSQL (pgvector) | 5432 | **operational**, **metrics** (Agente) e **rag** (Api.RAG) — database `ecd` |
| Redis | 6379 | Sessões de chat |

Senhas apenas para **dev local** — não usar em produção.

## Migrações EF

Com Docker em execução:

```powershell
dotnet ef database update --project src/Api.Agente.Infrastructure --startup-project src/Api.Agente.Host --context OperationalDbContext
dotnet ef database update --project src/Api.Agente.Infrastructure --startup-project src/Api.Agente.Host --context MetricsDbContext
dotnet ef database update --project src/Api.Rag.Infrastructure --startup-project src/Api.Rag.Host --context RagDbContext
```

## APIs

```powershell
dotnet run --project src/Api.Agente.Host
dotnet run --project src/Api.Rag.Host
```

- Agente: http://localhost:5100 — `/health/ready`, `/swagger`
- RAG: http://localhost:5201 — `/health/ready`

## Tokens de homologação

```powershell
$agenteToken = ./deploy/scripts/New-DevJwt.ps1 -Service Agente -UserId smoke-user
$ragToken    = ./deploy/scripts/New-DevJwt.ps1 -Service Rag -Roles RagAdmin
```

Chaves DEV-ONLY, iguais às de `appsettings.json`. Roteiro completo no
[quickstart](../specs/001-validacao-ecd/quickstart.md).

## Neon + SOFIA (produção)

[neon-postgres-sofia.md](./neon-postgres-sofia.md) — Postgres único grátis, Vercel e endpoints A2A.

## Vercel (Api.Agente)

[vercel-agente.md](./vercel-agente.md)
