using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Utility;
using NINA.Utility.Exceptions;
using NINA.Utility.Mediator;
using NINA.Utility.Notification;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static NINA.Model.CaptureSequence;

namespace NINA.ViewModel {

    internal class ImagingVM : DockableVM {

        public ImagingVM() : base() {
            Title = "LblImaging";
            ContentId = nameof(ImagingVM);
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["ImagingSVG"];

            SnapExposureDuration = 1;
            SnapCommand = new AsyncCommand<bool>(() => SnapImage(new Progress<ApplicationStatus>(p => Status = p)));
            CancelSnapCommand = new RelayCommand(CancelSnapImage);

            ImageControl = new ImageControlVM();

            RegisterMediatorMessages();
        }

        private void RegisterMediatorMessages() {
            Mediator.Instance.RegisterAsyncRequest(
                new CaptureImageMessageHandle(async (CaptureImageMessage msg) => {
                    return await CaptureImage(msg.Sequence, msg.Token, msg.Progress);
                })
            );

            Mediator.Instance.RegisterAsyncRequest(
                new CapturePrepareAndSaveImageMessageHandle(async (CapturePrepareAndSaveImageMessage msg) => {
                    return await CaptureAndSaveImage(msg.Sequence, msg.Save, msg.Token, msg.Progress, msg.TargetName);
                })
            );

            Mediator.Instance.RegisterAsyncRequest(
                new CaptureAndPrepareImageMessageHandle(async (CaptureAndPrepareImageMessage msg) => {
                    return await CaptureAndPrepareImage(msg.Sequence, msg.Token, msg.Progress);
                })
            );

            Mediator.Instance.Register((object o) => _cameraConnected = (bool)o, MediatorMessages.CameraConnectedChanged);
            Mediator.Instance.Register((object o) => {
                Cam = (ICamera)o;
            }, MediatorMessages.CameraChanged);
        }

        private ImageControlVM _imageControl;

        public ImageControlVM ImageControl {
            get { return _imageControl; }
            set { _imageControl = value; RaisePropertyChanged(); }
        }

        private bool _cameraConnected;

        private bool CameraConnected {
            get {
                return Cam != null && _cameraConnected;
            }
        }

        public bool CanSeeSubSampling => Cam != null && Cam.CanSubSample;

        private bool _snapSubSample;

        public bool SnapSubSample {
            get {
                return _snapSubSample;
            }
            set {
                _snapSubSample = value;
                RaisePropertyChanged();
            }
        }

        private ApplicationStatus _status;

        public ApplicationStatus Status {
            get {
                return _status;
            }
            set {
                _status = value;
                _status.Source = Title;
                RaisePropertyChanged();

                Mediator.Instance.Request(new StatusUpdateMessage() { Status = _status });
            }
        }

        private Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

        private bool _loop;

        public bool Loop {
            get {
                return _loop;
            }
            set {
                _loop = value;
                RaisePropertyChanged();
            }
        }

        private bool _snapSave;

        public bool SnapSave {
            get {
                return _snapSave;
            }
            set {
                _snapSave = value;
                RaisePropertyChanged();
            }
        }

        private ICamera _cam;

        public ICamera Cam {
            get {
                return _cam;
            }
            set {
                _cam = value;
                RaisePropertyChanged();
            }
        }

        private double _snapExposureDuration;

        public double SnapExposureDuration {
            get {
                return _snapExposureDuration;
            }

            set {
                _snapExposureDuration = value;
                RaisePropertyChanged();
            }
        }

        private int _exposureSeconds;

        public int ExposureSeconds {
            get {
                return _exposureSeconds;
            }
            set {
                _exposureSeconds = value;
                RaisePropertyChanged();
            }
        }

        private String _expStatus;

        public String ExpStatus {
            get {
                return _expStatus;
            }

            set {
                _expStatus = value;
                RaisePropertyChanged();
            }
        }

        public IAsyncCommand SnapCommand { get; private set; }

        public ICommand CancelSnapCommand { get; private set; }

        private void CancelSnapImage(object o) {
            _captureImageToken?.Cancel();
        }

        private CancellationTokenSource _captureImageToken;

