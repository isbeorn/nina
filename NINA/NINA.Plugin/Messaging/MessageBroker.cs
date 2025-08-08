using NINA.Plugin.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Plugin.Messaging {
    public class MessageBroker : IMessageBroker {
        private readonly Dictionary<string, List<ISubscriber>> subscribers = new Dictionary<string, List<ISubscriber>>();
        private readonly object lockObj = new object();

        public async Task Publish(IMessage message) {
            if (message == null || string.IsNullOrEmpty(message.Topic)) { 
                throw new ArgumentException("Message or message topic cannot be null.");
            }

            List<ISubscriber> handlers;
            lock (lockObj) {
                if (!subscribers.TryGetValue(message.Topic, out handlers)) {
                    return;
                }
                handlers = new List<ISubscriber>(handlers);
            }

            var tasks = new List<Task>();
            foreach (var handler in handlers) {
                tasks.Add(handler.OnMessageReceived(message));
            }
            await Task.WhenAll(tasks);
        }

        public void Subscribe(string topic, ISubscriber subscriber) {
            if (string.IsNullOrEmpty(topic) || subscriber == null) { 
                throw new ArgumentException("Topic or subscriber cannot be null.");
            }

            lock (lockObj) {
                if (!subscribers.ContainsKey(topic)) {
                    subscribers[topic] = new List<ISubscriber>();
                }
                subscribers[topic].Add(subscriber);
            }
        }

        public void Unsubscribe(string topic, ISubscriber subscriber) {
            if (string.IsNullOrEmpty(topic) || subscriber == null) { 
                throw new ArgumentException("Topic or subscriber cannot be null.");
            }

            lock (lockObj) {
                if (subscribers.TryGetValue(topic, out var handlers)) {
                    handlers.Remove(subscriber);

                    if (handlers.Count == 0) {
                        subscribers.Remove(topic);
                    }
                }
            }
        }
    }
}
