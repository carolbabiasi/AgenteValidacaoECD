# Contract: Api.RAG — MCP Tools

**Servidor MCP**: hospedado pela Api.RAG  
**Cliente**: Api.Agente (Semantic Kernel plugins via MCP connector)

## rag.search_manual

**Input**:

```json
{
  "query": "string",
  "layoutVersion": "string|null",
  "manualVersion": "string|null",
  "topK": 8,
  "minScore": 0.0
}
```

**Output**:

```json
{
  "results": [
    {
      "chunkId": "uuid",
      "manualVersion": "ECD-Manual-2025-03",
      "hierarchyPath": "Cap 3 > Registro I050",
      "sourceLocation": "p. 142",
      "excerpt": "texto...",
      "score": 0.87
    }
  ],
  "retrievalMode": "hybrid"
}
```

## rag.get_chunk

**Input**: `{ "chunkId": "uuid" }`  
**Output**: chunk completo + metadados para citação.

## rag.list_manual_versions

**Input**: `{ "layoutFamily": "string|null", "activeOnly": true }`  
**Output**: lista `{ manualVersion, publicationDate, status }`.

## Authorization

MCP session autenticada (OAuth/mTLS/API key serviço-a-serviço — definir em D4 plano infra).

Api.Agente **DEVE** logar chamadas MCP correlacionadas a `executionId` sem logar conteúdo integral ECD/manual em produção.
