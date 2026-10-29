# Probar Android ↔ Windows

1. Abrir `Windows/ConnectedRoom.Windows.exe`, o la versión local en `Builds/WindowsCompanion`.
2. Pulsar **Iniciar BLE**. Debe aparecer anuncio activo.
3. En la APK pulsar **Conectar** y conceder permisos. Debe aparecer el estado recibido y crecer **Lecturas** en Windows.
4. En Windows pulsar **Abrir movil**: solo cambia la puerta Unity. La puerta Windows conserva su estado.
5. En Android pulsar **Abre Windows**: solo cambia la puerta PC. Windows registra **Android / BLE: puerta abierta** y aumenta **Escrituras**. La puerta Android conserva su estado.
6. Repetir las otras combinaciones y probar detener/reiniciar BLE con reconexión manual.

## Contrato

Servicio: `7b9c0001-6d7a-4f20-9a11-2c6e815a4400`.
Característica Read + Write: `7b9c0002-6d7a-4f20-9a11-2c6e815a4400`.
Payload: exactamente un byte, `00` cerrado o `01` abierto; no texto ASCII.

El servidor conserva **dos estados independientes**: `OutgoingDoor`, dirigido a Android y expuesto por Read, y `ReceivedDoor`, dirigido a Windows y actualizado por Write. Windows solo anima su puerta al cambiar ReceivedDoor. Unity solo anima su puerta al recibir un valor Read. Enviar órdenes y recibir confirmaciones no anima la puerta del emisor. Prevalece el último comando aceptado para cada destinatario. ADB solo se usa para instalar y diagnosticar en desarrollo; el intercambio entre apps usa BLE.

El registro confirma órdenes recibidas y contadores de tráfico; no demuestra que exista un actuador físico. Las dos puertas son visualizaciones de software. Si se interrumpe BLE, Android muestra estado desconocido, no una confirmación física de cierre.

Para otro Android con nRF Connect, además de Read hay que habilitar **Write** con permiso sin cifrado y consultar/editar el valor de la conexión activa en SERVER. El recorrido principal para compartir utiliza la app Windows incluida.

## Generar y empaquetar

```powershell
./Tools/Build-AndroidPlugin.ps1
./Tools/Validate-Unity.ps1 -Action Android
./Tools/Publish-Windows.ps1
./Tools/Package-Demo.ps1
```

El ZIP contiene las dos compilaciones y LEEME.txt. Extraerlo entero antes de ejecutar Windows. La app Windows autocontenida no necesita instalar .NET, pero sí Windows 10 2004+ x64 y una radio BLE compatible con el rol periférico.

Son compilaciones de pruebas de portfolio; no constituyen una publicación en Google Play/App Store.
