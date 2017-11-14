using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Utility;
using nom.tam.fits;
using nom.tam.util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static NINA.Model.CaptureSequence;
using System.ComponentModel;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyTelescope;
using NINA.Utility.Notification;

namespace NINA.ViewModel {
    class ImagingVM : DockableVM {

        public ImagingVM() : base() {

            Title = "LblImaging";
            ContentId = nameof(ImagingVM);
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["ImagingSVG"];

            SnapExposureDuration = 1;
            SnapCommand = new AsyncCommand<bool>(() => CaptureImage(new Progress<string>(p => Status = p)));
            CancelSnapCommand = new RelayCommand(CancelCaptureImage);

            ImageControl = new ImageControlVM();

            RegisterMediatorMessages();
        }

        private void RegisterMediatorMessages() {
            Mediator.Instance.RegisterAsync(async (object o) => {
                var args = (object[])o;
                CaptureSequenceList seq = (CaptureSequenceList)args[0];
                bool save = (bool)args[1];
                CancellationTokenSource token = (CancellationTokenSource)args[2];
                IProgress<string> progress = (IProgress<string>)args[3];
                await StartSequence(seq, save, token, progress);
            }, AsyncMediatorMessages.StartSequence);

            Mediator.Instance.RegisterAsync(async (object o) => {
                var args = (object[])o;
                CaptureSequence seq = (CaptureSequence)args[0];
                bool save = (bool)args[1];
                IProgress<string> progress = (IProgress<string>)args[2];
                CancellationTokenSource token = (CancellationTokenSource)args[3];

                await CaptureImage(seq, save, progress, token);
            }, AsyncMediatorMessages.CaptureImage);


            Mediator.Instance.Register((object o) => _cameraConnected = (bool)o, MediatorMessages.CameraConnectedChanged);
            Mediator.Instance.Register((object o) => {
                Cam = (ICamera)o;
            }, MediatorMessages.CameraChanged);

        }

        private bool _cameraConnected;

