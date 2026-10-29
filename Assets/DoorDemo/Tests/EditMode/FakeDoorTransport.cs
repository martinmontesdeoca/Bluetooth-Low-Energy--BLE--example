using System;
using DoorDemo.Application;
using DoorDemo.Domain;

namespace DoorDemo.Tests
{
    public sealed class FakeDoorTransport : IDoorTransport, IDoorCommandWriter
    {
        public event Action<byte[]> ValueReceived;
        public event Action<ConnectionStatus> StatusChanged;
        public int ConnectCount { get; private set; }
        public bool Disposed { get; private set; }
        public DoorState? RequestedDoor { get; private set; }
        public event Action<DoorState> CommandAcknowledged;
        /// <summary>Injects peer acceptance separately from incoming door values in regression tests.</summary>
        public void Acknowledge(DoorState state) => CommandAcknowledged?.Invoke(state);
        /// <summary>Records the outgoing command without simulating an incoming echo.</summary>
        public void WriteDoorState(DoorState state) => RequestedDoor = state;
        /// <summary>Counts connection requests and supplies a searching transition for controller tests.</summary>
        public void Connect() { ConnectCount++; EmitStatus(ConnectionState.Searching); }
        /// <summary>Injects a disconnected status without accessing hardware.</summary>
        public void Disconnect() => EmitStatus(ConnectionState.Disconnected);
        /// <summary>Injects an arbitrary received payload to exercise validation and routing.</summary>
        public void Emit(byte[] bytes) => ValueReceived?.Invoke(bytes);
        /// <summary>Injects an observable connection transition for the application under test.</summary>
        public void EmitStatus(ConnectionState state) => StatusChanged?.Invoke(new ConnectionStatus(state, "test"));
        /// <summary>Provides the synchronous fake transport pump contract.</summary>
        public void Pump() { }
        /// <summary>Records ownership release so tests can verify controller cleanup.</summary>
        public void Dispose() => Disposed = true;
    }
}
