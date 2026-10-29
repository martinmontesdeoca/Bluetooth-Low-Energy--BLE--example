namespace DoorDemo.Domain
{
    /// <summary>The wire contract is exactly one binary byte: 00 or 01.</summary>
    public static class DoorCommand
    {
        /// <summary>Accepts exactly one binary byte and rejects ASCII, unsupported values and incorrect lengths.</summary>
        public static bool TryParse(byte[] payload, out DoorState state)
        {
            state = DoorState.Closed;
            if (payload == null || payload.Length != 1 || payload[0] > 1)
                return false;

            state = payload[0] == 1 ? DoorState.Open : DoorState.Closed;
            return true;
        }
    }
}
