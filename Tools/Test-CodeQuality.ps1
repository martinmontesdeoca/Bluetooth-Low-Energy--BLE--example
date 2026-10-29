<# .SYNOPSIS Enforces method documentation and the 300-line type limit in authored C# and Java sources. #>
param([string]$AndroidPlayer)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot 'UnityPaths.ps1')
$AndroidPlayer = Get-AndroidPlayerPath -ProjectRoot $projectRoot -AndroidPlayer $AndroidPlayer
$env:DOTNET_CLI_HOME = Join-Path $projectRoot 'Artifacts/dotnet-home'
$env:MSBuildEnableWorkloadResolver = 'false'
& dotnet run --project (Join-Path $projectRoot 'Tools/CodeQuality') -- --self-test
if ($LASTEXITCODE -ne 0) { throw 'Fallo la autoverificacion del analizador C#.' }
& dotnet run --project (Join-Path $projectRoot 'Tools/CodeQuality') -- $projectRoot
if ($LASTEXITCODE -ne 0) { throw 'Hay incumplimientos de documentacion o tamano en C#.' }
& (Join-Path $AndroidPlayer 'OpenJDK/bin/java.exe') (Join-Path $projectRoot 'Tools/JavaQualityCheck.java') $projectRoot
if ($LASTEXITCODE -ne 0) { throw 'Hay incumplimientos de documentacion o tamano en Java.' }
