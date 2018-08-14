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

    internal class PlateSolveMessageHandle : AsyncMessageHandle<PlateSolveResult> {

        public PlateSolveMessageHandle(Func<PlateSolveMessage, Task<PlateSolveResult>> callback) {
            Callback = (f) => callback((PlateSolveMessage)f);
        }

        public override string MessageType { get { return typeof(PlateSolveMessage).Name; } }
    }

    internal class CaptureImageMessageHandle : AsyncMessageHandle<ImageArray> {

        public CaptureImageMessageHandle(Func<CaptureImageMessage, Task<ImageArray>> callback) {
            Callback = (f) => callback((CaptureImageMessage)f);
        }

        public override string MessageType { get { return typeof(CaptureImageMessage).Name; } }
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

    internal class SetSequenceCoordinatesMessage : AsyncMediatorMessage<bool> {
        public DeepSkyObject DSO { get; set; }
    }

    internal class SetFramingAssistantCoordinatesMessage : AsyncMediatorMessage<bool> {
        public DeepSkyObject DSO { get; set; }
    }

    internal class PlateSolveMessage : AsyncMediatorMessage<PlateSolveResult> {
        public CaptureSequence Sequence { get; set; }
        public bool SyncReslewRepeat { get; set; }
        public BitmapSource Image { get; internal set; }
        public bool Silent { get; set; }
        public bool Blind { get; set; }
    }

    internal class CaptureImageMessage : AsyncMediatorMessage<ImageArray> {
        public CaptureSequence Sequence { get; set; }
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