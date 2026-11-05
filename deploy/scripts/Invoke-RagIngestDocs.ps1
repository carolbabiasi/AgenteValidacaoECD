<#
.SYNOPSIS
  Ingere os documentos oficiais de docs/ na Api.RAG (requer host em http://localhost:5201).

.EXAMPLE
  ./deploy/scripts/Invoke-RagIngestDocs.ps1
#>
[CmdletBinding()]
param(
    [string]$BaseUrl = 'http://localhost:5201',
    [int]$PollSeconds = 5,
    [int]$TimeoutMinutes = 90
)

$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Set-Location $root

$token = & "$PSScriptRoot/New-DevJwt.ps1" -Service Rag -Roles RagAdmin
$headers = @{ Authorization = "Bearer $token" }

function Submit-Ingestion {
    param(
        [string]$FilePath,
        [string]$ManualVersion,
        [string]$DocumentKind,
        [string]$DocumentCode
    )

    if (-not (Test-Path $FilePath)) {
        throw "Arquivo não encontrado: $FilePath"
    }

    $curlArgs = @(
        '-s', '-X', 'POST', "$BaseUrl/api/v1/ingestions",
        '-H', "Authorization: Bearer $token",
        '-F', "file=@$FilePath",
        '-F', "manualVersion=$ManualVersion",
        '-F', 'layoutFamily=ECD',
        '-F', 'activateOnSuccess=true',
        '-F', "documentKind=$DocumentKind",
        '-F', "documentCode=$DocumentCode"
    )

    $json = curl.exe @curlArgs | ConvertFrom-Json
    if (-not $json.ingestionJobId) {
        throw "Resposta inesperada ao ingerir $FilePath"
    }

    return [guid]$json.ingestionJobId
}

function Wait-Ingestion {
    param([guid]$JobId, [string]$Label)

    $deadline = (Get-Date).AddMinutes($TimeoutMinutes)
    while ((Get-Date) -lt $deadline) {
        $statusJson = curl.exe -s "$BaseUrl/api/v1/ingestions/$JobId" -H "Authorization: Bearer $token" | ConvertFrom-Json
        $state = $statusJson.status
        Write-Host "[$Label] $state — created=$($statusJson.chunksCreated) skipped=$($statusJson.chunksSkipped)"
        if ($state -eq 'Completed') {
            return $statusJson
        }

        if ($state -eq 'Failed') {
            throw "Ingestão $Label falhou: $($statusJson.errorLog)"
        }

        Start-Sleep -Seconds $PollSeconds
    }

    throw "Timeout aguardando ingestão $Label ($JobId)"
}

$jobs = @(
    @{
        Label = 'Manual maio/2026'
        File  = 'docs/Manual_de_Orientação_da_ECD_Leiaute_9_maio_2026.pdf'
        Version = 'ECD-L9-2026-05'
        Kind  = 'Manual'
        Code  = 'MANUAL'
    },
    @{
        Label = 'FAQ RFB'
        File  = 'docs/Perguntas_Frequentes_ECD_RFB.md'
        Version = 'FAQ-RFB-2026-09'
        Kind  = 'Faq'
        Code  = 'FAQ-RFB'
    },
    @{
        Label = 'IN RFB 2003'
        File  = 'docs/IN_RFB_2003_2021_vigente.md'
        Version = 'IN-RFB-2003-2021'
        Kind  = 'Norm'
        Code  = 'IN-RFB-2003-2021'
    },
    @{
        Label = 'ITG 2000 R1'
        File  = 'docs/ITG_2000_R1_Escrituracao_Contabil_CFC.pdf'
        Version = 'ITG-2000-R1'
        Kind  = 'Norm'
        Code  = 'ITG-2000-R1'
    }
)

foreach ($item in $jobs) {
    Write-Host "Enviando $($item.Label)..."
    $jobId = Submit-Ingestion -FilePath $item.File -ManualVersion $item.Version -DocumentKind $item.Kind -DocumentCode $item.Code
    Wait-Ingestion -JobId $jobId -Label $item.Label | Out-Null
}

Write-Host 'Ingestões concluídas.'
