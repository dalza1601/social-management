<#
.SYNOPSIS
    Verifica los contenedores, los endpoints técnicos y los recursos del entorno local.

.DESCRIPTION
    Comprueba:
      - Estado de los contenedores.
      - Health checks de APIs y Gateway, enrutamiento del Gateway y frontend.
      - Bases de datos en SQL Server y consola de RabbitMQ.
      - LocalStack (bucket S3 y cola SQS) y Ollama, si están en ejecución.
    Con -RunTests ejecuta además las pruebas automáticas de entorno (xUnit).
    Devuelve código 1 si falla alguna verificación obligatoria.

.EXAMPLE
    .\scripts\check.ps1
    .\scripts\check.ps1 -RunTests
#>
[CmdletBinding()]
param(
    [switch]$RunTests
)

. "$PSScriptRoot\_common.ps1"
Set-Location $script:RepoRoot

Assert-Docker
Assert-EnvFile
$envValues = Read-DotEnv
$script:passed = 0
$script:failed = 0

function Register-Result([bool]$Success, [string]$Name, [string]$Detail, [bool]$Mandatory = $true) {
    if ($Success) { $script:passed++; Write-Ok "$Name  $Detail" }
    elseif ($Mandatory) { $script:failed++; Write-Fail "$Name  $Detail" }
    else { Write-Warn "$Name  $Detail" }
}

function Invoke-HttpCheck([string]$Name, [string]$Url, [hashtable]$Headers = @{}, [string]$Expect = '', [bool]$Mandatory = $true) {
    try {
        $response = Invoke-WebRequest -Uri $Url -Headers $Headers -UseBasicParsing -TimeoutSec 15
        $ok = $response.StatusCode -eq 200 -and ($Expect -eq '' -or $response.Content -match [regex]::Escape($Expect))
        Register-Result $ok $Name "-> $($response.StatusCode) $Url" $Mandatory
    }
    catch {
        $status = 'sin respuesta'
        $exception = $_.Exception
        if ($exception.PSObject.Properties.Name -contains 'Response' -and $exception.Response) {
            $status = [int]$exception.Response.StatusCode
        }
        Register-Result $false $Name "-> $status $Url" $Mandatory
    }
}

function Test-ServiceRunning([object[]]$States, [string]$Service) {
    return [bool]($States | Where-Object { $_.Service -eq $Service -and $_.State -eq 'running' })
}

# ---------------------------------------------------------------------
Write-Title 'Contenedores'
# ---------------------------------------------------------------------
$states = @(Get-ComposeServiceStates)
if ($states.Count -eq 0) {
    Write-Fail 'No hay contenedores del proyecto. Inicia el entorno con .\scripts\start.ps1'
    exit 1
}
foreach ($service in $script:MandatoryServices) {
    $state = $states | Where-Object { $_.Service -eq $service } | Select-Object -First 1
    if (-not $state) { Register-Result $false $service 'no existe'; continue }
    if ($service -eq 'sqlserver-init') {
        Register-Result ($state.State -eq 'exited' -and $state.ExitCode -eq 0) $service "estado=$($state.State) código=$($state.ExitCode)"
    }
    else {
        Register-Result ($state.State -eq 'running' -and $state.Health -eq 'healthy') $service "estado=$($state.State) salud=$($state.Health)"
    }
}

# ---------------------------------------------------------------------
Write-Title 'Endpoints técnicos'
# ---------------------------------------------------------------------
$gateway = 'http://localhost:' + (Get-EnvValue $envValues 'GATEWAY_PORT' '8080')
$beneficiarios = 'http://localhost:' + (Get-EnvValue $envValues 'BENEFICIARIOS_API_PORT' '8081')
$solicitudes = 'http://localhost:' + (Get-EnvValue $envValues 'SOLICITUDES_API_PORT' '8082')
$frontend = 'http://localhost:' + (Get-EnvValue $envValues 'FRONTEND_PORT' '4200')

