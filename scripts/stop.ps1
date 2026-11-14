<#
.SYNOPSIS
    Detiene todos los servicios (incluidos los perfiles opcionales).

.DESCRIPTION
    Elimina los contenedores y la red, pero CONSERVA los volúmenes:
    las bases de datos, colas y modelos descargados no se pierden.
    Para borrar los datos usa reset.ps1.

.EXAMPLE
    .\scripts\stop.ps1
#>
[CmdletBinding()]
param()

. "$PSScriptRoot\_common.ps1"
Set-Location $script:RepoRoot

Write-Title 'Deteniendo servicios'
Assert-Docker
Assert-EnvFile

if ((Invoke-Compose -Profiles $script:OptionalProfiles -Arguments @('down', '--remove-orphans')) -ne 0) {
    Write-Fail 'docker compose down falló. Revisa los mensajes anteriores.'
    exit 1
}

Write-Ok 'Servicios detenidos. Los volúmenes de datos se conservaron.'
exit 0
