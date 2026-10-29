namespace BleBridge;

/// <summary>Runs the optional BLE client helper or console peripheral.</summary>
internal static class Program
{
    /// <summary>Routes the requested role and reports native failures through the helper protocol.</summary>
    private static async Task<int> Main(string[] args)
    {

        var writer = new ProtocolWriter();
        if (args.Length == 1 && args[0] == "--peripheral")
        {
            try { return await new GattSensorPeripheral().RunAsync(); }
            catch (Exception error)
            {
                Console.Error.WriteLine("Periferico BLE: " + error.Message);
                return 1;
            }
        }
        using var lifetime = new ConsoleShutdown();

        try
        {
            var options = BridgeOptions.Parse(args);
            writer.Status("searching", "Buscando el servicio BLE de la puerta...");
            ulong address = await new DeviceDiscovery().FindAsync(options.Service, options.ScanTimeout, lifetime.Token);
            writer.Status("connecting", "Telefono encontrado. Descubriendo servicio GATT...");

            using var client = new GattDoorClient(options);
            using (var connectTimeout = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token))
            {
                connectTimeout.CancelAfter(options.OperationTimeout);
                await client.ConnectAsync(address, connectTimeout.Token);
            }

            // Only report Connected after a successful real read, never just after device discovery.
            byte[]? previous = null;
            bool connected = false;
            while (!lifetime.IsCancellationRequested)
            {
                using var readTimeout = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
                readTimeout.CancelAfter(options.OperationTimeout);
                byte[] value = await client.ReadAsync(readTimeout.Token);
                if (!connected)
                {
                    connected = true;
                    writer.Status("connected", "BLE conectado. Cambia el valor 00/01 en el telefono.");
                }
                if (previous == null || !value.AsSpan().SequenceEqual(previous))
                {
                    writer.Value(value);
                    previous = value;
                }
                await Task.Delay(options.PollInterval, lifetime.Token);
            }
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
        {
            writer.Status("disconnected", "Conexion finalizada");
            return 0;
        }
        catch (OperationCanceledException)
        {
            writer.Status("error", "La operacion GATT supero el tiempo limite. Revisa el telefono y reconecta.");
            return 1;
        }
        catch (Exception error)
        {
            writer.Status("error", error.Message);
            return 1;
        }
        return 0;

    }
}
