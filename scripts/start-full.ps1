<#
.SYNOPSIS
    Inicia la infraestructura base y los perfiles opcionales disponibles (LocalStack y Ollama).

.DESCRIPTION
    - LocalStack solo se inicia si LOCALSTACK_AUTH_TOKEN está definido en .env.
      Si no se puede iniciar (token ausente o inválido, plan sin acceso), se informa
      cómo habilitarlo y el resto del entorno sigue funcionando.
    - Ollama se inicia sin descargar modelos.

.EXAMPLE
    .\scripts\start-full.ps1
    .\scripts\start-full.ps1 -SkipOllama
#>
[CmdletBinding()]
param(
    [switch]$NoBuild,
    [switch]$SkipLocalStack,
    [switch]$SkipOllama,
    [int]$TimeoutSeconds = 600
)

. "$PSScriptRoot\_common.ps1"
Set-Location $script:RepoRoot

Assert-Docker
Assert-EnvFile
$envValues = Read-DotEnv

# 1. Servicios obligatorios
$startArguments = @{ TimeoutSeconds = $TimeoutSeconds }
if ($NoBuild) { $startArguments['NoBuild'] = $true }
& (Join-Path $PSScriptRoot 'start.ps1') @startArguments
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# 2. Perfiles opcionales
Write-Title 'Perfiles opcionales'
$profiles = @()
$optionalServices = @()

if ($SkipLocalStack) { Write-Info 'LocalStack omitido (-SkipLocalStack).' }
elseif (-not (Get-EnvValue $envValues 'LOCALSTACK_AUTH_TOKEN')) {
    Write-Warn 'LocalStack no se iniciará: LOCALSTACK_AUTH_TOKEN está vacío en .env.'
    Write-Info '1. Crea una cuenta gratuita en https://app.localstack.cloud (plan Hobby, uso no comercial).'
    Write-Info '2. Copia tu token en Settings > Auth Tokens.'
    Write-Info '3. Pégalo en .env:  LOCALSTACK_AUTH_TOKEN=ls-...'
    Write-Info '4. Ejecuta de nuevo:  .\scripts\start-full.ps1'
}
else { $profiles += 'localstack'; $optionalServices += 'localstack' }

if ($SkipOllama) { Write-Info 'Ollama omitido (-SkipOllama).' }
else { $profiles += 'ollama'; $optionalServices += 'ollama' }

if ($profiles.Count -eq 0) {
    Write-Info 'No hay perfiles opcionales para iniciar.'
    exit 0
}

if ((Invoke-Compose -Profiles $profiles -Arguments (@('up', '--detach') + $optionalServices)) -ne 0) {
    Write-Warn 'No se pudieron iniciar todos los servicios opcionales. Los servicios obligatorios siguen activos.'
}

$warnings = 0
foreach ($service in $optionalServices) {
    if (Wait-ComposeServices -Services @($service) -TimeoutSeconds 180) {
        Write-Ok "$service disponible."
    }
    else {
        $warnings++
        Write-Warn "$service no quedó disponible. Revisa:  docker compose -f infra/docker/docker-compose.yml --profile $service logs $service"
        if ($service -eq 'localstack') {
            Write-Info 'Causas habituales: token inválido o expirado, o plan sin acceso a la imagen.'
            Write-Info 'Comprueba la licencia en http://localhost:4566/_localstack/info (is_license_activated).'
        }
    }
}

if ($optionalServices -contains 'ollama') {
    Write-Info ''
    Write-Info 'Ollama no incluye modelos. Para descargar uno liviano (ver docs/infrastructure.md):'
    Write-Info '  docker compose -f infra/docker/docker-compose.yml --profile ollama exec ollama ollama pull llama3.2:1b'
}

Write-Info ''
Write-Info 'Verificación completa:  .\scripts\check.ps1'
if ($warnings -gt 0) { exit 2 }
exit 0
