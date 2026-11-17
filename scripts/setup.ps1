<#
.SYNOPSIS
    Valida los requisitos y prepara la configuración inicial del entorno local.

.DESCRIPTION
    1. Comprueba Docker, Docker Compose, .NET SDK 10, Node.js y npm.
    2. Crea .env a partir de .env.example (nunca sobrescribe un .env existente).
    3. Valida los valores críticos de .env (contraseña de SQL Server, puertos libres).
    4. Guarda en user-secrets las credenciales que usan las APIs con "dotnet run".
    5. Restaura los paquetes NuGet e instala las dependencias del frontend.

.EXAMPLE
    .\scripts\setup.ps1
    .\scripts\setup.ps1 -SkipFrontend
#>
[CmdletBinding()]
param(
    # No instala las dependencias npm del frontend.
    [switch]$SkipFrontend,
    # No configura user-secrets para las APIs.
    [switch]$SkipUserSecrets
)

. "$PSScriptRoot\_common.ps1"
Set-Location $script:RepoRoot

Write-Host 'SocialManagement - Preparación del entorno local' -ForegroundColor White
$errors = 0

# ---------------------------------------------------------------------
Write-Title '1. Requisitos'
# ---------------------------------------------------------------------
if (Test-Command 'docker') {
    $dockerVersion = (Get-NativeOutput -FilePath 'docker' -Arguments @('--version')) -join ''
    Write-Ok $dockerVersion
    if ((Invoke-Native -FilePath 'docker' -Arguments @('info') -Quiet) -eq 0) { Write-Ok 'Motor de Docker en ejecución.' }
    else { Write-Warn 'Docker está instalado pero el motor no responde. Inicia Docker Desktop antes de start.ps1.' }
    if ((Invoke-Native -FilePath 'docker' -Arguments @('compose', 'version') -Quiet) -eq 0) {
        Write-Ok ((Get-NativeOutput -FilePath 'docker' -Arguments @('compose', 'version')) -join '')
    }
    else { Write-Fail 'Docker Compose v2 no disponible.'; $errors++ }
}
else {
    Write-Fail 'Docker no está instalado. Descarga Docker Desktop: https://www.docker.com/products/docker-desktop/'
    $errors++
}

$hasDotnet = Test-Command 'dotnet'
if ($hasDotnet) {
    $sdks = Get-NativeOutput -FilePath 'dotnet' -Arguments @('--list-sdks')
    $sdk10 = $sdks | Where-Object { $_ -match '^10\.' } | Select-Object -Last 1
    if ($sdk10) { Write-Ok ".NET SDK $($sdk10.Split(' ')[0])" }
    else { Write-Fail 'No se encontró el .NET SDK 10. Descárgalo en https://dotnet.microsoft.com/download/dotnet/10.0'; $errors++; $hasDotnet = $false }
}
else { Write-Fail '.NET SDK no instalado. Descárgalo en https://dotnet.microsoft.com/download/dotnet/10.0'; $errors++ }

$hasNode = Test-Command 'node'
if ($hasNode) {
    $nodeVersion = ((Get-NativeOutput -FilePath 'node' -Arguments @('--version')) -join '').TrimStart('v')
    $nodeMajor = [int]($nodeVersion.Split('.')[0])
    if ($nodeMajor -ge 22) { Write-Ok "Node.js $nodeVersion" }
    else { Write-Fail "Node.js $nodeVersion es antiguo. Angular 22 requiere Node.js 22.12+ o 24 LTS."; $errors++; $hasNode = $false }
    if (-not (Test-Command 'npm')) { Write-Fail 'npm no está disponible.'; $errors++; $hasNode = $false }
}
else { Write-Fail 'Node.js no instalado. Descarga la versión LTS en https://nodejs.org/'; $errors++ }

if (Test-Command 'git') { Write-Ok ((Get-NativeOutput -FilePath 'git' -Arguments @('--version')) -join '') }
else { Write-Warn 'Git no está instalado (recomendado).' }

