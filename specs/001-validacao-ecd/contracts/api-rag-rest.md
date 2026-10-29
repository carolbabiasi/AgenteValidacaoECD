# Contract: Api.RAG — REST (admin + ingestão)

**Base path**: `/api/v1`  
**Auth**: roles `RagAdmin`, `RagReader` (JWT)

## POST /ingestions

Inicia ingestão de nova versão do Manual ECD.

**Request**: `multipart/form-data` ou JSON com URL interna

| Campo | Descrição |
|-------|-----------|
| `file` | PDF/HTML/DOCX conforme suporte plano |
| `manualVersion` | identificador versão |
| `publicationDate` | ISO date |
| `layoutFamily` | família leiaute SPED |
| `activateOnSuccess` | bool |

**Response** `202`:

```json
{ "ingestionJobId": "uuid", "statusUrl": "/api/v1/ingestions/{id}" }
```

## GET /ingestions/{ingestionJobId}

Status, erros, contadores (FR-036).

## GET /manuals

Lista versões (`layoutFamily`, `status`).

## PATCH /manuals/{manualDocumentId}/status

Ativa/arquiva versão (não altera regras determinísticas — FR-039).

## GET /manuals/{manualDocumentId}/chunks/{chunkId}

Consulta admin de chunk (metadados + conteúdo).

**Nota**: Api.Agente **não** chama estes endpoints diretamente para recuperação em runtime — usa **MCP** (ver [mcp-rag-tools.md](./mcp-rag-tools.md)).
