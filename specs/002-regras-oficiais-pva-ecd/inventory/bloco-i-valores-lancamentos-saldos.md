# Inventário fase 0 — Bloco I (valores, saldos, lançamentos)

**Fonte:** Manual ECD Leiaute 9, jan/2026 (texto em `../artifacts/manual-jan2026-extract.txt`)  
**PVA:** Sped Contábil 10.4.1  
**Escopo deste arquivo:** regras oficiais com impacto em **valores, débitos/créditos, saldos e encerramento** nos registros I150–I250 e encerramento I350/I355 (parcial).  
**Total de códigos `REGRA_*` no manual (extração completa):** 409 únicos — deste inventário: **42 regras prioritárias** (onda 1, escrituração G/R/A, sem moeda funcional salvo nota).

Legenda **PVA:** `Erro` | `Aviso` conforme manual (“o PGE do Sped Contábil gera um erro/aviso”).  
**Status impl.:** `Planejado` até existir plugin `OfficialPva*` + evidência PVA.

## Prioridade 1 — Lançamentos (I200 + I250)

| Código oficial | Registro | Campos / fórmula | Resultado PVA | Pág. manual | ECD-CONTENT interno | Status impl. |
|----------------|----------|------------------|---------------|-------------|---------------------|--------------|
| `REGRA_VALIDACAO_VL_LCTO_DEB` | I200, I250 | Soma débitos I250 = `I200.VL_LCTO` se `I010.IND_ESC` ≠ A | Erro | ~145 | ECD-CONTENT-004 (parcial) | Implementado |
| `REGRA_VALIDACAO_VL_LCTO_CRED` | I200, I250 | Soma créditos I250 = `I200.VL_LCTO` se `I010.IND_ESC` ≠ A | Erro | ~145 | ECD-CONTENT-004 (parcial) | Implementado |
| `REGRA_VALIDACAO_VL_LCTO_ESC_AUXILIAR` | I200, I250 | IND_ESC = A: soma débitos **ou** créditos I250 = `VL_LCTO` | Aviso | ~145 | — | Implementado |
| `REGRA_LCTO_4_FORMULA` | I200, I250 | IND_LCTO = N: contagem partidas D e C &lt; 2 cada | Aviso | ~144 | — | Implementado |
| `REGRA_OBRIG_NAO_MF_I200` | I200 | `VL_LCTO` ≠ 0 | Erro | ~144 | — | Implementado |
| `REGRA_REGISTRO_DUPLICADO` | I200 | Chave `NUM_LCTO` única | Erro | ~145 | ECD-CONTENT-012 (data+nº) | Implementado |
| `REGRA_VALIDACAO_SALDO_CONTA` | I200, I350, I355 | Soma lanç. tipo E por data/conta = saldo antes encerramento (sinal invertido) | Erro | ~144 | — | Implementado |
| `REGRA_REGISTRO_OBRIGATORIO_I350` | I200, I350 | Existe I350 ⇒ existe I200 com IND_LCTO = E (G/R) | Erro | ~145 | — | Implementado |
| `REGRA_VALIDACAO_VALOR_DEB` | I250, I155 | Soma débitos por I150+conta = `I155.VL_DEB` (G/R/A) | Erro | ~149–150 | — | Implementado |
| `REGRA_VALIDACAO_VALOR_CRED` | I250, I155 | Soma créditos por I150+conta = `I155.VL_CRED` (G/R/A) | Erro | ~149–150 | — | Implementado |
| `REGRA_HISTORICO_OBRIGATORIO` | I250 | `HIST` ou `COD_HIST_PAD` preenchido | Erro | ~149 | — | Implementado |

## Prioridade 2 — Saldos periódicos (I155)

| Código oficial | Registro | Campos / fórmula | Resultado PVA | Pág. manual | Status impl. |
|----------------|----------|------------------|---------------|-------------|--------------|
| `REGRA_VALIDACAO_SALDO_FINAL` | I155 | VL_SLD_FIN = f(VL_SLD_INI, VL_DEB, VL_CRED, IND_DC_INI/FIN) | Erro | ~133–136 | Implementado |
| `REGRA_VALIDACAO_SOMA_SALDO_INICIAL` | I155 | Σ VL_SLD_INI (com sinal D/C) = 0 por I150 (G/R) | Erro | ~136 | Implementado |
| `REGRA_VALIDACAO_SOMA_SALDO_FINAL` | I155 | Σ VL_SLD_FIN (com sinal D/C) = 0 por I150 (G/R) | Erro | ~136 | Implementado |
| `REGRA_VALIDACAO_DEB_DIF_CRED` | I155 | Σ VL_DEB = Σ VL_CRED por I150 (G/R) | Erro | ~136 | Implementado |
| `REGRA_VALIDACAO_SALDO_INI_DIF_FIN` | I155 | 2º mês+: VL_SLD_INI = VL_SLD_FIN mês anterior (mesma conta/CC) | Erro | ~136 | Implementado |
| `REGRA_DUPLICIDADE_CONTA_SALDO_PERIODICO` | I155 | Chave COD_CTA + COD_CCUS por I150 | Erro | ~136 | Implementado |
| `REGRA_CAMPOS_SALDOS_PERIODICOS_DIFERENTE_ZERO` | I155 | Sem I157 filho: ao menos um valor ≠ 0 | Aviso | ~136 | Implementado |
| `REGRA_VALIDACAO_VALOR_DEB` | I155, I250 | Σ débitos I250 = VL_DEB por I150+conta (G/R/A) | Erro | ~136 | Implementado |
| `REGRA_VALIDACAO_VALOR_CRED` | I155, I250 | Σ créditos I250 = VL_CRED por I150+conta (G/R/A) | Erro | ~136 | Implementado |
| `REGRA_VALIDA_SLD_INI_SOMA_SLD_INI_I157` | I155, I157 | VL_SLD_INI = soma VL_SLD_INI dos I157 filhos | Erro | ~136 | Implementado |

