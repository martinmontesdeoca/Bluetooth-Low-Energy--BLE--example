using System.Text.Json;

namespace BleBridge;

public sealed class ProtocolWriter
{
    private readonly object gate = new();

    /// <summary>Writes a connection status using the versioned native message contract.</summary>
    public void Status(string state, string message) => Write(new { type = "status", state, message });
    /// <summary>Serializes actual payload bytes as hexadecimal without converting them to ASCII commands.</summary>
    public void Value(byte[] bytes) => Write(new { type = "value", hex = Convert.ToHexString(bytes) });

    /// <summary>Serializes and flushes one complete JSON line while preventing interleaved output.</summary>
    private void Write(object message)
    {
        lock (gate)
        {
            Console.WriteLine(JsonSerializer.Serialize(message));
            Console.Out.Flush();
        }
    }
}