        private string _status;
        public string Status {
            get {
                return _status;
            }
            set {
                _status = value;
                RaisePropertyChanged();

                Mediator.Instance.Notify(MediatorMessages.StatusUpdate, _status);
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

        private bool _isExposing;
        public bool IsExposing {
            get {
                return _isExposing;
            }
            set {
                _isExposing = value;
                RaisePropertyChanged();

                Mediator.Instance.Notify(MediatorMessages.IsExposingUpdate, _isExposing);
            }
        }

        public IAsyncCommand SnapCommand { get; private set; }

        public ICommand CancelSnapCommand { get; private set; }

        private void CancelCaptureImage(object o) {
            _captureImageToken?.Cancel();
        }

        CancellationTokenSource _captureImageToken;

        private async Task ChangeFilter(CaptureSequence seq, CancellationTokenSource tokenSource, IProgress<string> progress) {

            progress.Report(ExposureStatus.FILTERCHANGE);
            if (seq.FilterType != null) {
                await Mediator.Instance.NotifyAsync(AsyncMediatorMessages.ChangeFilterWheelPosition, new object[] { seq.FilterType, tokenSource });
            }
        }

        private void SetBinning(CaptureSequence seq) {
            if (seq.Binning == null) {
                Cam.SetBinning(1, 1);
            } else {
                Cam.SetBinning(seq.Binning.X, seq.Binning.Y);
            }
        }

        private async Task Capture(CaptureSequence seq, CancellationTokenSource tokenSource, IProgress<string> progress) {
            IsExposing = true;
            try {
                double duration = seq.ExposureTime;
                progress.Report(string.Format(ExposureStatus.EXPOSING, 0, duration));
                bool isLight = false;
                if (Cam.HasShutter) {
                    isLight = true;
                }
                Cam.StartExposure(duration, isLight);
                ExposureSeconds = 1;
                progress.Report(string.Format(ExposureStatus.EXPOSING, 1, duration));
                /* Wait for Capture */
                if (duration >= 1) {
                    await Task.Run(async () => {
                        do {
                            await Task.Delay(1000, tokenSource.Token);
                            tokenSource.Token.ThrowIfCancellationRequested();
                            ExposureSeconds += 1;
                            progress.Report(string.Format(ExposureStatus.EXPOSING, ExposureSeconds, duration));
                        } while ((ExposureSeconds < duration) && Cam.Connected);
                    });
                }
                tokenSource.Token.ThrowIfCancellationRequested();
            } catch (System.OperationCanceledException ex) {
                Logger.Trace(ex.Message);
            } catch (Exception ex) {
                Notification.ShowError(ex.Message);
            } finally {
                IsExposing = false;
            }


        }

        private async Task<ImageArray> Download(CancellationTokenSource tokenSource, IProgress<string> progress) {
            progress.Report(ExposureStatus.DOWNLOADING);
            return await Cam.DownloadExposure(tokenSource);
        }





        private async Task<bool> Save(CaptureSequenceList seq, CancellationTokenSource tokenSource, IProgress<string> progress) {
            progress.Report(ExposureStatus.SAVING);

            await ImageControl.SaveToDisk(seq, tokenSource, progress);

            return true;
        }

        private async Task<bool> Dither(CaptureSequence seq, CancellationTokenSource tokenSource, IProgress<string> progress) {
            if (seq.Dither && ((seq.ExposureCount % seq.DitherAmount) == 0)) {
                progress.Report(ExposureStatus.DITHERING);

                await Mediator.Instance.NotifyAsync(AsyncMediatorMessages.DitherGuider, tokenSource.Token);
            }
            tokenSource.Token.ThrowIfCancellationRequested();
            return true;
        }

        //Instantiate a Singleton of the Semaphore with a value of 1. This means that only 1 thread can be granted access at a time.
        static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        public async Task<bool> StartSequence(CaptureSequenceList sequence, bool bSave, CancellationTokenSource tokenSource, IProgress<string> progress) {

            //Asynchronously wait to enter the Semaphore. If no-one has been granted access to the Semaphore, code execution will proceed, otherwise this thread waits here until the semaphore is released 
            await semaphoreSlim.WaitAsync(tokenSource.Token);

            if (_cameraConnected != true) {
                Notification.ShowWarning(Locale.Loc.Instance["LblNoCameraConnected"]);
                semaphoreSlim.Release();
                return false;
            }

            return await Task.Run<bool>(async () => {


                try {

                    /* delay sequence start by given amount */
                    var delay = sequence.Delay;
                    while (delay > 0) {
                        await Task.Delay(TimeSpan.FromSeconds(1), tokenSource.Token);
                        delay--;
                        progress.Report(string.Format(Locale.Loc.Instance["LblSequenceDelayStatus"], delay));
                    }

                    CaptureSequence seq;
                    while ((seq = sequence.Next()) != null) {
                        await CheckMeridianFlip(seq, tokenSource, progress);

                        /*Change Filter*/
                        await ChangeFilter(seq, tokenSource, progress);

                        if (_cameraConnected != true) {
                            tokenSource.Cancel();
                            throw new OperationCanceledException();
                        }

                        /*Set Camera Gain */
                        SetGain(seq);

                        /*Set Camera Binning*/
                        SetBinning(seq);

                        if (_cameraConnected != true) {
                            tokenSource.Cancel();
                            throw new OperationCanceledException();
                        }

                        /*Capture*/
                        await Capture(seq, tokenSource, progress);

                        if (_cameraConnected != true) {
                            tokenSource.Cancel();
                            throw new OperationCanceledException();
                        }

                        /*Dither*/
                        var ditherTask = Dither(seq, tokenSource, progress);

                        /*Download Image */
                        ImageArray arr = await Download(tokenSource, progress);
                        if (arr == null) {
                            tokenSource.Cancel();
                            throw new OperationCanceledException();
                        }

                        ImageControl.ImgArr = arr;

                        /*Prepare Image for UI*/
                        progress.Report(ImagingVM.ExposureStatus.PREPARING);

                        await ImageControl.PrepareImage(progress, tokenSource);


                        if (_cameraConnected != true) {
                            tokenSource.Cancel();
                            throw new OperationCanceledException();
                        }

                        /*Save to disk*/
                        if (bSave) {
                            await Save(sequence, tokenSource, progress);
                        }

                        //Wait for dither to finish. Runs in parallel to save.
                        progress.Report(ExposureStatus.DITHERING);
                        await ditherTask;


                        if (_cameraConnected != true) {
                            tokenSource.Cancel();
                            throw new OperationCanceledException();
                        }
                    }
                } catch (System.OperationCanceledException ex) {
                    Logger.Trace(ex.Message);
                    if (_cameraConnected == true) {
                        Cam.AbortExposure();
                    }
                } catch (Exception ex) {
                    Notification.ShowError(ex.Message);
                    if (_cameraConnected == true) {
                        Cam.AbortExposure();
                    }
                } finally {
                    progress.Report(ExposureStatus.IDLE);
                    semaphoreSlim.Release();
                }
                return true;
            });

        }

        private void SetGain(CaptureSequence seq) {
            if (seq.Gain != -1) {
                Cam.Gain = seq.Gain;
            } else {

            }
        }

        /// <summary>
        /// Checks if auto meridian flip should be considered and executes it
        /// 1) Compare next exposure length with time to meridian - If exposure length is greater than time to flip the system will wait
        /// 2) Pause Guider
        /// 3) Execute the flip
        /// 4) If recentering is enabled, platesolve current position, sync and recenter to old target position
        /// 5) Resume Guider
        /// </summary>
        /// <param name="seq">Current Sequence row</param>
        /// <param name="tokenSource">cancel token</param>
        /// <param name="progress">progress reporter</param>
        /// <returns></returns>
        private async Task CheckMeridianFlip(CaptureSequence seq, CancellationTokenSource tokenSource, IProgress<string> progress) {
            progress.Report("Check Meridian Flip");

            /*Release lock in case meridian flip has to be done and thus a platesolve */
            semaphoreSlim.Release();
            await Mediator.Instance.NotifyAsync(AsyncMediatorMessages.CheckMeridianFlip, new object[] { seq, tokenSource });
            /*Reaquire lock*/
            await semaphoreSlim.WaitAsync(tokenSource.Token);
        }

        ImageControlVM _imageControl;
        public ImageControlVM ImageControl {
            get { return _imageControl; }
            set { _imageControl = value; RaisePropertyChanged(); }
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



        public async Task<bool> CaptureImage(IProgress<string> progress) {
            _captureImageToken = new CancellationTokenSource();

            do {
                CaptureSequenceList seq = new CaptureSequenceList(new CaptureSequence(SnapExposureDuration, ImageTypes.SNAP, SnapFilter, SnapBin, 1));
                await StartSequence(seq, SnapSave, _captureImageToken, progress);
                _captureImageToken.Token.ThrowIfCancellationRequested();
            } while (Loop);
            return true;

        }

        public async Task<bool> CaptureImage(CaptureSequence seq, bool bsave, IProgress<string> progress, CancellationTokenSource token) {

            var list = new CaptureSequenceList(seq);
            return await StartSequence(list, bsave, token, progress);

        }

        public static class ExposureStatus {
            public const string EXPOSING = "Exposing {0}/{1}...";
            public const string DOWNLOADING = "Downloading...";
            public const string FILTERCHANGE = "Switching Filter...";
            public const string PREPARING = "Preparing...";
            public const string CALCHFR = "Calculating HFR...";
            public const string SAVING = "Saving...";
            public const string IDLE = "";
            public const string DITHERING = "Dithering...";
            public const string SETTLING = "Settling...";
        }
    }
}