# ---------------------------------------------------------------------
Write-Title '2. Archivo .env'
# ---------------------------------------------------------------------
if (Test-Path $script:EnvFile) {
    # Nunca se cambia un valor existente; solo se AGREGAN las variables nuevas de .env.example
    # (p. ej. las de JWT de la sesión 04), con su valor de ejemplo de desarrollo.
    $currentKeys = (Read-DotEnv).Keys
    $newLines = Get-Content $script:EnvExampleFile -Encoding UTF8 | Where-Object {
        $trimmed = $_.Trim()
        $trimmed.Length -gt 0 -and -not $trimmed.StartsWith('#') -and $trimmed.Contains('=') -and
            ($currentKeys -notcontains $trimmed.Split('=', 2)[0].Trim())
    }
    if ($newLines) {
        $block = "`r`n# Variables nuevas agregadas por setup.ps1 desde .env.example`r`n" + (($newLines | ForEach-Object { $_.Trim() }) -join "`r`n") + "`r`n"
        [System.IO.File]::AppendAllText($script:EnvFile, $block, (New-Object System.Text.UTF8Encoding $false))
        Write-Ok ('.env ya existe; se agregaron las variables nuevas: ' + (($newLines | ForEach-Object { $_.Split('=', 2)[0].Trim() }) -join ', '))
    }
    else { Write-Ok '.env ya existe y tiene todas las variables (no se modifica).' }
}
else {
    Copy-Item $script:EnvExampleFile $script:EnvFile
    Write-Ok '.env creado a partir de .env.example.'
    Write-Info 'Los valores son exclusivos para desarrollo local. Puedes cambiarlos antes del primer arranque.'
}

$envValues = Read-DotEnv
$saPassword = Get-EnvValue $envValues 'MSSQL_SA_PASSWORD'
$passwordIsValid = $saPassword.Length -ge 8 -and
    $saPassword -cmatch '[A-Z]' -and $saPassword -cmatch '[a-z]' -and
    $saPassword -match '[0-9]' -and $saPassword -match '[^a-zA-Z0-9]'
if (-not $passwordIsValid) {
    Write-Fail 'MSSQL_SA_PASSWORD no cumple la política de SQL Server (8+ caracteres con mayúsculas, minúsculas, números y símbolos).'
    $errors++
}
elseif ($saPassword.Contains(';') -or $saPassword.Contains("'") -or $saPassword.Contains('"')) {
    Write-Fail 'MSSQL_SA_PASSWORD no debe contener ; comillas simples ni dobles (rompen la cadena de conexión).'
    $errors++
}
else { Write-Ok 'MSSQL_SA_PASSWORD cumple la política de SQL Server.' }

if ((Get-EnvValue $envValues 'JWT_SIGNING_KEY').Length -ge 32) { Write-Ok 'JWT_SIGNING_KEY tiene al menos 32 caracteres.' }
else { Write-Fail 'JWT_SIGNING_KEY debe tener al menos 32 caracteres (clave HMAC-SHA256 de los tokens JWT).'; $errors++ }

foreach ($key in @('DEMO_ADMIN_PASSWORD', 'DEMO_OPERADOR_PASSWORD')) {
    if ((Get-EnvValue $envValues $key).Length -lt 12) { Write-Fail "$key debe tener al menos 12 caracteres."; $errors++ }
}

if (Get-EnvValue $envValues 'LOCALSTACK_AUTH_TOKEN') { Write-Ok 'LOCALSTACK_AUTH_TOKEN definido (perfil localstack disponible).' }
else { Write-Info 'LOCALSTACK_AUTH_TOKEN vacío: el perfil opcional "localstack" no se iniciará (el resto funciona igual).' }

# Puertos ocupados en el host
$portKeys = @('FRONTEND_PORT', 'GATEWAY_PORT', 'BENEFICIARIOS_API_PORT', 'SOLICITUDES_API_PORT', 'SQLSERVER_PORT', 'RABBITMQ_AMQP_PORT', 'RABBITMQ_MANAGEMENT_PORT')
$busy = @()
foreach ($key in $portKeys) {
    $port = [int](Get-EnvValue $envValues $key '0')
    if ($port -gt 0 -and (Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue)) { $busy += "$key=$port" }
}
if ($busy.Count -gt 0) {
    Write-Warn ("Puertos en uso: " + ($busy -join ', '))
    Write-Info 'Si no son de un entorno SocialManagement ya iniciado, cámbialos en .env.'
}
else { Write-Ok 'Los puertos configurados están libres.' }

