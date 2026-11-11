# Inventário fase 0 — Encerramento (I350/I355) e Bloco J (demonstrações)

**Fonte:** Manual ECD Leiaute 9, jan/2026 (`../artifacts/manual-jan2026-extract.txt`, cap. registros ~p. 155–189)  
**PVA:** Sped Contábil 10.4.1  
**Escopo:** regras oficiais de **apuração, encerramento, valores em demonstrações** e **cruzamentos** com Bloco I / I052 / C650.

**Status impl.:** `Planejado` até plugin `OfficialPva*` + evidência PVA.

---

## Onda 2A — Encerramento do exercício (I350 / I355 / I200 tipo E)

| Código oficial | Registros | Condição / fórmula (resumo) | PVA | Pág. | Status |
|----------------|-----------|-----------------------------|-----|------|--------|
| `REGRA_ENCERRAMENTO_EXERCICIO` | I030, I010, I350, 0000 | DT_EX_SOCIAL no período + IND_ESC G/R/B ⇒ existe I350 com DT_RES = DT_EX_SOCIAL | Erro* | ~155 | Planejado |
| `REGRA_DT_RES_DUPLICIDADE` | I350 | Chave DT_RES única | Erro | ~155 | Planejado |
| `REGRA_COD_CTA_DT_RES_DUPLICIDADE` | I350, I355 | COD_CTA+COD_CCUS únicos por DT_RES | Erro | ~158 | Planejado |
| `REGRA_REGISTRO_OBRIGATORIO_I350` | I350, I200 | Existe I350 ⇒ existe I200 com IND_LCTO = E (G/R) | Erro | ~158 | Planejado |
| `REGRA_VALIDACAO_CONTA_RESULTADO` | I155, I355 | Na data encerramento: Σ VL_SLD_FIN contas resultado = 0 | Erro | ~158 | Planejado |
| `REGRA_VALIDACAO_SALDO_CONTA` | I200, I350, I355 | Soma lanç. tipo E por data/conta = VL_CTA (I355), sinal invertido (G/R) | Erro | ~158 | Planejado |
| `REGRA_CONTA_RESULTADO` | I355, I050 | COD_CTA com COD_NAT = 04 (resultado) | Erro | ~159 | Planejado |

\* Manual descreve condição sem repetir “gera erro” em um trecho; tratar como **erro** após confirmação no PVA 10.4.1.

**Ligação com Bloco I (onda 1):** `REGRA_VALIDACAO_SALDO_CONTA` aparece em I200 e I355 — mesma regra, implementação única.

Variantes `*_MF` e `REGRA_VALIDACAO_CONTA_RESULTADO_MF`: **onda 2C** (moeda funcional).

---

## Onda 2B — Demonstrações contábeis — cabeçalho (J005)

| Código oficial | Registros | Condição (resumo) | PVA | Pág. | Status |
|----------------|-----------|-------------------|-----|------|--------|
| `REGRA_OBRIGATORIA_DEMONSTRACAO` | J005, J100, J150 | J100 e J150 preenchidos quando aplicável | Erro | ~172 | Planejado |
| `REGRA_REGISTRO_OBRIGATORIO_J005_FIM_EXERCICIO` | J005, J100, J150, I030, 0000, I010 | Encerramento no período ⇒ J005.DT_FIN = DT_EX_SOCIAL + J100 + J150 | Erro | ~173 | Planejado |
| `REGRA_ENC_OBRIGATORIO` | J005, J100, J210, I350 | ID_DEM=1, período dentro ECD ⇒ I350.DT_RES = J005.DT_FIN | Erro | ~173 | Planejado |
| `REGRA_PERIODO_SUP_UM_ANO` | J005 | DT_FIN − DT_INI &gt; 1 ano | Aviso | ~173 | Planejado |

---

## Onda 2B — Balanço patrimonial (J100) — **valores e consistência**

