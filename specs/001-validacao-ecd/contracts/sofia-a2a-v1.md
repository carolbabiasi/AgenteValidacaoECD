# Contrato SOFIA v1.0 ↔ Agente Validação ECD

Perfil de transporte: **A2A v0.3.x JSON-RPC** (`message/send`). Payload de negócio em `TextPart` UTF-8.

Fonte normativa da turma: `skills/sofia-a2a-contrato/sofia-a2a-contrato/references/contrato-sofia-v1.md` e `exemplos-a2a-v03.md`.

## Agente

| Campo | Valor homolog |
|-------|----------------|
| `agente.idAgente` | `agente-validacao-ecd` |
| `agente.nome` | Agente Validação ECD |
| Agent Card | `GET /.well-known/agent-card.json` |
| Endpoint A2A | `POST /a2a` (URL publicada no card) |

## Mapeamento ECD

- `documentos[].texto` com conteúdo SPED `.txt` dispara o mesmo pipeline de `POST /api/v1/validations`.
- Achados do relatório viram `ResultadoAnaliseSofia.achados` (`severidade` ∈ `baixa|media|alta|critica`).
- IDs `idExecucao` / `idSolicitacao` são ecoados sem alteração.

## Autenticação

- Backend SOFIA → agente: header `X-Sofia-Service-Key` (Production) ou JWT homolog (`Validator`/`Admin`).
- Browser (UI SOFIA): política CORS `SofiaBrowser` quando `Cors:AllowedOrigins` estiver configurado.

## Fora desta entrega

- Tarefas A2A assíncronas longas (`Task` em estado `working` + polling) — backlog T069.
- Substituição do REST interno; REST permanece para smoke e UI Angular.
