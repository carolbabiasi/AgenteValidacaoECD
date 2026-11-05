# Evidência — corpus RAG oficial (2026-11-05)

Ambiente: Docker `deploy-postgres-1`, Api.RAG Development, embeddings `text-embedding-3-small` (OpenAI).

| Versão | Tipo | Série | Chunks | Status |
|--------|------|-------|--------|--------|
| `ECD-L9-2026-05` | Manual | `MANUAL` | 409 | Active |
| `FAQ-RFB-2026-09` | Faq | `FAQ-RFB` | 8 | Active |
| `IN-RFB-2003-2021` | Norm | `IN-RFB-2003-2021` | 17 | Active |
| `ITG-2000-R1` | Norm | `ITG-2000-R1` | 6 | Active |

Script: `deploy/scripts/Invoke-RagIngestDocs.ps1`

Validação automatizada: `OfficialCorpusSearchTests`, `DocumentKindActivationTests` (22 testes Api.RAG).
