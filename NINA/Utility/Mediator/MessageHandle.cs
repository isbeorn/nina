using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator {
    

    
    /* Handler definition */
    abstract class MessageHandle {
        public abstract string MessageType { get; }
    }

    abstract class MessageHandle<TResult> : MessageHandle {
        protected Func<MediatorMessage<TResult>, Task<TResult>> Callback { get; set; }
        public async Task<TResult> Send(MediatorMessage<TResult> msg) {
            return await Callback(msg);
        }
    }


    /* Specific handler */
    class PauseGuiderMessageHandle : MessageHandle<bool> {
        public PauseGuiderMessageHandle(Func<PauseGuiderMessage, Task<bool>> callback) {
            Callback = (f) => callback((PauseGuiderMessage)f);
        }
        public override string MessageType { get { return typeof(PauseGuiderMessage).Name; } }
    }

    class StartGuiderMessageHandle : MessageHandle<bool> {
        public StartGuiderMessageHandle(Func<StartGuiderMessage, Task<bool>> callback) {
            Callback = (f) => callback((StartGuiderMessage)f);
        }
        public override string MessageType { get { return typeof(StartGuiderMessage).Name; } }
    }

    class DitherGuiderMessageHandle : MessageHandle<bool> {
        public DitherGuiderMessageHandle(Func<DitherGuiderMessage, Task<bool>> callback) {
            Callback = (f) => callback((DitherGuiderMessage)f);
        }
        public override string MessageType { get { return typeof(DitherGuiderMessage).Name; } }
    }

    class AutoSelectGuideStarMessageHandle : MessageHandle<bool> {
        public AutoSelectGuideStarMessageHandle(Func<AutoSelectGuideStarMessage, Task<bool>> callback) {
            Callback = (f) => callback((AutoSelectGuideStarMessage)f);
        }
        public override string MessageType { get { return typeof(AutoSelectGuideStarMessage).Name; } }
    }

    /* Message definition */
    abstract class MediatorMessage<TMessageResult> {
        public CancellationToken Token { get; set; } = default(CancellationToken);
    }


    /* Specific message */
    class PauseGuiderMessage : MediatorMessage<bool> {
        public bool Pause { get; set; }
    }

    class StartGuiderMessage : MediatorMessage<bool> { }

    class DitherGuiderMessage : MediatorMessage<bool> { }

    class AutoSelectGuideStarMessage : MediatorMessage<bool> { }
}