| Código oficial | Campos / fórmula | PVA | Pág. | Status |
|----------------|------------------|-----|------|--------|
| `REGRA_SOMA_DAS_PARCELAS_BALANCO_INI` | Totalizador T: VL_CTA_INI = Σ filhos (COD_AGL_SUP) | Erro | ~177 | Planejado |
| `REGRA_SOMA_DAS_PARCELAS_BALANCO_FIN` | Totalizador T: VL_CTA_FIN = Σ filhos | Erro | ~177 | Planejado |
| `REGRA_VALIDA_ATIVO_PASSIVO_INI` | Nível 1: Ativo VL_CTA_INI = Passivo VL_CTA_INI | Erro | ~177 | Planejado |
| `REGRA_VALIDA_ATIVO_PASSIVO_FIN` | Nível 1: Ativo VL_CTA_FIN = Passivo VL_CTA_FIN | Erro | ~177 | Planejado |
| `REGRA_BALANCO_SALDO_INI` | Σ detalhe Ativo = Σ detalhe Passivo (saldo ini) | Erro | ~177 | Planejado |
| `REGRA_BALANCO_SALDO_FIN` | Σ detalhe Ativo = Σ detalhe Passivo (saldo fin) | Erro | ~177 | Planejado |
| `REGRA_VALIDA_BALANCO_SALDO_FIN` | VL_CTA_FIN detalhe J100 vs saldo calculado I052 (IDENT_MF=N) | Erro | ~178 | Planejado |
| `REGRA_VALIDA_BALANCO_SALDO_INI` | VL_CTA_INI detalhe J100 vs I052 | **Aviso** | ~178 | Planejado |

Regras estruturais de aglutinação (níveis 1, I052, notas J800): listadas no manual mas **prioridade 2D** se não envolverem valor — implementar só após ondas 2B valores.

---

## Onda 2B — DRE (J150) — **valores e consistência**

| Código oficial | Campos / fórmula | PVA | Pág. | Status |
|----------------|------------------|-----|------|--------|
| `REGRA_SOMA_NIVEIS_DRE` | Totalizador T: VL_CTA_FIN = Σ filhos (hierarquia COD_AGL) | Erro | ~183 | Planejado |
| `REGRA_VALIDA_SALDO_COM_DRE` | VL_CTA_FIN detalhe vs saldo calculado I052 (IDENT_MF=N) | Erro | ~183 | Planejado |
| `REGRA_VALIDA_SALDO_INI_DRE` | J150.VL_CTA_INI vs C650.VL_CTA_FIN (mesmo COD_AGL, ID_DEM=1) | Erro | ~184 | Planejado |

Cruzamento **ECD anterior / C650** exige fixture com bloco C ou recuperação simulada — marcar **DEP-C650** na primeira implementação.

---

## Onda 2C — DLPA / DMPL (J210 / J215) — valores

| Código oficial | Campos / fórmula | PVA | Pág. | Status |
|----------------|------------------|-----|------|--------|
| `REGRA_VALIDA_DMPL_COM_SALDO_FIN` | J210.VL_CTA_FIN vs Σ I155.VL_SLD_FIN (DT alinhada J005/J150) | Erro | ~188 | Planejado |
| `REGRA_VALIDA_DMPL_COM_SALDO_INI` | J210.VL_CTA_INI vs Σ I155.VL_SLD_INI | **Aviso** | ~188 | Planejado |
| `REGRA_VALIDA_TOT_AGLUTINACAO_J215` | VL_CTA_FIN = VL_CTA_INI + Σ J215.VL_FAT_CONT | Erro | ~189 | Planejado |

---

## Contagem onda 2 (valores / encerramento / demonstrações)

| Tema | Regras priorizadas (onda 2) |
|------|------------------------------|
| I350 / I355 / encerramento I200 | 7 |
| J005 (vínculo período) | 4 |
| J100 (valores) | 8 |
| J150 (valores + C650) | 3 |
| J210 / J215 (valores) | 3 |
| **Subtotal onda 2** | **25** |

Com **onda 1** (`bloco-i-valores-lancamentos-saldos.md`, 42 regras): **67 regras priorizadas** no inventário fase 0 (ainda ≠ 409 totais do manual).

---

## Próximo passo (fase 0)

- **Bloco C** (recuperação C650) e **I052** (aglutinação) — inventário 2D para suportar `REGRA_VALIDA_SALDO_*` e DMPL.  
- Ou iniciar **T210** + implementação `REGRA_VALIDACAO_VL_LCTO_DEB` (onda 1) com paridade PVA.

