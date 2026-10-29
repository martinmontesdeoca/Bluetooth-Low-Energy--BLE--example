using DoorDemo.Infrastructure;
using NUnit.Framework;

namespace DoorDemo.Tests
{
    public sealed class BridgeMessageQueueTests
    {
        /// <summary>Verifies that a previous session cannot inject messages after reconnect.</summary>
        [Test]
        public void APreviousSessionCannotInjectMessagesAfterReconnect()
        {
            var queue = new BridgeMessageQueue();
            int oldSession = queue.Reset();
            int newSession = queue.Reset();
            queue.Enqueue(new BridgeMessage { type = "value", hex = "01" }, oldSession);
            Assert.That(queue.TryDequeue(out _), Is.False);
            queue.Enqueue(new BridgeMessage { type = "value", hex = "00" }, newSession);
            Assert.That(queue.TryDequeue(out BridgeMessage message), Is.True);
            Assert.That(message.hex, Is.EqualTo("00"));
        }

        /// <summary>Verifies that overflow stays an error until explicit reconnect.</summary>
        [Test]
        public void OverflowStaysAnErrorUntilExplicitReconnect()
        {
            var queue = new BridgeMessageQueue();
            int session = queue.Reset();
            for (int i = 0; i < 200; i++) queue.Enqueue(new BridgeMessage { type = "value", hex = "01" }, session);
            Assert.That(queue.TryDequeue(out BridgeMessage error), Is.True);
            Assert.That(error.state, Is.EqualTo("error"));
            Assert.That(queue.TryDequeue(out _), Is.False);
            session = queue.Reset();
            queue.Enqueue(new BridgeMessage { type = "value", hex = "00" }, session);
            Assert.That(queue.TryDequeue(out BridgeMessage recovered), Is.True);
            Assert.That(recovered.type, Is.EqualTo("value"));
        }
    }
}
