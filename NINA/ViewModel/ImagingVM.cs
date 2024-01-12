#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using NINA.Equipment.Equipment.MyCamera;
using NINA.Profile.Interfaces;
using NINA.ViewModel.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Equipment.MyRotator;
using NINA.Equipment.Equipment.MyWeatherData;
using Dasync.Collections;
using NINA.Equipment.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Core.Model;
using NINA.Image.Interfaces;
using NINA.Image.ImageData;
using NINA.Equipment.Model;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Equipment.Exceptions;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Equipment.Equipment;
using NINA.WPF.Base.ViewModel;
using NINA.WPF.Base.Interfaces.ViewModel;

namespace NINA.ViewModel {

    internal class ImagingVM : BaseVM, IImagingVM {

        //Instantiate a Singleton of the Semaphore with a value of 1. This means that only 1 thread can be granted access at a time.
        private static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        private IImageControlVM _imageControl;

        private Task<IRenderedImage> _imageProcessingTask;

        private ApplicationStatus _status;

        private IApplicationStatusMediator applicationStatusMediator;

        private CameraInfo cameraInfo;

        private ICameraMediator cameraMediator;

        private FilterWheelInfo filterWheelInfo;

        private IFilterWheelMediator filterWheelMediator;

        private FocuserInfo focuserInfo;

        private IFocuserMediator focuserMediator;

        private IGuiderMediator guiderMediator;

        private IImagingMediator imagingMediator;

        private IProgress<ApplicationStatus> progress;

        private RotatorInfo rotatorInfo;

        private IRotatorMediator rotatorMediator;

        private TelescopeInfo telescopeInfo;

        private ITelescopeMediator telescopeMediator;

        private WeatherDataInfo weatherDataInfo;

        private IWeatherDataMediator weatherDataMediator;

        private IImageHistoryVM imageHistoryVM;

        public event EventHandler<ImagePreparedEventArgs> ImagePrepared {
            add { this._imageControl.ImagePrepared += value; }
            remove { this._imageControl.ImagePrepared -= value; }
        }

        public ImagingVM(IProfileService profileService,
                IImagingMediator imagingMediator,
                ICameraMediator cameraMediator,
                ITelescopeMediator telescopeMediator,
                IFilterWheelMediator filterWheelMediator,
                IFocuserMediator focuserMediator,
                IRotatorMediator rotatorMediator,
                IGuiderMediator guiderMediator,
                IWeatherDataMediator weatherDataMediator,
                IApplicationStatusMediator applicationStatusMediator,
                IImageControlVM imageControlVM,
                IImageStatisticsVM imageStatisticsVM,
                IImageHistoryVM imageHistoryVM
        ) : base(profileService) {
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
            this.imageHistoryVM = imageHistoryVM;

            progress = new Progress<ApplicationStatus>(p => Status = p);

            ImageControl = imageControlVM;
            ImgStatisticsVM = imageStatisticsVM;
        }

        public CameraInfo CameraInfo {
            get => cameraInfo ?? DeviceInfo.CreateDefaultInstance<CameraInfo>();
            set {
                cameraInfo = value;
                RaisePropertyChanged();
            }
        }

        public IImageControlVM ImageControl {
            get => _imageControl;
            set { _imageControl = value; RaisePropertyChanged(); }
        }

        public IImageStatisticsVM ImgStatisticsVM { get; }

        public ApplicationStatus Status {
            get => _status;
            set {
                _status = value;
                _status.Source = Loc.Instance["LblImaging"]; ;
                RaisePropertyChanged();

                applicationStatusMediator.StatusUpdate(_status);
            }
        }

