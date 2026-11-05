# Fixtures de smoke

Arquivos sintéticos, sem dados de cliente, usados pelo [quickstart](../specs/001-validacao-ecd/quickstart.md).

| Arquivo | Uso | Resultado esperado |
|---------|-----|--------------------|
| `ecd-sample.txt` | Validação feliz (mesmo formato de `SampleEcdFiles.ValidMinimal`) | `Completed`, sem achados |
| `ecd-sample-missing-opening.txt` | Regra SAMPLE de abertura ausente | `Completed` com achado `High` |
| `manual-ecd-sample.md` | Ingestão na Api.RAG | Job `Completed`; conteúdo **não normativo** |

A Api.RAG aceita `.pdf`, `.txt` e `.md`. O manual oficial (PDF) fica em `docs/`, não aqui.
