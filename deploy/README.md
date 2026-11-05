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

## Banco operacional (SQL Server)

Crie o database `ecd_operational` no SQL Server local (uma vez):

```powershell
docker exec -it deploy-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "Ecd_Dev_Passw0rd!" -C -Q "IF DB_ID('ecd_operational') IS NULL CREATE DATABASE ecd_operational;"
```

(Ajuste o nome do container se `docker ps` mostrar outro.)

## Migrações EF (Phase 2)

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

- Agente: http://localhost:5100 — `/health/ready`
- RAG: http://localhost:5201 — `/health/ready`

## Tokens de homologação

```powershell
$agenteToken = ./deploy/scripts/New-DevJwt.ps1 -Service Agente -UserId smoke-user
$ragToken    = ./deploy/scripts/New-DevJwt.ps1 -Service Rag -Roles RagAdmin
```

Chaves DEV-ONLY, iguais às de `appsettings.json`. Roteiro completo no
[quickstart](../specs/001-validacao-ecd/quickstart.md).
