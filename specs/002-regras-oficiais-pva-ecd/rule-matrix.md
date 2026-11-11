# Matriz de regras oficiais ECD (Leiaute 9) — valores e consistência contábil

**Versão da matriz:** 0.3 (+ encerramento I350/I355 e Bloco J — `inventory/encerramento-i350-i355-bloco-j.md`)  
**Manual:** Orientação ECD Leiaute 9, **janeiro/2026**  
**PVA de referência:** Sped Contábil **10.4.1** (JVM 21.0.3, SERPRO/RFB) — evidência: captura “Sobre” 2026-11-10

Legenda **status:** `Planejado` | `Implementado` | `Paridade PVA OK` | `N/A oficial` | `DEP` (dependência externa)

| Código oficial PVA | Registro(s) | Campos | Condição / fórmula (resumo) | Mensagem oficial (texto PVA) | Resultado PVA | Página manual | Implementação (`ImplementationKey`) | Fixture de teste | Status | Evidência PVA |
|--------------------|------------|--------|-----------------------------|------------------------------|---------------|---------------|----------------------------------------|----------------|--------|----------------|
| REGRA_VALIDACAO_VL_LCTO_DEB | I200, I250 | VL_LCTO (04), VL_DEB | IND_ESC≠A: Σ débitos I250 = VL_LCTO | (capturar no PVA) | Erro | ~145 | OfficialPvaVlLctoDebRule | PVA_REGRA_VALIDACAO_VL_LCTO_DEB_ERRO.txt | Implementado | evidence/pva-parity/REGRA_VALIDACAO_VL_LCTO_DEB |
| REGRA_VALIDACAO_VL_LCTO_CRED | I200, I250 | VL_LCTO (04), VL_CRED | IND_ESC≠A: Σ créditos I250 = VL_LCTO | (capturar no PVA) | Erro | ~145 | OfficialPvaVlLctoCredRule | PVA_REGRA_VALIDACAO_VL_LCTO_CRED_ERRO.txt | Implementado | evidence/pva-parity/REGRA_VALIDACAO_VL_LCTO_CRED |

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
| Lançamentos (I200/I250) | 11 (onda 1, inventário) | 2 | 0 |
| Saldos periódicos (I155/I157) | 14 (onda 1, inventário) | 0 | 0 |
| Encerramento / resultado (I350/I355) | 7 (inventário) | 0 | 0 |
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

