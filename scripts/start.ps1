<#
.SYNOPSIS
    Inicia la infraestructura base: SQL Server, RabbitMQ, APIs, Gateway y frontend.

.DESCRIPTION
    Construye las imágenes (salvo -NoBuild), inicia los contenedores en segundo plano y
    espera a que todos los servicios obligatorios estén sanos (health checks).

.EXAMPLE
    .\scripts\start.ps1
    .\scripts\start.ps1 -NoBuild
#>
[CmdletBinding()]
param(
    # Usa las imágenes ya construidas (más rápido).
    [switch]$NoBuild,
    # Tiempo máximo de espera en segundos.
    [int]$TimeoutSeconds = 600
)

. "$PSScriptRoot\_common.ps1"
Set-Location $script:RepoRoot

Write-Title 'Iniciando servicios obligatorios'
Assert-Docker
Assert-EnvFile

$upArguments = @('up', '--detach', '--remove-orphans')
if (-not $NoBuild) { $upArguments += '--build' }

if ((Invoke-Compose -Arguments $upArguments) -ne 0) {
    Write-Fail 'docker compose up falló. Revisa los mensajes anteriores.'
    exit 1
}

Write-Title 'Esperando a que los servicios estén sanos'
if (-not (Wait-ComposeServices -Services $script:MandatoryServices -TimeoutSeconds $TimeoutSeconds)) {
    Write-Info 'Estado actual:'
    Invoke-Compose -Arguments @('ps', '-a') | Out-Null
    Write-Info 'Consulta los logs con:  docker compose -f infra/docker/docker-compose.yml logs <servicio>'
    Write-Info 'Problemas frecuentes:   docs/troubleshooting.md'
    exit 1
}

Write-Ok 'Todos los servicios obligatorios están disponibles.'
Show-ServiceUrls (Read-DotEnv)
Write-Info ''
Write-Info 'Verificación completa:  .\scripts\check.ps1'
exit 0
