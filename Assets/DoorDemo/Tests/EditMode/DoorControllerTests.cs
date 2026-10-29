using System;
using DoorDemo.Application;
using DoorDemo.Domain;
using NUnit.Framework;

namespace DoorDemo.Tests
{
    public sealed class DoorControllerTests
    {
        private FakeDoorTransport transport;
        private DoorController controller;

        /// <summary>Creates an isolated fake transport and controller for each application regression test.</summary>
        [SetUp]
        public void SetUp()
        {
            transport = new FakeDoorTransport();
            controller = new DoorController(transport);
        }

        /// <summary>Releases the controller and subscriptions after every test.</summary>
        [TearDown]
        public void TearDown() => controller.Dispose();

        /// <summary>Verifies that values before connection cannot open door.</summary>
        [Test]
        public void ValuesBeforeConnectionCannotOpenDoor()
        {
            transport.Emit(new byte[] { 1 });
            Assert.That(controller.Door, Is.EqualTo(DoorState.Closed));
        }

        /// <summary>Verifies that first closed measurement is published and invalid initial data is unknown.</summary>
        [Test]
        public void FirstClosedMeasurementIsPublishedAndInvalidInitialDataIsUnknown()
        {
            int events = 0;
            controller.DoorChanged += _ => events++;
            transport.EmitStatus(ConnectionState.Connected);
            transport.Emit(new byte[] { 2 });
            Assert.That(controller.HasReading, Is.False);
            transport.Emit(new byte[] { 0 });
            Assert.That(controller.HasReading, Is.True);
            Assert.That(events, Is.EqualTo(1));
            transport.EmitStatus(ConnectionState.Disconnected);
            Assert.That(controller.HasReading, Is.False);
        }

        /// <summary>Verifies that connected commands open and close door.</summary>
        [Test]
        public void ConnectedCommandsOpenAndCloseDoor()
        {
            transport.EmitStatus(ConnectionState.Connected);
            transport.Emit(new byte[] { 1 });
            Assert.That(controller.Door, Is.EqualTo(DoorState.Open));
            transport.Emit(new byte[] { 0 });
            Assert.That(controller.Door, Is.EqualTo(DoorState.Closed));
        }

        /// <summary>Verifies that duplicate commands do not repeat door events.</summary>
        [Test]
        public void DuplicateCommandsDoNotRepeatDoorEvents()
        {
            int events = 0;
            controller.DoorChanged += _ => events++;
            transport.EmitStatus(ConnectionState.Connected);
            transport.Emit(new byte[] { 1 });
            transport.Emit(new byte[] { 1 });
            Assert.That(events, Is.EqualTo(1));
        }

        /// <summary>Verifies that losing connection closes the door.</summary>
        [TestCase(ConnectionState.Disconnected)]
        [TestCase(ConnectionState.Error)]
        [TestCase(ConnectionState.Searching)]
        public void LosingConnectionClosesTheDoor(ConnectionState state)
        {
            transport.EmitStatus(ConnectionState.Connected);
            transport.Emit(new byte[] { 1 });
            transport.EmitStatus(state);
            Assert.That(controller.Door, Is.EqualTo(DoorState.Closed));
            transport.Emit(new byte[] { 1 });
            Assert.That(controller.Door, Is.EqualTo(DoorState.Closed));
        }

        /// <summary>Verifies that invalid payload is reported and preserves last valid door state.</summary>
        [Test]
        public void InvalidPayloadIsReportedAndPreservesLastValidDoorState()
        {
            string error = null;
            controller.InvalidPayload += message => error = message;
            transport.EmitStatus(ConnectionState.Connected);
            transport.Emit(new byte[] { 1 });
            transport.Emit(new byte[] { 49 });
            Assert.That(error, Does.Contain("00 o 01"));
            Assert.That(controller.Door, Is.EqualTo(DoorState.Open));
        }

        /// <summary>Verifies that connect is idempotent while searching.</summary>
        [Test]
        public void ConnectIsIdempotentWhileSearching()
        {
            controller.Connect();
            controller.Connect();
            Assert.That(transport.ConnectCount, Is.EqualTo(1));
        }

        /// <summary>Verifies that can reconnect after failure.</summary>
        [Test]
        public void CanReconnectAfterFailure()
        {
            controller.Connect();
            transport.EmitStatus(ConnectionState.Error);
            controller.Connect();
            Assert.That(transport.ConnectCount, Is.EqualTo(2));
        }

        /// <summary>Verifies that dispose unsubscribes and releases transport.</summary>
        [Test]
        public void DisposeUnsubscribesAndReleasesTransport()
        {
            controller.Dispose();
            controller.Dispose();
            transport.EmitStatus(ConnectionState.Connected);
            transport.Emit(new byte[] { 1 });
            Assert.That(transport.Disposed, Is.True);
            Assert.That(controller.Door, Is.EqualTo(DoorState.Closed));
            Assert.Throws<ObjectDisposedException>(() => controller.Connect());
            Assert.Throws<ObjectDisposedException>(() => controller.Pump());
        }

        /// <summary>Verifies that missing dependency fails immediately.</summary>
        [Test]
        public void MissingDependencyFailsImmediately()
        {
            Assert.Throws<ArgumentNullException>(() => new DoorController(null));
        }
    }
}
