using NINA.Model;
using NINA.Model.MyFilterWheel;
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

    class SetTelescopeTrackingMessageHandle : MessageHandle<bool> {
        public SetTelescopeTrackingMessageHandle(Func<SetTelescopeTrackingMessage, bool> callback) {
            Callback = (f) => callback((SetTelescopeTrackingMessage)f);
        }
        public override string MessageType { get { return typeof(SetTelescopeTrackingMessage).Name; } }
    }

    class SendSnapPortMessageHandle : MessageHandle<bool> {
        public SendSnapPortMessageHandle(Func<SendSnapPortMessage, bool> callback) {
            Callback = (f) => callback((SendSnapPortMessage)f);
        }
        public override string MessageType { get { return typeof(SendSnapPortMessage).Name; } }
    }

    class GetCurrentFilterInfoMessageHandle : MessageHandle<FilterInfo> {
        public GetCurrentFilterInfoMessageHandle(Func<GetCurrentFilterInfoMessage, FilterInfo> callback) {
            Callback = (f) => callback((GetCurrentFilterInfoMessage)f);
        }
        public override string MessageType { get { return typeof(GetCurrentFilterInfoMessage).Name; } }
    }

    class GetAllFiltersMessageHandle : MessageHandle<ICollection<FilterInfo>> {
        public GetAllFiltersMessageHandle(Func<GetAllFiltersMessage, ICollection<FilterInfo>> callback) {
            Callback = (f) => callback((GetAllFiltersMessage)f);
        }
        public override string MessageType { get { return typeof(GetAllFiltersMessage).Name; } }
    }

    class ChangeApplicationTabMessageHandle : MessageHandle<bool> {
        public ChangeApplicationTabMessageHandle(Func<ChangeApplicationTabMessage, bool> callback) {
            Callback = (f) => callback((ChangeApplicationTabMessage)f);
        }
        public override string MessageType { get { return typeof(ChangeApplicationTabMessage).Name; } }
    }


    /* Message definition */
    abstract class MediatorMessage<TMessageResult> {
    }

    class StatusUpdateMessage : MediatorMessage<bool> {
        public ApplicationStatus Status { get; set; }
    }

    class SetTelescopeTrackingMessage : MediatorMessage<bool> {
        public bool Tracking { get; set; }
    }

    class GetAllFiltersMessage : MediatorMessage<ICollection<FilterInfo>> { }

    class GetCurrentFilterInfoMessage : MediatorMessage<FilterInfo> { }

    class SendSnapPortMessage : MediatorMessage<bool> {
        public bool Start { get; set; }
    }

    class ChangeApplicationTabMessage : MediatorMessage<bool> {
        public ViewModel.ApplicationTab Tab { get; set; }
    }
}
