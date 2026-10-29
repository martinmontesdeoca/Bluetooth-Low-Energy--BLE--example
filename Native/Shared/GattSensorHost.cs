using DoorDemo.Domain;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace ConnectedRoom.Native;

public sealed class GattSensorHost : IDisposable
{
    private GattServiceProvider? provider;
    private GattLocalCharacteristic? characteristic;
    private int generation;
    private int reads;
    private int writes;
    private bool disposed;
    public DoorSensorState OutgoingDoor { get; } = new();
    public DoorSensorState ReceivedDoor { get; } = new();
    public event Action<string>? Message;
    public event Action<bool>? AdvertisingChanged;
    public event Action<int, int>? TrafficChanged;

    /// <summary>Stages a Read and Write service and abandons startup when its ownership generation changes.</summary>
    public async Task StartAsync()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        Stop();
        int attempt = ++generation;
        var adapter = await BluetoothAdapter.GetDefaultAsync();
        EnsureCurrentAttempt(attempt);
        if (adapter == null || !adapter.IsPeripheralRoleSupported)
            throw new IOException("El adaptador de esta PC no admite el rol periferico BLE.");
        var result = await GattServiceProvider.CreateAsync(Guid.Parse(DoorProtocol.ServiceUuid));
        if (result.Error != BluetoothError.Success)
            throw new IOException("No se pudo crear GATT: " + result.Error + ". Enciende Bluetooth.");
        var created = result.ServiceProvider;
        EnsureCurrentAttempt(attempt);
        var local = await created.Service.CreateCharacteristicAsync(
            Guid.Parse(DoorProtocol.CharacteristicUuid), new GattLocalCharacteristicParameters
            {
                CharacteristicProperties = GattCharacteristicProperties.Read | GattCharacteristicProperties.Write,
                ReadProtectionLevel = GattProtectionLevel.Plain,
                WriteProtectionLevel = GattProtectionLevel.Plain,
                UserDescription = "Connected Room bidirectional door"
            });
        EnsureCurrentAttempt(attempt);
        if (local.Error != BluetoothError.Success)
            throw new IOException("No se pudo crear Read/Write: " + local.Error);
        provider = created;
        characteristic = local.Characteristic;
        characteristic.ReadRequested += OnRead;
        characteristic.WriteRequested += OnWrite;
        provider.AdvertisementStatusChanged += OnAdvertising;
        reads = writes = 0;
        try
        {
            provider.StartAdvertising(new GattServiceProviderAdvertisingParameters
            {
                IsConnectable = true,
                IsDiscoverable = true
            });
        }
        catch
        {
            Stop();
            throw;
        }
        Message?.Invoke("Servicio Read + Write creado. Conecta desde Android.");
    }

    /// <summary>Prevents a stopped or disposed startup operation from publishing a late service.</summary>
    private void EnsureCurrentAttempt(int attempt)
    {
        if (disposed || attempt != Volatile.Read(ref generation))
            throw new OperationCanceledException("El inicio del servidor BLE fue cancelado.");
    }

    /// <summary>Reports advertisement transitions only for the provider currently owned by this host.</summary>
    private void OnAdvertising(GattServiceProvider sender, GattServiceProviderAdvertisementStatusChangedEventArgs args)
    {
        if (sender != provider) return;
        AdvertisingChanged?.Invoke(args.Status == GattServiceProviderAdvertisementStatus.Started);
        Message?.Invoke("Anuncio BLE: " + args.Status + " / " + args.Error);
    }

    /// <summary>Returns commands addressed to Android and completes the request deferral on every path.</summary>
    private async void OnRead(GattLocalCharacteristic sender, GattReadRequestedEventArgs args)
    {
        var deferral = args.GetDeferral();
        try
        {
            var request = await args.GetRequestAsync();
            if (request == null) return;
            if (disposed || sender != characteristic) { request.RespondWithProtocolError(0x0E); return; }
            if (request.Offset != 0) { request.RespondWithProtocolError(0x07); return; }
            using var writer = new DataWriter();
            writer.WriteByte(OutgoingDoor.Value == DoorState.Open ? (byte)1 : (byte)0);
            request.RespondWithValue(writer.DetachBuffer());
            TrafficChanged?.Invoke(Interlocked.Increment(ref reads), Volatile.Read(ref writes));
        }
        catch (Exception error) { Message?.Invoke("Error al responder Read: " + error.Message); }
        finally { deferral.Complete(); }
    }

    /// <summary>Validates an active one-byte command addressed to Windows before acknowledging it.</summary>
    private async void OnWrite(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args)
    {
        var deferral = args.GetDeferral();
        try
        {
            var request = await args.GetRequestAsync();
            if (request == null) return;
            if (disposed || sender != characteristic)
            {
                if (request.Option == GattWriteOption.WriteWithResponse) request.RespondWithProtocolError(0x0E);
                return;
            }
            if (request.Offset != 0 || request.Value.Length != 1)
            {
                if (request.Option == GattWriteOption.WriteWithResponse)
                    request.RespondWithProtocolError(GattProtocolError.InvalidAttributeValueLength);
                Message?.Invoke("Escritura rechazada: se espera exactamente un byte.");
                return;
            }
            using var reader = DataReader.FromBuffer(request.Value);
            var payload = new byte[reader.UnconsumedBufferLength];
            reader.ReadBytes(payload);
            if (!ReceivedDoor.TryWrite(payload))
            {
                if (request.Option == GattWriteOption.WriteWithResponse)
                    request.RespondWithProtocolError(GattProtocolError.InvalidAttributeValueLength);
                Message?.Invoke("Escritura rechazada: se espera exactamente 00 o 01.");
                return;
            }
            if (request.Option == GattWriteOption.WriteWithResponse) request.Respond();
            TrafficChanged?.Invoke(Volatile.Read(ref reads), Interlocked.Increment(ref writes));
        }
        catch (Exception error) { Message?.Invoke("Error al responder Write: " + error.Message); }
        finally { deferral.Complete(); }
    }

    /// <summary>Invalidates startup and pending requests, detaches callbacks and stops the previous provider.</summary>
    public void Stop()
    {
        generation++;
        var previous = provider;
        provider = null;
        if (previous != null)
        {
            previous.AdvertisementStatusChanged -= OnAdvertising;
        }
        if (characteristic != null)
        {
            characteristic.ReadRequested -= OnRead;
            characteristic.WriteRequested -= OnWrite;
            characteristic = null;
        }
        try { previous?.StopAdvertising(); }
        finally { AdvertisingChanged?.Invoke(false); }
    }

    /// <summary>Permanently ends host ownership and makes repeated disposal harmless.</summary>
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Stop();
    }
}
