# =====================================================================
#  Funciones compartidas por los scripts de SocialManagement.
#  Se carga desde cada script con:  . "$PSScriptRoot\_common.ps1"
#  Compatible con Windows PowerShell 5.1 y PowerShell 7+.
# =====================================================================
Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

$script:RepoRoot = Split-Path -Parent $PSScriptRoot
$script:ComposeFile = Join-Path $script:RepoRoot 'infra\docker\docker-compose.yml'
$script:EnvFile = Join-Path $script:RepoRoot '.env'
$script:EnvExampleFile = Join-Path $script:RepoRoot '.env.example'
$script:OptionalProfiles = @('localstack', 'ollama')
$script:MandatoryServices = @('sqlserver', 'sqlserver-init', 'rabbitmq', 'beneficiarios-api', 'solicitudes-api', 'gateway', 'frontend')

# ---------------------------------------------------------------------
# Mensajes
# ---------------------------------------------------------------------
function Write-Title([string]$Text) { Write-Host ''; Write-Host "==> $Text" -ForegroundColor Cyan }
function Write-Ok([string]$Text) { Write-Host "  [OK]    $Text" -ForegroundColor Green }
function Write-Warn([string]$Text) { Write-Host "  [AVISO] $Text" -ForegroundColor Yellow }
function Write-Fail([string]$Text) { Write-Host "  [ERROR] $Text" -ForegroundColor Red }
function Write-Info([string]$Text) { Write-Host "          $Text" -ForegroundColor Gray }

# ---------------------------------------------------------------------
# Comandos nativos (docker, dotnet, npm). Se ejecutan con
# ErrorActionPreference=Continue para que la salida de error de estas
# herramientas no se convierta en excepciones de PowerShell 5.1.
# ---------------------------------------------------------------------
function Test-Command([string]$Name) {
    return [bool](Get-Command $Name -ErrorAction SilentlyContinue)
}

function Invoke-Native {
    param(
        [Parameter(Mandatory)][string]$FilePath,
        [string[]]$Arguments = @(),
        [switch]$Quiet
    )
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        if ($Quiet) { & $FilePath @Arguments *> $null }
        else { & $FilePath @Arguments | Out-Host }
        return $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $previous }
}

function Get-NativeOutput {
    param(
        [Parameter(Mandatory)][string]$FilePath,
        [string[]]$Arguments = @()
    )
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try { return @(& $FilePath @Arguments 2> $null) }
    finally { $ErrorActionPreference = $previous }
}

# ---------------------------------------------------------------------
# Requisitos y configuración
# ---------------------------------------------------------------------
function Assert-Docker {
    if (-not (Test-Command 'docker')) {
        Write-Fail 'Docker no está instalado o no está en el PATH.'
        Write-Info 'Instala Docker Desktop: https://www.docker.com/products/docker-desktop/'
        exit 1
    }
    if ((Invoke-Native -FilePath 'docker' -Arguments @('info') -Quiet) -ne 0) {
        Write-Fail 'Docker está instalado, pero el motor no responde.'
        Write-Info 'Inicia Docker Desktop, espera a que indique "Engine running" y vuelve a intentarlo.'
        exit 1
    }
    if ((Invoke-Native -FilePath 'docker' -Arguments @('compose', 'version') -Quiet) -ne 0) {
        Write-Fail 'Docker Compose v2 no está disponible (comando "docker compose").'
        exit 1
    }
}

function Assert-EnvFile {
    if (-not (Test-Path $script:EnvFile)) {
        Write-Fail 'No existe el archivo .env en la raíz del repositorio.'
        Write-Info 'Ejecuta primero:  .\scripts\setup.ps1'
        exit 1
    }
}

function Read-DotEnv([string]$Path = $script:EnvFile) {
    $values = @{}
    if (-not (Test-Path $Path)) { return $values }
    foreach ($line in Get-Content $Path -Encoding UTF8) {
        $trimmed = $line.Trim()
        if ($trimmed.Length -eq 0 -or $trimmed.StartsWith('#') -or -not $trimmed.Contains('=')) { continue }
        $parts = $trimmed.Split('=', 2)
        $values[$parts[0].Trim()] = $parts[1].Trim()
    }
    return $values
}

function Get-EnvValue([hashtable]$Values, [string]$Key, [string]$Default = '') {
    if ($Values.ContainsKey($Key) -and $Values[$Key]) { return [string]$Values[$Key] }
    return $Default
}

