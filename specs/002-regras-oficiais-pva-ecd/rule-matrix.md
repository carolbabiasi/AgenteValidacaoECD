# Matriz de regras oficiais ECD (Leiaute 9) — valores e consistência contábil

**Versão da matriz:** 0.3 (+ encerramento I350/I355 e Bloco J — `inventory/encerramento-i350-i355-bloco-j.md`)  
**Manual:** Orientação ECD Leiaute 9, **janeiro/2026**  
**PVA de referência:** Sped Contábil **10.4.1** (JVM 21.0.3, SERPRO/RFB) — evidência: captura “Sobre” 2026-11-10

Legenda **status:** `Planejado` | `Implementado` | `Paridade PVA OK` | `N/A oficial` | `DEP` (dependência externa)

| Código oficial PVA | Registro(s) | Campos | Condição / fórmula (resumo) | Mensagem oficial (texto PVA) | Resultado PVA | Página manual | Implementação (`ImplementationKey`) | Fixture de teste | Status | Evidência PVA |
|--------------------|------------|--------|-----------------------------|------------------------------|---------------|---------------|----------------------------------------|----------------|--------|----------------|
| REGRA_VALIDACAO_VL_LCTO_DEB | I200, I250 | VL_LCTO (04), VL_DEB | IND_ESC≠A: Σ débitos I250 = VL_LCTO | (capturar no PVA) | Erro | ~145 | OfficialPvaVlLctoDebRule | PVA_REGRA_VALIDACAO_VL_LCTO_DEB_ERRO.txt | Implementado | evidence/pva-parity/REGRA_VALIDACAO_VL_LCTO_DEB |
| REGRA_VALIDACAO_VL_LCTO_CRED | I200, I250 | VL_LCTO (04), VL_CRED | IND_ESC≠A: Σ créditos I250 = VL_LCTO | (capturar no PVA) | Erro | ~145 | OfficialPvaVlLctoCredRule | PVA_REGRA_VALIDACAO_VL_LCTO_CRED_ERRO.txt | Implementado | evidence/pva-parity/REGRA_VALIDACAO_VL_LCTO_CRED |
| REGRA_VALIDACAO_VL_LCTO_ESC_AUXILIAR | I200, I250 | VL_LCTO (04) | IND_ESC=A: Σ débitos **ou** créditos = VL_LCTO | (capturar no PVA) | Aviso | ~145 | OfficialPvaVlLctoEscAuxiliarRule | PVA_REGRA_VALIDACAO_VL_LCTO_ESC_AUXILIAR_ERRO.txt | Implementado | evidence/pva-parity/REGRA_VALIDACAO_VL_LCTO_ESC_AUXILIAR |
| REGRA_OBRIG_NAO_MF_I200 | I200 | VL_LCTO (04) | IDENT_MF≠S: VL_LCTO ≠ 0 | (capturar no PVA) | Erro | ~144 | OfficialPvaObrigNaoMfI200Rule | PVA_REGRA_OBRIG_NAO_MF_I200_ERRO.txt | Implementado | — |
| REGRA_REGISTRO_DUPLICADO | I200 | NUM_LCTO (02) | NUM_LCTO único no arquivo | (capturar no PVA) | Erro | ~145 | OfficialPvaRegistroDuplicadoI200Rule | PVA_REGRA_REGISTRO_DUPLICADO_ERRO.txt | Implementado | — |
| REGRA_LCTO_4_FORMULA | I200, I250 | IND_LCTO, IND_DC | IND_LCTO=N: #D&lt;2 e #C&lt;2 em I250 | (capturar no PVA) | Aviso | ~144 | OfficialPvaLcto4FormulaRule | PVA_REGRA_LCTO_4_FORMULA_ERRO.txt | Implementado | — |
| REGRA_HISTORICO_OBRIGATORIO | I250 | HIST (08), COD_HIST_PAD (07) | Um dos dois preenchido | (capturar no PVA) | Erro | ~149 | OfficialPvaHistoricoObrigatorioRule | PVA_REGRA_HISTORICO_OBRIGATORIO_ERRO.txt | Implementado | — |
| REGRA_VALIDACAO_VALOR_DEB | I155, I250 | VL_DEB (06) | Σ débitos I250 = VL_DEB por período/conta (G/R/A) | (capturar no PVA) | Erro | ~149 | OfficialPvaValidacaoValorDebRule | PVA_REGRA_VALIDACAO_VALOR_DEB_ERRO.txt | Implementado | — |
| REGRA_VALIDACAO_VALOR_CRED | I155, I250 | VL_CRED (07) | Σ créditos I250 = VL_CRED por período/conta (G/R/A) | (capturar no PVA) | Erro | ~149 | OfficialPvaValidacaoValorCredRule | PVA_REGRA_VALIDACAO_VALOR_CRED_ERRO.txt | Implementado | — |
| REGRA_VALIDACAO_SALDO_FINAL | I155 | VL_SLD_FIN (08) | Coerência com VL_SLD_INI + VL_DEB − VL_CRED | (capturar no PVA) | Erro | ~136 | OfficialPvaValidacaoSaldoFinalRule | PVA_REGRA_VALIDACAO_SALDO_FINAL_ERRO.txt | Implementado | — |
| REGRA_VALIDACAO_DEB_DIF_CRED | I155 | VL_DEB, VL_CRED | Σ débitos = Σ créditos por I150 (G/R) | (capturar no PVA) | Erro | ~136 | OfficialPvaValidacaoDebDifCredRule | PVA_REGRA_VALIDACAO_DEB_DIF_CRED_ERRO.txt | Implementado | — |
| REGRA_VALIDACAO_SOMA_SALDO_INICIAL | I155 | VL_SLD_INI (04) | Σ saldos iniciais assinados = 0 (G/R) | (capturar no PVA) | Erro | ~136 | OfficialPvaValidacaoSomaSaldoInicialRule | PVA_REGRA_VALIDACAO_SOMA_SALDO_INICIAL_ERRO.txt | Implementado | — |
| REGRA_VALIDACAO_SOMA_SALDO_FINAL | I155 | VL_SLD_FIN (08) | Σ saldos finais assinados = 0 (G/R) | (capturar no PVA) | Erro | ~136 | OfficialPvaValidacaoSomaSaldoFinalRule | PVA_REGRA_VALIDACAO_SOMA_SALDO_FINAL_ERRO.txt | Implementado | — |
| REGRA_REGISTRO_OBRIGATORIO_I350 | I350, I200 | DT_RES (02), IND_LCTO (05) | I350 informado ⇒ I200 IND_LCTO=E na mesma data (G/R) | (capturar no PVA) | Erro | ~145, ~158 | OfficialPvaRegistroObrigatorioI350Rule | PVA_REGRA_REGISTRO_OBRIGATORIO_I350_ERRO.txt | Implementado | — |
| REGRA_VALIDACAO_SALDO_CONTA | I200, I350, I355 | VL_CTA (04), IND_DC (05) | Σ lanç. E por DT_RES/conta = VL_CTA (IND_DC invertido) G/R | (capturar no PVA) | Erro | ~144, ~158 | OfficialPvaValidacaoSaldoContaRule | PVA_REGRA_VALIDACAO_SALDO_CONTA_ERRO.txt | Implementado | — |
| REGRA_VALIDACAO_CONTA_RESULTADO | I155, I350 | VL_SLD_FIN (08) | Na DT_RES: Σ VL_SLD_FIN contas COD_NAT=04 = 0 | (capturar no PVA) | Erro | ~158 | OfficialPvaValidacaoContaResultadoRule | PVA_REGRA_VALIDACAO_CONTA_RESULTADO_ERRO.txt | Implementado | — |
| REGRA_DT_RES_DUPLICIDADE | I350 | DT_RES (02) | DT_RES única no arquivo | (capturar no PVA) | Erro | ~155 | OfficialPvaDtResDuplicidadeRule | PVA_REGRA_DT_RES_DUPLICIDADE_ERRO.txt | Implementado | — |
| REGRA_COD_CTA_DT_RES_DUPLICIDADE | I350, I355 | COD_CTA, COD_CCUS | Por DT_RES, COD_CTA+COD_CCUS únicos | (capturar no PVA) | Erro | ~158 | OfficialPvaCodCtaDtResDuplicidadeRule | PVA_REGRA_COD_CTA_DT_RES_DUPLICIDADE_ERRO.txt | Implementado | — |
| REGRA_ENCERRAMENTO_EXERCICIO | I030, I010, I350, 0000 | DT_EX_SOCIAL (12), DT_RES | DT_EX_SOCIAL no 0000 + G/R/B ⇒ I350 DT_RES igual | (capturar no PVA) | Erro | ~155 | OfficialPvaEncerramentoExercicioRule | PVA_REGRA_ENCERRAMENTO_EXERCICIO_ERRO.txt | Implementado | — |
| REGRA_CONTA_RESULTADO | I355, I050 | COD_CTA, COD_NAT | I355 conta com COD_NAT=04 | (capturar no PVA) | Erro | ~159 | OfficialPvaContaResultadoI355Rule | PVA_REGRA_CONTA_RESULTADO_I355_ERRO.txt | Implementado | — |
| REGRA_DATA_INTERVALO_DO_ARQUIVO | I150, I200, I350 | DT_* | Datas no intervalo 0000.DT_INI..DT_FIN | (capturar no PVA) | Erro | ~155+ | OfficialPvaDataIntervaloDoArquivoRule | PVA_REGRA_DATA_INTERVALO_DO_ARQUIVO_ERRO.txt | Implementado | — |
| REGRA_DUPLICIDADE_CONTA_SALDO_PERIODICO | I155 | COD_CTA, COD_CCUS | Únicos por I150 | (capturar no PVA) | Erro | ~136 | OfficialPvaDuplicidadeContaSaldoPeriodicoRule | PVA_REGRA_DUPLICIDADE_CONTA_SALDO_PERIODICO_ERRO.txt | Implementado | — |
| REGRA_CAMPOS_SALDOS_PERIODICOS_DIFERENTE_ZERO | I155 | VL_* | Sem I157: algum saldo/movimento ≠ 0 | (capturar no PVA) | Aviso | ~136 | OfficialPvaCamposSaldosPeriodicosDiferenteZeroRule | PVA_REGRA_CAMPOS_SALDOS_PERIODICOS_DIFERENTE_ZERO_ERRO.txt | Implementado | — |
| REGRA_VALIDACAO_SALDO_INI_DIF_FIN | I155, I150 | VL_SLD_INI, VL_SLD_FIN | 2º+ período: ini = fin período anterior | (capturar no PVA) | Erro | ~136 | OfficialPvaValidacaoSaldoIniDifFinRule | PVA_REGRA_VALIDACAO_SALDO_INI_DIF_FIN_ERRO.txt | Implementado | — |

