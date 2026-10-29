using System;
using System.IO;
using DoorDemo.Application;
using DoorDemo.Infrastructure;
using NUnit.Framework;

namespace DoorDemo.Tests
{
    public sealed class BridgeMessageTests
    {
        /// <summary>Verifies that parses native value message.</summary>
        [Test]
        public void ParsesNativeValueMessage()
        {
            var message = BridgeMessage.Parse("{\"type\":\"value\",\"hex\":\"01\"}");
            Assert.That(message.ToPayload(), Is.EqualTo(new byte[] { 1 }));
        }

        /// <summary>Verifies that parses native status message.</summary>
        [Test]
        public void ParsesNativeStatusMessage()
        {
            var message = BridgeMessage.Parse("{\"type\":\"status\",\"state\":\"connected\",\"message\":\"OK\"}");
            Assert.That(message.ToStatus().State, Is.EqualTo(ConnectionState.Connected));
            Assert.That(message.ToStatus().Message, Is.EqualTo("OK"));
        }

        /// <summary>Verifies that malformed hex is rejected.</summary>
        [TestCase(null)]
        [TestCase("0")]
        [TestCase("GG")]
        public void MalformedHexIsRejected(string hex)
        {
            Assert.Throws<FormatException>(() => new BridgeMessage { hex = hex }.ToPayload());
        }

        /// <summary>Verifies that unknown messages and states are rejected.</summary>
        [Test]
        public void UnknownMessagesAndStatesAreRejected()
        {
            Assert.Throws<FormatException>(() => BridgeMessage.Parse("{\"type\":\"unknown\"}"));
            Assert.Throws<FormatException>(() => new BridgeMessage { state = "unknown" }.ToStatus());
        }

        /// <summary>Verifies that missing bridge reports actionable error.</summary>
        [Test]
        public void MissingBridgeReportsActionableError()
        {
            using var transport = new WindowsBridgeTransport(Path.Combine(Path.GetTempPath(),
                Guid.NewGuid() + ".exe"), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            ConnectionStatus status = default;
            transport.StatusChanged += value => status = value;
            transport.Connect();
            Assert.That(status.State, Is.EqualTo(ConnectionState.Error));
            Assert.That(status.Message, Does.Contain("Publish-Bridge.ps1"));
        }
    }
}
