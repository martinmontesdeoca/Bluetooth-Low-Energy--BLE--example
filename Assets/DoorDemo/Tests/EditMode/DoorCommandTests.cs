using DoorDemo.Domain;
using NUnit.Framework;

namespace DoorDemo.Tests
{
    public sealed class DoorCommandTests
    {
        /// <summary>Verifies that binary byte maps to door state.</summary>
        [TestCase(0, DoorState.Closed)]
        [TestCase(1, DoorState.Open)]
        public void BinaryByteMapsToDoorState(byte value, DoorState expected)
        {
            Assert.That(DoorCommand.TryParse(new[] { value }, out DoorState state), Is.True);
            Assert.That(state, Is.EqualTo(expected));
        }

        /// <summary>Verifies that invalid or ASCII commands are rejected.</summary>
        [TestCase(null)]
        [TestCase(new byte[0])]
        [TestCase(new byte[] { 2 })]
        [TestCase(new byte[] { 255 })]
        [TestCase(new byte[] { 48 })]
        [TestCase(new byte[] { 49 })]
        [TestCase(new byte[] { 0, 1 })]
        public void InvalidOrAsciiCommandsAreRejected(byte[] payload)
        {
            Assert.That(DoorCommand.TryParse(payload, out _), Is.False);
        }
    }
}
