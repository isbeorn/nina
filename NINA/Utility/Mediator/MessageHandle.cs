using NINA.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator {
    /* Handler definition */
    abstract class MessageHandle {
        public abstract string MessageType { get; }
    }

    abstract class MessageHandle<TResult> : MessageHandle {
        protected Func<MediatorMessage<TResult>, TResult> Callback { get; set; }
        public TResult Send(MediatorMessage<TResult> msg) {
            return Callback(msg);
        }
    }

    class StatusUpdateMessageHandle : MessageHandle<bool> {
        public StatusUpdateMessageHandle(Func<StatusUpdateMessage, bool> callback) {
            Callback = (f) => callback((StatusUpdateMessage)f);
        }
        public override string MessageType { get { return typeof(StatusUpdateMessage).Name; } }
    }

    /* Message definition */
    abstract class MediatorMessage<TMessageResult> {
    }

    class StatusUpdateMessage : MediatorMessage<bool> {
        public ApplicationStatus Status { get; set; }
    }
}
