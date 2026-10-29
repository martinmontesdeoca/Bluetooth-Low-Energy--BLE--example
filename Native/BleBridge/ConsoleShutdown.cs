namespace BleBridge;

/// <summary>Owns console cancellation and prevents late stdin completion from using a disposed token source.</summary>
internal sealed class ConsoleShutdown : IDisposable
{
    private readonly object gate = new();
    private readonly CancellationTokenSource source = new();
    private bool disposed;
    public CancellationToken Token => source.Token;
    public bool IsCancellationRequested => source.IsCancellationRequested;

    /// <summary>Watches stdin without blocking discovery and handles Ctrl+C for this process lifetime.</summary>
    public ConsoleShutdown()
    {
        Console.CancelKeyPress += OnCancel;
        _ = Task.Run(WatchInputAsync);
    }

    /// <summary>Ends the client when Unity closes stdin and reports stream failures explicitly.</summary>
    private async Task WatchInputAsync()
    {
        try { await Console.In.ReadLineAsync(); }
        catch (Exception error) when (error is IOException || error is ObjectDisposedException)
        {
            Console.Error.WriteLine("Entrada del puente BLE: " + error.Message);
        }
        finally { RequestStop(); }
    }

    /// <summary>Converts Ctrl+C into cooperative cancellation instead of abrupt process termination.</summary>
    private void OnCancel(object? sender, ConsoleCancelEventArgs args)
    {
        args.Cancel = true;
        RequestStop();
    }

    /// <summary>Serializes cancellation with disposal so a late input callback cannot access released state.</summary>
    private void RequestStop()
    {
        lock (gate)
        {
            if (!disposed) source.Cancel();
        }
    }

    /// <summary>Unsubscribes process events and releases cancellation ownership exactly once.</summary>
    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            Console.CancelKeyPress -= OnCancel;
            source.Dispose();
        }
    }
}
