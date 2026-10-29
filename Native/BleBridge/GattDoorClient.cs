using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace BleBridge;

/// <summary>Sequential uncached reads also support a phone characteristic with Read only.</summary>
public sealed class GattDoorClient : IDisposable
{
    private readonly BridgeOptions options;
    private BluetoothLEDevice? device;
    private GattDeviceService? service;
    private GattCharacteristic? characteristic;

    /// <summary>Requires explicit client options before opening Windows GATT resources.</summary>
    public GattDoorClient(BridgeOptions options) => this.options = options ?? throw new ArgumentNullException(nameof(options));

    /// <summary>Discovers the configured service and readable characteristic on the selected peripheral.</summary>
    public async Task ConnectAsync(ulong address, CancellationToken token)
    {
        device = await BluetoothLEDevice.FromBluetoothAddressAsync(address).AsTask(token);
        if (device == null)
            throw new IOException("Windows no permite acceder al dispositivo BLE.");

        var services = await device.GetGattServicesForUuidAsync(options.Service,
            BluetoothCacheMode.Uncached).AsTask(token);
        EnsureSuccess(services.Status, "Descubrimiento del servicio");
        if (services.Services.Count == 0)
            throw new IOException("El telefono no expone el servicio GATT configurado.");
        service = services.Services[0];
        for (int i = 1; i < services.Services.Count; i++)
            services.Services[i].Dispose();

        var characteristics = await service.GetCharacteristicsForUuidAsync(options.Characteristic,
            BluetoothCacheMode.Uncached).AsTask(token);
        EnsureSuccess(characteristics.Status, "Descubrimiento de la caracteristica");
        if (characteristics.Characteristics.Count == 0)
            throw new IOException("No se encontro la caracteristica de la puerta.");
        characteristic = characteristics.Characteristics[0];
        if (!characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Read))
            throw new IOException("La caracteristica debe tener la propiedad Read.");
    }

    /// <summary>Performs one uncached read and returns the actual GATT payload.</summary>
    public async Task<byte[]> ReadAsync(CancellationToken token)
    {
        if (characteristic == null)
            throw new InvalidOperationException("El cliente GATT todavia no esta conectado.");
        var result = await characteristic.ReadValueAsync(BluetoothCacheMode.Uncached).AsTask(token);
        EnsureSuccess(result.Status, "Lectura de la puerta");
        using var reader = DataReader.FromBuffer(result.Value);
        var bytes = new byte[reader.UnconsumedBufferLength];
        reader.ReadBytes(bytes);
        return bytes;
    }

    /// <summary>Explains native communication failures instead of returning an invented reading.</summary>
    private static void EnsureSuccess(GattCommunicationStatus status, string operation)
    {
        if (status != GattCommunicationStatus.Success)
            throw new IOException(operation + " fallo: " + status +
                ". Comprueba Bluetooth, permisos y que nRF Connect siga abierto.");
    }

    /// <summary>Releases the characteristic references, service and device owned by this client.</summary>
    public void Dispose()
    {
        characteristic = null;
        service?.Dispose();
        service = null;
        device?.Dispose();
        device = null;
    }
}
