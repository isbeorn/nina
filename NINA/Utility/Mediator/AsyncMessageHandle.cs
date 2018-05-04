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

    internal class PauseGuiderMessageHandle : AsyncMessageHandle<bool> {

        public PauseGuiderMessageHandle(Func<PauseGuiderMessage, Task<bool>> callback) {
            Callback = (f) => callback((PauseGuiderMessage)f);
        }

        public override string MessageType { get { return typeof(PauseGuiderMessage).Name; } }
    }

    internal class StartGuiderMessageHandle : AsyncMessageHandle<bool> {

        public StartGuiderMessageHandle(Func<StartGuiderMessage, Task<bool>> callback) {
            Callback = (f) => callback((StartGuiderMessage)f);
        }

        public override string MessageType { get { return typeof(StartGuiderMessage).Name; } }
    }

    internal class StopGuiderMessageHandle : AsyncMessageHandle<bool> {

        public StopGuiderMessageHandle(Func<StopGuiderMessage, Task<bool>> callback) {
            Callback = (f) => callback((StopGuiderMessage)f);
        }

        public override string MessageType { get { return typeof(StopGuiderMessage).Name; } }
    }

    internal class DitherGuiderMessageHandle : AsyncMessageHandle<bool> {

        public DitherGuiderMessageHandle(Func<DitherGuiderMessage, Task<bool>> callback) {
            Callback = (f) => callback((DitherGuiderMessage)f);
        }

        public override string MessageType { get { return typeof(DitherGuiderMessage).Name; } }
    }

    internal class AutoSelectGuideStarMessageHandle : AsyncMessageHandle<bool> {

        public AutoSelectGuideStarMessageHandle(Func<AutoSelectGuideStarMessage, Task<bool>> callback) {
            Callback = (f) => callback((AutoSelectGuideStarMessage)f);
        }

        public override string MessageType { get { return typeof(AutoSelectGuideStarMessage).Name; } }
    }

    internal class CheckMeridianFlipMessageHandle : AsyncMessageHandle<bool> {

        public CheckMeridianFlipMessageHandle(Func<CheckMeridianFlipMessage, Task<bool>> callback) {
            Callback = (f) => callback((CheckMeridianFlipMessage)f);
        }

        public override string MessageType { get { return typeof(CheckMeridianFlipMessage).Name; } }
    }

    internal class SlewTocoordinatesMessageHandle : AsyncMessageHandle<bool> {

        public SlewTocoordinatesMessageHandle(Func<SlewToCoordinatesMessage, Task<bool>> callback) {
            Callback = (f) => callback((SlewToCoordinatesMessage)f);
        }

        public override string MessageType { get { return typeof(SlewToCoordinatesMessage).Name; } }
    }

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

    internal class MoveFocuserMessageHandle : AsyncMessageHandle<int> {

        public MoveFocuserMessageHandle(Func<MoveFocuserMessage, Task<int>> callback) {
            Callback = (f) => callback((MoveFocuserMessage)f);
        }

        public override string MessageType { get { return typeof(MoveFocuserMessage).Name; } }
    }

    internal class PlateSolveMessageHandle : AsyncMessageHandle<PlateSolveResult> {

        public PlateSolveMessageHandle(Func<PlateSolveMessage, Task<PlateSolveResult>> callback) {
            Callback = (f) => callback((PlateSolveMessage)f);
        }

        public override string MessageType { get { return typeof(PlateSolveMessage).Name; } }
    }

    internal class ChangeFilterWheelPositionMessageHandle : AsyncMessageHandle<FilterInfo> {

        public ChangeFilterWheelPositionMessageHandle(Func<ChangeFilterWheelPositionMessage, Task<FilterInfo>> callback) {
            Callback = (f) => callback((ChangeFilterWheelPositionMessage)f);
        }

        public override string MessageType { get { return typeof(ChangeFilterWheelPositionMessage).Name; } }
    }

    internal class StartAutoFocusMessageHandle : AsyncMessageHandle<bool> {

        public StartAutoFocusMessageHandle(Func<StartAutoFocusMessage, Task<bool>> callback) {
            Callback = (f) => callback((StartAutoFocusMessage)f);
        }

        public override string MessageType { get { return typeof(StartAutoFocusMessage).Name; } }
    }

    internal class ConnectCameraMessageHandle : AsyncMessageHandle<bool> {

        public ConnectCameraMessageHandle(Func<ConnectCameraMessage, Task<bool>> callback) {
            Callback = (f) => callback((ConnectCameraMessage)f);
        }

        public override string MessageType { get { return typeof(ConnectCameraMessage).Name; } }
    }

    internal class LiveViewImageMessageHandle : AsyncMessageHandle<bool> {

        public LiveViewImageMessageHandle(Func<LiveViewImageMessage, Task<bool>> callback) {
            Callback = (f) => callback((LiveViewImageMessage)f);
        }

        public override string MessageType { get { return typeof(LiveViewImageMessage).Name; } }
    }

    internal class ConnectFilterWheelMessageHandle : AsyncMessageHandle<bool> {

        public ConnectFilterWheelMessageHandle(Func<ConnectFilterWheelMessage, Task<bool>> callback) {
            Callback = (f) => callback((ConnectFilterWheelMessage)f);
        }

        public override string MessageType { get { return typeof(ConnectFilterWheelMessage).Name; } }
    }

    internal class ConnectFocuserMessageHandle : AsyncMessageHandle<bool> {

        public ConnectFocuserMessageHandle(Func<ConnectFocuserMessage, Task<bool>> callback) {
            Callback = (f) => callback((ConnectFocuserMessage)f);
        }

        public override string MessageType { get { return typeof(ConnectFocuserMessage).Name; } }
    }

    internal class ConnectTelescopeMessageHandle : AsyncMessageHandle<bool> {

        public ConnectTelescopeMessageHandle(Func<ConnectTelescopeMessage, Task<bool>> callback) {
            Callback = (f) => callback((ConnectTelescopeMessage)f);
        }

        public override string MessageType { get { return typeof(ConnectTelescopeMessage).Name; } }
    }

    internal class CaptureImageMessageHandle : AsyncMessageHandle<ImageArray> {

        public CaptureImageMessageHandle(Func<CaptureImageMessage, Task<ImageArray>> callback) {
            Callback = (f) => callback((CaptureImageMessage)f);
        }

        public override string MessageType { get { return typeof(CaptureImageMessage).Name; } }
    }

    internal class CalculateHFRMessageHandle : AsyncMessageHandle<double> {

        public CalculateHFRMessageHandle(Func<CalculateHFRMessage, Task<double>> callback) {
            Callback = (f) => callback((CalculateHFRMessage)f);
        }

        public override string MessageType { get { return typeof(CalculateHFRMessage).Name; } }
    }

    internal class CaptureAndPrepareImageMessageHandle : AsyncMessageHandle<BitmapSource> {

        public CaptureAndPrepareImageMessageHandle(Func<CaptureAndPrepareImageMessage, Task<BitmapSource>> callback) {
            Callback = (f) => callback((CaptureAndPrepareImageMessage)f);
        }

        public override string MessageType { get { return typeof(CaptureAndPrepareImageMessage).Name; } }
    }

    internal class CapturePrepareAndSaveImageMessageHandle : AsyncMessageHandle<bool> {

        public CapturePrepareAndSaveImageMessageHandle(Func<CapturePrepareAndSaveImageMessage, Task<bool>> callback) {
            Callback = (f) => callback((CapturePrepareAndSaveImageMessage)f);
        }

        public override string MessageType { get { return typeof(CapturePrepareAndSaveImageMessage).Name; } }
    }

    internal class AddThumbnailMessageHandle : AsyncMessageHandle<bool> {

        public AddThumbnailMessageHandle(Func<AddThumbnailMessage, Task<bool>> callback) {
            Callback = (f) => callback((AddThumbnailMessage)f);
        }

        public override string MessageType { get { return typeof(AddThumbnailMessage).Name; } }
    }

    internal class SetImageMessageHandle : AsyncMessageHandle<bool> {

        public SetImageMessageHandle(Func<SetImageMessage, Task<bool>> callback) {
            Callback = (f) => callback((SetImageMessage)f);
        }

        public override string MessageType { get { return typeof(SetImageMessage).Name; } }
    }

    /* Message definition */

    internal abstract class AsyncMediatorMessage<TMessageResult> {
        public CancellationToken Token { get; set; } = default(CancellationToken);
        public IProgress<ApplicationStatus> Progress { get; set; }
    }

    /* Specific message */

    internal class PauseGuiderMessage : AsyncMediatorMessage<bool> {
        public bool Pause { get; set; }
    }

    internal class StartGuiderMessage : AsyncMediatorMessage<bool> { }

    internal class StopGuiderMessage : AsyncMediatorMessage<bool> { }

    internal class DitherGuiderMessage : AsyncMediatorMessage<bool> { }

    internal class AutoSelectGuideStarMessage : AsyncMediatorMessage<bool> { }

    internal class CheckMeridianFlipMessage : AsyncMediatorMessage<bool> {
        public CaptureSequence Sequence { get; set; }
    }

    internal class SlewToCoordinatesMessage : AsyncMediatorMessage<bool> {
        public Coordinates Coordinates { get; set; }
    }

    internal class SetSequenceCoordinatesMessage : AsyncMediatorMessage<bool> {
        public DeepSkyObject DSO { get; set; }
    }

    internal class SetFramingAssistantCoordinatesMessage : AsyncMediatorMessage<bool> {
        public DeepSkyObject DSO { get; set; }
    }

    internal class MoveFocuserMessage : AsyncMediatorMessage<int> {
        public int Position { get; set; }
        public bool Absolute { get; set; } = true;
    }

    internal class PlateSolveMessage : AsyncMediatorMessage<PlateSolveResult> {
        public CaptureSequence Sequence { get; set; }
        public bool SyncReslewRepeat { get; set; }
        public BitmapSource Image { get; internal set; }
        public bool Silent { get; set; }
        public bool Blind { get; set; }
    }

    internal class ChangeFilterWheelPositionMessage : AsyncMediatorMessage<FilterInfo> {
        public FilterInfo Filter { get; set; }
    }

    internal class StartAutoFocusMessage : AsyncMediatorMessage<bool> {
        public FilterInfo Filter { get; set; }
    }

    internal class ConnectCameraMessage : AsyncMediatorMessage<bool> { }

    internal class ConnectFilterWheelMessage : AsyncMediatorMessage<bool> { }

    internal class ConnectFocuserMessage : AsyncMediatorMessage<bool> { }

    internal class ConnectTelescopeMessage : AsyncMediatorMessage<bool> { }

    internal class LiveViewImageMessage : AsyncMediatorMessage<bool> {
        public ImageArray Image { get; set; }
    }

    internal class CaptureImageMessage : AsyncMediatorMessage<ImageArray> {
        public CaptureSequence Sequence { get; set; }
    }

    //todo
    internal class CalculateHFRMessage : AsyncMediatorMessage<double> {
        public ImageArray ImageArray { get; set; }
    }

    internal class CaptureAndPrepareImageMessage : AsyncMediatorMessage<BitmapSource> {
        public CaptureSequence Sequence { get; set; }
    }

    internal class CapturePrepareAndSaveImageMessage : AsyncMediatorMessage<bool> {
        public CaptureSequence Sequence { get; set; }
        public bool Save { get; set; }
        public string TargetName { get; set; }
    }

    internal class AddThumbnailMessage : AsyncMediatorMessage<bool> {
        public BitmapSource Image { get; set; }
        public double Mean { get; set; }
        public Uri PathToImage { get; set; }
        public FileTypeEnum FileType { get; set; }
        public double HFR { get; internal set; }
        public bool IsBayered { get; internal set; }
        public double Duration { get; internal set; }
        public string Filter { get; internal set; }
        public int StatisticsId { get; internal set; }
    }

    internal class SetImageMessage : AsyncMediatorMessage<bool> {
        public ImageArray ImageArray { get; set; }
        public double Mean { get; set; }
        public Double ExposureTime { get; set; }
    }
}