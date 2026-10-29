using System;
using DoorDemo.Domain;

namespace DoorDemo.Application
{
    /// <summary>Coordinates the demo without depending on Unity or Windows.</summary>
    public sealed class DoorController : IDisposable
    {
        private readonly IDoorTransport transport;
        private readonly IDoorCommandWriter writer;
        private bool disposed;

        /// <summary>Injects the receive transport and optional peer command writer, then subscribes to their events.</summary>
        public DoorController(IDoorTransport transport, IDoorCommandWriter writer = null)
        {
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
            this.writer = writer;
            if (writer != null) writer.CommandAcknowledged += OnCommandAcknowledged;
            transport.ValueReceived += OnValue;
            transport.StatusChanged += OnStatus;
        }

        public event Action<DoorState> DoorChanged;
        public event Action<ConnectionStatus> ConnectionChanged;
        public event Action<string> InvalidPayload;
        public event Action<string> CommandRejected;
        public event Action<DoorState> CommandAcknowledged;
        public bool CanControl => Connection.State == ConnectionState.Connected && writer != null;
        public DoorState Door { get; private set; } = DoorState.Closed;
        public bool HasReading { get; private set; }
        public ConnectionStatus Connection { get; private set; } =
            new ConnectionStatus(ConnectionState.Disconnected, "Listo para conectar");

        /// <summary>Starts discovery only when no connection attempt or established session is already active.</summary>
        public void Connect()
        {
            ThrowIfDisposed();
            if (Connection.State == ConnectionState.Connected ||
                Connection.State == ConnectionState.Searching ||
                Connection.State == ConnectionState.Connecting)
                return;
            transport.Connect();
        }

        /// <summary>Ends the transport session so its status resets the local view to unknown.</summary>
        public void Disconnect()
        {
            ThrowIfDisposed();
            transport.Disconnect();
        }

        /// <summary>Dispatches transport events while this controller still owns the session.</summary>
        public void Pump()
        {
            ThrowIfDisposed();
            transport.Pump();
        }

        /// <summary>Validates and sends a command addressed to the peer without changing the local door.</summary>
        public void RequestDoor(DoorState state)
        {
            ThrowIfDisposed();
            if (state != DoorState.Closed && state != DoorState.Open)
                throw new ArgumentOutOfRangeException(nameof(state));
            if (!CanControl)
            {
                CommandRejected?.Invoke("Conecta un periferico con escritura BLE antes de controlar la puerta.");
                return;
            }
            // Commands target the peer. Acknowledgements never change this device's door.
            writer.WriteDoorState(state);
        }

        /// <summary>Forwards valid acknowledgements only while their connection remains active.</summary>
        private void OnCommandAcknowledged(DoorState state)
        {
            if (Connection.State == ConnectionState.Connected &&
                (state == DoorState.Open || state == DoorState.Closed))
                CommandAcknowledged?.Invoke(state);
        }

        /// <summary>Applies a valid command received for this device and reports malformed payloads.</summary>
        private void OnValue(byte[] payload)
        {
            if (Connection.State != ConnectionState.Connected)
                return;
            if (!DoorCommand.TryParse(payload, out DoorState state))
            {
                InvalidPayload?.Invoke("Dato rechazado: se espera un byte hexadecimal 00 o 01.");
                return;
            }
            bool firstReading = !HasReading;
            HasReading = true;
            SetDoor(state, firstReading);
        }

        /// <summary>Updates connectivity and invalidates the local reading when the connection is lost.</summary>
        private void OnStatus(ConnectionStatus status)
        {
            Connection = status;
            // A disconnected demo must not retain an old open command.
            if (status.State != ConnectionState.Connected)
            {
                HasReading = false;
                SetDoor(DoorState.Closed);
            }
            ConnectionChanged?.Invoke(status);
        }

        /// <summary>Publishes state transitions and the first valid reading, suppressing duplicate measurements.</summary>
        private void SetDoor(DoorState state, bool forceEvent = false)
        {
            if (Door == state && !forceEvent)
                return;
            Door = state;
            DoorChanged?.Invoke(state);
        }

        /// <summary>Rejects operations after ownership of the transport has been released.</summary>
        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(DoorController));
        }

        /// <summary>Detaches callbacks before disposing the transport and prevents reentrant use during cleanup.</summary>
        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            transport.ValueReceived -= OnValue;
            transport.StatusChanged -= OnStatus;
            if (writer != null) writer.CommandAcknowledged -= OnCommandAcknowledged;
            transport.Dispose();
        }
    }
}