        private void AddMetaData(
                ImageMetaData metaData,
                CaptureSequence sequence,
                DateTime start,
                DateTime midpoint,
                RMS rms,
                string targetName) {
            metaData.Image.Id = this.imageHistoryVM.GetNextImageId();
            metaData.Image.ExposureStart = start;
            metaData.Image.ExposureMidPoint = midpoint;
            metaData.Image.Binning = sequence.Binning.Name;
            metaData.Image.ExposureNumber = sequence.ProgressExposureCount;
            metaData.Image.ExposureTime = sequence.ExposureTime;
            metaData.Image.ImageType = sequence.ImageType;
            metaData.Image.RecordedRMS = rms;
            metaData.Target.Name = targetName;

            // Fill all available info from profile
            metaData.FromProfile(profileService.ActiveProfile);
            metaData.FromTelescopeInfo(telescopeInfo);
            metaData.FromFilterWheelInfo(filterWheelInfo);
            metaData.FromRotatorInfo(rotatorInfo);
            metaData.FromFocuserInfo(focuserInfo);
            metaData.FromWeatherDataInfo(weatherDataInfo);

            metaData.FilterWheel.Filter = sequence.FilterType?.Name ?? metaData.FilterWheel.Filter;
            if (metaData.Target.Coordinates == null || double.IsNaN(metaData.Target.Coordinates.RA))
                metaData.Target.Coordinates = metaData.Telescope.Coordinates;
        }

