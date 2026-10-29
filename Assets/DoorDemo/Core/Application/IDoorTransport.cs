using System;

namespace DoorDemo.Application
{
    /// <summary>Events must be delivered on the caller's thread by Pump or Connect/Disconnect.</summary>
    public interface IDoorTransport : IDisposable
    {
        event Action<byte[]> ValueReceived;
        event Action<ConnectionStatus> StatusChanged;
        /// <summary>Starts a connection attempt and delivers status changes on the caller thread.</summary>
        void Connect();
        /// <summary>Cancels the current session and releases its transport resources.</summary>
        void Disconnect();
        /// <summary>Delivers queued native events on the caller thread without touching Unity from background callbacks.</summary>
        void Pump();
    }
}
