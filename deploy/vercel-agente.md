# Deploy Api.Agente na Vercel (container)

## Arquivos

| Arquivo | Função |
|---------|--------|
| `Dockerfile.vercel` | Build/publish `Api.Agente.Host` |
| `vercel.json` | Projeto container (região `gru1`) |
| `appsettings.Production.json` | **`OpenApi:ExposeUi: true`** → `/swagger` e `/openapi/v1.json` |

## Variáveis (Production)

Cadastre em **Project Settings → Environment Variables** (nunca commitar):

| Variável | Descrição |
|----------|-----------|
| `ConnectionStrings__Operational` | PostgreSQL (schema `operational`) — Neon/Supabase |
| `ConnectionStrings__Metrics` | Mesmo Postgres (schema `metrics`) |
| `ConnectionStrings__Redis` | Redis gerenciado (chat) |
| `Rag__BaseUrl` | URL pública do **Api.RAG** (MCP) |
| `Rag__Jwt__SigningKey` | Chave JWT serviço → RAG |
| `Auth__Jwt__SigningKey` | JWT homolog/prod do Agente |
| `OpenAI__ApiKey` | Opcional (LLM em prod) |
| `Sofia__PublicBaseUrl` | URL Vercel do Agente (Agent Card) |
| `SOFIA_A2A_TOKEN` | Bearer compartilhado SOFIA → Agent Card + `/a2a` |
| `Cors__AllowedOrigins__0` | UI browser SOFIA (se aplicável) |

`PORT` e `VERCEL` são injetados pela plataforma.

## Limitações

- **Api.RAG** não entra neste container — hospede RAG à parte (segundo projeto Vercel, Fly.io, etc.) e aponte `Rag__BaseUrl`.
- Uploads em `Storage:RootPath` são **efêmeros** (`/tmp`); validações longas podem estourar timeout serverless.
- Jobs em background rodam no mesmo processo; cold start reinicia fila pendente no SQL.

## Comandos

```powershell
npm install -g vercel
vercel login
vercel link
vercel env ls
vercel deploy          # preview
vercel deploy --prod   # produção
```

## Smoke pós-deploy

- `GET /` → 200
- `GET /swagger` → UI Scalar
- `GET /openapi/v1.json` → OpenAPI
- `GET /.well-known/agent-card.json` → card SOFIA (ajustar `Sofia__PublicBaseUrl`)
