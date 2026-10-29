using System;

namespace DoorDemo.Application
{
    public readonly struct SessionActivity
    {
        /// <summary>Captures the time and description of an observed session event.</summary>
        public SessionActivity(DateTimeOffset timestamp, string description)
        {
            Timestamp = timestamp;
            Description = description;
        }

        public DateTimeOffset Timestamp { get; }
        public string Description { get; }
    }
}
