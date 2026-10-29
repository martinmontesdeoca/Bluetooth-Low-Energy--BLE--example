# Desarrollo y revisión

Usar Unity 6000.4.3f1 con Android Build Support y .NET SDK 10 en Windows. Después de clonar, ejecutar `Tools/Build-AndroidPlugin.ps1` antes de compilar Android.

- Mantener el dominio y la aplicación independientes de Unity y de APIs nativas. Inyectar adaptadores en `DemoBootstrap`; la vista Windows recibe su host en el constructor.
- Cada clase debe ocupar como máximo 300 líneas, incluidos comentarios y espacios. En clases parciales se suman todas sus partes. Extraer responsabilidades mediante composición cuando sea necesario.
- Documentar cada método C# con un `<summary>` significativo, incluidos privados, constructores y pruebas. Usar Javadoc para los métodos Java. Explicar contratos y decisiones, sin repetir instrucciones obvias.
- Conservar dos estados independientes: las órdenes salientes y los ACK no modifican la puerta local. Solo las lecturas u órdenes entrantes la modifican.
- Entregar eventos nativos al hilo de la interfaz, invalidar callbacks de sesiones anteriores y liberar operaciones y suscripciones al desconectar.
- Añadir pruebas de comportamiento y regresión cuando cambie lógica. No versionar cachés, ejecutables, claves de firma ni secretos.

Antes de entregar:

```powershell
./Tools/Test-CodeQuality.ps1
./Tools/Build-AndroidPlugin.ps1
./Tools/Validate-Unity.ps1 -Action EditMode
./Tools/Validate-Unity.ps1 -Action PlayMode
./Tools/Validate-Unity.ps1 -Action Android
dotnet build ./Native/WindowsCompanion/WindowsCompanion.csproj
dotnet build ./Native/BleBridge/BleBridge.csproj
```

Los tests Unity se ejecutan secuencialmente sobre la misma carpeta. Los resultados y logs se guardan en `Artifacts`. Un cambio en radio requiere además verificar ambas aplicaciones con hardware BLE compatible; las pruebas automatizadas no certifican otros teléfonos o adaptadores.
