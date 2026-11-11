# Evidências de paridade PVA ECD

**PVA de referência:** Sped Contábil **10.4.1** (JVM 21.0.3)

## Estrutura

```
pva-parity/
  REGRA_VALIDACAO_VL_LCTO_DEB/
    screenshot.png ou log.txt
    notas.md          # versão PVA, data, arquivo ECD usado
```

## Passos

1. Importar o fixture `ecds-teste-pva/PVA_{CODIGO}.txt` no PVA.
2. Executar validação até a mensagem da regra aparecer.
3. Copiar a mensagem **exatamente** como exibida (código + texto).
4. Comparar com o achado do agente (`RuleId` = código oficial).

Não commitar arquivos ECD com dados reais de clientes — apenas fixtures sintéticas do repositório.

**Importação PVA:** erros do tipo “campo obrigatório / quantidade de campos / bloco não importado” são **layout de importação** — o arquivo precisa seguir o leiaute 9 completo (base `ECD_001_BASICA`). Os fixtures em `ecds-teste-pva/` foram ampliados para isso; use sempre a cópia gerada por `Invoke-PvaParityT222.ps1`.

## Automação local

```powershell
./deploy/scripts/Invoke-PvaParityT222.ps1 -RuleCode REGRA_VALIDACAO_VL_LCTO_DEB
```

