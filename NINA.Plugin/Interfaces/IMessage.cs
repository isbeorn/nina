using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Plugin.Interfaces {
    public interface IMessage {
        /// <summary>
        /// The unique identifier of the sender - this should be filled with the plugin Id
        /// </summary>
        Guid SenderId { get; }

        /// <summary>
        /// The name of the sender
        /// </summary>
        string Sender { get; }

        /// <summary>
        /// The UTC timestamp when the message was sent
        /// </summary>
        DateTimeOffset SentAt { get; }
        /// <summary>
        /// A unique identifier for each message
        /// </summary>
        Guid MessageId { get; }

        /// <summary>
        /// The expiration time of the message, after which it can be considered outdated
        /// </summary>
        DateTimeOffset? Expiration { get; }

        /// <summary>
        /// A unique identifier to correlate related messages
        /// </summary>
        Guid? CorrelationId { get; }

        /// <summary>
        /// Custom headers or metadata for additional information
        /// </summary>
        IDictionary<string, object> CustomHeaders { get; }

        /// <summary>
        /// The key topic that describes the intent of the message. This is used by subscribers to identify the message
        /// </summary>
        string Topic { get; }

        /// <summary>
        /// The generic body of the message
        /// </summary>
        object Content { get; }
    }
}
