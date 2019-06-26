#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Utility;
using NINA.Utility.Exceptions;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.Profile;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Async;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using static NINA.Model.CaptureSequence;
using NINA.Model.ImageData;
using NINA.Model.MyTelescope;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyFocuser;
using NINA.Model.MyRotator;
using NINA.Model.MyWeatherData;

namespace NINA.ViewModel {

    internal class ImagingVM : DockableVM, IImagingVM {

        public ImagingVM(
                IProfileService profileService,
                IImagingMediator imagingMediator,
                ICameraMediator cameraMediator,
                ITelescopeMediator telescopeMediator,
                IFilterWheelMediator filterWheelMediator,
                IFocuserMediator focuserMediator,
                IRotatorMediator rotatorMediator,
                IGuiderMediator guiderMediator,
                IWeatherDataMediator weatherDataMediator,
                IApplicationStatusMediator applicationStatusMediator
        ) : base(profileService) {
            Title = "LblImaging";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["ImagingSVG"];

            this.imagingMediator = imagingMediator;
            this.imagingMediator.RegisterHandler(this);

            this.cameraMediator = cameraMediator;
            this.cameraMediator.RegisterConsumer(this);

            this.telescopeMediator = telescopeMediator;
            this.telescopeMediator.RegisterConsumer(this);

            this.filterWheelMediator = filterWheelMediator;
            this.filterWheelMediator.RegisterConsumer(this);

            this.focuserMediator = focuserMediator;
            this.focuserMediator.RegisterConsumer(this);

            this.rotatorMediator = rotatorMediator;
            this.rotatorMediator.RegisterConsumer(this);

            this.guiderMediator = guiderMediator;
            this.applicationStatusMediator = applicationStatusMediator;

            this.weatherDataMediator = weatherDataMediator;
            this.weatherDataMediator.RegisterConsumer(this);

            SnapExposureDuration = 1;
            progress = new Progress<ApplicationStatus>(p => Status = p);
            SnapCommand = new AsyncCommand<bool>(() => SnapImage(progress));
            CancelSnapCommand = new RelayCommand(CancelSnapImage);
            StartLiveViewCommand = new AsyncCommand<bool>(StartLiveView);
            StopLiveViewCommand = new RelayCommand(StopLiveView);

            ImageControl = new ImageControlVM(profileService, cameraMediator, telescopeMediator, imagingMediator, applicationStatusMediator);
        }

        private IProgress<ApplicationStatus> progress;
        private ImageControlVM _imageControl;

        public ImageControlVM ImageControl {
            get { return _imageControl; }
            set { _imageControl = value; RaisePropertyChanged(); }
        }

        private CameraInfo cameraInfo;

        public CameraInfo CameraInfo {
            get {
                return cameraInfo ?? DeviceInfo.CreateDefaultInstance<CameraInfo>();
            }
            set {
                cameraInfo = value;
                RaisePropertyChanged();
            }
        }

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

        private bool _liveViewEnabled;

        public bool LiveViewEnabled {
            get {
                return _liveViewEnabled;
            }
            set {
                _liveViewEnabled = value;
                RaisePropertyChanged();
            }
        }

        private CancellationTokenSource _liveViewCts;

        private async Task<bool> StartLiveView() {
            ImageControl.IsLiveViewEnabled = true;
            _liveViewCts?.Dispose();
            _liveViewCts = new CancellationTokenSource();
            try {
                await Task.Run(async () => {
                    var liveViewEnumerable = cameraMediator.LiveView(_liveViewCts.Token);
                    await liveViewEnumerable.ForEachAsync(async iarr => {
                        await ImageControl.PrepareImage(iarr, _liveViewCts.Token);
                    });
                });
            } catch (OperationCanceledException) {
            } finally {
                ImageControl.IsLiveViewEnabled = false;
            }

            return true;
        }

        public void SetImage(BitmapSource img) {
            ImageControl.Image = img;
        }

        private void StopLiveView(object o) {
            _liveViewCts?.Cancel();
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

                applicationStatusMediator.StatusUpdate(_status);
            }
        }

        private ImageStatisticsVM imgStatisticsVM;

        public ImageStatisticsVM ImgStatisticsVM {
            get {
                if (imgStatisticsVM == null) {
                    imgStatisticsVM = new ImageStatisticsVM(profileService);
                }
                return imgStatisticsVM;
            }
            set {
                imgStatisticsVM = value;
                RaisePropertyChanged();
            }
        }

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

        private double _snapExposureDuration;
        private IFilterWheelMediator filterWheelMediator;
        private IGuiderMediator guiderMediator;
        private IWeatherDataMediator weatherDataMediator;
        private IApplicationStatusMediator applicationStatusMediator;

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
        public IAsyncCommand StartLiveViewCommand { get; private set; }
        public ICommand StopLiveViewCommand { get; private set; }

