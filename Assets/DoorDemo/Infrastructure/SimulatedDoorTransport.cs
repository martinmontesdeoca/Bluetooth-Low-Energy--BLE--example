using System;
using DoorDemo.Application;
using DoorDemo.Domain;

namespace DoorDemo.Infrastructure
{
    public sealed class SimulatedDoorTransport : IDoorTransport, IDoorCommandWriter
    {
        private bool connected;
        public event Action<byte[]> ValueReceived;
        public event Action<ConnectionStatus> StatusChanged;
        public event Action<DoorState> CommandAcknowledged;

        /// <summary>Starts an explicitly simulated session and supplies its initial local reading.</summary>
        public void Connect()
        {
            connected = true;
            StatusChanged?.Invoke(new ConnectionStatus(ConnectionState.Connected,
                "Simulacion activa: no hay trafico Bluetooth"));
            SetOpen(false);
        }

        /// <summary>Injects an incoming command for preview or tests while the simulated session is connected.</summary>
        public void SetOpen(bool open)
        {
            if (connected)
                ValueReceived?.Invoke(new[] { (byte)(open ? 1 : 0) });
        }

        /// <summary>Acknowledges a simulated outgoing command without changing the sender door.</summary>
        public void WriteDoorState(DoorState state)
        {
            if (connected) CommandAcknowledged?.Invoke(state);
        }

        /// <summary>Marks the simulated session disconnected and notifies its consumers.</summary>
        public void Disconnect()
        {
            connected = false;
            StatusChanged?.Invoke(new ConnectionStatus(ConnectionState.Disconnected,
                "Simulacion desconectada"));
        }

        /// <summary>Provides the transport pump contract; simulated events are already delivered synchronously.</summary>
        public void Pump() { }
        /// <summary>Ends the simulated session and releases its logical connection.</summary>
        public void Dispose() => Disconnect();
    }
}
