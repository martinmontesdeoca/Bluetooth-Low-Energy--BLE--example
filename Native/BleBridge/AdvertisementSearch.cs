using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;

namespace BleBridge;

/// <summary>Owns one advertisement watcher and its callbacks for an independent discovery attempt.</summary>
internal sealed class AdvertisementSearch : IDisposable
{
    private readonly Guid service;
    private readonly TaskCompletionSource<ulong> found = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly BluetoothLEAdvertisementWatcher watcher = new() { ScanningMode = BluetoothLEScanningMode.Active };

    /// <summary>Registers callbacks for the requested service before scanning can deliver events.</summary>
    public AdvertisementSearch(Guid service)
    {
        this.service = service;
        watcher.Received += OnReceived;
        watcher.Stopped += OnStopped;
    }

    /// <summary>Starts discovery and waits for a matching address, cancellation or the specified deadline.</summary>
    public async Task<ulong> WaitAsync(TimeSpan timeout, CancellationToken token)
    {
        watcher.Start();
        return await found.Task.WaitAsync(timeout, token);
    }

    /// <summary>Completes discovery only when an advertisement contains the expected service UUID.</summary>
    private void OnReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
    {
        if (args.Advertisement.ServiceUuids.Contains(service)) found.TrySetResult(args.BluetoothAddress);
    }

    /// <summary>Propagates radio failures instead of allowing the caller to wait for a misleading timeout.</summary>
    private void OnStopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
    {
        if (args.Error != BluetoothError.Success)
            found.TrySetException(new IOException("Busqueda BLE interrumpida: " + args.Error));
    }

    /// <summary>Unsubscribes callbacks and stops this attempt without affecting other searches.</summary>
    public void Dispose()
    {
        watcher.Received -= OnReceived;
        watcher.Stopped -= OnStopped;
        if (watcher.Status == BluetoothLEAdvertisementWatcherStatus.Started) watcher.Stop();
    }
}
