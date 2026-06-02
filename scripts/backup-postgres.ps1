[CmdletBinding()]
param(
    [string]$ConnectionString = $env:STAGING_DATABASE_URL,
    [string]$OutputDirectory,
    [int]$KeepLast = 14
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-ConfiguredConnectionString {
    param([string]$ConfiguredValue)

    if (-not [string]::IsNullOrWhiteSpace($ConfiguredValue)) {
        return $ConfiguredValue
    }

    if (-not [string]::IsNullOrWhiteSpace($env:ConnectionStrings__DefaultConnection)) {
        return $env:ConnectionStrings__DefaultConnection
    }

    throw "Configure STAGING_DATABASE_URL ou ConnectionStrings__DefaultConnection antes de rodar o backup."
}

function Escape-LibpqValue {
    param([string]$Value)

    return "'" + (($Value -replace "\\", "\\\\") -replace "'", "\\'") + "'"
}

function Convert-ToPgClientConnectionString {
    param([string]$RawConnectionString)

    $trimmed = $RawConnectionString.Trim()

    if ($trimmed -match "^(postgres|postgresql)://") {
        return $trimmed
    }

    $parts = @{}

    foreach ($piece in $trimmed -split ";") {
        if ([string]::IsNullOrWhiteSpace($piece)) {
            continue
        }

        $separatorIndex = $piece.IndexOf("=")

        if ($separatorIndex -lt 1) {
            continue
        }

        $key = $piece.Substring(0, $separatorIndex).Trim().ToLowerInvariant()
        $value = $piece.Substring($separatorIndex + 1).Trim()
        $parts[$key] = $value
    }

    $hostValue = Get-FirstConnectionValue $parts @("host", "server")
    $portValue = Get-FirstConnectionValue $parts @("port")
    $databaseValue = Get-FirstConnectionValue $parts @("database", "dbname")
    $userValue = Get-FirstConnectionValue $parts @("username", "user id", "userid", "user")
    $passwordValue = Get-FirstConnectionValue $parts @("password", "pwd")
    $sslModeValue = Get-FirstConnectionValue $parts @("ssl mode", "sslmode")

    if ([string]::IsNullOrWhiteSpace($hostValue) -or
        [string]::IsNullOrWhiteSpace($databaseValue) -or
        [string]::IsNullOrWhiteSpace($userValue)) {
        throw "Use uma URL postgresql://... em STAGING_DATABASE_URL ou uma connection string Npgsql com Host, Database e Username."
    }

    $items = @(
        "host=$(Escape-LibpqValue $hostValue)",
        "dbname=$(Escape-LibpqValue $databaseValue)",
        "user=$(Escape-LibpqValue $userValue)"
    )

    if (-not [string]::IsNullOrWhiteSpace($portValue)) {
        $items += "port=$(Escape-LibpqValue $portValue)"
    }

    if (-not [string]::IsNullOrWhiteSpace($passwordValue)) {
        $items += "password=$(Escape-LibpqValue $passwordValue)"
    }

    if (-not [string]::IsNullOrWhiteSpace($sslModeValue)) {
        $normalizedSslMode = $sslModeValue.ToLowerInvariant()
        $items += "sslmode=$(Escape-LibpqValue $normalizedSslMode)"
    }

    return $items -join " "
}

function Get-FirstConnectionValue {
    param(
        [hashtable]$Parts,
        [string[]]$Keys
    )

    foreach ($key in $Keys) {
        if ($Parts.ContainsKey($key)) {
            return $Parts[$key]
        }
    }

    return $null
}

$configuredConnectionString = Get-ConfiguredConnectionString $ConnectionString
$pgConnectionString = Convert-ToPgClientConnectionString $configuredConnectionString
$pgDump = Get-Command pg_dump -ErrorAction SilentlyContinue

if (-not $pgDump) {
    throw "pg_dump nao foi encontrado no PATH. Instale o cliente PostgreSQL antes de rodar o backup."
}

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $repositoryRoot = Split-Path -Parent $PSScriptRoot
    $OutputDirectory = Join-Path $repositoryRoot "backups"
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

$timestamp = (Get-Date).ToUniversalTime().ToString("yyyyMMdd-HHmmss")
$sqlPath = Join-Path $OutputDirectory "casamulher-postgres-$timestamp.sql"
$zipPath = Join-Path $OutputDirectory "casamulher-postgres-$timestamp.zip"

try {
    & $pgDump.Source `
        --dbname="$pgConnectionString" `
        --format=plain `
        --no-owner `
        --no-privileges `
        --file="$sqlPath"

    if ($LASTEXITCODE -ne 0) {
        throw "pg_dump terminou com codigo $LASTEXITCODE."
    }

    Compress-Archive -Path $sqlPath -DestinationPath $zipPath -CompressionLevel Optimal -Force
    Remove-Item -LiteralPath $sqlPath -Force

    if ($KeepLast -gt 0) {
        Get-ChildItem -Path $OutputDirectory -Filter "casamulher-postgres-*.zip" |
            Sort-Object LastWriteTime -Descending |
            Select-Object -Skip $KeepLast |
            Remove-Item -Force
    }

    Write-Host "Backup gerado: $zipPath"
} catch {
    if (Test-Path -LiteralPath $sqlPath) {
        Remove-Item -LiteralPath $sqlPath -Force
    }

    throw
}
