#Requires -Version 7.0
<#
.SYNOPSIS
  Checklist T222 — evidência de paridade PVA para regras VL_LCTO (épico 002).

.DESCRIPTION
  Copia fixtures para uma pasta de trabalho, lista passos no PVA 10.4.1 e
  prepara arquivos de log em specs/002-regras-oficiais-pva-ecd/evidence/pva-parity/.

.EXAMPLE
  ./deploy/scripts/Invoke-PvaParityT222.ps1 -RuleCode REGRA_VALIDACAO_VL_LCTO_DEB
#>
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet(
        'REGRA_VALIDACAO_VL_LCTO_DEB',
        'REGRA_VALIDACAO_VL_LCTO_CRED',
        'REGRA_VALIDACAO_VL_LCTO_ESC_AUXILIAR')]
    [string] $RuleCode
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$fixture = switch ($RuleCode) {
    'REGRA_VALIDACAO_VL_LCTO_DEB' { 'PVA_REGRA_VALIDACAO_VL_LCTO_DEB_ERRO.txt' }
    'REGRA_VALIDACAO_VL_LCTO_CRED' { 'PVA_REGRA_VALIDACAO_VL_LCTO_CRED_ERRO.txt' }
    'REGRA_VALIDACAO_VL_LCTO_ESC_AUXILIAR' { 'PVA_REGRA_VALIDACAO_VL_LCTO_ESC_AUXILIAR_ERRO.txt' }
}

$fixturePath = Join-Path $root "ecds-teste-pva/$fixture"
if (-not (Test-Path $fixturePath)) {
    throw "Fixture não encontrado: $fixturePath"
}

$evidenceDir = Join-Path $root "specs/002-regras-oficiais-pva-ecd/evidence/pva-parity/$RuleCode"
New-Item -ItemType Directory -Force -Path $evidenceDir | Out-Null
$workCopy = Join-Path $evidenceDir "fixture-usado.txt"
Copy-Item -Force $fixturePath $workCopy

Write-Host @"

=== T222 Paridade PVA — $RuleCode ===
PVA: Sped Contábil 10.4.1 (JVM 21+)
Fixture copiado: $workCopy

Passos:
  1. Abrir o PVA ECD e importar o arquivo acima (Importar / Validar).
  2. Localizar a mensagem da regra '$RuleCode' na lista de erros/avisos.
  3. Copiar texto literal para: $evidenceDir/log.txt (ou screenshot.png).
  4. Preencher notas.md com data, versão PVA e se bate com o agente (RuleId igual).
  5. Atualizar rule-matrix.md: status -> Paridade PVA OK quando confirmado.

Agente (produção/local): validar o mesmo arquivo e comparar ruleId + severidade.

"@