        private Task<IExposureData> CaptureImage(
                CaptureSequence sequence,
                PrepareImageParameters parameters,
                CancellationToken token,
                string targetName = "",
                bool skipProcessing = false
                ) {
            return Task.Run(async () => {
                try {
                    IExposureData data = null;
                    //Asynchronously wait to enter the Semaphore. If no-one has been granted access to the Semaphore, code execution will proceed, otherwise this thread waits here until the semaphore is released
                    progress.Report(new ApplicationStatus() { Status = Loc.Instance["LblWaitingForCamera"] });
                    await semaphoreSlim.WaitAsync(token);

                    try {
                        if (CameraInfo.Connected != true) {
                            Notification.ShowWarning(Loc.Instance["LblNoCameraConnected"]);
                            throw new CameraConnectionLostException();
                        }

                        /*Change Filter*/
                        await ChangeFilter(sequence, token, progress);

                        /* Start RMS Recording */
                        var rmsHandle = this.guiderMediator.StartRMSRecording();

                        /*Capture*/
                        var exposureStart = DateTime.UtcNow;
                        await cameraMediator.Capture(sequence, token, progress);
                        DateTime midpointDateTime = exposureStart + TimeSpan.FromTicks((DateTime.UtcNow - exposureStart).Ticks / 2);

                        /* Stop RMS Recording */
                        var rms = this.guiderMediator.StopRMSRecording(rmsHandle);

                        /*Download Image */
                        data = await Download(token, progress);

                        token.ThrowIfCancellationRequested();

                        if (data == null) {
                            throw new CameraDownloadFailedException(sequence);
                        }

                        AddMetaData(data.MetaData, sequence, exposureStart, midpointDateTime, rms, targetName);

                        if (!skipProcessing) {
                            //Wait for previous prepare image task to complete
                            if (_imageProcessingTask != null && !_imageProcessingTask.IsCompleted) {
                                progress.Report(new ApplicationStatus() { Status = Loc.Instance["LblWaitForImageProcessing"] });
                                await _imageProcessingTask;
                            }

                            _imageProcessingTask = PrepareImage(data, parameters, token);
                        }
                    } catch (OperationCanceledException) {
                        cameraMediator.AbortExposure();
                        throw;
                    } catch (CameraDownloadFailedException ex) {
                        Logger.Error(ex.Message);
                        Notification.ShowError(string.Format(Loc.Instance["LblCameraDownloadFailed"], sequence.ExposureTime, sequence.ImageType, sequence.Gain, sequence.FilterType?.Name ?? string.Empty));
                        throw;
                    } catch (CameraExposureFailedException ex) {
                        Logger.Error(ex.Message);
                        Notification.ShowError(ex.Message);
                        throw;
                    } catch (CameraConnectionLostException ex) {
                        Logger.Error(ex);
                        Notification.ShowError(Loc.Instance["LblCameraConnectionLost"]);
                        throw;
                    } catch (Exception ex) {
                        Notification.ShowError(Loc.Instance["LblUnexpectedError"] + Environment.NewLine + ex.Message);
                        Logger.Error(ex);
                        cameraMediator.AbortExposure();
                        throw;
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

        private async Task ChangeFilter(CaptureSequence seq, CancellationToken token, IProgress<ApplicationStatus> progress) {
            if (seq.FilterType != null) {
                await filterWheelMediator.ChangeFilter(seq.FilterType, token, progress);
            }
        }

        private Task<IExposureData> Download(CancellationToken token, IProgress<ApplicationStatus> progress) {
            progress.Report(new ApplicationStatus() { Status = Loc.Instance["LblDownloading"] });
            return cameraMediator.Download(token);
        }

        public async Task<IRenderedImage> CaptureAndPrepareImage(
            CaptureSequence sequence,
            PrepareImageParameters parameters,
            CancellationToken token,
            IProgress<ApplicationStatus> progress) {
            var iarr = await CaptureImage(sequence, parameters, token, string.Empty);
            if (iarr != null) {
                return await _imageProcessingTask;
            } else {
                return null;
            }
        }

        public Task<IExposureData> CaptureImage(CaptureSequence sequence, CancellationToken token, IProgress<ApplicationStatus> progress, string targetName = "") {
            return CaptureImage(sequence, new PrepareImageParameters(), token, targetName, true);
        }

        public void DestroyImage() {
            ImageControl.Image = null;
            ImageControl.RenderedImage = null;
        }

        public void Dispose() {
            this.cameraMediator.RemoveConsumer(this);
            this.telescopeMediator.RemoveConsumer(this);
            this.filterWheelMediator.RemoveConsumer(this);
            this.focuserMediator.RemoveConsumer(this);
            this.rotatorMediator.RemoveConsumer(this);
            this.weatherDataMediator.RemoveConsumer(this);
        }

        public Task<IRenderedImage> PrepareImage(
            IExposureData data,
            PrepareImageParameters parameters,
            CancellationToken cancelToken) {
            _imageProcessingTask = Task.Run(async () => {
                var imageData = await data.ToImageData(progress, cancelToken);
                var processedData = await ImageControl.PrepareImage(imageData, parameters, cancelToken);
                await ImgStatisticsVM.UpdateStatistics(imageData);
                return processedData;
            }, cancelToken);
            return _imageProcessingTask;
        }

        public Task<IRenderedImage> PrepareImage(
            IImageData data,
            PrepareImageParameters parameters,
            CancellationToken cancelToken) {
            _imageProcessingTask = Task.Run(async () => {
                try {
                    var processedData = await ImageControl.PrepareImage(data, parameters, cancelToken);
                    await ImgStatisticsVM.UpdateStatistics(data);
                    return processedData;
                } catch (OperationCanceledException) {
                    throw;
                } catch (Exception e) {
                    Logger.Error("Failed to prepare image", e);
                    Notification.ShowError($"Failed to prepare image for display: {e.Message}");
                    throw;
                }
            }, cancelToken);
            return _imageProcessingTask;
        }

        public void SetImage(BitmapSource img) {
            ImageControl.Image = img;
        }

        public async Task<bool> StartLiveView(CaptureSequence sequence, CancellationToken ct) {
            //todo: see if this is necessary
            //ImageControl.IsLiveViewEnabled = true;
            try {
                var liveViewEnumerable = cameraMediator.LiveView(sequence, ct);
                await liveViewEnumerable.ForEachAsync(async exposureData => {
                    var imageData = await exposureData.ToImageData(progress, ct);
                    await ImageControl.PrepareImage(imageData, new PrepareImageParameters(), ct);
                });
            } catch (OperationCanceledException) {
            } finally {
                //ImageControl.IsLiveViewEnabled = false;
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

        public void UpdateEndAutoFocusRun(AutoFocusInfo info) {
            ;
        }

        public void UpdateUserFocused(FocuserInfo info) {
            ;
        }

        public int GetImageRotation() {
            return _imageControl.ImageRotation;
        }

        public void SetImageRotation(int rotation) {
            _imageControl.ImageRotation = rotation;
        }

    }
}