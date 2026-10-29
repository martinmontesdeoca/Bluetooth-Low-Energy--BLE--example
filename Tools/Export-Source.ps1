<# .SYNOPSIS Copies Git-visible sources to a destination repository while preserving its .git directory. #>
param([Parameter(Mandatory)][string]$Destination)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$destinationRoot = [IO.Path]::GetFullPath($Destination)
if ($destinationRoot.TrimEnd('\', '/') -eq $projectRoot.TrimEnd('\', '/')) { throw 'Origen y destino deben ser distintos.' }
$paths = @(& git -C $projectRoot -c core.quotepath=false ls-files --cached --others --exclude-standard)
if ($LASTEXITCODE -ne 0) { throw 'No se pudo enumerar el codigo visible para Git.' }
New-Item -ItemType Directory -Path $destinationRoot -Force | Out-Null
foreach ($relative in $paths) {
    $source = Join-Path $projectRoot $relative
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { continue }
    $target = [IO.Path]::GetFullPath((Join-Path $destinationRoot $relative))
    if (-not $target.StartsWith($destinationRoot.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Ruta de exportacion fuera del destino: $relative"
    }
    New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
    Copy-Item -LiteralPath $source -Destination $target -Force
    if ((Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $target).Hash) {
        throw "La copia no coincide: $relative"
    }
}
Write-Output "Codigo exportado a $destinationRoot; carpeta .git conservada."
