namespace DoorDemo.Application
{
    public enum ConnectionState
    {
        Disconnected,
        Searching,
        Connecting,
        Connected,
        Error
    }

    public readonly struct ConnectionStatus
    {
        /// <summary>Creates a connection snapshot with a non-null diagnostic message.</summary>
        public ConnectionStatus(ConnectionState state, string message)
        {
            State = state;
            Message = message ?? string.Empty;
        }

        public ConnectionState State { get; }
        public string Message { get; }
    }
}
