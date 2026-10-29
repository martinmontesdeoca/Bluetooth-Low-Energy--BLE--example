using System;
using System.Linq;
using DoorDemo.Application;
using NUnit.Framework;

namespace DoorDemo.Tests
{
    public sealed class SessionMonitorTests
    {
        /// <summary>Verifies that counts real transitions and does not treat disconnect as sensor measurement.</summary>
        [Test]
        public void CountsRealTransitionsAndDoesNotTreatDisconnectAsSensorMeasurement()
        {
            var source = new FakeDoorTransport();
            using var controller = new DoorController(source);
            var now = new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);
            using var monitor = new SessionMonitor(controller, () => now);
            source.EmitStatus(ConnectionState.Connected);
            source.Emit(new byte[] { 1 });
            source.Emit(new byte[] { 1 });
            source.EmitStatus(ConnectionState.Disconnected);
            Assert.That(monitor.Openings, Is.EqualTo(1));
            Assert.That(monitor.RecordedEvents, Is.EqualTo(3));
            Assert.That(monitor.Activity.All(entry => entry.Timestamp == now), Is.True);
            Assert.That(monitor.Activity.Last().Description, Does.Contain("Disconnected"));
        }

        /// <summary>Verifies that history is bounded and dispose unsubscribes.</summary>
        [Test]
        public void HistoryIsBoundedAndDisposeUnsubscribes()
        {
            var source = new FakeDoorTransport();
            using var controller = new DoorController(source);
            var monitor = new SessionMonitor(controller, () => DateTimeOffset.UtcNow);
            for (int i = 0; i < 12; i++)
                source.EmitStatus(ConnectionState.Searching);
            Assert.That(monitor.Activity.Count(), Is.EqualTo(6));
            Assert.That(monitor.RecordedEvents, Is.EqualTo(12));
            monitor.Dispose();
            source.EmitStatus(ConnectionState.Connected);
            Assert.That(monitor.RecordedEvents, Is.EqualTo(12));
        }
    }
}
