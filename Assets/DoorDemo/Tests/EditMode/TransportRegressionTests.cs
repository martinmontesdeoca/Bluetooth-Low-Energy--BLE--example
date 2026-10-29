using DoorDemo.Application;
using DoorDemo.Domain;
using DoorDemo.Infrastructure;
using NUnit.Framework;
using System;

namespace DoorDemo.Tests
{
    public sealed class TransportRegressionTests
    {
        /// <summary>Verifies that an acknowledgement after disconnect is ignored.</summary>
        [Test]
        public void AnAcknowledgementAfterDisconnectIsIgnored()
        {
            var transport = new FakeDoorTransport();
            using var controller = new DoorController(transport, transport);
            int acknowledgements = 0;
            controller.CommandAcknowledged += _ => acknowledgements++;
            transport.EmitStatus(ConnectionState.Connected);
            transport.EmitStatus(ConnectionState.Disconnected);
            transport.Acknowledge(DoorState.Open);
            Assert.That(acknowledgements, Is.Zero);
            Assert.That(controller.Door, Is.EqualTo(DoorState.Closed));
        }

        /// <summary>Verifies that malformed native messages use one explicit error contract.</summary>
        [TestCase(null)]
        [TestCase("{ invalid json")]
        [TestCase("{\"type\":\"unknown\"}")]
        public void MalformedNativeMessagesUseOneExplicitErrorContract(string source)
        {
            Assert.Throws<FormatException>(() => BridgeMessage.Parse(source));
        }
    }
}
