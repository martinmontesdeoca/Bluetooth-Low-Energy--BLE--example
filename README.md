# Connected Room · Unity + BLE + Android

Demo bidireccional: app móvil Unity Android y aplicación Windows portátil, ambas con una puerta y botones Abrir/Cerrar. Cada botón controla **solo la puerta del receptor**. Windows actúa como servidor GATT: Read lleva órdenes a Android y Write lleva órdenes a Windows por **BLE real**. Los dos estados son independientes.

**Para compartir:** ejecutar `Tools/Package-Demo.ps1`. Genera `Builds/Share/ConnectedRoom-Demo.zip` con APK, app Windows autocontenida y [guía rápida](docs/QUICKSTART.txt). Ver [protocolo bidireccional](docs/BIDIRECTIONAL.md).

Proyecto de portfolio que demuestra integración de Unity/C# con hardware BLE mediante un plugin Android nativo propio (Java/JNI) y un servidor GATT Windows (WinRT). Incluye separación de dominio, aplicación e infraestructura, manejo de permisos y errores, y pruebas automatizadas.

Para evaluar el proyecto: comenzar por el [recorrido Android ↔ Windows](docs/BIDIRECTIONAL.md), revisar la [arquitectura detallada](docs/ARCHITECTURE.md) y los [flujos del sistema](docs/FLOWS.md), y consultar los [resultados y límites de validación](docs/VALIDATION.md). La app Android es un prototipo distribuible como APK; no tiene una publicación en Google Play o App Store.

## Abrir y probar

1. Agregar **esta carpeta** a Unity Hub y abrirla con **Unity 6000.4.3f1**, la versión usada para validar el proyecto.
2. Abrir `Assets/DoorDemo/Scenes/DoorDemo.unity` y pulsar Play.
3. Para probar la escena primero: **Modo de prueba sin BLE** → **Conectar**. Los botones confirman órdenes simuladas hacia el receptor; la puerta local no cambia al enviar. Las pruebas automatizadas inyectan las lecturas entrantes y verifican la animación.
4. Para la demo móvil: compilar el APK e instalarlo en un Android ARM64. Seguir [la guía móvil](docs/MOBILE.md), preparar un periférico y pulsar **Conectar**.

La escena ya está guardada. Si necesitás regenerarla en un checkout incompleto: menú **Door Demo → Create or open demo scene**. Ese comando conserva una escena existente.

## Compilar la app Android

Requisitos: Unity 6000.4.3f1 con Android Build Support, OpenJDK, SDK y NDK instalados. Android 8/API 26 o posterior, arquitectura ARM64, con BLE. La app trabaja en primer plano y solicita los permisos de Bluetooth correspondientes a la versión del sistema.

```powershell
./Tools/Build-AndroidPlugin.ps1
./Tools/Validate-Unity.ps1 -Action Android
```

Salida: `Builds/Android/ConnectedRoom.apk`, IL2CPP ARM64 sin consola de desarrollo, versión 0.3.0. Usa un plugin **Java propio empaquetado como AAR**, invocado mediante JNI (`AndroidJavaObject`); no depende de un asset comercial. Sus fuentes están en `Native/AndroidBle`. El AAR se genera con el primer comando y se excluye de Git; debe reconstruirse después de clonar.

El APK de desarrollo no es una publicación en Google Play. Una publicación requiere cuenta, firma de distribución, ficha, políticas y las verificaciones que exija Google Play. No se generó ningún enlace de tienda. iOS queda fuera de este prototipo.

## Aplicación Windows para la demo

```powershell
./Tools/Publish-Windows.ps1
```

Salida: `Builds/WindowsCompanion/ConnectedRoom.Windows.exe`. Conservar toda la carpeta: incluye su runtime, por lo que los evaluadores no necesitan instalar .NET. Pulsar **Iniciar BLE**, conectar desde Android y probar los botones de ambas aplicaciones. El registro diferencia órdenes Windows/Android y muestra lecturas y escrituras reales.

La app Windows usa WinForms y un servidor GATT nativo WinRT. La app Android está hecha con Unity/C#. La versión Windows no es un player Unity.

## Herramienta de consola Windows opcional

La compilación local se genera en `Builds/Windows/BleDoorDemo.exe`. Hay que conservar **toda** la carpeta `Builds/Windows`, incluidos los datos de Unity y StreamingAssets.

Windows puede actuar como cliente para ensayar desde el editor, o como periférico para la app móvil. Requisitos:

