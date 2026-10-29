<# .SYNOPSIS Runs scene preparation, tests or player builds and rejects failed or stale test reports. #>
param(
    [ValidateSet('Prepare', 'EditMode', 'PlayMode', 'Build', 'Android')]
    [string]$Action = 'EditMode',
    [string]$UnityPath
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot 'UnityPaths.ps1')
$UnityPath = Get-UnityEditorPath -ProjectRoot $projectRoot -UnityPath $UnityPath
$artifacts = Join-Path $projectRoot 'Artifacts'
New-Item -ItemType Directory -Path $artifacts -Force | Out-Null
$logPath = Join-Path $artifacts "unity-$Action.log"
$arguments = @('-batchmode', '-projectPath', "`"$projectRoot`"", '-logFile', "`"$logPath`"")
if ($Action -eq 'Build' -or $Action -eq 'Prepare' -or $Action -eq 'Android') {
    $method = switch ($Action) { 'Build' { 'BuildWindows' } 'Android' { 'BuildAndroid' } default { 'Prepare' } }
    $arguments += @('-quit', '-nographics', '-executeMethod', "DoorDemo.Editor.DemoProjectTools.$method")
} else {
    $results = Join-Path $artifacts "$Action-results.xml"
    $arguments += @('-runTests', '-testPlatform', $Action, '-testResults', "`"$results`"")
    if ($Action -eq 'EditMode') { $arguments += '-nographics' }
}
$startedAt = Get-Date
$unityProcess = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait -RedirectStandardOutput (Join-Path $artifacts "unity-$Action.stdout.log") -RedirectStandardError (Join-Path $artifacts "unity-$Action.stderr.log")
if ($unityProcess.ExitCode -ne 0) {
    throw "Unity termino con codigo $($unityProcess.ExitCode). Revisar $logPath"
}
if ($Action -eq 'EditMode' -or $Action -eq 'PlayMode') {
    if (-not (Test-Path -LiteralPath $results) -or (Get-Item -LiteralPath $results).LastWriteTime -lt $startedAt) {
        throw 'Unity no genero resultados de pruebas nuevos.'
    }
    [xml]$testReport = Get-Content -LiteralPath $results
    if ($testReport.'test-run'.result -ne 'Passed' -or [int]$testReport.'test-run'.total -eq 0) {
        throw "Las pruebas no pasaron. Revisar $results"
    }
}
Write-Output "Unity $Action completado. Log: $logPath"
