using System.Collections.Generic;

namespace DoorDemo.Infrastructure
{
    /// <summary>Bounds native output and latches an overflow error until a new connection starts.</summary>
    public sealed class BridgeMessageQueue
    {
        private const int Capacity = 128;
        private readonly object gate = new object();
        private readonly Queue<BridgeMessage> pending = new Queue<BridgeMessage>();
        private bool overflowed;
        private int generation;

        /// <summary>Enqueues native output, replacing an overflowing backlog with one actionable error.</summary>
        public void Enqueue(BridgeMessage message, int session)
        {
            lock (gate)
            {
                if (overflowed || session != generation) return;
                if (pending.Count == Capacity)
                {
                    pending.Clear();
                    overflowed = true;
                    pending.Enqueue(new BridgeMessage
                    {
                        type = "status", state = "error",
                        message = "Cola del puente BLE saturada. Desconecta y vuelve a conectar."
                    });
                    return;
                }
                pending.Enqueue(message);
            }
        }

        /// <summary>Removes one message atomically for dispatch on Unity's main thread.</summary>
        public bool TryDequeue(out BridgeMessage message)
        {
            lock (gate)
            {
                if (pending.Count == 0) { message = null; return false; }
                message = pending.Dequeue();
                return true;
            }
        }

        /// <summary>Clears stale messages and permits a fresh connection after overflow or cancellation.</summary>
        public int Reset()
        {
            lock (gate)
            {
                pending.Clear();
                overflowed = false;
                return ++generation;
            }
        }
    }
}
