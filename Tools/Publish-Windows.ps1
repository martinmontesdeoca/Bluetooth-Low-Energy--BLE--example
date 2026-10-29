param([switch]$FrameworkDependent)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$env:DOTNET_CLI_HOME = Join-Path $projectRoot 'Artifacts/dotnet-home'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:MSBuildEnableWorkloadResolver = 'false'
$contained = if ($FrameworkDependent) { 'false' } else { 'true' }
& dotnet publish (Join-Path $projectRoot 'Native/WindowsCompanion/WindowsCompanion.csproj') -c Release -r win-x64 --self-contained $contained -p:DebugType=None -o (Join-Path $projectRoot 'Builds/WindowsCompanion')
if ($LASTEXITCODE -ne 0) { throw 'No se pudo publicar la app Windows.' }
Write-Output 'Windows listo: Builds/WindowsCompanion/ConnectedRoom.Windows.exe'
