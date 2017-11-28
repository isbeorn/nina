using NINA.Model;
using NINA.Model.MyFilterWheel;
using NINA.PlateSolving;
using NINA.Utility.Astrometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator {
    

    
    /* Handler definition */
    abstract class AsyncMessageHandle {
        public abstract string MessageType { get; }
    }

    abstract class AsyncMessageHandle<TResult> : AsyncMessageHandle {
        protected Func<AsyncMediatorMessage<TResult>, Task<TResult>> Callback { get; set; }
        public async Task<TResult> Send(AsyncMediatorMessage<TResult> msg) {
            return await Callback(msg);
        }
    }


    /* Specific handler */
    class PauseGuiderMessageHandle : AsyncMessageHandle<bool> {
        public PauseGuiderMessageHandle(Func<PauseGuiderMessage, Task<bool>> callback) {
            Callback = (f) => callback((PauseGuiderMessage)f);
        }
        public override string MessageType { get { return typeof(PauseGuiderMessage).Name; } }
    }

    class StartGuiderMessageHandle : AsyncMessageHandle<bool> {
        public StartGuiderMessageHandle(Func<StartGuiderMessage, Task<bool>> callback) {
            Callback = (f) => callback((StartGuiderMessage)f);
        }
        public override string MessageType { get { return typeof(StartGuiderMessage).Name; } }
    }

    class DitherGuiderMessageHandle : AsyncMessageHandle<bool> {
        public DitherGuiderMessageHandle(Func<DitherGuiderMessage, Task<bool>> callback) {
            Callback = (f) => callback((DitherGuiderMessage)f);
        }
        public override string MessageType { get { return typeof(DitherGuiderMessage).Name; } }
    }

    class AutoSelectGuideStarMessageHandle : AsyncMessageHandle<bool> {
        public AutoSelectGuideStarMessageHandle(Func<AutoSelectGuideStarMessage, Task<bool>> callback) {
            Callback = (f) => callback((AutoSelectGuideStarMessage)f);
        }
        public override string MessageType { get { return typeof(AutoSelectGuideStarMessage).Name; } }
    }

    class CheckMeridianFlipMessageHandle : AsyncMessageHandle<bool> {
        public CheckMeridianFlipMessageHandle(Func<CheckMeridianFlipMessage, Task<bool>> callback) {
            Callback = (f) => callback((CheckMeridianFlipMessage)f);
        }
        public override string MessageType { get { return typeof(CheckMeridianFlipMessage).Name; } }
    }

    class SlewTocoordinatesMessageHandle : AsyncMessageHandle<bool> {
        public SlewTocoordinatesMessageHandle(Func<SlewToCoordinatesMessage, Task<bool>> callback) {
            Callback = (f) => callback((SlewToCoordinatesMessage)f);
        }
        public override string MessageType { get { return typeof(SlewToCoordinatesMessage).Name; } }
    }

    class SetSequenceCoordinatesMessageHandle : AsyncMessageHandle<bool> {
        public SetSequenceCoordinatesMessageHandle(Func<SetSequenceCoordinatesMessage, Task<bool>> callback) {
            Callback = (f) => callback((SetSequenceCoordinatesMessage)f);
        }
        public override string MessageType { get { return typeof(SetSequenceCoordinatesMessage).Name; } }
    }

    class MoveFocuserMessageHandle : AsyncMessageHandle<int> {
        public MoveFocuserMessageHandle(Func<MoveFocuserMessage, Task<int>> callback) {
            Callback = (f) => callback((MoveFocuserMessage)f);
        }
        public override string MessageType { get { return typeof(MoveFocuserMessage).Name; } }
    }

    class PlateSolveMessageHandle : AsyncMessageHandle<PlateSolveResult> {
        public PlateSolveMessageHandle(Func<PlateSolveMessage, Task<PlateSolveResult>> callback) {
            Callback = (f) => callback((PlateSolveMessage)f);
        }
        public override string MessageType { get { return typeof(PlateSolveMessage).Name; } }
    }

    class ChangeFilterWheelPositionMessageHandle : AsyncMessageHandle<bool> {
        public ChangeFilterWheelPositionMessageHandle(Func<ChangeFilterWheelPositionMessage, Task<bool>> callback) {
            Callback = (f) => callback((ChangeFilterWheelPositionMessage)f);
        }
        public override string MessageType { get { return typeof(ChangeFilterWheelPositionMessage).Name; } }
    }

    


    /* Message definition */
    abstract class AsyncMediatorMessage<TMessageResult> {
        public CancellationToken Token { get; set; } = default(CancellationToken);
    }


    /* Specific message */
    class PauseGuiderMessage : AsyncMediatorMessage<bool> {
        public bool Pause { get; set; }
    }

    class StartGuiderMessage : AsyncMediatorMessage<bool> { }

    class DitherGuiderMessage : AsyncMediatorMessage<bool> { }

    class AutoSelectGuideStarMessage : AsyncMediatorMessage<bool> { }

    class CheckMeridianFlipMessage : AsyncMediatorMessage<bool> {
        public CaptureSequence Sequence { get; set; }
    }

    class SlewToCoordinatesMessage : AsyncMediatorMessage<bool> {
        public Coordinates Coordinates { get; set; }
    }

    class SetSequenceCoordinatesMessage : AsyncMediatorMessage<bool> {
        public DeepSkyObject DSO { get; set; }
    }

    class MoveFocuserMessage : AsyncMediatorMessage<int> {
        public int Position { get; set; }
        public bool Absolute { get; set; } = true;
    }

    class PlateSolveMessage : AsyncMediatorMessage<PlateSolveResult> {
        public IProgress<string> Progress { get; set; }
        public CaptureSequence Sequence { get; set; }
        public bool SyncReslewRepeat { get; set; }
    }

    class ChangeFilterWheelPositionMessage : AsyncMediatorMessage<bool> {
        public FilterInfo Filter { get; set; }
        public IProgress<string> Progress { get; set; }
    }


}
