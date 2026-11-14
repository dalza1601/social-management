<#
.SYNOPSIS
    Elimina los contenedores, la red y los VOLÚMENES DE DATOS del entorno local.

.DESCRIPTION
    Operación destructiva: borra las bases de datos de SQL Server, las colas de RabbitMQ,
    el estado de LocalStack y los modelos descargados en Ollama.
    Requiere confirmación explícita escribiendo BORRAR (o el parámetro -Force).
    No modifica .env, user-secrets ni el código fuente.

.EXAMPLE
    .\scripts\reset.ps1
    .\scripts\reset.ps1 -RemoveImages
#>
[CmdletBinding()]
param(
    # Elimina también las imágenes construidas por el proyecto (social-management/*:dev).
    [switch]$RemoveImages,
    # Omite la confirmación interactiva (úsalo solo si sabes lo que haces).
    [switch]$Force
)

. "$PSScriptRoot\_common.ps1"
Set-Location $script:RepoRoot

Assert-Docker
Assert-EnvFile

$volumes = @('social-management-sqlserver-data', 'social-management-rabbitmq-data', 'social-management-localstack-data', 'social-management-ollama-data')
$images = @('social-management/beneficiarios-api:dev', 'social-management/solicitudes-api:dev', 'social-management/gateway:dev', 'social-management/frontend:dev')

Write-Title 'ATENCIÓN: operación destructiva'
Write-Warn 'Se eliminarán los contenedores, la red y estos volúmenes con TODOS sus datos:'
foreach ($volume in $volumes) { Write-Info "- $volume" }
if ($RemoveImages) {
    Write-Warn 'También se eliminarán estas imágenes:'
    foreach ($image in $images) { Write-Info "- $image" }
}
Write-Info 'No se modifican .env, user-secrets ni el código fuente.'

if (-not $Force) {
    $answer = Read-Host 'Escribe BORRAR para confirmar (cualquier otra respuesta cancela)'
    if ($answer -cne 'BORRAR') {
        Write-Ok 'Operación cancelada. No se eliminó nada.'
        exit 0
    }
}

Write-Title 'Eliminando recursos'
if ((Invoke-Compose -Profiles $script:OptionalProfiles -Arguments @('down', '--volumes', '--remove-orphans')) -ne 0) {
    Write-Fail 'docker compose down falló. Revisa los mensajes anteriores.'
    exit 1
}
Write-Ok 'Contenedores, red y volúmenes eliminados.'

if ($RemoveImages) {
    foreach ($image in $images) {
        if ((Invoke-Native -FilePath 'docker' -Arguments @('image', 'rm', $image) -Quiet) -eq 0) { Write-Ok "Imagen eliminada: $image" }
        else { Write-Info "Imagen no encontrada: $image" }
    }
}

Write-Info 'Para volver a crear el entorno:  .\scripts\start.ps1'
exit 0