## Prioridade 3 — Transferência de plano (I157) e período (I150)

| Código oficial | Registro | Condição (resumo) | Resultado PVA | Pág. manual | Status impl. |
|----------------|----------|-------------------|---------------|-------------|--------------|
| `REGRA_CONTINUIDADE_SALDOS_PERIODICOS` | I150, I155 | I150 cobre todos os meses do 0000 | Erro | ~131 | Implementado |
| `REGRA_VALIDA_MES_I157` | I150, I157 | Mês DT_INI I150 vs 0000.DT_INI | Erro | ~131 | Implementado |
| `REGRA_EXISTE_I157_PERIODO_POSTERIOR` | I157 | I157 ⇒ 1º I150.DT_INI = 0000.DT_INI | Erro | ~140 | Implementado |
| `REGRA_NATUREZA_CONTA_PAI_I157` | I157, I155, C155 | Natureza I157/C155 vs I155 (01–03 vs 04) | Erro | ~140 | Implementado |
| `REGRA_DUPLICIDADE_PERIODO_SALDO_PERIODICO` | I150 | DT_INI+DT_FIN únicos | Erro | ~131 | Implementado |
| `REGRA_DATA_MES` | I150 | DT_INI e DT_FIN no mesmo mês | Erro | ~131 | Implementado |
| `REGRA_CONTA_I155_INEXISTENTE_C155` | C155, I155 | I155 ⇒ C155 sem mudança plano | Erro | ~139 | Implementado |
| `REGRA_CONTA_C155_INEXISTENTE_I155` | C155, I155, C050, I030 | Cenários 1–4 DT_EX/IND_MUDANCA | Erro | ~97 | Implementado |
| `REGRA_EXISTE_I155_COM_I157_NO_C155` | C155, I157 | Cruzamento I157/C155 | Erro | ~139 | Implementado |
| `REGRA_NATUREZA_CONTA_C155` | C050, C155, I050 | Natureza recuperação | Erro | ~97 | Implementado |
| `REGRA_NATUREZA_CONTA_I155` | C050, C155, I050, I155 | I050 vs C050 (01–03 / 04) | Erro | ~139 | Implementado |
| `REGRA_CONTA_I157_INEXISTENTE` | C155, I157 | Mudança plano | Erro | ~141 | Implementado |

## Fora da onda 1 (documentar, implementar depois)

- Variantes **moeda funcional** (`*_MF`, `REGRA_OBRIG_MF_*`, campos I020).
- Escrituração **B** + balancete I300/I310: `REGRA_VALIDACAO_VALOR_*_BALANCETE`, `REGRA_VALIDACAO_DC_BALANCETE`.
- Encerramento e Bloco **J**: ver [`encerramento-i350-i355-bloco-j.md`](encerramento-i350-i355-bloco-j.md).
- Recuperação **C155**: `REGRA_CONTA_C155_INEXISTENTE_I155`, `REGRA_CONTA_I155_INEXISTENTE_C155`.

Lista filtrada (valores/saldos/lançamentos) extraída do manual: ver saída do inventário em `../artifacts/regra-codigos-valores.txt` (gerado pelo script abaixo).

## Procedimento PVA (onda 1)

1. Partir de `ecds-teste/ECD_001_BASICA.txt` (base válida estruturalmente).
2. Clonar para `ecds-teste-pva/PVA_{CODIGO}.txt` com **uma** violação da condição.
3. Validar no Sped Contábil 10.4.1; salvar log/captura em `../evidence/pva-parity/{CODIGO}/`.
4. Registrar mensagem **literal** do PVA na coluna “Mensagem oficial” de `../rule-matrix.md` (próxima revisão da matriz).

## Próxima implementação sugerida (código)

Ordem técnica após **T210–T212** (índices I150/I155/I200–I250):

1. `REGRA_VALIDACAO_VL_LCTO_DEB` + `REGRA_VALIDACAO_VL_LCTO_CRED` (substitui equivalência informal de ECD-CONTENT-004, **sem remover** Content até paridade OK).
2. `REGRA_VALIDACAO_SALDO_FINAL` (I155).
3. `REGRA_VALIDACAO_SALDO_INI_DIF_FIN`.

