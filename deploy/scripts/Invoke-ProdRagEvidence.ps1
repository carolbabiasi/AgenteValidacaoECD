#Requires -Version 5.1
<#
.SYNOPSIS
  Valida ECD em produção (A2A) e busca relatório com evidências RAG.
#>
[CmdletBinding()]
param(
    [string]$AgenteUrl = 'https://infis-agente-validacao-ecd.vercel.app',
    [string]$EcdFile = 'fixtures/ecd-sample-missing-opening.txt'
)

$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '../..')
$envFile = Join-Path $root 'deploy/local/supabase.env'
if (-not (Test-Path $envFile)) { throw "Arquivo $envFile não encontrado." }

function Get-EnvValue([string]$Name) {
    foreach ($line in Get-Content $envFile) {
        if ($line -match "^$Name=(.+)$") { return $matches[1].Trim() }
    }
    return $null
}

$authKey = Get-EnvValue 'AUTH_JWT_SIGNING_KEY'
if (-not $authKey) { throw 'AUTH_JWT_SIGNING_KEY obrigatório em supabase.env' }

$ecdPath = Join-Path $root $EcdFile
if (-not (Test-Path $ecdPath)) { throw "ECD não encontrado: $ecdPath" }

$tmpDir = Join-Path $root 'deploy/local/a2a-rag-evidence'
New-Item -ItemType Directory -Force -Path $tmpDir | Out-Null
$leaf = Split-Path $ecdPath -Leaf
Copy-Item $ecdPath (Join-Path $tmpDir $leaf) -Force

$prevEap = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
& dotnet run --project (Join-Path $root 'deploy/scripts/ProdEcdsA2aSmoke/ProdEcdsA2aSmoke.csproj') -c Release -- `
    $AgenteUrl $tmpDir 1>$null 2>$null
$ErrorActionPreference = $prevEap

$batch = Get-Content (Join-Path $root 'deploy/local/prod-ecds-teste-result.json') -Raw | ConvertFrom-Json
$row = $batch | Where-Object { $_.arquivo -eq $leaf } | Select-Object -First 1
if (-not $row -or -not $row.ok) { throw "A2A falhou: $($row.erro)" }

$executionId = $null
foreach ($o in @($row.observacoes)) {
    if ($o -match '^executionId=(.+)$') {
        $executionId = $matches[1].Trim()
        break
    }
}

if (-not $executionId) { throw 'executionId não encontrado nas observações SOFIA.' }

$adminJwt = & (Join-Path $PSScriptRoot 'New-DevJwt.ps1') -Service Agente -UserId 'rag-evidence' -Roles Admin -SigningKey $authKey
$status = curl.exe -s "$AgenteUrl/api/v1/validations/$executionId" -H "Authorization: Bearer $adminJwt" | ConvertFrom-Json
$reportRaw = curl.exe -s "$AgenteUrl/api/v1/validations/$executionId/report" -H "Authorization: Bearer $adminJwt"
$report = $reportRaw | ConvertFrom-Json

$ragRefs = [System.Collections.Generic.List[object]]::new()
foreach ($f in @($report.findings)) {
    foreach ($r in @($f.ragReferences)) {
        $ragRefs.Add([pscustomobject]@{
            findingTitle = $f.title
            ruleId = $f.ruleId
            severity = $f.severity
            chunkId = $r.chunkId
            manualVersion = $r.manualVersion
            hierarchyPath = $r.hierarchyPath
        })
    }
}

$narrative = $report.summary.narrative
if ($narrative.Length -gt 800) { $narrative = $narrative.Substring(0, 800) + '...' }

$out = [ordered]@{
    executionId = $executionId
    ecdFile = $EcdFile
    sofia = @{
        status = $row.status
        qtdAchados = $row.qtdAchados
        analise = $row.analise
        observacoes = @($row.observacoes)
    }
    validationStatus = $status
    limitations = @($report.limitations)
    skippedValidations = @($report.skippedValidations)
    requiresHumanReview = $report.requiresHumanReview
    summaryNarrative = $narrative
    ragReferences = $ragRefs
}

$outPath = Join-Path $root 'deploy/local/prod-rag-evidence.json'
$json = $out | ConvertTo-Json -Depth 8
Set-Content $outPath $json -Encoding UTF8
Write-Output $outPath
