using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.PlateSolving;
using NINA.Utility.Astrometry;
using NINA.Utility.Enum;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.Utility.Mediator {
    /* Handler definition */

    internal abstract class AsyncMessageHandle {
        public abstract string MessageType { get; }
    }

    internal abstract class AsyncMessageHandle<TResult> : AsyncMessageHandle {
        protected Func<AsyncMediatorMessage<TResult>, Task<TResult>> Callback { get; set; }

        public async Task<TResult> Send(AsyncMediatorMessage<TResult> msg) {
            return await Callback(msg);
        }
    }

    /* Specific handler */

    internal class SetSequenceCoordinatesMessageHandle : AsyncMessageHandle<bool> {

        public SetSequenceCoordinatesMessageHandle(Func<SetSequenceCoordinatesMessage, Task<bool>> callback) {
            Callback = (f) => callback((SetSequenceCoordinatesMessage)f);
        }

        public override string MessageType { get { return typeof(SetSequenceCoordinatesMessage).Name; } }
    }

    internal class SetFramingAssistantCoordinatesMessageHandle : AsyncMessageHandle<bool> {

        public SetFramingAssistantCoordinatesMessageHandle(Func<SetFramingAssistantCoordinatesMessage, Task<bool>> callback) {
            Callback = (f) => callback((SetFramingAssistantCoordinatesMessage)f);
        }

        public override string MessageType { get { return typeof(SetFramingAssistantCoordinatesMessage).Name; } }
    }

    /* Message definition */

    internal abstract class AsyncMediatorMessage<TMessageResult> {
        public CancellationToken Token { get; set; } = default(CancellationToken);
        public IProgress<ApplicationStatus> Progress { get; set; }
    }

    /* Specific message */

    internal class SetSequenceCoordinatesMessage : AsyncMediatorMessage<bool> {
        public DeepSkyObject DSO { get; set; }
    }

    internal class SetFramingAssistantCoordinatesMessage : AsyncMediatorMessage<bool> {
        public DeepSkyObject DSO { get; set; }
    }
}