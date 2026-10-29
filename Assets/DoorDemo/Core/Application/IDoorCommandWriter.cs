using DoorDemo.Domain;
using System;

namespace DoorDemo.Application
{
    public interface IDoorCommandWriter
    {
        event Action<DoorState> CommandAcknowledged;
        /// <summary>Sends a command to the peer; its acknowledgement must not change the local door.</summary>
        void WriteDoorState(DoorState state);
    }
}
