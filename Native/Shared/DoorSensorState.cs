using DoorDemo.Domain;

namespace ConnectedRoom.Native;

public sealed class DoorSensorState
{
    private int value;
    public DoorState Value => Volatile.Read(ref value) == 1 ? DoorState.Open : DoorState.Closed;
    public event Action<DoorState, string>? Changed;

    /// <summary>Atomically stores a valid receiver state and reports which peer supplied the command.</summary>
    public void Set(DoorState state, string origin)
    {
        if (state != DoorState.Closed && state != DoorState.Open)
            throw new ArgumentOutOfRangeException(nameof(state));
        Volatile.Write(ref value, state == DoorState.Open ? 1 : 0);
        Changed?.Invoke(state, origin);
    }

    /// <summary>Accepts the shared one-byte command contract before changing the Windows receiver state.</summary>
    public bool TryWrite(byte[] payload)
    {
        if (!DoorCommand.TryParse(payload, out DoorState state)) return false;
        Set(state, "Android / BLE");
        return true;
    }
}
