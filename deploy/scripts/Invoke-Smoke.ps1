<#
.SYNOPSIS
  Smoke end-to-end do quickstart (Api.RAG + Api.Agente já em execução).

.EXAMPLE
  ./deploy/scripts/Invoke-Smoke.ps1
#>
[CmdletBinding()]
param(
    [string]$AgenteUrl = 'http://localhost:5100',
    [string]$RagUrl = 'http://localhost:5201',
    [int]$TimeoutSeconds = 60
)

$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '../..')
$fixtures = Join-Path $root 'fixtures'
$work = Join-Path $root 'storage/smoke'
New-Item -ItemType Directory -Force $work | Out-Null
$utf8NoBom = [Text.UTF8Encoding]::new($false)

$runId = [Guid]::NewGuid().ToString('N').Substring(0, 8)
$userId = "smoke-$runId"
$agenteToken = & (Join-Path $PSScriptRoot 'New-DevJwt.ps1') -Service Agente -UserId $userId
$intruderToken = & (Join-Path $PSScriptRoot 'New-DevJwt.ps1') -Service Agente -UserId "intruder-$runId"
$ragToken = & (Join-Path $PSScriptRoot 'New-DevJwt.ps1') -Service Rag -UserId "rag-admin-$runId" -Roles RagAdmin

$results = [System.Collections.Generic.List[object]]::new()

function Invoke-Api {
    param([string]$Method, [string]$Url, [string]$Token, [string[]]$Form, [string]$JsonBody, [string]$OutFile)

    $bodyFile = if ($OutFile) { $OutFile } else { Join-Path $work "resp-$([Guid]::NewGuid().ToString('N')).tmp" }
    $curlArgs = @('-s', '-X', $Method, $Url, '-H', "Authorization: Bearer $Token", '-o', $bodyFile, '-w', '%{http_code}')
    foreach ($field in $Form) { $curlArgs += @('-F', $field) }
    if ($JsonBody) {
        $jsonFile = Join-Path $work "req-$([Guid]::NewGuid().ToString('N')).json"
        [IO.File]::WriteAllText($jsonFile, $JsonBody, $utf8NoBom)
        $curlArgs += @('-H', 'Content-Type: application/json', '--data-binary', "@$jsonFile")
    }

    $code = [int](& curl.exe @curlArgs)
    $text = if (Test-Path $bodyFile) { Get-Content -Raw $bodyFile } else { '' }
    if (-not $OutFile) { Remove-Item $bodyFile -ErrorAction SilentlyContinue }
    $json = $null
    if ($text -and $text.TrimStart().StartsWith('{')) { $json = $text | ConvertFrom-Json }
    [pscustomobject]@{ Status = $code; Text = $text; Json = $json }
}

function Add-Check([string]$Step, [bool]$Passed, [string]$Detail) {
    $results.Add([pscustomobject]@{ Step = $Step; Passed = $Passed; Detail = $Detail })
    $mark = if ($Passed) { 'PASS' } else { 'FAIL' }
    Write-Host "[$mark] $Step — $Detail"
}

function Wait-Status([string]$Url, [string]$Token, [string[]]$Terminal) {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        $resp = Invoke-Api -Method GET -Url $Url -Token $Token
        if ($resp.Json -and $Terminal -contains $resp.Json.status) { return $resp }
        Start-Sleep -Milliseconds 500
    } while ((Get-Date) -lt $deadline)
    return $resp
}

function New-UniqueEcd([string]$Template) {
    $path = Join-Path $work "ecd-$([Guid]::NewGuid().ToString('N')).txt"
    $content = (Get-Content -Raw (Join-Path $fixtures $Template)) + "|9900|SMOKE|$([Guid]::NewGuid().ToString('N'))|`r`n"
    [IO.File]::WriteAllText($path, $content, $utf8NoBom)
    $path
}

# 1. Ingestão do manual (RAG admin)
$ingest = Invoke-Api -Method POST -Url "$RagUrl/api/v1/ingestions" -Token $ragToken -Form @(
    "file=@$(Join-Path $fixtures 'manual-ecd-sample.md')",
    "manualVersion=ECD-Manual-SMOKE-$runId",
    'layoutFamily=ECD',
    'activateOnSuccess=true')