## Auditoria das regras internas atuais (não oficiais)

Estas regras **permanecem** até mapeamento explícito; **não** substituem código PVA.

| ID interno atual | Nome resumido | Equivalente oficial conhecido? | Ação neste épico |
|------------------|---------------|-------------------------------|------------------|
| ECD-CONTENT-004 | Partida I200 balanceada | A confirmar no PVA | Mapear linha na matriz; não remover |
| ECD-CONTENT-005 | Conta inexistente I250/I155 | A confirmar | Idem |
| ECD-CONTENT-006 | Conta duplicada I050 | A confirmar | Idem |
| ECD-CONTENT-007 | Natureza conta I050 | A confirmar | Idem |
| ECD-CONTENT-008 | Conta pai inexistente | A confirmar | Idem |
| ECD-CONTENT-012 | I200 duplicado | A confirmar | Idem |
| ECD-CONTENT-001…003, 009–011 | Estrutura/formato | Fora do escopo valores | Sem alteração |

## Contagem (preencher na fase 0)

| Bloco / tema | Qtd regras oficiais (manual+PVA) | Implementadas | Paridade OK |
|--------------|-----------------------------------|---------------|-------------|
| Lançamentos (I200/I250) | 11 (onda 1, inventário) | 3 | 0 |
| Saldos periódicos (I155/I157) | 14 (onda 1, inventário) | 0 | 0 |
| Encerramento / resultado (I350/I355) | 7 (inventário) | 7 | 0 |
| Demonstrações (J005/J100/J150/J210) | 18 (inventário) | 0 | 0 |
| Cruzamentos entre registros | _TBD_ | 0 | 0 |
| **Total manual (todos REGRA_*)** | 409 códigos únicos | 0 | 0 |
| **Total onda 1 (Bloco I valores)** | 42 priorizadas | 0 | 0 |
| **Total onda 2 (encerramento + Bloco J valores)** | 25 priorizadas | 0 | 0 |
| **Total inventário fase 0 (ondas 1+2)** | 67 | 0 | 0 |

## Instrução de preenchimento (fase 0)

1. Para cada registro no manual, extrair subseções de **validação** / **regras de preenchimento** com impacto em **valor ou saldo**.
2. Cruzar com mensagens exibidas no PVA ao importar arquivos de teste RFB (quando disponíveis).
3. Uma linha por código oficial; proibir linha sem código salvo DEP registrado em `spec.md`.