- Windows 10 2004 o posterior, arquitectura x64 y adaptador compatible con BLE encendido.
- Para actuar como periférico, el adaptador debe admitir ese rol. El programa comprueba la capacidad y los errores de radio.
- .NET **10 Runtime x64** para el proceso auxiliar en la publicación normal. Alternativamente publicar con `-SelfContained` para incluir el runtime.

Para usar la PC como sensor de prueba:

```powershell
./Tools/Publish-Bridge.ps1
./Artifacts/BleBridge/BleBridge.exe --peripheral
```

En esa consola: `0` + Enter cierra, `1` + Enter abre, `q` + Enter termina el anuncio. El intercambio con Android se hace por BLE; los valores del sensor se generan manualmente.

## Compilar desde las fuentes

Para el cliente de escritorio opcional, con .NET SDK 10 y Windows Build Support (Mono):

```powershell
./Tools/Publish-Bridge.ps1
# Alternativa para otra PC que no tenga .NET 10:
./Tools/Publish-Bridge.ps1 -SelfContained
```

Después abrir Unity y usar **Door Demo → Build Windows demo**, o:

```powershell
./Tools/Validate-Unity.ps1 -Action Build
```

El bridge utiliza las APIs de Windows incluidas a través del TFM `net10.0-windows10.0.19041.0`. No requiere un asset comercial de Bluetooth. La primera restauración necesita acceso a NuGet para las proyecciones oficiales del SDK. Los binarios del bridge son generados y se excluyen de Git; hay que publicarlos después de clonar.

## Protocolo BLE

| Campo | Valor |
|---|---|
| Servicio personalizado | `7b9c0001-6d7a-4f20-9a11-2c6e815a4400` |
| Característica | `7b9c0002-6d7a-4f20-9a11-2c6e815a4400` |
| Propiedades necesarias | Read y Write con respuesta |
| Cerrar | Un byte binario hexadecimal `00` |
| Abrir | Un byte binario hexadecimal `01` |

Windows anuncia el servicio GATT. Android lee cada 350 ms después de completar la lectura anterior para recibir órdenes dirigidas a su puerta. Los botones Android envían una escritura con respuesta dirigida a Windows; la confirmación se muestra como mensaje y no cambia la puerta móvil. Los botones Windows actualizan su canal de salida hacia Android, sin cambiar la puerta local. Las órdenes rápidas se agrupan conservando la última intención y se serializan con las lecturas: no hay dos operaciones GATT simultáneas.

Los UUID se editan en `Assets/DoorDemo/Configuration/DoorDemoSettings.asset` y deben coincidir con el periférico. El servidor PC usa los UUID predeterminados. Se conecta al primer anunciante del servicio: para una demo usar uno solo o UUID propios.

Los datos inválidos se rechazan conservando el último estado válido y mostrando un aviso. Al desconectar o fallar el transporte, se reinicia la animación y el indicador pasa a gris: la interfaz muestra **SIN DATO DEL SENSOR**. Ese reinicio no es una medición física ni se registra como cierre del acceso. La reconexión es manual con **Conectar**; no hay bucles de reintento ocultos.

## Arquitectura

La [documentación de arquitectura](docs/ARCHITECTURE.md) explica capas, dependencias, composición, integración nativa y decisiones de diseño. Los [flujos del sistema](docs/FLOWS.md) incluyen diagramas de conexión, comandos en ambos sentidos, estados y recuperación de errores.

```text
Presentación Unity ──► Aplicación ──► Dominio
       │                    ▲
       └─ composición ─► IDoorTransport
                          ▲           ▲                 ▲
                     Simulado  AndroidDoorTransport  WindowsBridgeTransport
                                      │ JNI             │ stdin-stdout
                               AndroidBleBridge     BleBridge / WinRT
                                      │ BLE GATT         │ BLE GATT
                                      └────── Periferico ┘
```

