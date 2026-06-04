param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$CommandArgs
)

$ErrorActionPreference = "Stop"

$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$ApiProject = Join-Path $ProjectRoot "CasaMulher.Api\CasaMulher.Api.csproj"
$ApiDir = Join-Path $ProjectRoot "CasaMulher.Api"
$FrontDir = Join-Path $ProjectRoot "projetocasadamulher\telas"
$RuntimeDir = Join-Path $ProjectRoot ".runtime"
$ApiPidFile = Join-Path $RuntimeDir "api.pid"
$FrontPidFile = Join-Path $RuntimeDir "front.pid"
$ApiUrl = "http://localhost:5001"
$FrontUrl = "http://localhost:5500"
$StatusApiUrl = "$ApiUrl/swagger/index.html"
$StatusFrontUrl = "$FrontUrl/index.html"

function Write-Info {
    param([string]$Message)
    Write-Host "[Casa da Mulher] $Message"
}

function Write-Ok {
    param([string]$Message)
    Write-Host "[OK] $Message" -ForegroundColor Green
}

function Write-Warn {
    param([string]$Message)
    Write-Host "[Aviso] $Message" -ForegroundColor Yellow
}

function Write-Fail {
    param([string]$Message)
    Write-Host "[Erro] $Message" -ForegroundColor Red
}

function Show-Help {
    Write-Host ""
    Write-Host "Uso:"
    Write-Host "  .\casa_da_mulher.cmd serve on"
    Write-Host "  .\casa_da_mulher.cmd serve off"
    Write-Host "  .\casa_da_mulher.cmd status"
    Write-Host "  .\casa_da_mulher.cmd update"
    Write-Host ""
}

function Assert-ProjectRoot {
    if (-not (Test-Path $ApiProject) -or -not (Test-Path (Join-Path $FrontDir "index.html"))) {
        throw "Este comando precisa estar dentro da raiz do projeto Sistema Casa da Mulher."
    }
}

function Ensure-RuntimeDir {
    if (-not (Test-Path $RuntimeDir)) {
        New-Item -ItemType Directory -Path $RuntimeDir | Out-Null
    }
}

function Get-RequiredCommand {
    param(
        [string]$Name,
        [string]$InstallMessage
    )

    $command = Get-Command $Name -ErrorAction SilentlyContinue

    if (-not $command) {
        throw $InstallMessage
    }

    return $command.Source
}

function Get-PythonLauncher {
    $python = Get-Command python -ErrorAction SilentlyContinue

    if ($python) {
        try {
            & $python.Source --version *> $null

            if ($LASTEXITCODE -eq 0) {
                return @{
                    File = $python.Source
                    Args = @()
                }
            }
        } catch {
        }
    }

    $py = Get-Command py -ErrorAction SilentlyContinue

    if ($py) {
        try {
            & $py.Source -3 --version *> $null

            if ($LASTEXITCODE -eq 0) {
                return @{
                    File = $py.Source
                    Args = @("-3")
                }
            }
        } catch {
        }
    }

    throw "Python não encontrado. Instale Python 3 ou abra as telas manualmente."
}

function Invoke-Checked {
    param(
        [string]$FilePath,
        [string[]]$Arguments,
        [string]$WorkingDirectory,
        [string]$ErrorMessage
    )

    Push-Location $WorkingDirectory

    try {
        & $FilePath @Arguments

        if ($LASTEXITCODE -ne 0) {
            throw $ErrorMessage
        }
    } finally {
        Pop-Location
    }
}

function Test-HttpUrl {
    param([string]$Url)

    try {
        $response = Invoke-WebRequest -UseBasicParsing -Uri $Url -TimeoutSec 2
        return $response.StatusCode -ge 200 -and $response.StatusCode -lt 500
    } catch {
        return $false
    }
}

function Wait-ForUrl {
    param(
        [string]$Url,
        [string]$Name,
        [int]$TimeoutSeconds = 30
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)

    while ((Get-Date) -lt $deadline) {
        if (Test-HttpUrl $Url) {
            Write-Ok "$Name ligado em $Url"
            return $true
        }

        Start-Sleep -Milliseconds 800
    }

    Write-Warn "$Name ainda não respondeu em $Url. Veja os logs em .runtime."
    return $false
}

