@echo off
setlocal
set "BRIDGE=%~dp0..\Artifacts\BleBridge\BleBridge.exe"
if not exist "%BRIDGE%" (
    echo Primero ejecuta Tools/Publish-Bridge.ps1 para compilar el sensor BLE.
    pause
    exit /b 1
)
"%BRIDGE%" --peripheral
if errorlevel 1 pause
