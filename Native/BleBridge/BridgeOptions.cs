namespace BleBridge;

public sealed class BridgeOptions
{
    public required Guid Service { get; init; }
    public required Guid Characteristic { get; init; }
    public TimeSpan ScanTimeout { get; init; } = TimeSpan.FromSeconds(15);
    public TimeSpan OperationTimeout { get; init; } = TimeSpan.FromSeconds(20);
    public TimeSpan PollInterval { get; init; } = TimeSpan.FromMilliseconds(350);

    /// <summary>Validates the complete command-line GATT identity and rejects unsupported invocation shapes.</summary>
    public static BridgeOptions Parse(string[] args)
    {
        if (args == null || args.Length != 4 || args[0] != "--service" || args[2] != "--characteristic" ||
            !Guid.TryParse(args[1], out Guid service) || service == Guid.Empty ||
            !Guid.TryParse(args[3], out Guid characteristic) || characteristic == Guid.Empty)
            throw new ArgumentException("Uso: BleBridge --service <UUID> --characteristic <UUID>");
        return new BridgeOptions { Service = service, Characteristic = characteristic };
    }
}