Add-Check 'RAG POST /ingestions' ($ingest.Status -eq 202) "HTTP $($ingest.Status)"
if ($ingest.Status -eq 202) {
    $job = Wait-Status -Url "$RagUrl/api/v1/ingestions/$($ingest.Json.ingestionJobId)" -Token $ragToken -Terminal @('Completed', 'Failed')
    Add-Check 'RAG ingestão concluída' ($job.Json.status -eq 'Completed') "status=$($job.Json.status) body=$($job.Text)"
}

# 2. Validação feliz
$happyFile = New-UniqueEcd 'ecd-sample.txt'
$submit = Invoke-Api -Method POST -Url "$AgenteUrl/api/v1/validations" -Token $agenteToken -Form @("file=@$happyFile")
Add-Check 'Agente POST /validations' ($submit.Status -eq 202) "HTTP $($submit.Status) executionId=$($submit.Json.executionId)"
$executionId = $submit.Json.executionId

$status = Wait-Status -Url "$AgenteUrl/api/v1/validations/$executionId" -Token $agenteToken -Terminal @('Completed', 'Failed', 'Cancelled')
Add-Check 'Execução Completed' ($status.Json.status -eq 'Completed') "status=$($status.Json.status) layout=$($status.Json.detectedLayoutVersion) rules=$($status.Json.summary.rulesExecuted) findings=$($status.Json.summary.findingsCount) manual=$($status.Json.manualVersionUsed) durationMs=$($status.Json.operational.durationMs)"

$report = Invoke-Api -Method GET -Url "$AgenteUrl/api/v1/validations/$executionId/report" -Token $agenteToken -OutFile (Join-Path $work "report-$runId.json")
$hasSections = $report.Json -and ($null -ne $report.Json.findings) -and ($null -ne $report.Json.limitations) -and ($null -ne $report.Json.skippedValidations)
Add-Check 'Relatório JSON com findings/limitations/skippedValidations' ($report.Status -eq 200 -and $hasSections) "HTTP $($report.Status) findings=$(@($report.Json.findings).Count) limitations=$(@($report.Json.limitations).Count) skipped=$(@($report.Json.skippedValidations).Count)"
$ragEvidence = ($status.Json.manualVersionUsed) -or (@($report.Json.limitations).Count -gt 0)
Add-Check 'SC-007 manualVersionUsed ou limitations' $ragEvidence "manualVersionUsed=$($status.Json.manualVersionUsed)"
$ragUnavailable = @($report.Json.limitations | Where-Object { "$_" -match 'RAG_UNAVAILABLE' }).Count -gt 0
Add-Check 'RAG consultado via MCP (sem RAG_UNAVAILABLE)' ((-not $ragUnavailable) -and [bool]$status.Json.manualVersionUsed) "manualVersionUsed=$($status.Json.manualVersionUsed) ragUnavailable=$ragUnavailable"

$csvPath = Join-Path $work "report-$runId.csv"
$export = Invoke-Api -Method GET -Url "$AgenteUrl/api/v1/validations/$executionId/report/export?format=csv" -Token $agenteToken -OutFile $csvPath
Add-Check 'Export CSV' ($export.Status -eq 200) "HTTP $($export.Status) bytes=$((Get-Item $csvPath).Length)"

$history = Invoke-Api -Method GET -Url "$AgenteUrl/api/v1/validations?page=1&pageSize=20" -Token $agenteToken
$inHistory = @($history.Json.items | Where-Object { $_.executionId -eq $executionId }).Count -eq 1
Add-Check 'Histórico paginado contém execução' ($history.Status -eq 200 -and $inHistory) "HTTP $($history.Status) totalCount=$($history.Json.totalCount)"

$peer = Invoke-Api -Method GET -Url "$AgenteUrl/api/v1/validations/$executionId" -Token $intruderToken
Add-Check 'RBAC: outro usuário recebe 403' ($peer.Status -eq 403) "HTTP $($peer.Status)"

