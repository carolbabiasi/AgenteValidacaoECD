# Plano técnico — Regras oficiais PVA ECD

## Estado atual (baseline)

- **Motor:** `RuleEngine` → `RunStructuralAsync` + `RunAccountingAsync` sobre catálogo Postgres.
- **Regras MVP:** 12 plugins `Content*` com IDs internos `ECD-CONTENT-001`…`012` (não são códigos RFB/PVA).
- **Manual:** PDF janeiro/2026 no repositório; RAG pode usar maio/2026 — alinhar ingestão à **versão janeiro/2026** para este épico.
- **PVA:** não versionado no repo; paridade depende de ambiente Windows com PVA ECD instalado.

## Arquitetura alvo (additive)

```
src/Api.Agente.Application/Rules/
  Content/          # intacto (feature 001)
  OfficialPva/    # nova: uma classe por regra oficial (ou por família agrupável)
  OfficialPva/Indexing/   # índices: plano I050, saldos I155/I157, DRE/J150, etc.
```

- **IDs de catálogo:** usar código oficial do PVA como `ValidationRule.Id` (string), quando existir; prefixo reservado `ECD-PVA-` apenas se o PVA não expuser código estável (registrar em DEP).
- **Metadados:** preencher `OfficialReference` = `Manual ECD Leiaute 9, jan/2026, p.{N}` + opcional `PVA mensagem {id}`.
- **Mensagens:** constantes geradas a partir da matriz (`rule-matrix.md` → `OfficialRuleMessages` ou recurso `.resx` por regra) — sem texto livre no código.
- **Pipeline:** sem alteração de fases; novas regras só em `RunAccountingAsync` (e `NotVerified` se dependência de registro ausente).

## Fases de entrega

| Fase | Entrega | Saída |
|------|---------|--------|
| **0 — Inventário** | Varredura manual (por registro) + checklist PVA; preencher matriz completa | `rule-matrix.md` v1 com todas as linhas, maioria `Planejado` |
| **1 — Fundação** | Índices contábeis (I050, I155/I157, I200/I250, I350, J…), testes de parser | Infra sem regras novas ativas |
| **2 — Bloco I (valores)** | Regras oficiais de lançamento, saldos, partida — prioridade PVA | Fixtures + evidência PVA por regra |
| **3 — Encerramento / resultado** | Regras de encerramento e apuração | Idem |
| **4 — Demonstrações e cruzamentos** | DRE, BP, consistência entre blocos | Idem |
| **5 — Fechamento épico** | Matriz 100% implementado ou explicitamente fora de escopo com DEP | DoD + `evidence/pva-parity/` |

## Metodologia de paridade PVA

1. Gerar ECD mínimo válido estruturalmente (fixtures 001).
2. Aplicar **uma** violação alinhada à condição oficial.
3. Validar no PVA; salvar log/captura em `specs/002-regras-oficiais-pva-ecd/evidence/pva-parity/{codigo-regra}/`.
4. Executar `dotnet test` com o mesmo arquivo; comparar código da regra + mensagem (normalizar espaços).

## Testes

- `tests/Api.Agente.Tests/Rules/OfficialPva/` — unitários por regra.
- `tests/Api.Agente.Tests/Integration/PvaParity/` — opcional, marcados `[Trait("PVA")]` se exigirem arquivo de evidência externo.
- Não reutilizar `ERRO_*` genéricos sem mapear para código oficial na matriz.

## Não fazer

- Não editar classes em `Rules/Content/` exceto correções de bug **estritamente estruturais** aprovadas fora deste épico.
- Não renumerar `ECD-CONTENT-*` para parecer oficial.

