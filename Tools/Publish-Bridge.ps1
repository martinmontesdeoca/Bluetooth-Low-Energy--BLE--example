param([switch]$SelfContained)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$bridgeProject = Join-Path $projectRoot 'Native/BleBridge/BleBridge.csproj'
$outputPath = Join-Path $projectRoot 'Artifacts/BleBridge'
$env:DOTNET_CLI_HOME = Join-Path $projectRoot 'Artifacts/dotnet-home'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:MSBuildEnableWorkloadResolver = 'false'
$contained = if ($SelfContained) { 'true' } else { 'false' }
& dotnet publish $bridgeProject -c Release -r win-x64 --self-contained $contained -o $outputPath
if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar el puente BLE.' }
Write-Output "Puente listo en $outputPath"