# 3. Duplicidade
$duplicate = Invoke-Api -Method POST -Url "$AgenteUrl/api/v1/validations" -Token $agenteToken -Form @("file=@$happyFile")
Add-Check 'Reenvio mesmo hash → 409 DUPLICATE_SUBMISSION' ($duplicate.Status -eq 409 -and $duplicate.Text -match 'DUPLICATE_SUBMISSION') "HTTP $($duplicate.Status)"

# 4. Cancelamento
$cancelDone = Invoke-Api -Method DELETE -Url "$AgenteUrl/api/v1/validations/$executionId" -Token $agenteToken
Add-Check 'DELETE em Completed → 409 NOT_CANCELLABLE' ($cancelDone.Status -eq 409 -and $cancelDone.Text -match 'NOT_CANCELLABLE') "HTTP $($cancelDone.Status)"

$cancelFile = New-UniqueEcd 'ecd-sample.txt'
$toCancel = Invoke-Api -Method POST -Url "$AgenteUrl/api/v1/validations" -Token $agenteToken -Form @("file=@$cancelFile")
$cancel = Invoke-Api -Method DELETE -Url "$AgenteUrl/api/v1/validations/$($toCancel.Json.executionId)" -Token $agenteToken
if ($cancel.Status -eq 200) {
    $afterCancel = Invoke-Api -Method GET -Url "$AgenteUrl/api/v1/validations/$($toCancel.Json.executionId)" -Token $agenteToken
    Add-Check 'DELETE logo após submit → Cancelled' ($afterCancel.Json.status -eq 'Cancelled') "previousStatus=$($cancel.Json.previousStatus) status=$($afterCancel.Json.status)"
    $resubmit = Invoke-Api -Method POST -Url "$AgenteUrl/api/v1/validations" -Token $agenteToken -Form @("file=@$cancelFile")
    Add-Check 'Reenvio após cancelamento aceito' ($resubmit.Status -eq 202) "HTTP $($resubmit.Status)"
}
else {
    Add-Check 'DELETE logo após submit → Cancelled' $false "worker concluiu antes do DELETE: HTTP $($cancel.Status) $($cancel.Text)"
}

# 5. Regra SAMPLE de abertura ausente
$missingFile = New-UniqueEcd 'ecd-sample-missing-opening.txt'
$missing = Invoke-Api -Method POST -Url "$AgenteUrl/api/v1/validations" -Token $agenteToken -Form @("file=@$missingFile")
$missingStatus = Wait-Status -Url "$AgenteUrl/api/v1/validations/$($missing.Json.executionId)" -Token $agenteToken -Terminal @('Completed', 'Failed', 'Cancelled')
$missingReport = Invoke-Api -Method GET -Url "$AgenteUrl/api/v1/validations/$($missing.Json.executionId)/report" -Token $agenteToken
$high = @($missingReport.Json.findings | Where-Object { "$($_.severity)" -match 'High|Alta' }).Count
Add-Check 'Abertura ausente gera achado High' ($missingStatus.Json.status -eq 'Completed' -and $high -ge 1) "status=$($missingStatus.Json.status) findings=$(@($missingReport.Json.findings).Count) high=$high"

# 6. Chat
$chat = Invoke-Api -Method POST -Url "$AgenteUrl/api/v1/validations/$($missing.Json.executionId)/chat" -Token $agenteToken -JsonBody '{"message":"Resuma os achados desta validação."}'
Add-Check 'Chat pós-validação' ($chat.Status -eq 200 -and $chat.Json.sessionId) "HTTP $($chat.Status) disclaimer=$($chat.Json.disclaimer)"

Get-ChildItem $work -Filter 'req-*.json' | Remove-Item -ErrorAction SilentlyContinue

$failed = @($results | Where-Object { -not $_.Passed }).Count
Write-Host ''
Write-Host "Smoke $runId — $($results.Count - $failed)/$($results.Count) checks OK"
$results
if ($failed -gt 0) { exit 1 }
