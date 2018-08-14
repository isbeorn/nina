using NINA.Model;
using NINA.Model.MyFilterWheel;
using System;
using System.Collections.Generic;

namespace NINA.Utility.Mediator {
    /* Handler definition */

    internal abstract class MessageHandle {
        public abstract string MessageType { get; }
        public Type RegisteredClass { get; private set; }

        public MessageHandle(Type registeredClass) {
            RegisteredClass = registeredClass;
        }
    }

    internal abstract class MessageHandle<TResult> : MessageHandle {

        public MessageHandle(Type registeredClass = null) : base(registeredClass) {
        }

        protected Func<MediatorMessage<TResult>, TResult> Callback { get; set; }

        public TResult Send(MediatorMessage<TResult> msg) {
            return Callback(msg);
        }
    }

    internal class StatusUpdateMessageHandle : MessageHandle<bool> {

        public StatusUpdateMessageHandle(Func<StatusUpdateMessage, bool> callback) {
            Callback = (f) => callback((StatusUpdateMessage)f);
        }

        public override string MessageType { get { return typeof(StatusUpdateMessage).Name; } }
    }

    internal class ChangeApplicationTabMessageHandle : MessageHandle<bool> {

        public ChangeApplicationTabMessageHandle(Func<ChangeApplicationTabMessage, bool> callback) {
            Callback = (f) => callback((ChangeApplicationTabMessage)f);
        }

        public override string MessageType { get { return typeof(ChangeApplicationTabMessage).Name; } }
    }

    /* Message definition */

    internal abstract class MediatorMessage<TMessageResult> {
    }

    internal class StatusUpdateMessage : MediatorMessage<bool> {
        public ApplicationStatus Status { get; set; }
    }

    internal class GetCameraNameById : MediatorMessage<string> {
        private string Id { get; set; }
    }

    internal class ChangeApplicationTabMessage : MediatorMessage<bool> {
        public ViewModel.ApplicationTab Tab { get; set; }
    }
}