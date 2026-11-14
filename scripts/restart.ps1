<#
.SYNOPSIS
    Reinicia el entorno: detiene todo y vuelve a iniciarlo (los datos se conservan).

.EXAMPLE
    .\scripts\restart.ps1           # servicios obligatorios
    .\scripts\restart.ps1 -Full     # + perfiles opcionales
#>
[CmdletBinding()]
param(
    # Inicia también los perfiles opcionales (start-full.ps1).
    [switch]$Full,
    [switch]$NoBuild
)

. "$PSScriptRoot\_common.ps1"

& (Join-Path $PSScriptRoot 'stop.ps1')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$startScript = 'start.ps1'
if ($Full) { $startScript = 'start-full.ps1' }

$startArguments = @{}
if ($NoBuild) { $startArguments['NoBuild'] = $true }

& (Join-Path $PSScriptRoot $startScript) @startArguments
exit $LASTEXITCODE
