using FluentAssertions;
using NINA.Plugin.Interfaces;
using NINA.Plugin.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Test.Plugin {
    public class MessageBrokerTest {
        

        private MessageBroker _messageBroker;

        [SetUp]
        public void Setup() {
            _messageBroker = new MessageBroker();
        }

        [Test]
        public async Task PublishAsync_ShouldNotifySubscribers() {
            // Arrange
            var message = new ExampleMessage("test_topic", "Hello, World!", "TestSender", Guid.NewGuid());
            var subscriber = new TestSubscriber();
            _messageBroker.Subscribe("test_topic", subscriber);

            // Act
            await _messageBroker.Publish(message);

            // Assert
            subscriber.ReceivedMessages.Should().ContainSingle()
                .Which.Should().Be(message);
        }

        [Test]
        public async Task PublishAsync_ShouldNotNotifyUnsubscribedTopics() {
            // Arrange
            var message = new ExampleMessage("test_topic", "Hello, World!", "TestSender", Guid.NewGuid());
            var subscriber = new TestSubscriber();
            _messageBroker.Subscribe("other_topic", subscriber);

            // Act
            await _messageBroker.Publish(message);

            // Assert
            subscriber.ReceivedMessages.Should().BeEmpty();
        }

        [Test]
        public async Task PublishAsync_ShouldHandleMultipleSubscribers() {
            // Arrange
            var message = new ExampleMessage("test_topic", "Hello, World!", "TestSender", Guid.NewGuid());
            var subscriber1 = new TestSubscriber();
            var subscriber2 = new TestSubscriber();
            _messageBroker.Subscribe("test_topic", subscriber1);
            _messageBroker.Subscribe("test_topic", subscriber2);

            // Act
            await _messageBroker.Publish(message);

            // Assert
            subscriber1.ReceivedMessages.Should().ContainSingle()
                .Which.Should().Be(message);
            subscriber2.ReceivedMessages.Should().ContainSingle()
                .Which.Should().Be(message);
        }

        [Test]
        public void Subscribe_ShouldAddSubscriber() {
            // Arrange
            var subscriber = new TestSubscriber();

            // Act
            Action act = () => _messageBroker.Subscribe("test_topic", subscriber);

            // Assert
            act.Should().NotThrow();
        }

        [Test]
        public void Unsubscribe_ShouldRemoveSubscriber() {
            // Arrange
            var subscriber = new TestSubscriber();
            _messageBroker.Subscribe("test_topic", subscriber);

            // Act
            Action act = () => _messageBroker.Unsubscribe("test_topic", subscriber);

            // Assert
            act.Should().NotThrow();
        }

        [Test]
        public Task PublishAsync_ShouldThrowException_ForNullMessage() {
            // Act
            Func<Task> act = async () => await _messageBroker.Publish(null);

            // Assert
            return act.Should().ThrowAsync<ArgumentException>().WithMessage("Message or message topic cannot be null.");
        }

        [Test]
        public Task PublishAsync_ShouldThrowException_ForMessageWithEmptyTopic() {
            // Arrange
            var message = new ExampleMessage(string.Empty, "Hello, World!", "TestSender", Guid.NewGuid());

            // Act
            Func<Task> act = async () => await _messageBroker.Publish(message);

            // Assert
            return act.Should().ThrowAsync<ArgumentException>().WithMessage("Message or message topic cannot be null.");
        }

        [Test]
        public void Subscribe_ShouldThrowException_ForNullTopic() {
            // Arrange
            var subscriber = new TestSubscriber();

            // Act
            Action act = () => _messageBroker.Subscribe(null, subscriber);

            // Assert
            act.Should().Throw<ArgumentException>().WithMessage("Topic or subscriber cannot be null.");
        }

        [Test]
        public void Subscribe_ShouldThrowException_ForNullSubscriber() {
            // Act
            Action act = () => _messageBroker.Subscribe("test_topic", null);

            // Assert
            act.Should().Throw<ArgumentException>().WithMessage("Topic or subscriber cannot be null.");
        }

        [Test]
        public void Unsubscribe_ShouldThrowException_ForNullTopic() {
            // Arrange
            var subscriber = new TestSubscriber();

            // Act
            Action act = () => _messageBroker.Unsubscribe(null, subscriber);

            // Assert
            act.Should().Throw<ArgumentException>().WithMessage("Topic or subscriber cannot be null.");
        }

        [Test]
        public void Unsubscribe_ShouldThrowException_ForNullSubscriber() {
            // Act
            Action act = () => _messageBroker.Unsubscribe("test_topic", null);

            // Assert
            act.Should().Throw<ArgumentException>().WithMessage("Topic or subscriber cannot be null.");
        }

        private class TestSubscriber : ISubscriber {
            public List<IMessage> ReceivedMessages { get; } = new List<IMessage>();

            public Task OnMessageReceived(IMessage message) {
                ReceivedMessages.Add(message);
                return Task.CompletedTask;
            }
        }

        private class ExampleMessage : IMessage {
            public ExampleMessage(
                string topic,
                object content,
                string sender,
                Guid senderId,
                int priority = 0,
                DateTimeOffset? expiration = null,
                Guid? correlationId = null,
                IDictionary<string, object>? customHeaders = null) {
                Topic = topic;
                Content = content;
                Sender = sender;
                SenderId = senderId;
                SentAt = DateTimeOffset.UtcNow;
                MessageId = Guid.NewGuid();
                Priority = priority;
                Expiration = expiration;
                CorrelationId = correlationId;
                CustomHeaders = customHeaders ?? new Dictionary<string, object>();
            }

            public string Topic { get; }
            public object Content { get; }
            public string Sender { get; }
            public Guid SenderId { get; }
            public DateTimeOffset SentAt { get; }
            public Guid MessageId { get; }
            public int Priority { get; }
            public DateTimeOffset? Expiration { get; }
            public Guid? CorrelationId { get; }
            public IDictionary<string, object> CustomHeaders { get; }
            public int Version => 1;
        }
    }
}
