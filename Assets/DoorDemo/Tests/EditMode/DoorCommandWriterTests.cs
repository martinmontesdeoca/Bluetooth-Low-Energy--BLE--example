using System;
using DoorDemo.Application;
using DoorDemo.Domain;
using NUnit.Framework;

namespace DoorDemo.Tests
{
    public sealed class DoorCommandWriterTests
    {
        /// <summary>Verifies that outgoing command and acknowledgement do not change local door.</summary>
        [Test]
        public void OutgoingCommandAndAcknowledgementDoNotChangeLocalDoor()
        {
            var transport = new FakeDoorTransport();
            using var controller = new DoorController(transport, transport);
            transport.EmitStatus(ConnectionState.Connected);
            controller.RequestDoor(DoorState.Open);
            Assert.That(transport.RequestedDoor, Is.EqualTo(DoorState.Open));
            Assert.That(controller.Door, Is.EqualTo(DoorState.Closed));
            transport.Acknowledge(DoorState.Open);
            Assert.That(controller.Door, Is.EqualTo(DoorState.Closed));
            transport.Emit(new byte[] { 0 });
            Assert.That(controller.Door, Is.EqualTo(DoorState.Closed));
            transport.Emit(new byte[] { 1 });
            Assert.That(controller.Door, Is.EqualTo(DoorState.Open));
        }

        /// <summary>Verifies that closing remote does not close an already open local door.</summary>
        [Test]
        public void ClosingRemoteDoesNotCloseAnAlreadyOpenLocalDoor()
        {
            var transport = new FakeDoorTransport();
            using var controller = new DoorController(transport, transport);
            transport.EmitStatus(ConnectionState.Connected);
            transport.Emit(new byte[] { 1 });
            controller.RequestDoor(DoorState.Closed);
            transport.Acknowledge(DoorState.Closed);
            Assert.That(controller.Door, Is.EqualTo(DoorState.Open));
        }

        /// <summary>Verifies that incoming command does not echo an outgoing command.</summary>
        [Test]
        public void IncomingCommandDoesNotEchoAnOutgoingCommand()
        {
            var transport = new FakeDoorTransport();
            using var controller = new DoorController(transport, transport);
            transport.EmitStatus(ConnectionState.Connected);
            transport.Emit(new byte[] { 1 });
            Assert.That(transport.RequestedDoor, Is.Null);
        }

        /// <summary>Verifies that disconnected or read only transport cannot receive commands.</summary>
        [Test]
        public void DisconnectedOrReadOnlyTransportCannotReceiveCommands()
        {
            var transport = new FakeDoorTransport();
            using var writable = new DoorController(transport, transport);
            writable.RequestDoor(DoorState.Open);
            Assert.That(transport.RequestedDoor, Is.Null);
            using var readOnly = new DoorController(transport);
            transport.EmitStatus(ConnectionState.Connected);
            readOnly.RequestDoor(DoorState.Open);
            Assert.That(readOnly.CanControl, Is.False);
            Assert.That(transport.RequestedDoor, Is.Null);
        }

        /// <summary>Verifies that invalid state cannot reach the transport.</summary>
        [Test]
        public void InvalidStateCannotReachTheTransport()
        {
            var transport = new FakeDoorTransport();
            using var controller = new DoorController(transport, transport);
            transport.EmitStatus(ConnectionState.Connected);
            Assert.Throws<ArgumentOutOfRangeException>(() => controller.RequestDoor((DoorState)2));
            Assert.That(transport.RequestedDoor, Is.Null);
        }
    }
}
