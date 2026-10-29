<# .SYNOPSIS Packages the current Android and portable Windows builds with their quick-start guide. #>
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$windowsPath = Join-Path $projectRoot 'Builds/WindowsCompanion'
$apkPath = Join-Path $projectRoot 'Builds/Android/ConnectedRoom.apk'
$guidePath = Join-Path $projectRoot 'docs/QUICKSTART.txt'
if (-not (Test-Path (Join-Path $windowsPath 'ConnectedRoom.Windows.exe')) -or -not (Test-Path $apkPath)) {
    throw 'Primero compila Android y publica Windows.'
}
$sharePath = Join-Path $projectRoot 'Builds/Share'
New-Item -ItemType Directory -Path $sharePath -Force | Out-Null
$zipPath = Join-Path $sharePath 'ConnectedRoom-Demo.zip'
$zipStream = [IO.File]::Open($zipPath, [IO.FileMode]::Create, [IO.FileAccess]::Write)
$archive = $null
try {
    $archive = [IO.Compression.ZipArchive]::new($zipStream, [IO.Compression.ZipArchiveMode]::Create)
    foreach ($file in Get-ChildItem -LiteralPath $windowsPath -Recurse -File) {
        $relative = $file.FullName.Substring($windowsPath.Length + 1).Replace('\', '/')
        [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, "Windows/$relative")
    }
    [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $apkPath, 'Android/ConnectedRoom.apk')
    [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $guidePath, 'LEEME.txt')
} finally {
    if ($null -ne $archive) { $archive.Dispose() }
    $zipStream.Dispose()
}
Write-Output "Paquete para compartir: $zipPath"
