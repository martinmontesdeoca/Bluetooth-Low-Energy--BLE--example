# Demo móvil Android / BLE real

## Elegir el periférico

| Opción | Cuándo conviene | Requisito |
|---|---|---|
| Segundo Android con nRF Connect | La ruta más rápida sin montar electrónica | Ese teléfono debe admitir modo periférico/advertising |
| PC con servidor incluido | Un solo Android disponible | Adaptador Windows con rol periférico y radio habilitada |
| Placa o sensor BLE | Demostrar integración con un dispositivo dedicado | Debe exponer los UUID y el byte de este contrato, o adaptar el protocolo |

La opción principal es la app Windows incluida: `Builds/WindowsCompanion/ConnectedRoom.Windows.exe`. Tiene puerta, botones y servidor GATT Read/Write. Android es central/cliente. Para compartir usar el ZIP generado por `Tools/Package-Demo.ps1`.

nRF Connect, si se usa, se ejecuta en un **segundo Android**, como periférico/servidor GATT con Read y Write. La demo no presupone comunicación de una app consigo misma en el mismo celular.

## Compilar

```powershell
./Tools/Build-AndroidPlugin.ps1
./Tools/Validate-Unity.ps1 -Action Android
```

También existe el menú **Door Demo → Build Android APK**. La salida es `Builds/Android/ConnectedRoom.apk`, con IL2CPP y ARM64, mínimo Android 8. El APK de desarrollo usa firma de desarrollo: no se publica automáticamente ni se sube a ninguna tienda.

## Probar

1. Instalar el APK en el Android que ejecutará Unity. Para instalación USB, autorizar ese equipo en el teléfono y usar `adb install -r Builds/Android/ConnectedRoom.apk`, si se desea. Revisar y aceptar los avisos de instalación personalmente.
2. Preparar el periférico. Con PC ejecutar `./Tools/Publish-Windows.ps1`, abrir `Builds/WindowsCompanion/ConnectedRoom.Windows.exe` y pulsar **Iniciar BLE**. Con otro Android seguir [ANDROID.md](ANDROID.md). La consola opcional se prepara con `./Tools/Publish-Bridge.ps1` y `Tools/Start-PC-Sensor.cmd`; acepta `0`/`1` + Enter y `q` para salir.
3. Abrir Connected Room, que usa orientación horizontal.
4. Pulsar **Conectar** y conceder los permisos solicitados. Android 12+ usa Nearby devices/Bluetooth; Android 8–11 usa permiso de ubicación y requiere el servicio de ubicación activo para escanear.
5. Comprobar estado **Connected** y lectura inicial. Cambiar el valor en el periférico de `00` a `01` y nuevamente a `00`.
6. Con la app Windows, probar **Abrir/Cerrar en ambos extremos**. El móvil escribe órdenes hacia Windows y recibe una confirmación de escritura; las lecturas llevan las órdenes de Windows hacia el móvil. Solo cambia la puerta receptora. Si se apaga el periférico, debe aparecer error y **SIN DATO DEL SENSOR** después del fallo/watchdog.
7. Restaurar el anuncio y pulsar **Conectar**. Cambiar de app suspende una conexión ya establecida; al volver se reconecta manualmente.

El escaneo tiene un límite de 15 segundos. Conexión, descubrimiento y cada lectura tienen watchdog de 20 segundos. No hay servicio de segundo plano ni reintentos automáticos. Se debe mantener la app y el periférico disponibles durante la presentación.

## Arquitectura nativa

```text
Unity C# / DoorController
       │ IDoorTransport
AndroidDoorTransport + permisos
       │ JNI / AndroidJavaObject
AndroidBleBridge (Java / AAR)
       ├─ BleDiscovery: BluetoothLeScanner + ScanFilter por UUID
       ├─ GattDoorConnection: connectGatt, discoverServices, readCharacteristic
       └─ BridgeEvents: cola acotada JSON consumida por Pump en Unity
       │ BLE real
Periferico GATT: Read y Write de un byte
```

Los callbacks usan un Handler del main Looper de Android. Ningún callback nativo modifica GameObjects. El puente C# consume mensajes desde el Update de Unity. En Android 13+ se usa el callback con copia del valor; en sistemas anteriores se usa el callback legado. Las lecturas se serializan y los recursos GATT se cierran explícitamente.

## Alcance comprobable

- Tener las fuentes, un AAR o un APK no equivale a una app publicada.
- El resultado de las pruebas automatizadas valida lógica y animación; no demuestra que haya ocurrido una conexión física BLE.
- El teléfono/PC periférico genera datos manuales que viajan por BLE real. Para afirmar integración con un sensor físico hace falta conectarlo y documentar esa prueba.
- Esta implementación es Android. No se implementó CoreBluetooth para iOS ni se creó una publicación en App Store.

Para documentar una prueba BLE, grabar un video continuo mostrando los dos dispositivos y registrar modelo, versión Android, periférico y fecha. Completar esos datos después de ejecutar el recorrido.
