# Contract: Api.Agente — REST (V1)

**Base path**: `/api/v1`  
**Auth**: Bearer JWT (Entra ID) ou `X-Api-Key` (homolog interna)  
**OpenAPI**: documento em `/openapi/v1.json`; UI Scalar em **`/swagger`** (Development ou `OpenApi:ExposeUi=true`)

## POST /validations

Upload ECD e enfileira validação.

**Request**: `multipart/form-data`

| Part | Tipo | Obrigatório |
|------|------|-------------|
| `file` | binary | sim |
| `correlationId` | string | não |

**Response** `202 Accepted`:

```json
{
  "executionId": "uuid",
  "status": "Queued",
  "statusUrl": "/api/v1/validations/{executionId}"
}
```

## GET /validations/{executionId}

Status e resumo.

**Response** `200`:

```json
{
  "executionId": "uuid",
  "status": "Running",
  "submittedAt": "2026-10-29T12:00:00Z",
  "detectedLayoutVersion": "9.0.0|null",
  "detectedPeriod": { "start": "2025-01-01", "end": "2025-12-31" },
  "progress": { "phase": "StructuralValidation", "percent": 45 },
  "summary": {
    "recordsAnalyzed": 12000,
    "rulesExecuted": 80,
    "findingsCount": 12,
    "pendingMandatoryRules": 2
  },
  "manualVersionUsed": "ECD-Manual-2025-03|null",
  "operational": {
    "correlationId": "string",
    "durationMs": 90000,
    "llmUsageSummary": { "promptTokens": 4000, "completionTokens": 800, "estimatedCostUsd": 0.02 }
  }
}
```

Metadados em `operational` **nunca** vêm do LLM (constitution P12).

## GET /validations/{executionId}/report

Relatório completo (JSON).

**Response** `200`: ver [agent-response-schema.json](./agent-response-schema.json) envelope + `findings[]`.

## GET /validations/{executionId}/report/export

Export tabular (`format=xlsx|csv`).

## GET /validations

Histórico paginado (`page`, `pageSize`, filtros `from`, `to`, `status`).

## POST /validations/{executionId}/chat

Pergunta sobre resultados (memória Redis).

**Request**:

```json
{ "sessionId": "uuid|null", "message": "Explique o achado F-003" }
```

**Response** `200`:

```json
{
  "sessionId": "uuid",
  "answer": { "text": "...", "citations": [] },
  "disclaimer": "Orientação profissional; não altera arquivo fonte."
}
```

## DELETE /validations/{executionId}

Cancela se `Queued`/`Running` (política configurável).

- `Queued`: sempre cancelável; o job sai da fila (`ValidationJobState.Cancelled`).
- `Running`: cancelável quando `Validation:AllowCancelRunning=true` (padrão). O worker recebe sinal pelo token da execução e, em multi-instância, interrompe na próxima transição de fase ao ler o status persistido.
- `Completed`/`Failed`/`Cancelled`: `409 NOT_CANCELLABLE`.
- Autorização igual à leitura: dono da execução ou `Admin`.

**Response** `200`:

```json
{
  "executionId": "uuid",
  "previousStatus": "Running",
  "status": "Cancelled",
  "cancelledAt": "2026-11-05T12:00:00Z"
}
```

Execuções `Cancelled` ou `Failed` não contam para `DUPLICATE_SUBMISSION`: o mesmo arquivo pode ser reenviado logo após o cancelamento.

## Errors

| HTTP | Code | Quando |
|------|------|--------|
| 400 | `INVALID_FILE` | formato/tamanho |
| 401/403 | `UNAUTHORIZED` | auth |
| 404 | `NOT_FOUND` | executionId |
| 409 | `DUPLICATE_SUBMISSION` | hash+usuário janela curta |
| 409 | `NOT_CANCELLABLE` | DELETE em execução finalizada |
| 503 | `DEPENDENCY_UNAVAILABLE` | RAG/LLM indisponível |
