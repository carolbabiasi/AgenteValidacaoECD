#Requires -Version 5.1
<#
.SYNOPSIS
  Aplica migrations EF (operational, metrics, rag) no Postgres Supabase.

.EXAMPLE
  $env:SUPABASE_DATABASE_URL = "Host=db.xxx.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=***;SSL Mode=Require;Trust Server Certificate=true"
  ./deploy/scripts/Invoke-SupabaseMigrations.ps1
#>
param(
    [string]$ConnectionString = $env:SUPABASE_DATABASE_URL
)

$ErrorActionPreference = "Stop"
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
if (-not $ConnectionString) {
    throw "Defina SUPABASE_DATABASE_URL ou passe -ConnectionString (Settings → Database no Supabase)."
}

$env:ConnectionStrings__Operational = $ConnectionString
$env:ConnectionStrings__Metrics = $ConnectionString
$env:ConnectionStrings__Rag = $ConnectionString

function Invoke-EfUpdate {
    param([string]$Context, [string]$Project, [string]$StartupProject)

    Write-Host "$Context..."
    dotnet ef database update --project $Project --startup-project $StartupProject --context $Context

    # dotnet ef e nativo: ErrorActionPreference nao captura o codigo de saida.
    if ($LASTEXITCODE -ne 0) {
        throw "Falha ao aplicar migrations do $Context (dotnet ef retornou $LASTEXITCODE)."
    }
}

Push-Location $root
try {
    Invoke-EfUpdate "OperationalDbContext" "src/Api.Agente.Infrastructure" "src/Api.Agente.Host"

    Invoke-EfUpdate "MetricsDbContext" "src/Api.Agente.Infrastructure" "src/Api.Agente.Host"

    Invoke-EfUpdate "RagDbContext" "src/Api.Rag.Infrastructure" "src/Api.Rag.Host"

    Write-Host "OK — schemas operational, metrics e rag no Supabase."
}
finally {
    Pop-Location
}