function Stop-PidFileProcess {
    param(
        [string]$PidFile,
        [string]$Name
    )

    if (-not (Test-Path $PidFile)) {
        return
    }

    $rawPid = (Get-Content $PidFile -ErrorAction SilentlyContinue | Select-Object -First 1)
    $processId = 0

    if ([int]::TryParse($rawPid, [ref]$processId)) {
        $process = Get-Process -Id $processId -ErrorAction SilentlyContinue

        if ($process) {
            Write-Info "Parando $Name antigo (PID $processId)..."
            Stop-Process -Id $processId -Force
            Start-Sleep -Milliseconds 500
        }
    }

    Remove-Item $PidFile -Force -ErrorAction SilentlyContinue
}

function Stop-PortProcess {
    param(
        [int]$Port,
        [string[]]$AllowedProcessNames
    )

    $connections = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue

    foreach ($connection in $connections) {
        $process = Get-Process -Id $connection.OwningProcess -ErrorAction SilentlyContinue

        if ($process -and $AllowedProcessNames -contains $process.ProcessName) {
            Write-Info "Parando processo antigo na porta $Port (PID $($process.Id), $($process.ProcessName))..."
            Stop-Process -Id $process.Id -Force
            Start-Sleep -Milliseconds 500
        }
    }
}

function Stop-ProjectExecutableProcesses {
    $processes = Get-Process -Name "CasaMulher.Api" -ErrorAction SilentlyContinue

    foreach ($process in $processes) {
        Write-Info "Parando API antiga (PID $($process.Id), CasaMulher.Api)..."
        Stop-Process -Id $process.Id -Force
        Start-Sleep -Milliseconds 500
    }
}

function Stop-System {
    Ensure-RuntimeDir
    Stop-PidFileProcess -PidFile $ApiPidFile -Name "API"
    Stop-PidFileProcess -PidFile $FrontPidFile -Name "front"
    Stop-ProjectExecutableProcesses
    Write-Ok "Sistema desligado."
}

function Restore-Api {
    $dotnet = Get-RequiredCommand -Name "dotnet" -InstallMessage ".NET SDK não encontrado. Instale o .NET 8 SDK e tente novamente."
    Write-Info "Restaurando pacotes da API..."
    Invoke-Checked -FilePath $dotnet -Arguments @("restore", $ApiProject) -WorkingDirectory $ProjectRoot -ErrorMessage "Falha ao restaurar os pacotes da API."
}

function Update-Database {
    $dotnet = Get-RequiredCommand -Name "dotnet" -InstallMessage ".NET SDK não encontrado. Instale o .NET 8 SDK e tente novamente."
    $previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
    $env:ASPNETCORE_ENVIRONMENT = "Development"

    try {
        Write-Info "Preparando ferramenta de migrations..."
        Invoke-Checked -FilePath $dotnet -Arguments @("tool", "restore") -WorkingDirectory $ApiDir -ErrorMessage "Não foi possível restaurar o dotnet-ef local."

        Write-Info "Aplicando migrations no banco local..."
        Invoke-Checked -FilePath $dotnet -Arguments @("tool", "run", "dotnet-ef", "database", "update") -WorkingDirectory $ApiDir -ErrorMessage "Não foi possível aplicar as migrations."
    } finally {
        $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment
    }
}

function Start-Api {
    Ensure-RuntimeDir
    $dotnet = Get-RequiredCommand -Name "dotnet" -InstallMessage ".NET SDK não encontrado. Instale o .NET 8 SDK e tente novamente."
    $apiOut = Join-Path $RuntimeDir "api.out.log"
    $apiErr = Join-Path $RuntimeDir "api.err.log"

    Write-Info "Subindo API em $ApiUrl..."
    $process = Start-Process -FilePath $dotnet `
        -ArgumentList @("run", "--project", "CasaMulher.Api\CasaMulher.Api.csproj", "--environment", "Development", "--urls", $ApiUrl) `
        -WorkingDirectory $ProjectRoot `
        -WindowStyle Hidden `
        -RedirectStandardOutput $apiOut `
        -RedirectStandardError $apiErr `
        -PassThru

    Set-Content -Path $ApiPidFile -Value $process.Id
    Wait-ForUrl -Url $StatusApiUrl -Name "API" | Out-Null
}