        private void CancelSnapImage(object o) {
            _captureImageToken?.Cancel();
        }

        private CancellationTokenSource _captureImageToken;

        private async Task ChangeFilter(CaptureSequence seq, CancellationToken token, IProgress<ApplicationStatus> progress) {
            if (seq.FilterType != null) {
                await filterWheelMediator.ChangeFilter(seq.FilterType, token, progress);
            }
        }

        private async Task Capture(CaptureSequence seq, CancellationToken token, IProgress<ApplicationStatus> progress) {
            if (ImageControl.IsLiveViewEnabled) _liveViewCts?.Cancel();
            else await cameraMediator.Capture(seq, token, progress);
        }

        private Task<IImageData> Download(CancellationToken token, IProgress<ApplicationStatus> progress) {
            progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblDownloading"] });
            return cameraMediator.Download(token);
        }

        //Instantiate a Singleton of the Semaphore with a value of 1. This means that only 1 thread can be granted access at a time.
        private static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        public async Task<IImageData> CaptureAndPrepareImage(CaptureSequence sequence, CancellationToken token, IProgress<ApplicationStatus> progress) {
            var iarr = await CaptureImage(sequence, token, string.Empty);
            if (iarr != null) {
                return await _imageProcessingTask;
            } else {
                return null;
            }
        }

        public Task<IImageData> CaptureImage(CaptureSequence sequence, CancellationToken token, IProgress<ApplicationStatus> progress) {
            return CaptureImage(sequence, token, string.Empty, true);
        }

        private Task<IImageData> CaptureImage(
                CaptureSequence sequence,
                CancellationToken token,
                string targetName = "",
                bool skipProcessing = false
                ) {
            return Task.Run(async () => {
                try {
                    IImageData data = null;
                    //Asynchronously wait to enter the Semaphore. If no-one has been granted access to the Semaphore, code execution will proceed, otherwise this thread waits here until the semaphore is released
                    progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblWaitingForCamera"] });
                    await semaphoreSlim.WaitAsync(token);

                    try {
                        if (CameraInfo.Connected != true) {
                            Notification.ShowWarning(Locale.Loc.Instance["LblNoCameraConnected"]);
                            throw new CameraConnectionLostException();
                        }

                        /*Change Filter*/
                        await ChangeFilter(sequence, token, progress);

                        /* Start RMS Recording */
                        var rmsHandle = this.guiderMediator.StartRMSRecording();

                        /*Capture*/
                        var exposureStart = DateTime.Now;
                        await Capture(sequence, token, progress);

                        /* Stop RMS Recording */
                        var rms = this.guiderMediator.StopRMSRecording(rmsHandle);

                        /*Download Image */
                        data = await Download(token, progress);

                        if (data == null) {
                            Logger.Error(new CameraDownloadFailedException(sequence));
                            Notification.ShowError(string.Format(Locale.Loc.Instance["LblCameraDownloadFailed"], sequence.ExposureTime, sequence.ImageType, sequence.Gain, sequence.FilterType?.Name ?? string.Empty));
                            return null;
                        }

                        AddMetaData(data, sequence, exposureStart, rms, targetName);

                        if (!skipProcessing) {
                            //Wait for previous prepare image task to complete
                            if (_imageProcessingTask != null && !_imageProcessingTask.IsCompleted) {
                                progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblWaitForImageProcessing"] });
                                await _imageProcessingTask;
                            }

                            _imageProcessingTask = PrepareImage(data, token);
                        }
                    } catch (System.OperationCanceledException ex) {
                        cameraMediator.AbortExposure();
                        throw ex;
                    } catch (CameraConnectionLostException ex) {
                        Logger.Error(ex);
                        Notification.ShowError(Locale.Loc.Instance["LblCameraConnectionLost"]);
                        throw ex;
                    } catch (Exception ex) {
                        Notification.ShowError(Locale.Loc.Instance["LblUnexpectedError"] + Environment.NewLine + ex.Message);
                        Logger.Error(ex);
                        cameraMediator.AbortExposure();
                        throw ex;
                    } finally {
                        progress.Report(new ApplicationStatus() { Status = "" });
                        semaphoreSlim.Release();
                    }
                    return data;
                } finally {
                    progress.Report(new ApplicationStatus() { Status = string.Empty });
                }
            });
        }

        private void AddMetaData(IImageData data, CaptureSequence sequence, DateTime start, RMS rms, string targetName) {
            data.MetaData.Image.ExposureStart = start;
            data.MetaData.Image.Binning = sequence.Binning.Name;
            data.MetaData.Image.ExposureNumber = sequence.ProgressExposureCount;
            data.MetaData.Image.ExposureTime = sequence.ExposureTime;
            data.MetaData.Image.ImageType = sequence.ImageType;
            data.MetaData.Image.RecordedRMS = rms;

            data.MetaData.Target.Name = targetName;
            data.MetaData.FilterWheel.Filter = sequence.FilterType?.Name ?? string.Empty;

            // Fill all available info from profile
            data.MetaData.FromProfile(profileService.ActiveProfile);
            data.MetaData.FromTelescopeInfo(telescopeInfo);
            data.MetaData.FromFilterWheelInfo(filterWheelInfo);
            data.MetaData.FromRotatorInfo(rotatorInfo);
            data.MetaData.FromFocuserInfo(focuserInfo);
            data.MetaData.FromWeatherDataInfo(weatherDataInfo);
        }

