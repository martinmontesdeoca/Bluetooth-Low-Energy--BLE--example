using ConnectedRoom.Native;
using DoorDemo.Domain;

namespace BleBridge;

public sealed class GattSensorPeripheral
{
    /// <summary>Runs the optional console peripheral with independent incoming and outgoing door states.</summary>
    public async Task<int> RunAsync()
    {
        using var host = new GattSensorHost();
        host.Message += Console.WriteLine;
        host.ReceivedDoor.Changed += (state, origin) => Console.WriteLine(origin + ": " + state);
        await host.StartAsync();
        Console.WriteLine("Sensor bidireccional: 0 = cerrar; 1 = abrir; q = salir. Confirma con Enter.");
        while (await Console.In.ReadLineAsync() is { } line && line != "q")
        {
            if (line == "0" || line == "1")
                host.OutgoingDoor.Set(line == "1" ? DoorState.Open : DoorState.Closed, "Windows CLI → Android");
            else Console.WriteLine("Valor invalido. Usa 0, 1 o q.");
        }
        return 0;
    }
}