# ---------------------------------------------------------------------
# Docker Compose
# ---------------------------------------------------------------------
function Get-ComposeArguments([string[]]$Profiles = @()) {
    $arguments = @('compose', '-f', $script:ComposeFile, '--env-file', $script:EnvFile)
    foreach ($composeProfile in $Profiles) { $arguments += @('--profile', $composeProfile) }
    return $arguments
}

function Invoke-Compose {
    param(
        [Parameter(Mandatory)][string[]]$Arguments,
        [string[]]$Profiles = @()
    )
    $all = (Get-ComposeArguments -Profiles $Profiles) + $Arguments
    return Invoke-Native -FilePath 'docker' -Arguments $all
}

# Devuelve el estado de los contenedores: Service, State, Health, ExitCode.
function Get-ComposeServiceStates {
    $arguments = (Get-ComposeArguments -Profiles $script:OptionalProfiles) + @('ps', '-a', '--format', 'json')
    $lines = Get-NativeOutput -FilePath 'docker' -Arguments $arguments
    $text = ($lines -join "`n").Trim()
    $states = @()
    if (-not $text) { return $states }

    # Según la versión de Compose, la salida es un arreglo JSON o un objeto JSON por línea.
    if ($text.StartsWith('[')) { $items = $text | ConvertFrom-Json }
    else { $items = foreach ($line in $lines) { if ($line.Trim()) { $line | ConvertFrom-Json } } }

    foreach ($item in $items) {
        $health = ''
        if ($item.PSObject.Properties.Name -contains 'Health' -and $item.Health) { $health = [string]$item.Health }
        $exitCode = 0
        if ($item.PSObject.Properties.Name -contains 'ExitCode') { $exitCode = [int]$item.ExitCode }
        $states += [pscustomobject]@{ Service = [string]$item.Service; State = [string]$item.State; Health = $health; ExitCode = $exitCode }
    }
    return $states
}

# Espera a que cada servicio esté "healthy" (o termine con código 0 si es una tarea de inicialización).
function Wait-ComposeServices {
    param(
        [Parameter(Mandatory)][string[]]$Services,
        [int]$TimeoutSeconds = 600
    )
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $lastReport = Get-Date '2000-01-01'

    while ((Get-Date) -lt $deadline) {
        $states = @(Get-ComposeServiceStates)
        $pending = @()
        $failed = @()

        foreach ($service in $Services) {
            $state = $states | Where-Object { $_.Service -eq $service } | Select-Object -First 1
            if (-not $state) { $pending += $service; continue }

            if ($state.State -eq 'exited') {
                if ($service -eq 'sqlserver-init' -and $state.ExitCode -eq 0) { continue }
                $failed += "$service (terminó con código $($state.ExitCode))"
            }
            elseif ($state.Health -eq 'unhealthy') { $failed += "$service (unhealthy)" }
            elseif ($state.Health -eq 'healthy') { continue }
            elseif ($state.State -eq 'running' -and -not $state.Health) { continue }
            else { $pending += $service }
        }

        if ($failed.Count -gt 0) {
            foreach ($item in $failed) { Write-Fail $item }
            return $false
        }
        if ($pending.Count -eq 0) { return $true }

        if (((Get-Date) - $lastReport).TotalSeconds -ge 15) {
            Write-Info ("Esperando: " + ($pending -join ', '))
            $lastReport = Get-Date
        }
        Start-Sleep -Seconds 3
    }

    Write-Fail "Tiempo de espera agotado ($TimeoutSeconds s)."
    return $false
}

function Show-ServiceUrls([hashtable]$Values) {
    Write-Title 'Accesos'
    Write-Info ("Frontend Angular      http://localhost:{0}" -f (Get-EnvValue $Values 'FRONTEND_PORT' '4200'))
    Write-Info ("API Gateway           http://localhost:{0}/health/ready" -f (Get-EnvValue $Values 'GATEWAY_PORT' '8080'))
    Write-Info ("Beneficiarios API     http://localhost:{0}/version" -f (Get-EnvValue $Values 'BENEFICIARIOS_API_PORT' '8081'))
    Write-Info ("Solicitudes API       http://localhost:{0}/version" -f (Get-EnvValue $Values 'SOLICITUDES_API_PORT' '8082'))
    Write-Info ("RabbitMQ (consola)    http://localhost:{0}" -f (Get-EnvValue $Values 'RABBITMQ_MANAGEMENT_PORT' '15672'))
    Write-Info ("SQL Server            localhost,{0}  (usuario sa)" -f (Get-EnvValue $Values 'SQLSERVER_PORT' '14333'))
}
