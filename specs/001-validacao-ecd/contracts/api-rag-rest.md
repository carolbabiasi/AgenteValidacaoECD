# Contract: Api.RAG — REST (admin + ingestão)

**Base path**: `/api/v1`  
**Auth**: roles `RagAdmin`, `RagReader` (JWT)  
**OpenAPI**: `/openapi/v1.json`; UI **`/swagger`** (Development ou `OpenApi:ExposeUi=true`)

## POST /ingestions

Inicia ingestão de nova versão do Manual ECD.

**Request**: `multipart/form-data` ou JSON com URL interna

| Campo | Descrição |
|-------|-----------|
| `file` | `.pdf` (manual oficial; página vira `sourceLocation`), `.txt` ou `.md` |
| `manualVersion` | identificador versão (≤ 32 caracteres) |
| `publicationDate` | ISO date |
| `layoutFamily` | família leiaute SPED (≤ 32 caracteres) |
| `documentKind` | `Manual` (padrão), `Faq` ou `Norm` |
| `documentCode` | série do documento (≤ 64 caracteres, normalizada em maiúsculas); padrão `MANUAL` quando `documentKind=Manual`, obrigatório nos demais tipos |
| `activateOnSuccess` | bool; arquiva somente a versão ativa anterior da mesma `(layoutFamily, documentCode)` |

**Response** `202`:

```json
{ "ingestionJobId": "uuid", "statusUrl": "/api/v1/ingestions/{id}" }
```

## GET /ingestions/{ingestionJobId}

Status, erros, contadores (FR-036).

## GET /manuals

Lista versões (`layoutFamily`, `status`, `documentKind`). Cada item traz `documentKind` e `documentCode`.

## PATCH /manuals/{manualDocumentId}/status

Ativa/arquiva versão (não altera regras determinísticas — FR-039). Ativar arquiva apenas a versão ativa da mesma série.

## GET /manuals/{manualDocumentId}/chunks/{chunkId}

Consulta admin de chunk (metadados + conteúdo).

**Nota**: Api.Agente **não** chama estes endpoints diretamente para recuperação em runtime — usa **MCP** (ver [mcp-rag-tools.md](./mcp-rag-tools.md)).
