<#
.SYNOPSIS
  Emite JWT HS256 de homologação para Api.Agente ou Api.RAG.

.DESCRIPTION
  Usa issuer/audience/chave dev de appsettings.json (DEV-ONLY). Não use em produção.

.EXAMPLE
  $token = ./deploy/scripts/New-DevJwt.ps1 -Service Agente -UserId smoke-user
  $admin = ./deploy/scripts/New-DevJwt.ps1 -Service Rag -Roles RagAdmin
#>
[CmdletBinding()]
param(
    [ValidateSet('Agente', 'Rag')]
    [string]$Service = 'Agente',

    [string]$UserId = 'smoke-user',

    [string[]]$Roles,

    [int]$ExpiresMinutes = 60,

    [string]$SigningKey
)

$defaults = @{
    Agente = @{
        Issuer     = 'AgenteValidacaoECD-Dev'
        SigningKey = 'DEV-ONLY-ECD-AGENTE-SIGNING-KEY-MIN-32-CHARS!!'
        Roles      = @('Validator')
    }
    Rag    = @{
        Issuer     = 'AgenteValidacaoECD-Rag-Dev'
        SigningKey = 'DEV-ONLY-ECD-RAG-SIGNING-KEY-MIN-32-CHARS!!!!'
        Roles      = @('RagAdmin')
    }
}[$Service]

if (-not $Roles) { $Roles = $defaults.Roles }
if (-not $SigningKey) { $SigningKey = $defaults.SigningKey }

function ConvertTo-Base64Url([byte[]]$bytes) {
    [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}

$now = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$header = @{ alg = 'HS256'; typ = 'JWT' } | ConvertTo-Json -Compress
$payload = [ordered]@{
    sub  = $UserId
    role = @($Roles)
    iss  = $defaults.Issuer
    aud  = $defaults.Issuer
    nbf  = $now
    iat  = $now
    exp  = $now + ($ExpiresMinutes * 60)
} | ConvertTo-Json -Compress

$utf8 = [Text.Encoding]::UTF8
$unsigned = "$(ConvertTo-Base64Url $utf8.GetBytes($header)).$(ConvertTo-Base64Url $utf8.GetBytes($payload))"

$hmac = [Security.Cryptography.HMACSHA256]::new($utf8.GetBytes($SigningKey))
try {
    $signature = ConvertTo-Base64Url $hmac.ComputeHash($utf8.GetBytes($unsigned))
}
finally {
    $hmac.Dispose()
}

"$unsigned.$signature"
