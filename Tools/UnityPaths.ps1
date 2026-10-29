function Get-UnityEditorPath {
    <# .SYNOPSIS Resolves the project's declared editor version or validates an explicit installation path. #>
    param([string]$ProjectRoot, [string]$UnityPath)
    if (-not $UnityPath) {
        $versionFile = Join-Path $ProjectRoot 'ProjectSettings/ProjectVersion.txt'
        $versionLine = Get-Content -LiteralPath $versionFile | Where-Object { $_ -match '^m_EditorVersion: ' }
        if (-not $versionLine) { throw "No se encontro la version Unity en $versionFile" }
        $version = ($versionLine -split ': ', 2)[1].Trim()
        $UnityPath = Join-Path $env:ProgramFiles "Unity/Hub/Editor/$version/Editor/Unity.exe"
    }
    if (-not (Test-Path -LiteralPath $UnityPath)) {
        throw "No se encontro Unity en $UnityPath. Indica -UnityPath con la instalacion correcta."
    }
    return (Resolve-Path -LiteralPath $UnityPath).Path
}

function Get-AndroidPlayerPath {
    <# .SYNOPSIS Locates Android Build Support without embedding a developer's machine-specific directory. #>
    param([string]$ProjectRoot, [string]$AndroidPlayer)
    if (-not $AndroidPlayer) {
        $editor = Get-UnityEditorPath -ProjectRoot $ProjectRoot
        $AndroidPlayer = Join-Path (Split-Path -Parent $editor) 'Data/PlaybackEngines/AndroidPlayer'
    }
    if (-not (Test-Path -LiteralPath (Join-Path $AndroidPlayer 'OpenJDK/bin/java.exe'))) {
        throw 'Instala Android Build Support con OpenJDK o indica -AndroidPlayer.'
    }
    return (Resolve-Path -LiteralPath $AndroidPlayer).Path
}
