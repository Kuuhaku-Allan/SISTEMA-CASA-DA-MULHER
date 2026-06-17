param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$CommandArgs
)

$ErrorActionPreference = "Stop"

$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$ApiProject = Join-Path $ProjectRoot "CasaMulher.Api\CasaMulher.Api.csproj"
$ApiDir = Join-Path $ProjectRoot "CasaMulher.Api"
$FrontDir = $ProjectRoot
$RuntimeDir = Join-Path $ProjectRoot ".runtime"
$ApiPidFile = Join-Path $RuntimeDir "api.pid"
$FrontPidFile = Join-Path $RuntimeDir "front.pid"
$ApiUrl = "http://localhost:5001"
$FrontUrl = "http://localhost:5500"
$StatusApiUrl = "$ApiUrl/swagger/index.html"
$StatusFrontUrl = "$FrontUrl/projetocasadamulher/telas/index.html"
$EquipeUrl = "$FrontUrl/equipe.html"

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
    Write-Host "  .\casa_da_mulher.cmd equipe"
    Write-Host "  .\casa_da_mulher.cmd equipe bootstrap"
    Write-Host "  .\casa_da_mulher.cmd equipe sync"
    Write-Host "  .\casa_da_mulher.cmd status"
    Write-Host "  .\casa_da_mulher.cmd update"
    Write-Host ""
}

function Assert-ProjectRoot {
    $temApi = Test-Path $ApiProject
    $temIndexRaiz = Test-Path (Join-Path $ProjectRoot "index.html")
    $temIndexTelas = Test-Path (Join-Path $ProjectRoot "projetocasadamulher\telas\index.html")

    if (-not $temApi -or -not $temIndexRaiz -or -not $temIndexTelas) {
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
    param([string]$OpenUrl = $StatusFrontUrl)

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
    Start-Process $OpenUrl
    Write-Ok "Sistema ligado."
    Write-Host ""
    Write-Host "Links principais:"
    Write-Host "  Área da Equipe: $EquipeUrl"
    Write-Host "  Ativar EQP:     $FrontUrl/projetocasadamulher/telas/equipe-ativar.html"
    Write-Host "  Login:          $StatusFrontUrl"
    Write-Host "  Protótipos:     $FrontUrl/prototipos/index.html"
}

function Invoke-EquipeBootstrap {
    param([int]$QuantidadeIntegrantes = 5)

    $body = @{
        quantidadeIntegrantes = $QuantidadeIntegrantes
        regenerarCodigosDisponiveis = $true
    } | ConvertTo-Json

    Write-Info "Gerando/atualizando convites iniciais EQP..."

    try {
        $resultado = Invoke-RestMethod `
            -Uri "$ApiUrl/api/equipe/convites/bootstrap" `
            -Method Post `
            -ContentType "application/json" `
            -Body $body `
            -TimeoutSec 30
    } catch {
        throw "Não foi possível executar o bootstrap EQP. Confirme se a API está ligada em $ApiUrl."
    }

    Write-Host ""
    Write-Host "Convites iniciais EQP ($($resultado.ambiente))"
    Write-Host "Guarde estes códigos em local privado. Não faça commit deles."
    Write-Host ""
    Write-Host ("{0,-12} {1,-12} {2,-12} {3}" -f "ID", "Código", "Papel", "Observação")
    Write-Host ("{0,-12} {1,-12} {2,-12} {3}" -f "--", "------", "-----", "----------")

    foreach ($convite in $resultado.convites) {
        $codigo = if ($convite.codigoAtivacao) { $convite.codigoAtivacao } else { "(não exibido)" }
        $observacao = $convite.observacao

        if ($convite.criado) {
            $observacao = "$observacao - criado agora"
        } elseif ($convite.regenerado) {
            $observacao = "$observacao - código regenerado agora"
        }

        Write-Host ("{0,-12} {1,-12} {2,-12} {3}" -f $convite.codigoEquipe, $codigo, $convite.papelEquipe, $observacao)
    }

    Write-Host ""
    Write-Host "Use o EQP-000001 para o mantenedor. Entregue os demais códigos individualmente."
}

function Invoke-EquipeSync {
    Write-Info "Sincronizando equipe a partir do ACESSO-EQUIPE..."

    if (-not (Test-HttpUrl $StatusApiUrl)) {
        Write-Warn "API desligada. Subindo sistema antes de sincronizar."
        Start-System -OpenUrl $EquipeUrl
    }

    $json = $null
    $gh = Get-Command gh -ErrorAction SilentlyContinue

    if ($gh) {
        try {
            Write-Info "Lendo data/equipe-db.json pelo gh CLI..."
            $contentBase64 = (& $gh.Source api "repos/Sistema-Casa-da-Mulher/ACESSO-EQUIPE/contents/data/equipe-db.json" --jq ".content") -join ""
            $contentBase64 = $contentBase64 -replace "\s", ""

            if (-not [string]::IsNullOrWhiteSpace($contentBase64)) {
                $json = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($contentBase64))
            }
        } catch {
            Write-Warn "Nao foi possivel ler com gh CLI. Vou tentar via token de leitura configurado na API."
        }
    } else {
        Write-Warn "gh CLI nao encontrado. Vou tentar via GITHUB_EQP_READ_TOKEN/GITHUB_EQP_WRITE_TOKEN configurado na API."
    }

    try {
        if ($json) {
            $resultado = Invoke-RestMethod `
                -Uri "$ApiUrl/api/equipe/sincronizar-github-db" `
                -Method Post `
                -ContentType "application/json" `
                -Body $json `
                -TimeoutSec 60
        } else {
            $resultado = Invoke-RestMethod `
                -Uri "$ApiUrl/api/equipe/sincronizar-github-db" `
                -Method Post `
                -ContentType "application/json" `
                -Body "{}" `
                -TimeoutSec 60
        }
    } catch {
        throw "Nao foi possivel sincronizar a equipe. Confirme login no gh CLI ou configure GITHUB_EQP_READ_TOKEN."
    }

    Write-Ok $resultado.mensagem
    Write-Host "Membros importados:      $($resultado.membrosImportados)"
    Write-Host "Usuarios criados:        $($resultado.usuariosCriados)"
    Write-Host "Usuarios atualizados:    $($resultado.usuariosAtualizados)"
    Write-Host "Identificadores criados: $($resultado.identificadoresCriados)"
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
        "equipe" {
            switch ($secondary) {
                "" { Start-System -OpenUrl $EquipeUrl }
                "on" { Start-System -OpenUrl $EquipeUrl }
                "bootstrap" {
                    $quantidadeIntegrantes = 5

                    if ($CommandArgs.Count -gt 2) {
                        [int]::TryParse($CommandArgs[2], [ref]$quantidadeIntegrantes) | Out-Null
                    }

                    Start-System -OpenUrl $EquipeUrl
                    Invoke-EquipeBootstrap -QuantidadeIntegrantes $quantidadeIntegrantes
                }
                "sync" {
                    Invoke-EquipeSync
                }
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
