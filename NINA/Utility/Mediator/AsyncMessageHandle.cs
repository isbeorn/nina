using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.PlateSolving;
using NINA.Utility.Astrometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

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

    class ChangeFilterWheelPositionMessageHandle : AsyncMessageHandle<FilterInfo> {
        public ChangeFilterWheelPositionMessageHandle(Func<ChangeFilterWheelPositionMessage, Task<FilterInfo>> callback) {
            Callback = (f) => callback((ChangeFilterWheelPositionMessage)f);
        }
        public override string MessageType { get { return typeof(ChangeFilterWheelPositionMessage).Name; } }
    }

    class StartAutoFocusMessageHandle : AsyncMessageHandle<bool> {
        public StartAutoFocusMessageHandle(Func<StartAutoFocusMessage, Task<bool>> callback) {
            Callback = (f) => callback((StartAutoFocusMessage)f);
        }
        public override string MessageType { get { return typeof(StartAutoFocusMessage).Name; } }
    }

    class ConnectCameraMessageHandle : AsyncMessageHandle<bool> {
        public ConnectCameraMessageHandle(Func<ConnectCameraMessage, Task<bool>> callback) {
            Callback = (f) => callback((ConnectCameraMessage)f);
        }
        public override string MessageType { get { return typeof(ConnectCameraMessage).Name; } }
    }

    class ConnectFilterWheelMessageHandle : AsyncMessageHandle<bool> {
        public ConnectFilterWheelMessageHandle(Func<ConnectFilterWheelMessage, Task<bool>> callback) {
            Callback = (f) => callback((ConnectFilterWheelMessage)f);
        }
        public override string MessageType { get { return typeof(ConnectFilterWheelMessage).Name; } }
    }

    class ConnectFocuserMessageHandle : AsyncMessageHandle<bool> {
        public ConnectFocuserMessageHandle(Func<ConnectFocuserMessage, Task<bool>> callback) {
            Callback = (f) => callback((ConnectFocuserMessage)f);
        }
        public override string MessageType { get { return typeof(ConnectFocuserMessage).Name; } }
    }

    class ConnectTelescopeMessageHandle : AsyncMessageHandle<bool> {
        public ConnectTelescopeMessageHandle(Func<ConnectTelescopeMessage, Task<bool>> callback) {
            Callback = (f) => callback((ConnectTelescopeMessage)f);
        }
        public override string MessageType { get { return typeof(ConnectTelescopeMessage).Name; } }
    }




    class CaptureImageMessageHandle : AsyncMessageHandle<ImageArray> {
        public CaptureImageMessageHandle(Func<CaptureImageMessage, Task<ImageArray>> callback) {
            Callback = (f) => callback((CaptureImageMessage)f);
        }
        public override string MessageType { get { return typeof(CaptureImageMessage).Name; } }
    }

    class CalculateHFRMessageHandle : AsyncMessageHandle<double> {
        public CalculateHFRMessageHandle(Func<CalculateHFRMessage, Task<double>> callback) {
            Callback = (f) => callback((CalculateHFRMessage)f);
        }
        public override string MessageType { get { return typeof(CalculateHFRMessage).Name; } }
    }

    class CaptureAndPrepareImageMessageHandle : AsyncMessageHandle<BitmapSource> {
        public CaptureAndPrepareImageMessageHandle(Func<CaptureAndPrepareImageMessage, Task<BitmapSource>> callback) {
            Callback = (f) => callback((CaptureAndPrepareImageMessage)f);
        }
        public override string MessageType { get { return typeof(CaptureAndPrepareImageMessage).Name; } }
    }

    class CapturePrepareAndSaveImageMessageHandle : AsyncMessageHandle<bool> {
        public CapturePrepareAndSaveImageMessageHandle(Func<CapturePrepareAndSaveImageMessage, Task<bool>> callback) {
            Callback = (f) => callback((CapturePrepareAndSaveImageMessage)f);
        }
        public override string MessageType { get { return typeof(CapturePrepareAndSaveImageMessage).Name; } }
    }

    class AddThumbnailMessageHandle : AsyncMessageHandle<bool> {
        public AddThumbnailMessageHandle(Func<AddThumbnailMessage, Task<bool>> callback) {
            Callback = (f) => callback((AddThumbnailMessage)f);
        }
        public override string MessageType { get { return typeof(AddThumbnailMessage).Name; } }
    }

    class SetImageMessageHandle : AsyncMessageHandle<bool> {
        public SetImageMessageHandle(Func<SetImageMessage, Task<bool>> callback) {
            Callback = (f) => callback((SetImageMessage)f);
        }
        public override string MessageType { get { return typeof(SetImageMessage).Name; } }
    }


    /* Message definition */
    abstract class AsyncMediatorMessage<TMessageResult> {
        public CancellationToken Token { get; set; } = default(CancellationToken);
        public IProgress<ApplicationStatus> Progress { get; set; }
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
        public CaptureSequence Sequence { get; set; }
        public bool SyncReslewRepeat { get; set; }
        public BitmapSource Image { get; internal set; }
    }

    class ChangeFilterWheelPositionMessage : AsyncMediatorMessage<FilterInfo> {
        public FilterInfo Filter { get; set; }
    }

    class StartAutoFocusMessage : AsyncMediatorMessage<bool> { }

    class ConnectCameraMessage : AsyncMediatorMessage<bool> { }
    class ConnectFilterWheelMessage : AsyncMediatorMessage<bool> { }
    class ConnectFocuserMessage : AsyncMediatorMessage<bool> { }
    class ConnectTelescopeMessage : AsyncMediatorMessage<bool> { }




    class CaptureImageMessage : AsyncMediatorMessage<ImageArray> {        
        public CaptureSequence Sequence { get; set; }
    }

    //todo
    class CalculateHFRMessage : AsyncMediatorMessage<double> {
        public ImageArray ImageArray { get; set; }
    }

    class CaptureAndPrepareImageMessage : AsyncMediatorMessage<BitmapSource> {
        public CaptureSequence Sequence { get; set; }
    }
    
    class CapturePrepareAndSaveImageMessage : AsyncMediatorMessage<bool> {
        public CaptureSequence Sequence { get; set; }
        public bool Save { get; set; }
        public string TargetName { get; set; }
    }

    class AddThumbnailMessage : AsyncMediatorMessage<bool> {
        public BitmapSource Image { get; set; }
        public double Mean { get; set; }
        public Uri PathToImage { get; set; }
        public FileTypeEnum FileType { get; set; }
        public double HFR { get; internal set; }
        public bool IsBayered { get; internal set; }
    }

    class SetImageMessage : AsyncMediatorMessage<bool> {
        public BitmapSource Image { get; set; }
        public double Mean { get; set; }
        public bool IsBayered { get; internal set; }
    }

}