Invoke-HttpCheck 'Beneficiarios /health' "$beneficiarios/health" -Expect 'Healthy'
Invoke-HttpCheck 'Beneficiarios /health/ready' "$beneficiarios/health/ready" -Expect 'Healthy'
Invoke-HttpCheck 'Beneficiarios /version' "$beneficiarios/version" -Expect 'beneficiarios-api'
Invoke-HttpCheck 'Solicitudes /health' "$solicitudes/health" -Expect 'Healthy'
Invoke-HttpCheck 'Solicitudes /health/ready' "$solicitudes/health/ready" -Expect 'Healthy'
Invoke-HttpCheck 'Solicitudes /version' "$solicitudes/version" -Expect 'solicitudes-api'
Invoke-HttpCheck 'Gateway /health' "$gateway/health" -Expect 'Healthy'
Invoke-HttpCheck 'Gateway /health/ready' "$gateway/health/ready" -Expect 'Healthy'
Invoke-HttpCheck 'Gateway -> Beneficiarios' "$gateway/api/beneficiarios/version" -Expect 'beneficiarios-api'
Invoke-HttpCheck 'Gateway -> Solicitudes' "$gateway/api/solicitudes/version" -Expect 'solicitudes-api'
Invoke-HttpCheck 'Beneficiarios Swagger UI' "$beneficiarios/swagger/index.html" -Expect 'swagger-ui'
Invoke-HttpCheck 'Solicitudes Swagger UI' "$solicitudes/swagger/index.html" -Expect 'swagger-ui'
Invoke-HttpCheck 'Gateway -> Swagger Beneficiarios' "$gateway/api/beneficiarios/openapi/v1.json" -Expect "$gateway/api/beneficiarios"
Invoke-HttpCheck 'Gateway -> Swagger Solicitudes' "$gateway/api/solicitudes/openapi/v1.json" -Expect "$gateway/api/solicitudes"
Invoke-HttpCheck 'Frontend' "$frontend/" -Expect '<app-root'
Invoke-HttpCheck 'Frontend -> Gateway' "$frontend/gateway/health" -Expect 'Healthy'

# ---------------------------------------------------------------------
Write-Title 'SQL Server y RabbitMQ'
# ---------------------------------------------------------------------
# La contraseña se lee DENTRO del contenedor ($MSSQL_SA_PASSWORD): no viaja por la línea de comandos del host.
$query = "SET NOCOUNT ON; SELECT name FROM sys.databases WHERE name LIKE 'SocialManagement[_]%' ORDER BY name"
$sqlCommand = '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -h -1 -W -Q "' + $query + '"'
$databases = Get-NativeOutput -FilePath 'docker' -Arguments ((Get-ComposeArguments) + @('exec', '-T', 'sqlserver', 'sh', '-c', $sqlCommand))
foreach ($database in @('SocialManagement_Beneficiarios', 'SocialManagement_Solicitudes')) {
    Register-Result ([bool]($databases | Where-Object { $_.Trim() -eq $database })) "Base de datos $database" ''
}

$rabbitUser = Get-EnvValue $envValues 'RABBITMQ_USER'
$rabbitPassword = Get-EnvValue $envValues 'RABBITMQ_PASSWORD'
$basicAuth = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("${rabbitUser}:${rabbitPassword}"))
$rabbitPort = Get-EnvValue $envValues 'RABBITMQ_MANAGEMENT_PORT' '15672'
Invoke-HttpCheck 'RabbitMQ consola de administración' "http://localhost:$rabbitPort/api/overview" -Headers @{ Authorization = "Basic $basicAuth" } -Expect 'rabbitmq_version'
$vhost = [uri]::EscapeDataString((Get-EnvValue $envValues 'RABBITMQ_VHOST' 'social-management'))
Invoke-HttpCheck 'RabbitMQ vhost' "http://localhost:$rabbitPort/api/vhosts/$vhost" -Headers @{ Authorization = "Basic $basicAuth" }

