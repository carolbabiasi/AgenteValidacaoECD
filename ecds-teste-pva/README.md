# Fixtures PVA — duas camadas

## 1. Agente (`ecds-teste-pva/*.txt`)

Arquivos **simplificados** para testes unitários e SOFIA. O parser do agente aceita layout compacto (como `ecds-teste/`).

**Não use** estes arquivos direto no PVA 10.4.1 se o relatório for só **“Erros de Importação”**.

## 2. PVA / Leiaute 9 (T222 — importação completa)

O PVA exige **Leiaute 9.00** rigoroso, por exemplo:

- `0000` com **todos** os campos (até `COD_PLAN_REF`); sem plano referencial → **não** enviar `I051`
- `I030` no formato do manual (não é o `I030` curto de `ecds-teste`)
- `I200`: `|NUM_LCTO|DT_LCTO|VL_LCTO|IND_LCTO|` (número **antes** da data)
- `I050`: `|DT_ALT|COD_NAT|IND_CTA|NIVEL|COD_CTA|COD_CTA_SUP|NOME|`

**Erro típico (50 importação):** `I051` sem `0000.COD_PLAN_REF`, contagem de campos no `0000`/`I030`, CNPJ inválido.

### Paridade T222 — caminhos práticos

1. **Curto (agente):** validar o mesmo cenário na SOFIA com `PVA_REGRA_VALIDACAO_VL_LCTO_DEB_ERRO.txt` e gravar o `ruleId`.
2. **Oficial (PVA):** pegar um ECD **real seu que já importa** no PVA, alterar **um** `I200` (campo 04 = VL_LCTO) e **um** conjunto `I250` para forçar soma débitos ≠ VL_LCTO; importar de novo e copiar a mensagem para `evidence/pva-parity/.../log.txt`.
3. **Leiaute 9:** pasta `ecds-teste-pva-leiaute9/` (importação PVA); o script T222 **prefere** esse arquivo quando existir.

Script de checklist (copia fixture para evidência):

```powershell
./deploy/scripts/Invoke-PvaParityT222.ps1 -RuleCode REGRA_VALIDACAO_VL_LCTO_DEB
```
