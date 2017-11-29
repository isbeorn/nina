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

    /* Message definition */
    abstract class MediatorMessage<TMessageResult> {
    }
}
