#Requires -Version 5.1
<#
.SYNOPSIS
  Migrations Supabase + variáveis Production na Vercel (Agente SOFIA).

.DESCRIPTION
  1. Lê deploy/local/supabase.env (ou -ConnectionString).
  2. Aplica migrations (operational, metrics, rag).
  3. Cadastra env vars na Vercel (Production) e opcionalmente redeploy.

.EXAMPLE
  Copy-Item deploy/local/supabase.env.example deploy/local/supabase.env
  # edite SUPABASE_DATABASE_URL
  ./deploy/scripts/Invoke-SofiaCloudBootstrap.ps1

.EXAMPLE
  $env:SUPABASE_DATABASE_URL = "Host=db....;Password=...;SSL Mode=Require;Trust Server Certificate=true"
  ./deploy/scripts/Invoke-SofiaCloudBootstrap.ps1 -SkipDeploy
#>
[CmdletBinding()]
param(
    [string]$ConnectionString = $env:SUPABASE_DATABASE_URL,
    [string]$EnvFile = "",
    [string]$RagBaseUrl = "",
    [string]$AgentPublicUrl = "https://infis-agente-validacao-ecd.vercel.app",
    [switch]$SkipVercel,
    [switch]$SkipDeploy
)

$ErrorActionPreference = "Stop"
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$defaultEnvFile = Join-Path $root "deploy/local/supabase.env"

function Read-DotEnvFile {
    param([string]$Path)
    $map = @{}
    if (-not (Test-Path $Path)) { return $map }
    Get-Content $Path | ForEach-Object {
        $line = $_.Trim()
        if ($line -eq "" -or $line.StartsWith("#")) { return }
        $idx = $line.IndexOf("=")
        if ($idx -lt 1) { return }
        $k = $line.Substring(0, $idx).Trim()
        $v = $line.Substring($idx + 1).Trim()
        $map[$k] = $v
    }
    return $map
}

function New-Secret {
    param([int]$Bytes = 32)
    $b = New-Object byte[] $Bytes
    [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b)
    return [Convert]::ToBase64String($b).TrimEnd("=").Replace("+", "-").Replace("/", "_")
}

function Set-VercelEnv {
    param(
        [string]$Name,
        [string]$Value,
        [string]$Environment = "production"
    )
    if ($SkipVercel) { return }
    Write-Host "Vercel env: $Name ($Environment)"
    $Value | vercel env add $Name $Environment --force 2>&1 | Out-Host
}

if (-not $EnvFile) { $EnvFile = $defaultEnvFile }
$fromFile = Read-DotEnvFile $EnvFile

if (-not $ConnectionString -and $fromFile.ContainsKey("SUPABASE_DATABASE_URL")) {
    $ConnectionString = $fromFile["SUPABASE_DATABASE_URL"]
}
if (-not $ConnectionString) {
    throw "Defina SUPABASE_DATABASE_URL, -ConnectionString ou deploy/local/supabase.env"
}

if (-not $RagBaseUrl -and $fromFile.ContainsKey("RAG_BASE_URL")) {
    $RagBaseUrl = $fromFile["RAG_BASE_URL"]
}
if (-not $RagBaseUrl) {
    $RagBaseUrl = "http://localhost:5201"
    Write-Warning "RAG_BASE_URL não definido — usando $RagBaseUrl (SOFIA precisa de RAG público em prod)."
}

$sofiaKey = $fromFile["SOFIA_A2A_SERVICE_KEY"]
if (-not $sofiaKey) { $sofiaKey = New-Secret }

$authJwt = $fromFile["AUTH_JWT_SIGNING_KEY"]
if (-not $authJwt) { $authJwt = (New-Secret) + (New-Secret) }

$ragJwt = $fromFile["RAG_JWT_SIGNING_KEY"]
if (-not $ragJwt) { $ragJwt = (New-Secret) + (New-Secret) }

$openAi = $fromFile["OPENAI_API_KEY"]

Push-Location $root
try {
    Write-Host "=== Migrations (Supabase/Postgres) ==="
    & "$PSScriptRoot/Invoke-SupabaseMigrations.ps1" -ConnectionString $ConnectionString

    if (-not $SkipVercel) {
        Write-Host "=== Vercel Production env ==="
        Set-VercelEnv "ConnectionStrings__Operational" $ConnectionString
        Set-VercelEnv "ConnectionStrings__Metrics" $ConnectionString
        Set-VercelEnv "Rag__BaseUrl" $RagBaseUrl
        Set-VercelEnv "Rag__Jwt__SigningKey" $ragJwt
        Set-VercelEnv "Auth__Jwt__SigningKey" $authJwt
        Set-VercelEnv "Sofia__PublicBaseUrl" $AgentPublicUrl
        Set-VercelEnv "Sofia__A2aServiceKey" $sofiaKey
        if ($openAi) {
            Set-VercelEnv "OpenAI__ApiKey" $openAi
        }
        Write-Host ""
        Write-Host "Guarde para a SOFIA (header X-Sofia-Service-Key):"
        Write-Host $sofiaKey
        Write-Host ""
        Write-Host "Api.RAG na nuvem deve usar a mesma ConnectionStrings__Rag e Rag JWT SigningKey."
    }

    if (-not $SkipDeploy -and -not $SkipVercel) {
        Write-Host "=== Redeploy Production ==="
        vercel deploy --prod 2>&1 | Out-Host
    }

    Write-Host "OK."
}
finally {
    Pop-Location
}