        private Task<IImageData> _imageProcessingTask;

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
        private ICameraMediator cameraMediator;
        private IImagingMediator imagingMediator;
        private ITelescopeMediator telescopeMediator;
        private IFocuserMediator focuserMediator;
        private IRotatorMediator rotatorMediator;
        private TelescopeInfo telescopeInfo;
        private FilterWheelInfo filterWheelInfo;
        private FocuserInfo focuserInfo;
        private RotatorInfo rotatorInfo;
        private WeatherDataInfo weatherDataInfo;

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
            _captureImageToken?.Dispose();
            _captureImageToken = new CancellationTokenSource();

            try {
                var success = true;
                if (Loop) IsLooping = true;
                do {
                    var seq = new CaptureSequence(SnapExposureDuration, ImageTypes.SNAPSHOT, SnapFilter, SnapBin, 1);
                    seq.EnableSubSample = SnapSubSample;
                    seq.Gain = SnapGain;

                    var data = await CaptureAndPrepareImage(seq, _captureImageToken.Token, progress);
                    if (SnapSave) {
                        var path = await data.SaveToDisk(
                            profileService.ActiveProfile.ImageFileSettings.FilePath,
                            profileService.ActiveProfile.ImageFileSettings.FilePattern,
                            profileService.ActiveProfile.ImageFileSettings.FileType,
                            _captureImageToken.Token
                        );
                        imagingMediator.OnImageSaved(
                            new ImageSavedEventArgs() {
                                PathToImage = new Uri(path),
                                Image = data.Image,
                                FileType = profileService.ActiveProfile.ImageFileSettings.FileType,
                                Mean = data.Statistics.Mean,
                                HFR = data.Statistics.HFR,
                                Duration = data.MetaData.Image.ExposureTime,
                                IsBayered = data.Statistics.IsBayered,
                                Filter = data.MetaData.FilterWheel.Filter,
                                StatisticsId = data.Statistics.Id
                            }
                        );
                    }

                    _captureImageToken.Token.ThrowIfCancellationRequested();
                } while (Loop && success);
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(ex.Message);
            } finally {
                if (_imageProcessingTask != null) {
                    await _imageProcessingTask;
                }
                IsLooping = false;
                progress.Report(new ApplicationStatus() { Status = string.Empty });
            }

            return true;
        }

        public void UpdateDeviceInfo(CameraInfo cameraStatus) {
            CameraInfo = cameraStatus;
        }

        public void UpdateDeviceInfo(TelescopeInfo deviceInfo) {
            this.telescopeInfo = deviceInfo;
        }

        public void UpdateDeviceInfo(FilterWheelInfo deviceInfo) {
            this.filterWheelInfo = deviceInfo;
        }

        public void UpdateDeviceInfo(FocuserInfo deviceInfo) {
            this.focuserInfo = deviceInfo;
        }

        public void UpdateDeviceInfo(RotatorInfo deviceInfo) {
            this.rotatorInfo = deviceInfo;
        }

        public void UpdateDeviceInfo(WeatherDataInfo deviceInfo) {
            this.weatherDataInfo = deviceInfo;
        }

        public bool SetDetectStars(bool value) {
            var oldval = ImageControl.DetectStars;
            ImageControl.DetectStars = value;
            return oldval;
        }

        public bool SetAutoStretch(bool value) {
            var oldval = ImageControl.AutoStretch;
            ImageControl.AutoStretch = value;
            return oldval;
        }

        public Task<IImageData> PrepareImage(IImageData data, CancellationToken token) {
            _imageProcessingTask = Task.Run(async () => {
                Task<IImageData> process = ImageControl.PrepareImage(data, token);

                var processedData = await process;

                ImgStatisticsVM.Add(data.Statistics);
                return processedData;
            }, token);
            return _imageProcessingTask;
        }

        public void DestroyImage() {
            ImageControl.Image = null;
            ImageControl.ImgArr = null;
        }

        public void Dispose() {
            this.cameraMediator.RemoveConsumer(this);
            this.telescopeMediator.RemoveConsumer(this);
            this.filterWheelMediator.RemoveConsumer(this);
            this.focuserMediator.RemoveConsumer(this);
            this.rotatorMediator.RemoveConsumer(this);
            this.weatherDataMediator.RemoveConsumer(this);
        }

        public bool IsLooping { get; set; }
    }
}