        private async Task ChangeFilter(CaptureSequence seq, CancellationToken token, IProgress<ApplicationStatus> progress) {
            if (seq.FilterType != null) {
                await Mediator.Instance.RequestAsync(new ChangeFilterWheelPositionMessage() { Filter = seq.FilterType, Token = token, Progress = progress });
            }
        }

        private void SetBinning(CaptureSequence seq) {
            if (seq.Binning == null) {
                Cam.SetBinning(1, 1);
            } else {
                Cam.SetBinning(seq.Binning.X, seq.Binning.Y);
            }
        }

        private void SetSubSample(CaptureSequence seq) {
            Cam.EnableSubSample = seq.EnableSubSample;
        }

        private async Task Capture(CaptureSequence seq, CancellationToken token, IProgress<ApplicationStatus> progress) {
            double duration = seq.ExposureTime;
            bool isLight = false;
            if (Cam.HasShutter) {
                isLight = true;
            }
            Cam.StartExposure(duration, isLight);
            var start = DateTime.Now;
            var elapsed = 0.0d;
            ExposureSeconds = 0;
            progress.Report(new ApplicationStatus() {
                Status = Locale.Loc.Instance["LblExposing"],
                Progress = ExposureSeconds,
                MaxProgress = (int)duration,
                ProgressType = ApplicationStatus.StatusProgressType.ValueOfMaxValue
            });
            /* Wait for Capture */
            if (duration >= 1) {
                await Task.Run(async () => {
                    do {
                        var delta = await Utility.Utility.Delay(500, token);
                        elapsed += delta.TotalSeconds;
                        ExposureSeconds = (int)elapsed;
                        token.ThrowIfCancellationRequested();

                        progress.Report(new ApplicationStatus() {
                            Status = Locale.Loc.Instance["LblExposing"],
                            Progress = ExposureSeconds,
                            MaxProgress = (int)duration,
                            ProgressType = ApplicationStatus.StatusProgressType.ValueOfMaxValue
                        });
                    } while ((elapsed < duration) && Cam?.Connected == true);
                });
            }
            token.ThrowIfCancellationRequested();
        }

        private async Task<ImageArray> Download(CancellationToken token, IProgress<ApplicationStatus> progress) {
            progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblDownloading"] });
            return await Cam.DownloadExposure(token);
        }

        private async Task<bool> Dither(CaptureSequence seq, CancellationToken token, IProgress<ApplicationStatus> progress) {
            if (seq.Dither && ((seq.ProgressExposureCount % seq.DitherAmount) == 0)) {
                return await Mediator.Instance.RequestAsync(new DitherGuiderMessage() { Token = token });
            }
            token.ThrowIfCancellationRequested();
            return false;
        }

        //Instantiate a Singleton of the Semaphore with a value of 1. This means that only 1 thread can be granted access at a time.
        private static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        public async Task<BitmapSource> CaptureAndPrepareImage(CaptureSequence sequence, CancellationToken token, IProgress<ApplicationStatus> progress) {
            var iarr = await CaptureImage(sequence, token, progress);
            if (iarr != null) {
                return await _currentPrepareImageTask;
            } else {
                return null;
            }
        }

