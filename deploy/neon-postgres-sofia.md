# Neon/Supabase (Postgres único) + SOFIA

A partir deste refactor, **todo** persistência relacional do Agente usa **PostgreSQL** (schema `operational` + `metrics`). O RAG usa o **mesmo database** (schema `rag` + extensão `vector`).

## 1. Criar Postgres grátis (Neon ou Supabase)

1. Crie um projeto/database (ex.: `ecd`).
2. No SQL do console, execute:
   ```sql
   CREATE EXTENSION IF NOT EXISTS vector;
   ```
3. Copie a connection string **com SSL** (Neon já inclui `sslmode=require`).
   No Supabase use a string do **Session pooler** (porta 5432, usuário `postgres.<project-ref>`):
   a conexão *Direct* é IPv6 por padrão (IPv4 só com add-on pago) e o *Transaction pooler*
   (6543) conflita com os prepared statements do Npgsql.

Exemplo (substitua pelos valores reais):

```text
Host=ep-xxx.us-east-1.aws.neon.tech;Database=ecd;Username=ecd_owner;Password=***;SSL Mode=Require
```

## 2. Migrations + Vercel (automatizado)

No Supabase SQL Editor (uma vez):

```sql
CREATE EXTENSION IF NOT EXISTS vector;
```

Copie `deploy/local/supabase.env.example` → `deploy/local/supabase.env` e preencha `SUPABASE_DATABASE_URL`.

```powershell
./deploy/scripts/Invoke-SofiaCloudBootstrap.ps1
```

Só migrations (sem Vercel):

```powershell
./deploy/scripts/Invoke-SupabaseMigrations.ps1
```

Schemas criados: `operational`, `metrics`, `rag`.

## 3. Api.RAG na nuvem

O SOFIA precisa do Agente; o Agente precisa do **MCP RAG**. Segundo projeto Vercel (container):

```powershell
# na raiz do repo, novo projeto Vercel
copy deploy/Dockerfile.rag.vercel Dockerfile
copy deploy/vercel-rag.json vercel.json
vercel link   # nome sugerido: infis-agente-validacao-ecd-rag
# Production: ConnectionStrings__Rag (mesma do Supabase), Auth__Jwt__SigningKey (= Rag__Jwt__SigningKey do Agente)
vercel deploy --prod
./deploy/scripts/Invoke-RagIngestDocs.ps1 -BaseUrl https://<rag>.vercel.app
```

Atualize `RAG_BASE_URL` em `deploy/local/supabase.env` e rode de novo o bootstrap (ou `vercel env add Rag__BaseUrl` no projeto Agente).

## 4. Vercel — Api.Agente (SOFIA)

**Environment Variables (Production):**

| Variável | Valor |
|----------|--------|
| `ConnectionStrings__Operational` | Neon (operacional) |
| `ConnectionStrings__Metrics` | Neon (métricas) |
| `Rag__BaseUrl` | URL pública do Api.RAG |
| `Rag__Jwt__SigningKey` | Igual ao RAG |
| `Auth__Jwt__SigningKey` | JWT do Agente |
| `Sofia__PublicBaseUrl` | `https://infis-agente-validacao-ecd.vercel.app` |
| `Sofia__A2aServiceKey` | Segredo SOFIA → header `X-Sofia-Service-Key` |
| `OpenAI__ApiKey` | Se usar LLM em prod |

Redis: use Upstash Redis free e `ConnectionStrings__Redis`.

**Expor para SOFIA (backend):**

- Agent Card: `GET https://<agente>/.well-known/agent-card.json`
- A2A: `POST https://<agente>/a2a` (`message/send`, JSON SOFIA v1.0 no TextPart)
- Auth: `X-Sofia-Service-Key` ou JWT `Validator`

**Deployment Protection:** em Project Settings, desative proteção em **Production** (ou configure bypass) para a SOFIA alcançar `/a2a` sem login Vercel.

**Browser UI SOFIA (opcional):** `Cors__AllowedOrigins__0` = origem da UI.

## 5. Smoke SOFIA

```powershell
Invoke-RestMethod "https://<agente>/.well-known/agent-card.json"
# POST /a2a com envelope em specs/001-validacao-ecd/evidence/sofia-a2a-deploy.md
```

## 6. Dev local (sem Neon)

```powershell
docker compose -f deploy/docker-compose.dev.yml up -d
# migrations com strings localhost do appsettings.json
dotnet run --project src/Api.Rag.Host
dotnet run --project src/Api.Agente.Host
```
