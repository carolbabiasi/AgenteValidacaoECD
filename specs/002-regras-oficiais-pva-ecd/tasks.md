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
- [x] T213b `EcdClosingIndex` (I350 DT_RES, I355 VL_CTA/IND_DC)

## Fase 2 — Primeira onda (VL_LCTO)

- [x] T220 `OfficialPvaVlLctoDebRule` + fixture `PVA_REGRA_VALIDACAO_VL_LCTO_DEB_*`
- [x] T221 `OfficialPvaVlLctoCredRule` + fixture `PVA_REGRA_VALIDACAO_VL_LCTO_CRED_ERRO`
- [ ] T222 Paridade PVA (log/screenshot em `evidence/pva-parity/`; script `deploy/scripts/Invoke-PvaParityT222.ps1`)
- [x] T224 Fixtures importáveis PVA em `ecds-teste-pva-leiaute9/` (DEB/CRED/ESC_AUXILIAR ERRO)
- [x] T223 `OfficialPvaVlLctoEscAuxiliarRule` + fixtures `PVA_REGRA_VALIDACAO_VL_LCTO_ESC_AUXILIAR_*`

## Fase 2A — Encerramento (I350/I355)

- [x] T225 `OfficialPvaRegistroObrigatorioI350Rule` + fixtures `PVA_REGRA_REGISTRO_OBRIGATORIO_I350_*`
- [x] T226 `OfficialPvaValidacaoSaldoContaRule` + fixtures `PVA_REGRA_VALIDACAO_SALDO_CONTA_*`
- [ ] T227 Paridade PVA (T222) para `REGRA_REGISTRO_OBRIGATORIO_I350` e `REGRA_VALIDACAO_SALDO_CONTA`
- [x] T228 `REGRA_VALIDACAO_CONTA_RESULTADO` + fixtures `PVA_REGRA_VALIDACAO_CONTA_RESULTADO_*`
- [x] T228a Duplicidades I350/I355 (`REGRA_DT_RES_DUPLICIDADE`, `REGRA_COD_CTA_DT_RES_DUPLICIDADE`)
- [x] T229 Onda 2A encerramento completa (`REGRA_ENCERRAMENTO_EXERCICIO`, `REGRA_CONTA_RESULTADO`, `REGRA_DATA_INTERVALO_DO_ARQUIVO`)
- [x] T230 I155 onda 1b (`REGRA_DUPLICIDADE_CONTA_SALDO_PERIODICO`, `REGRA_CAMPOS_SALDOS_PERIODICOS_DIFERENTE_ZERO`, `REGRA_VALIDACAO_SALDO_INI_DIF_FIN`)
- [x] T231 I157 / continuidade I150 / C155 (`REGRA_VALIDA_SLD_INI_SOMA_SLD_INI_I157`, `REGRA_CONTINUIDADE_SALDOS_PERIODICOS`, `REGRA_VALIDA_MES_I157`, `REGRA_EXISTE_I157_PERIODO_POSTERIOR`, `REGRA_NATUREZA_CONTA_PAI_I157`, `REGRA_DUPLICIDADE_PERIODO_SALDO_PERIODICO`, `REGRA_DATA_MES`, `REGRA_CONTA_I155_INEXISTENTE_C155`, `REGRA_CONTA_C155_INEXISTENTE_I155`)
- [x] T231a `REGRA_EXISTE_I155_COM_I157_NO_C155` + cenários C155 (IND_MUDANCA_PC campo 22, DT_EX_SOCIAL, natureza, I157)

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

