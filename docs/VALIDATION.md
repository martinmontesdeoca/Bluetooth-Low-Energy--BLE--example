# Validacion - 29 de octubre de 2026

## Revision de fuentes para Git

- 41 pruebas EditMode y 3 PlayMode aprobadas (44 en total). Incluyen rechazo de ACK tras desconectar, aislamiento de callbacks de una sesion anterior, recuperacion de cola saturada y errores de protocolo con un contrato uniforme.
- Analizadores sintacticos de C# y Java: todos los metodos documentados, ningun tipo por encima de 300 lineas. El control C# incluye ramas de compilacion Android/editor, constructores y la suma de partes de clases parciales; el control Java incluye callbacks anonimos.
- Plugin Android compilado desde Java y APK Android IL2CPP ARM64 compilado con Unity 6000.4.3f1 despues del refactor.
- Aplicaciones nativas WindowsCompanion y BleBridge compiladas con cero errores y cero advertencias. Formato verificado con `dotnet format --verify-no-changes` en ambos proyectos y en CodeQuality.
- Ciclos de vida separados para descubrimiento, proceso auxiliar y host GATT. Colas acotadas, callbacks invalidados al reconectar, EOF consumido antes de informar cierre del proceso y limpieza de recursos protegida durante el cierre.
- Herramientas portables: resolucion de Unity por ProjectVersion.txt y reconstruccion del AAR tras clonar. Fuentes y recursos preparados para Git; builds, caches, AAR generado y archivos de firma excluidos.

Los cambios de esta revision se verificaron con compilacion y pruebas automatizadas. No se reinstalo este APK ni se repitio en hardware el recorrido BLE despues del refactor.

## Validacion previa en dispositivos

Los siguientes resultados corresponden a la prueba de hardware anterior a la revision de fuentes; no describen una nueva instalacion de las builds refactorizadas.

- Unity 6000.4.3f1: APK 0.3.0 (versionCode 3), IL2CPP ARM64, min API 26 y target API 36; compilacion final sin consola de desarrollo.
- APK de esa prueba: 15.730.782 bytes. Firma APK v2 valida verificada con apksigner; metadatos comprobados con aapt.
- APK 0.3.0 instalada mediante ADB con Success y version verificada en el telefono. Captura real revisada: controles grandes, paneles de contraste, sala 3D y estado BLE conectado. Consulta de errores Unity/AndroidRuntime del proceso actualizado sin salida.
- Corregidos dos problemas detectados al probar el player: shader omitido y dependencia dinamica de BoxCollider/stripping. Material y malla se incluyen en Resources; la geometria se instancia con MeshFilter/MeshRenderer explicitos.
- App Windows WinForms publicada con runtime autocontenido para win-x64. Ventana, puerta animada, controles y anuncio BLE revisados con computer-use.
- Trafico BLE real observado entre PC y telefono: lecturas y escrituras registradas por Windows y confirmaciones visibles en Android.
- Comportamiento definitivo: Windows envia hacia la puerta Android; Android envia hacia la puerta Windows. OutgoingDoor y ReceivedDoor son estados separados. La puerta del emisor conserva su estado al enviar y al recibir el ACK.
- Paquete generado: Builds/Share/ConnectedRoom-Demo.zip, con Android/ConnectedRoom.apk, Windows/ y LEEME.txt.

## Limites

Prototipo independiente de portfolio. Las puertas son visualizaciones de software, sin actuadores, autenticacion ni persistencia. No existe publicacion en Google Play/App Store y no se implemento iOS. El adaptador de otra PC debe admitir rol periferico BLE.

La prueba controlada de todas las combinaciones de botones, perdida de radio y reconexion en diferentes modelos de telefono sigue siendo parte de la aceptacion manual. El intercambio BLE real observado y las pruebas de aislamiento de estados no equivalen a una certificacion de compatibilidad con otros dispositivos.