# ---------------------------------------------------------------------
Write-Title 'Servicios opcionales'
# ---------------------------------------------------------------------
$localStackRunning = Test-ServiceRunning $states 'localstack'
if ($localStackRunning) {
    $localStackPort = Get-EnvValue $envValues 'LOCALSTACK_PORT' '4566'
    Invoke-HttpCheck 'LocalStack health' "http://localhost:$localStackPort/_localstack/health" -Expect 's3' -Mandatory $false
    $bucket = Get-EnvValue $envValues 'AWS_S3_DOCUMENTS_BUCKET' 'social-management-documents-dev'
    $queue = Get-EnvValue $envValues 'AWS_SQS_NOTIFICATIONS_QUEUE' 'social-management-notifications-dev'
    $localStackArgs = (Get-ComposeArguments -Profiles @('localstack')) + @('exec', '-T', 'localstack', 'awslocal')
    $bucketCode = Invoke-Native -FilePath 'docker' -Arguments ($localStackArgs + @('s3api', 'head-bucket', '--bucket', $bucket)) -Quiet
    Register-Result ($bucketCode -eq 0) "LocalStack S3 bucket $bucket" '' $false
    $queueCode = Invoke-Native -FilePath 'docker' -Arguments ($localStackArgs + @('sqs', 'get-queue-url', '--queue-name', $queue)) -Quiet
    Register-Result ($queueCode -eq 0) "LocalStack SQS cola $queue" '' $false
}
else { Write-Info 'LocalStack no está en ejecución (perfil opcional).' }

$ollamaRunning = Test-ServiceRunning $states 'ollama'
if ($ollamaRunning) {
    $ollamaPort = Get-EnvValue $envValues 'OLLAMA_PORT' '11434'
    Invoke-HttpCheck 'Ollama /api/version' "http://localhost:$ollamaPort/api/version" -Expect 'version' -Mandatory $false
    try {
        $models = (Invoke-RestMethod -Uri "http://localhost:$ollamaPort/api/tags" -TimeoutSec 10).models
        if ($models -and @($models).Count -gt 0) { Write-Info ('Modelos descargados: ' + ((@($models) | ForEach-Object { $_.name }) -join ', ')) }
        else { Write-Info 'Ollama no tiene modelos descargados (es lo esperado en la Fase 0).' }
    }
    catch { Write-Info 'No se pudo listar los modelos de Ollama.' }
}
else { Write-Info 'Ollama no está en ejecución (perfil opcional).' }

# ---------------------------------------------------------------------
if ($RunTests) {
    Write-Title 'Pruebas automáticas de entorno (xUnit)'
    $env:SM_ENVIRONMENT_TESTS = 'true'
    if ($localStackRunning) { $env:SM_LOCALSTACK_TESTS = 'true' }
    if ($ollamaRunning) { $env:SM_OLLAMA_TESTS = 'true' }
    try {
        $testCode = Invoke-Native -FilePath 'dotnet' -Arguments @('test', '--project', 'tests\environment\SocialManagement.LocalEnvironment.Tests')
        Register-Result ($testCode -eq 0) 'Pruebas de entorno' "(código $testCode)"
    }
    finally {
        Remove-Item Env:SM_ENVIRONMENT_TESTS, Env:SM_LOCALSTACK_TESTS, Env:SM_OLLAMA_TESTS -ErrorAction SilentlyContinue
    }
}

# ---------------------------------------------------------------------
Write-Title 'Resumen'
# ---------------------------------------------------------------------
if ($script:failed -gt 0) {
    Write-Fail "$($script:passed) verificaciones correctas, $($script:failed) con error."
    Write-Info 'Consulta docs/troubleshooting.md'
    exit 1
}
Write-Ok "$($script:passed) verificaciones correctas."
exit 0
