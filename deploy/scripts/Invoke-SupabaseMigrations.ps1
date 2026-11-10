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

Push-Location $root
try {
    Write-Host "OperationalDbContext..."
    dotnet ef database update `
        --project src/Api.Agente.Infrastructure `
        --startup-project src/Api.Agente.Host `
        --context OperationalDbContext

    Write-Host "MetricsDbContext..."
    dotnet ef database update `
        --project src/Api.Agente.Infrastructure `
        --startup-project src/Api.Agente.Host `
        --context MetricsDbContext

    Write-Host "RagDbContext..."
    dotnet ef database update `
        --project src/Api.Rag.Infrastructure `
        --startup-project src/Api.Rag.Host `
        --context RagDbContext

    Write-Host "OK — schemas operational, metrics e rag no Supabase."
}
finally {
    Pop-Location
}
