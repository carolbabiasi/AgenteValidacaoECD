# Tasks — 002 Regras oficiais PVA ECD

## Fase 0 — Inventário e governança

- [x] T200 Registrar versão exata do PVA ECD e do manual (janeiro/2026) em `rule-matrix.md`
- [x] T201 Script `deploy/scripts/ExtractManualText` + `artifacts/manual-jan2026-extract.txt`
- [ ] T202 Preencher matriz com **todas** as regras oficiais (ondas 1+2 em `inventory/`; pendentes: I052, C650, balancete B, cap. 4 nível 1–2 completo)
- [x] T202a Inventário encerramento I350/I355 + Bloco J (`inventory/encerramento-i350-i355-bloco-j.md`)
- [ ] T203 Revisar ingestão RAG para priorizar PDF janeiro/2026 (não maio) neste épico
- [ ] T204 Branch `feat/002-regras-oficiais-pva` + gate: nenhum merge de regra sem linha na matriz

## Fase 1 — Infraestrutura contábil (sem mudar Content)

- [x] T210 `EcdChartOfAccountsIndexView` (natureza, analítica, conta resultado)
- [x] T211 `EcdPeriodicBalanceIndex` (I150/I155 por período e conta)
- [x] T212 `EcdJournalIndex` (I200/I250, somas D/C)
- [x] T213 Testes `EcdAccountingIndexTests` com `ECD_001_BASICA.txt`

## Fase 2 — Primeira onda (VL_LCTO)

- [x] T220 `OfficialPvaVlLctoDebRule` + fixture `PVA_REGRA_VALIDACAO_VL_LCTO_DEB_*`
- [x] T221 `OfficialPvaVlLctoCredRule` + fixture `PVA_REGRA_VALIDACAO_VL_LCTO_CRED_ERRO`
- [ ] T222 Paridade PVA (log/screenshot em `evidence/pva-parity/`; script `deploy/scripts/Invoke-PvaParityT222.ps1`)
- [x] T223 `OfficialPvaVlLctoEscAuxiliarRule` + fixtures `PVA_REGRA_VALIDACAO_VL_LCTO_ESC_AUXILIAR_*`

## Fase 2+ — Implementação por bloco (repetir por regra da matriz)

Para cada regra com status `Planejado` → `Implementado`:

- [ ] T2xx Implementar `OfficialPva/*Rule.cs`
- [ ] T2xx Registrar no seeder com `OfficialReference` e mensagem oficial
- [ ] T2xx Fixture `ecds-teste-pva/PVA_{codigo}.txt`
- [ ] T2xx Teste unitário + evidência em `evidence/pva-parity/{codigo}/`

## Fase 5 — Entrega

- [ ] T250 Matriz com totais e links para evidências
- [ ] T251 Atualizar `AGENTS.md` / skill de domínio se necessário
- [ ] T252 DoD: FR-201…206 na checklist do PR