        public async Task<ImageArray> CaptureImage(CaptureSequence sequence, CancellationToken token, IProgress<ApplicationStatus> progress, bool bSave = false, string targetname = "") {
            //Asynchronously wait to enter the Semaphore. If no-one has been granted access to the Semaphore, code execution will proceed, otherwise this thread waits here until the semaphore is released
            progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblWaitingForCamera"] });
            await semaphoreSlim.WaitAsync(token);

            if (CameraConnected != true) {
                Notification.ShowWarning(Locale.Loc.Instance["LblNoCameraConnected"]);
                semaphoreSlim.Release();
                return null;
            }

            return await Task.Run<ImageArray>(async () => {
                ImageArray arr = null;

                try {
                    if (CameraConnected != true) {
                        throw new CameraConnectionLostException();
                    }

                    /*Change Filter*/
                    await ChangeFilter(sequence, token, progress);

                    if (CameraConnected != true) {
                        throw new CameraConnectionLostException();
                    }

                    token.ThrowIfCancellationRequested();

                    /*Set Camera Gain */
                    SetGain(sequence);

                    /*Set Camera Binning*/
                    SetBinning(sequence);

                    SetSubSample(sequence);

                    if (CameraConnected != true) {
                        throw new CameraConnectionLostException();
                    }

                    /*Capture*/
                    await Capture(sequence, token, progress);

                    token.ThrowIfCancellationRequested();

                    if (CameraConnected != true) {
                        throw new CameraConnectionLostException();
                    }

                    /*Dither*/
                    var ditherTask = Dither(sequence, token, progress);

                    /*Download Image */
                    arr = await Download(token, progress);
                    if (arr == null) {
                        throw new OperationCanceledException();
                    }

                    if (CameraConnected != true) {
                        throw new CameraConnectionLostException();
                    }

                    //Wait for previous prepare image task to complete
                    if (_currentPrepareImageTask != null && !_currentPrepareImageTask.IsCompleted) {
                        progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblWaitForImageProcessing"] });
                        await _currentPrepareImageTask;
                    }

                    var parameters = new ImageParameters() {
                        Binning = sequence.Binning.Name,
                        ExposureNumber = sequence.ProgressExposureCount,
                        ExposureTime = sequence.ExposureTime,
                        FilterName = sequence.FilterType?.Name ?? string.Empty,
                        ImageType = sequence.ImageType,
                        TargetName = targetname
                    };
                    _currentPrepareImageTask = ImageControl.PrepareImage(arr, token, bSave, parameters);

                    //Wait for dither to finish. Runs in parallel to download and save.
                    progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblWaitForDither"] });
                    await ditherTask;
                } catch (System.OperationCanceledException ex) {
                    if (Cam == null || _cameraConnected == true) {
                        Cam?.AbortExposure();
                    }
                    throw ex;
                } catch (CameraConnectionLostException ex) {
                    Logger.Error(ex);
                    Notification.ShowError(Locale.Loc.Instance["LblCameraConnectionLost"]);
                    throw ex;
                } catch (Exception ex) {
                    Notification.ShowError(Locale.Loc.Instance["LblUnexpectedError"]);
                    Logger.Error(ex);
                    if (_cameraConnected == true) {
                        Cam.AbortExposure();
                    }
                    throw ex;
                } finally {
                    progress.Report(new ApplicationStatus() { Status = string.Empty });
                    semaphoreSlim.Release();
                }
                return arr;
            });
        }

        private Task<BitmapSource> _currentPrepareImageTask;

        private void SetGain(CaptureSequence seq) {
            if (seq.Gain != -1) {
                Cam.Gain = seq.Gain;
            } else {
            }
        }

        private Model.MyFilterWheel.FilterInfo _snapFilter;

        public Model.MyFilterWheel.FilterInfo SnapFilter {
            get {
                return _snapFilter;
            }
            set {
                _snapFilter = value;
                RaisePropertyChanged();
            }
        }

        private BinningMode _snapBin;

        public BinningMode SnapBin {
            get {
                if (_snapBin == null) {
                    _snapBin = new BinningMode(1, 1);
                }
                return _snapBin;
            }
            set {
                _snapBin = value;
                RaisePropertyChanged();
            }
        }

        private short _snapGain = -1;

        public short SnapGain {
            get {
                return _snapGain;
            }
            set {
                _snapGain = value;
                RaisePropertyChanged();
            }
        }

        public async Task<bool> SnapImage(IProgress<ApplicationStatus> progress) {
            _captureImageToken = new CancellationTokenSource();

            try {
                var success = true;
                do {
                    var seq = new CaptureSequence(SnapExposureDuration, ImageTypes.SNAP, SnapFilter, SnapBin, 1);
                    seq.EnableSubSample = SnapSubSample;
                    seq.Gain = SnapGain;
                    success = await CaptureAndSaveImage(seq, SnapSave, _captureImageToken.Token, progress);
                    _captureImageToken.Token.ThrowIfCancellationRequested();
                } while (Loop && success);
            } catch (OperationCanceledException) {
            } finally {
                await _currentPrepareImageTask;
                progress.Report(new ApplicationStatus() { Status = string.Empty });
            }

            return true;
        }

        public async Task<bool> CaptureAndSaveImage(CaptureSequence seq, bool bsave, CancellationToken ct, IProgress<ApplicationStatus> progress, string targetname = "") {
            await CaptureImage(seq, ct, progress, bsave, targetname);
            return true;
        }
    }
}