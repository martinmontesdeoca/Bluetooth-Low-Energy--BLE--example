# Configurar un segundo Android como periférico nRF Connect

Este es el teléfono periférico; la app Unity se ejecuta en otro Android. Alternativamente se puede probar con Unity en la PC. Se necesita un Android compatible con advertising BLE/modo periférico. Que el teléfono tenga Bluetooth no garantiza esa capacidad.

## 1. Crear el servidor GATT

1. Instalar **nRF Connect for Mobile**, de Nordic Semiconductor, y encender Bluetooth.
2. Conceder los permisos de dispositivos cercanos/Bluetooth que solicite Android. En versiones antiguas puede pedir ubicación para las funciones de escaneo.
3. En el menú de nRF Connect abrir **Configure GATT server** (el nombre puede variar ligeramente según la versión).
4. Crear una configuración, por ejemplo **Unity Door**.
5. Añadir un **Custom service**, de tipo primario, con este UUID:

   `7b9c0001-6d7a-4f20-9a11-2c6e815a4400`

6. Añadir una característica con UUID:

   `7b9c0002-6d7a-4f20-9a11-2c6e815a4400`

7. Habilitar **Read** y **Write**, con sus permisos sin cifrado para esta demo. Si la versión permite establecer el valor inicial aquí, usar **Byte array / Hexadecimal** y `00`.
8. Guardar y dejar seleccionada esta configuración del servidor. No hace falta habilitar Notify ni agregar descriptores.

## 2. Anunciar el servicio

1. Abrir **Advertiser** y crear un anuncio con **+**.
2. Habilitar **Connectable**.
3. Añadir a **Advertising data** una lista de **Service UUIDs** con el UUID del servicio (el que termina en `0001` en su primer grupo).
4. Iniciar el anuncio. El nombre es opcional: la búsqueda usa el UUID del servicio, no el nombre ni la dirección MAC.
5. Si aparece un error de tamaño, quitar el nombre del paquete de advertising o trasladarlo a **Scan response**; mantener el UUID del servicio en el anuncio.

Dejar nRF Connect abierto y el teléfono cerca de la PC durante la demo.

## 3. Conectar y controlar la puerta

1. Ejecutar la app Unity Android `ConnectedRoom.apk` (o el cliente de escritorio opcional).
2. Seleccionar **Bluetooth real** y pulsar **Conectar**.
3. Cuando el cliente conecte, abrir en nRF Connect la conexión entrante y su pestaña **SERVER**. Ahí están los valores locales que la app lee. La pestaña CLIENT corresponde al dispositivo remoto.
4. Expandir el servicio y la característica de la puerta. Usar la acción de edición del **valor local** (lápiz/editar, según versión).
5. Elegir **Byte array / Hexadecimal**, introducir `01` y confirmar: la puerta debe abrirse tras una lectura.
6. Cambiar el valor a `00`: la puerta debe cerrarse.

El valor debe ser **un solo byte**. El texto ASCII `"1"` produce `31` hexadecimal y se rechaza. No ingresar `0001` (dos bytes). Los valores locales pertenecen a la conexión: después de reconectar puede ser necesario volver a establecerlos en SERVER. Modificar la plantilla del servidor mientras ya existe una conexión no necesariamente cambia los valores de esa conexión.

## Recorrido de aceptación con hardware

| Acción | Resultado esperado |
|---|---|
| Anunciar servicio y conectar | Estado Connected tras una lectura GATT correcta |
| Establecer `01` en SERVER | Puerta abierta e indicador verde |
| Establecer `00` en SERVER | Puerta cerrada e indicador naranja |
| Establecer `02` o ASCII `1` | Aviso de dato rechazado; conserva el último estado válido |
| Apagar Bluetooth del periférico | Error tras el fallo/límite de lectura, indicador gris y estado del sensor desconocido |
| Restaurar anuncio y pulsar Conectar | Nueva conexión; se puede controlar otra vez |
| Salir de Play/cerrar la demo | El proceso BleBridge finaliza |

## Resolver problemas

- **No se encontró el servicio:** verificar que el anuncio está activo, es conectable y contiene el UUID del servicio correcto. El escaneo expira tras 15 segundos y permite volver a intentarlo.
- **Servicio/característica no encontrados:** comprobar que el servidor GATT seleccionado está habilitado y que los UUID coinciden. Si cambiaste la estructura, desconectar y volver a anunciar/conectar para evitar cachés del sistema.
- **Lectura denegada o ProtocolError:** habilitar Read y su permiso. Comprobar el valor en la pestaña SERVER de la conexión activa.
- **La puerta no cambia:** usar Byte array hexadecimal, revisar el aviso en la interfaz y confirmar que no se está usando el modo simulado.
- **RadioNotAvailable / adaptador:** encender Bluetooth en Windows y verificar que el adaptador admite BLE.
- **Falta BleBridge.exe:** ejecutar `Tools/Publish-Bridge.ps1` antes de Play o de compilar el player.
- **El proceso termina al iniciar:** si falta .NET 10 Runtime x64, instalarlo o publicar el bridge con `-SelfContained`.
- **El teléfono no admite advertising:** probar otro Android compatible; el modo simulado permite ensayar la escena, pero no demuestra BLE real.

Referencia de las funciones de servidor y advertiser: [documentación oficial de Nordic](https://github.com/nordicsemi/Android-nRF-Connect/blob/main/documentation/README.md).