# ---------------------------------------------------------------------
Write-Title '3. Secretos locales para "dotnet run" (user-secrets)'
# ---------------------------------------------------------------------
if ($SkipUserSecrets) { Write-Info 'Omitido (-SkipUserSecrets).' }
elseif (-not $hasDotnet) { Write-Warn 'Omitido: .NET SDK no disponible.' }
else {
    $sqlPort = Get-EnvValue $envValues 'SQLSERVER_PORT' '14333'
    # Sesión 04: clave JWT y usuarios demo de POST /api/auth/dev-token (solo la API de Beneficiarios).
    $jwtSecrets = [ordered]@{
        'Jwt:SigningKey'                       = (Get-EnvValue $envValues 'JWT_SIGNING_KEY')
        'DevelopmentTokens:Users:0:UserName'   = (Get-EnvValue $envValues 'DEMO_ADMIN_USER' 'admin.demo')
        'DevelopmentTokens:Users:0:Password'   = (Get-EnvValue $envValues 'DEMO_ADMIN_PASSWORD')
        'DevelopmentTokens:Users:0:Roles:0'    = 'Administrador'
        'DevelopmentTokens:Users:1:UserName'   = (Get-EnvValue $envValues 'DEMO_OPERADOR_USER' 'operador.demo')
        'DevelopmentTokens:Users:1:Password'   = (Get-EnvValue $envValues 'DEMO_OPERADOR_PASSWORD')
        'DevelopmentTokens:Users:1:Roles:0'    = 'Operador'
    }
    $services = @(
        @{ Project = 'src\backend\services\beneficiarios\SocialManagement.Beneficiarios.Api'; ConnectionName = 'BeneficiariosDb'; Database = 'SocialManagement_Beneficiarios'; Extra = $jwtSecrets },
        @{ Project = 'src\backend\services\solicitudes\SocialManagement.Solicitudes.Api'; ConnectionName = 'SolicitudesDb'; Database = 'SocialManagement_Solicitudes'; Extra = @{} }
    )
    foreach ($service in $services) {
        $connectionString = "Server=localhost,$sqlPort;Database=$($service.Database);User Id=sa;Password=$saPassword;TrustServerCertificate=True;Connect Timeout=5"
        $secrets = [ordered]@{
            "ConnectionStrings:$($service.ConnectionName)" = $connectionString
            'RabbitMq:Port'                                = (Get-EnvValue $envValues 'RABBITMQ_AMQP_PORT' '5672')
            'RabbitMq:UserName'                            = (Get-EnvValue $envValues 'RABBITMQ_USER')
            'RabbitMq:Password'                            = (Get-EnvValue $envValues 'RABBITMQ_PASSWORD')
            'RabbitMq:VirtualHost'                         = (Get-EnvValue $envValues 'RABBITMQ_VHOST' 'social-management')
            'Aws:ServiceUrl'                               = ('http://localhost:' + (Get-EnvValue $envValues 'LOCALSTACK_PORT' '4566'))
        }
        foreach ($extra in $service.Extra.GetEnumerator()) { $secrets[$extra.Key] = $extra.Value }
        $failedSecrets = 0
        foreach ($entry in $secrets.GetEnumerator()) {
            $code = Invoke-Native -FilePath 'dotnet' -Arguments @('user-secrets', 'set', $entry.Key, $entry.Value, '--project', $service.Project) -Quiet
            if ($code -ne 0) { $failedSecrets++ }
        }
        $name = Split-Path $service.Project -Leaf
        if ($failedSecrets -eq 0) { Write-Ok "$name configurado." }
        else { Write-Fail "$name : no se pudieron guardar $failedSecrets secretos."; $errors++ }
    }
}

# ---------------------------------------------------------------------
Write-Title '4. Dependencias'
# ---------------------------------------------------------------------
if ($hasDotnet) {
    if ((Invoke-Native -FilePath 'dotnet' -Arguments @('restore', 'SocialManagement.slnx', '--verbosity', 'quiet') -Quiet) -eq 0) { Write-Ok 'Paquetes NuGet restaurados.' }
    else { Write-Fail 'dotnet restore falló. Ejecuta "dotnet restore SocialManagement.slnx" para ver el detalle.'; $errors++ }
}

if ($SkipFrontend) { Write-Info 'Frontend omitido (-SkipFrontend).' }
elseif (-not $hasNode) { Write-Warn 'Frontend omitido: Node.js no disponible.' }
elseif (Test-Path (Join-Path $script:RepoRoot 'src\frontend\node_modules')) { Write-Ok 'Dependencias del frontend ya instaladas.' }
else {
    Write-Info 'Instalando dependencias del frontend (npm ci)...'
    Push-Location (Join-Path $script:RepoRoot 'src\frontend')
    try { $code = Invoke-Native -FilePath 'npm' -Arguments @('ci', '--no-audit', '--no-fund') -Quiet }
    finally { Pop-Location }
    if ($code -eq 0) { Write-Ok 'Dependencias del frontend instaladas.' }
    else { Write-Fail 'npm ci falló. Ejecuta "npm ci" en src\frontend para ver el detalle.'; $errors++ }
}

# ---------------------------------------------------------------------
Write-Title 'Resultado'
# ---------------------------------------------------------------------
if ($errors -gt 0) {
    Write-Fail "Se encontraron $errors problema(s). Corrígelos y vuelve a ejecutar setup.ps1."
    exit 1
}

Write-Ok 'Entorno preparado.'
Write-Info 'Siguiente paso:  .\scripts\start.ps1        (servicios obligatorios)'
Write-Info '                 .\scripts\start-full.ps1   (+ LocalStack y Ollama)'
exit 0