function Start-Front {
    Ensure-RuntimeDir
    $python = Get-PythonLauncher
    $frontOut = Join-Path $RuntimeDir "front.out.log"
    $frontErr = Join-Path $RuntimeDir "front.err.log"
    $arguments = @($python.Args) + @("-m", "http.server", "5500")

    Write-Info "Subindo front em $FrontUrl..."
    $process = Start-Process -FilePath $python.File `
        -ArgumentList $arguments `
        -WorkingDirectory $FrontDir `
        -WindowStyle Hidden `
        -RedirectStandardOutput $frontOut `
        -RedirectStandardError $frontErr `
        -PassThru

    Set-Content -Path $FrontPidFile -Value $process.Id
    Wait-ForUrl -Url $StatusFrontUrl -Name "Front" | Out-Null
}

function Start-System {
    Assert-ProjectRoot
    Ensure-RuntimeDir
    Get-RequiredCommand -Name "dotnet" -InstallMessage ".NET SDK não encontrado. Instale o .NET 8 SDK e tente novamente." | Out-Null
    Get-PythonLauncher | Out-Null

    Write-Info "Verificando instâncias antigas..."
    Stop-PidFileProcess -PidFile $ApiPidFile -Name "API"
    Stop-PidFileProcess -PidFile $FrontPidFile -Name "front"
    Stop-ProjectExecutableProcesses
    Stop-PortProcess -Port 5001 -AllowedProcessNames @("dotnet")
    Stop-PortProcess -Port 5500 -AllowedProcessNames @("python", "py")

    Restore-Api
    Update-Database
    Start-Api
    Start-Front

    Write-Info "Abrindo navegador..."
    Start-Process $StatusFrontUrl
    Write-Ok "Sistema ligado."
}

function Show-Status {
    Assert-ProjectRoot
    $apiOnline = Test-HttpUrl $StatusApiUrl
    $frontOnline = Test-HttpUrl $StatusFrontUrl

    if ($apiOnline) {
        Write-Ok "API: ligada ($StatusApiUrl)"
    } else {
        Write-Warn "API: desligada ($StatusApiUrl)"
    }

    if ($frontOnline) {
        Write-Ok "Front: ligado ($StatusFrontUrl)"
    } else {
        Write-Warn "Front: desligado ($StatusFrontUrl)"
    }
}

function Update-System {
    Assert-ProjectRoot
    $git = Get-RequiredCommand -Name "git" -InstallMessage "Git não encontrado. Instale o Git ou atualize o projeto manualmente."

    $localChanges = & $git -C $ProjectRoot status --porcelain

    if ($localChanges) {
        Write-Warn "Há alterações locais no projeto."
        $answer = Read-Host "Deseja continuar com git pull mesmo assim? [S/N]"

        if ($answer -notmatch "^[sS]") {
            Write-Warn "Atualização cancelada."
            return
        }
    }

    Write-Info "Baixando atualizações do GitHub..."
    Invoke-Checked -FilePath $git -Arguments @("-C", $ProjectRoot, "pull") -WorkingDirectory $ProjectRoot -ErrorMessage "Falha ao executar git pull."
    Restore-Api
    Update-Database
    Write-Ok "Projeto atualizado."
}

try {
    Assert-ProjectRoot

    if (-not $CommandArgs -or $CommandArgs.Count -eq 0) {
        Show-Help
        exit 1
    }

    $primary = $CommandArgs[0].ToLowerInvariant()
    $secondary = if ($CommandArgs.Count -gt 1) { $CommandArgs[1].ToLowerInvariant() } else { "" }

    switch ($primary) {
        "serve" {
            switch ($secondary) {
                "on" { Start-System }
                "off" { Stop-System }
                default {
                    Show-Help
                    exit 1
                }
            }
        }
        "status" { Show-Status }
        "update" { Update-System }
        default {
            Show-Help
            exit 1
        }
    }
} catch {
    Write-Fail $_.Exception.Message
    exit 1
}
