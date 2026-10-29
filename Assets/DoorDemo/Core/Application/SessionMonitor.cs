using System;
using System.Collections.Generic;
using DoorDemo.Domain;

namespace DoorDemo.Application
{
    /// <summary>Bounded, in-memory operational history for one demo session, not a persistent audit log.</summary>
    public sealed class SessionMonitor : IDisposable
    {
        private const int HistoryLimit = 6;
        private readonly DoorController controller;
        private readonly Func<DateTimeOffset> utcNow;
        private readonly Queue<SessionActivity> activity = new Queue<SessionActivity>();

        /// <summary>Observes one controller with an injected clock and a bounded in-memory event history.</summary>
        public SessionMonitor(DoorController controller, Func<DateTimeOffset> utcNow)
        {
            this.controller = controller ?? throw new ArgumentNullException(nameof(controller));
            this.utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
            controller.DoorChanged += OnDoor;
            controller.ConnectionChanged += OnConnection;
            controller.InvalidPayload += OnInvalid;
        }

        public int Openings { get; private set; }
        public int RecordedEvents { get; private set; }
        public IEnumerable<SessionActivity> Activity => activity;

        /// <summary>Records received door states without counting a disconnected visual reset as a measurement.</summary>
        private void OnDoor(DoorState state)
        {
            // A visual reset after connection loss is not a new physical measurement.
            if (controller.Connection.State != ConnectionState.Connected)
                return;
            if (state == DoorState.Open)
                Openings++;
            Record(state == DoorState.Open ? "Sensor: acceso abierto" : "Sensor: acceso cerrado");
        }

        /// <summary>Records an observed connectivity transition with the injected clock.</summary>
        private void OnConnection(ConnectionStatus status) => Record("Conexion: " + status.State);
        /// <summary>Records rejected sensor data without treating it as a valid state change.</summary>
        private void OnInvalid(string message) => Record("Dato rechazado: formato de sensor invalido");

        /// <summary>Adds a timestamped event while retaining only the configured recent history.</summary>
        private void Record(string description)
        {
            if (activity.Count == HistoryLimit)
                activity.Dequeue();
            activity.Enqueue(new SessionActivity(utcNow(), description));
            RecordedEvents++;
        }

        /// <summary>Removes subscriptions so a replaced monitor cannot receive later session events.</summary>
        public void Dispose()
        {
            controller.DoorChanged -= OnDoor;
            controller.ConnectionChanged -= OnConnection;
            controller.InvalidPayload -= OnInvalid;
        }
    }
}
