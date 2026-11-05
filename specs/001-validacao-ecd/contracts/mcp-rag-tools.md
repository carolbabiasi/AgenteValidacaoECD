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
  "documentKind": "Manual|Faq|Norm|null",
  "topK": 8,
  "minScore": 0.0
}
```

Sem `manualVersion`, a busca cobre todos os documentos **ativos** da família (manual, FAQ e
normas); `documentKind` restringe o tipo. Com `manualVersion`, busca só aquela versão.

**Output**:

```json
{
  "results": [
    {
      "chunkId": "uuid",
      "manualVersion": "ECD-L9-2026-05",
      "documentKind": "Manual",
      "documentCode": "MANUAL",
      "hierarchyPath": "Capítulo 3 > REGISTRO I050: PLANO DE CONTAS",
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
**Output**: chunk completo + metadados para citação (`manualVersion`, `documentKind`, `documentCode`,
`hierarchyPath`, `sourceLocation`, `content`).

## rag.list_manual_versions

**Input**: `{ "layoutFamily": "string|null", "activeOnly": true, "documentKind": "Manual|Faq|Norm|null" }`  
**Output**: lista `{ manualVersion, publicationDate, status, documentKind, documentCode }`.

O Agente resolve a versão do manual da execução (FR-035) com `documentKind=Manual`.

## Authorization

MCP session autenticada (OAuth/mTLS/API key serviço-a-serviço — definir em D4 plano infra).

Api.Agente **DEVE** logar chamadas MCP correlacionadas a `executionId` sem logar conteúdo integral ECD/manual em produção.