- **Core/Domain:** valida el contrato binario y define el estado de la puerta. Sin referencias a Unity.
- **Core/Application:** `DoorController` coordina conexión, validación y estado mediante `IDoorTransport`. Sin dependencias de plataforma.
- **IDoorCommandWriter:** puerto separado de escritura; permite transportes solo de lectura. `RequestDoor` y `CommandAcknowledged` no cambian la puerta local: solo las órdenes recibidas lo hacen.
- **SessionMonitor:** mantiene seis eventos recientes, sus horarios y la cantidad de aperturas de la sesión. El reloj se inyecta para poder probarlo. El historial se reinicia al cambiar de modo y no persiste.
- **Infrastructure:** simulación y adaptadores Android/Windows. Los mensajes nativos se entregan en `Pump`, en el hilo de Unity; Android solicita permisos antes de iniciar la radio.
- **Presentation:** escena, animación, interfaz y configuración. `DemoBootstrap` es el punto de composición que inyecta el transporte.
- **Native/BleBridge:** descubrimiento, GATT, opciones y salida JSON en colaboradores separados. Las operaciones tienen límites de tiempo y liberan servicios/dispositivo al salir.
- **Native/AndroidBle:** `AndroidBleBridge` compone descubrimiento, conexión GATT y cola acotada de eventos. Handler/Looper serializan operaciones y callbacks. Solo se inicia una lectura cuando termina la anterior; hay watchdogs de búsqueda/operación y cierre de GATT al desconectar.
- **Editor:** preparación de escena y compilación reproducible.
- **Native/Shared:** estado del servidor y host GATT Read/Write compartidos por la consola y Windows. Reutiliza el contrato de dominio de Unity mediante archivos enlazados.
- **Native/WindowsCompanion:** ventana y animación Windows, sin lógica de radio dentro de la vista. Los callbacks nativos se entregan al hilo de WinForms.

Material y geometría se incluyen en `Resources` y se cargan explícitamente. No se depende de `Shader.Find` ni de `CreatePrimitive` durante el arranque del player: el stripping de IL2CPP había dejado fuera recursos usados únicamente de forma dinámica.

Cada clase está por debajo de 300 líneas. Para incorporar otro periférico con el mismo protocolo, basta cambiar sus UUID. Para un protocolo diferente, cambiar su decodificador de dominio; para otro sistema operativo, implementar `IDoorTransport` y seleccionarlo en la composición.

El destino principal es Android ARM64; Windows x64 es una herramienta auxiliar. La puerta es una representación visual de demostración, sin autenticación ni persistencia. No se presenta como sistema de control de acceso físico. La app desconecta una sesión establecida al pasar a segundo plano; se reconecta manualmente al volver.

## Validación

```powershell
./Tools/Test-CodeQuality.ps1
./Tools/Validate-Unity.ps1 -Action EditMode
./Tools/Validate-Unity.ps1 -Action PlayMode
```

Las pruebas EditMode cubren contrato binario, entradas inválidas, estados de conexión, reconexión, liberación de suscripciones y mensajes del puente. La prueba PlayMode integra transporte simulado, controlador y animación de apertura/cierre, incluido el cierre por desconexión. Los resultados quedan en `Artifacts/`.

Estas pruebas no sustituyen la comprobación con dispositivos. El recorrido móvil está en [MOBILE.md](docs/MOBILE.md); la configuración de un segundo Android como periférico está en [ANDROID.md](docs/ANDROID.md).

El control de calidad requiere .NET SDK 10 y el JDK de Android Build Support. Usa los analizadores sintácticos de Roslyn y Java, sin descargar un framework de análisis: exige un `<summary>` no vacío en cada método C# (incluidos privados, constructores y pruebas), Javadoc en Java y un máximo de 300 líneas por clase. Suma todas las partes de clases parciales. Ver [guía para contribuir](CONTRIBUTING.md).

## Preparar el repositorio

Se versionan `Assets`, sus `.meta`, `Packages`, `ProjectSettings`, las fuentes nativas y las herramientas. `Library`, `Artifacts`, `Builds`, `bin`, `obj`, el plugin compilado y archivos de firma quedan excluidos. Para compartir ejecutables, usar el ZIP de la demo como descarga de una release.

Los scripts resuelven la instalación de Unity desde `ProjectSettings/ProjectVersion.txt`; `Validate-Unity.ps1` admite `-UnityPath` y los scripts de Java admiten `-AndroidPlayer` para instalaciones personalizadas. No dependen de la carpeta de un desarrollador.

`Tools/Export-Source.ps1 -Destination <carpeta>` copia los archivos visibles para Git, comprueba sus hashes y conserva la carpeta `.git` del destino. No crea commits ni publica el repositorio.

## Referencias

- [nRF Connect para Android: servidor GATT y advertising](https://github.com/nordicsemi/Android-nRF-Connect/blob/main/documentation/README.md).
- [Android: permisos Bluetooth](https://developer.android.com/develop/connectivity/bluetooth/bt-permissions).
- [Android: callbacks de lectura GATT](https://developer.android.com/reference/android/bluetooth/BluetoothGattCallback).
- [Microsoft: cliente GATT](https://learn.microsoft.com/en-us/windows/apps/develop/devices-sensors/gatt-client).
- [Microsoft: APIs WinRT en aplicaciones de escritorio .NET](https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/winrt-apis-desktop-apps).
