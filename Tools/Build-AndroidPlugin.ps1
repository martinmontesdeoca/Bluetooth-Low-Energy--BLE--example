<# .SYNOPSIS Compiles the authored Java BLE plugin into a generated AAR using Unity's Android toolchain. #>
param([string]$AndroidPlayer)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot 'UnityPaths.ps1')
$AndroidPlayer = Get-AndroidPlayerPath -ProjectRoot $projectRoot -AndroidPlayer $AndroidPlayer
$buildPath = Join-Path $projectRoot ('Artifacts/android-plugin/' + [Guid]::NewGuid().ToString('N'))
$classesPath = Join-Path $buildPath 'classes'
$packagePath = Join-Path $buildPath 'aar'
$pluginPath = Join-Path $projectRoot 'Assets/Plugins/Android'
New-Item -ItemType Directory -Path $classesPath,$packagePath,$pluginPath -Force | Out-Null
$sources = Get-ChildItem (Join-Path $projectRoot 'Native/AndroidBle/src') -Recurse -Filter *.java
$androidJar = Join-Path $AndroidPlayer 'SDK/platforms/android-36/android.jar'
$javac = Join-Path $AndroidPlayer 'OpenJDK/bin/javac.exe'
$jar = Join-Path $AndroidPlayer 'OpenJDK/bin/jar.exe'
& $javac -encoding UTF-8 -source 8 -target 8 -Xlint:all,-options,-deprecation -classpath $androidJar -d $classesPath $sources.FullName
if ($LASTEXITCODE -ne 0) { throw 'Fallo la compilacion Java del plugin BLE.' }
& $jar "-J-Djava.io.tmpdir=$buildPath" --create --file (Join-Path $packagePath 'classes.jar') -C $classesPath .
if ($LASTEXITCODE -ne 0) { throw 'Fallo la creacion de classes.jar.' }
Copy-Item -LiteralPath (Join-Path $projectRoot 'Native/AndroidBle/AndroidManifest.xml') -Destination $packagePath
Copy-Item -LiteralPath (Join-Path $projectRoot 'Native/AndroidBle/consumer-rules.pro') -Destination (Join-Path $packagePath 'proguard.txt')
& $jar "-J-Djava.io.tmpdir=$buildPath" --create --file (Join-Path $pluginPath 'ConnectedRoomBle.aar') -C $packagePath .
if ($LASTEXITCODE -ne 0) { throw 'Fallo la creacion del archivo AAR.' }
Write-Output 'Plugin nativo Android BLE compilado: Assets/Plugins/Android/ConnectedRoomBle.aar'
