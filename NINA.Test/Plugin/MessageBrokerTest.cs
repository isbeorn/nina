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

        /// <summary>
        /// Verifies that publishing a plugin message delivers the exact message object to a subscriber
        /// registered for the same topic.
        /// </summary>
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

        /// <summary>
        /// Verifies that topic routing isolates plugin messages from subscribers registered for a
        /// different topic.
        /// </summary>
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

        /// <summary>
        /// Verifies that all subscribers on a plugin message topic receive the published message.
        /// </summary>
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

        /// <summary>
        /// Verifies that a valid subscriber can be registered for a topic without side effects.
        /// </summary>
        [Test]
        public void Subscribe_ShouldAddSubscriber() {
            // Arrange
            var subscriber = new TestSubscriber();

            // Act
            Action act = () => _messageBroker.Subscribe("test_topic", subscriber);

            // Assert
            act.Should().NotThrow();
        }

        /// <summary>
        /// Verifies that a subscriber can be removed from a topic without throwing.
        /// </summary>
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

        /// <summary>
        /// Verifies that an unsubscribed plugin listener no longer receives messages for that topic.
        /// </summary>
        [Test]
        public async Task Unsubscribe_ShouldStopFutureDeliveryToSubscriber() {
            // Arrange
            var message = new ExampleMessage("test_topic", "Hello, World!", "TestSender", Guid.NewGuid());
            var subscriber = new TestSubscriber();
            _messageBroker.Subscribe("test_topic", subscriber);
            _messageBroker.Unsubscribe("test_topic", subscriber);

            // Act
            await _messageBroker.Publish(message);

            // Assert
            subscriber.ReceivedMessages.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that publishing a null message is rejected before routing is attempted.
        /// </summary>
        [Test]
        public Task PublishAsync_ShouldThrowException_ForNullMessage() {
            // Act
            Func<Task> act = async () => await _messageBroker.Publish(null);

            // Assert
            return act.Should().ThrowAsync<ArgumentException>().WithMessage("Message or message topic cannot be null.");
        }

        /// <summary>
        /// Verifies that a message with an empty topic is rejected because topic is the broker's
        /// routing key.
        /// </summary>
        [Test]
        public Task PublishAsync_ShouldThrowException_ForMessageWithEmptyTopic() {
            // Arrange
            var message = new ExampleMessage(string.Empty, "Hello, World!", "TestSender", Guid.NewGuid());

            // Act
            Func<Task> act = async () => await _messageBroker.Publish(message);

            // Assert
            return act.Should().ThrowAsync<ArgumentException>().WithMessage("Message or message topic cannot be null.");
        }

        /// <summary>
        /// Verifies that topic validation prevents subscriptions with null routing keys.
        /// </summary>
        [Test]
        public void Subscribe_ShouldThrowException_ForNullTopic() {
            // Arrange
            var subscriber = new TestSubscriber();

            // Act
            Action act = () => _messageBroker.Subscribe(null, subscriber);

            // Assert
            act.Should().Throw<ArgumentException>().WithMessage("Topic or subscriber cannot be null.");
        }

        /// <summary>
        /// Verifies that subscriber validation prevents null handlers from being stored for a topic.
        /// </summary>
        [Test]
        public void Subscribe_ShouldThrowException_ForNullSubscriber() {
            // Act
            Action act = () => _messageBroker.Subscribe("test_topic", null);

            // Assert
            act.Should().Throw<ArgumentException>().WithMessage("Topic or subscriber cannot be null.");
        }

        /// <summary>
        /// Verifies that topic validation is also enforced when removing subscribers.
        /// </summary>
        [Test]
        public void Unsubscribe_ShouldThrowException_ForNullTopic() {
            // Arrange
            var subscriber = new TestSubscriber();

            // Act
            Action act = () => _messageBroker.Unsubscribe(null, subscriber);

            // Assert
            act.Should().Throw<ArgumentException>().WithMessage("Topic or subscriber cannot be null.");
        }

        /// <summary>
        /// Verifies that unsubscribe rejects null subscribers instead of silently mutating broker
        /// state.
        /// </summary>
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
