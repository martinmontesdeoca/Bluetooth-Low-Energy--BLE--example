using System.Threading;

namespace DoorDemo.Infrastructure
{
    /// <summary>Tracks EOF for one helper instance so late callbacks cannot complete a newer process.</summary>
    internal sealed class BridgeStreamState
    {
        private int outputClosed;
        private int errorClosed;
        public int Generation { get; }
        public bool IsComplete => Volatile.Read(ref outputClosed) != 0 && Volatile.Read(ref errorClosed) != 0;

        /// <summary>Binds stream completion to the queue session that owns the child process.</summary>
        public BridgeStreamState(int generation) => Generation = generation;

        /// <summary>Marks the standard output of this process as fully drained.</summary>
        public void CompleteOutput() => Volatile.Write(ref outputClosed, 1);

        /// <summary>Marks the standard error of this process as fully drained.</summary>
        public void CompleteError() => Volatile.Write(ref errorClosed, 1);
    }
}
