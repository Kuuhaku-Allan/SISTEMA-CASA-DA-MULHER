[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BackupPath,

    [string]$ConnectionString = $env:STAGING_DATABASE_URL,

    [switch]$Force
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

    throw "Configure STAGING_DATABASE_URL ou ConnectionStrings__DefaultConnection antes de rodar o restore."
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

function Expand-BackupSql {
    param(
        [string]$ResolvedBackupPath,
        [string]$TempDirectory
    )

    if ($ResolvedBackupPath.EndsWith(".zip", [StringComparison]::OrdinalIgnoreCase)) {
        Expand-Archive -LiteralPath $ResolvedBackupPath -DestinationPath $TempDirectory -Force
        $sqlFile = Get-ChildItem -Path $TempDirectory -Filter "*.sql" -File | Select-Object -First 1

        if (-not $sqlFile) {
            throw "Nenhum arquivo .sql foi encontrado dentro do backup zip."
        }

        return $sqlFile.FullName
    }

    if ($ResolvedBackupPath.EndsWith(".gz", [StringComparison]::OrdinalIgnoreCase)) {
        $sqlPath = Join-Path $TempDirectory ([IO.Path]::GetFileNameWithoutExtension($ResolvedBackupPath))
        $inputStream = [IO.File]::OpenRead($ResolvedBackupPath)

        try {
            $gzipStream = [IO.Compression.GzipStream]::new($inputStream, [IO.Compression.CompressionMode]::Decompress)
            $outputStream = [IO.File]::Create($sqlPath)

            try {
                $gzipStream.CopyTo($outputStream)
            } finally {
                $outputStream.Dispose()
                $gzipStream.Dispose()
            }
        } finally {
            $inputStream.Dispose()
        }

        return $sqlPath
    }

    if ($ResolvedBackupPath.EndsWith(".sql", [StringComparison]::OrdinalIgnoreCase)) {
        return $ResolvedBackupPath
    }

    throw "Formato de backup nao suportado. Use .zip, .sql.gz ou .sql."
}

$configuredConnectionString = Get-ConfiguredConnectionString $ConnectionString
$pgConnectionString = Convert-ToPgClientConnectionString $configuredConnectionString
$psql = Get-Command psql -ErrorAction SilentlyContinue

if (-not $psql) {
    throw "psql nao foi encontrado no PATH. Instale o cliente PostgreSQL antes de rodar o restore."
}

$resolvedBackup = (Resolve-Path -LiteralPath $BackupPath).Path
$tempDirectory = Join-Path ([IO.Path]::GetTempPath()) "casamulher-restore-$((Get-Date).ToUniversalTime().ToString("yyyyMMdd-HHmmss"))"
New-Item -ItemType Directory -Force -Path $tempDirectory | Out-Null

try {
    $sqlPath = Expand-BackupSql $resolvedBackup $tempDirectory

    if (-not $Force) {
        Write-Host "ATENCAO: este restore vai executar o SQL do backup no banco configurado."
        Write-Host "Confira se a connection string aponta para o banco correto e, de preferencia, vazio."
        $confirmation = Read-Host "Digite RESTAURAR para continuar"

        if ($confirmation -ne "RESTAURAR") {
            throw "Restore cancelado pelo usuario."
        }
    }

    & $psql.Source `
        --dbname="$pgConnectionString" `
        --set=ON_ERROR_STOP=on `
        --file="$sqlPath"

    if ($LASTEXITCODE -ne 0) {
        throw "psql terminou com codigo $LASTEXITCODE."
    }

    Write-Host "Restore concluido."
} finally {
    if (Test-Path -LiteralPath $tempDirectory) {
        Remove-Item -LiteralPath $tempDirectory -Recurse -Force
    }
}
