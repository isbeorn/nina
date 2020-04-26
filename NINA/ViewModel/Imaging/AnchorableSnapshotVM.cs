#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

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
using NINA.Profile;
using NINA.Utility;
using NINA.Utility.Mediator;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static NINA.Model.CaptureSequence;

namespace NINA.ViewModel.Imaging {

    internal class AnchorableSnapshotVM : DockableVM, ICameraConsumer {
        private CancellationTokenSource _captureImageToken;
        private CancellationTokenSource _liveViewCts;
        private bool _liveViewEnabled;
        private BinningMode _snapBin;
        private bool _snapSubSample;

        private ApplicationStatus _status;

        private IApplicationStatusMediator applicationStatusMediator;
        private CameraInfo cameraInfo;
        private ICameraMediator cameraMediator;
        private IImagingMediator imagingMediator;

        private IProgress<ApplicationStatus> progress;

        public AnchorableSnapshotVM(
                IProfileService profileService,
                IImagingMediator imagingMediator,
                ICameraMediator cameraMediator,
                IApplicationStatusMediator applicationStatusMediator) : base(profileService) {
            Title = "LblImaging";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["ImagingSVG"];
            this.applicationStatusMediator = applicationStatusMediator;
            this.cameraMediator = cameraMediator;
            this.cameraMediator.RegisterConsumer(this);
            this.imagingMediator = imagingMediator;
            progress = new Progress<ApplicationStatus>(p => Status = p);
            SnapCommand = new AsyncCommand<bool>(() => SnapImage(progress));
            CancelSnapCommand = new RelayCommand(CancelSnapImage);
            StartLiveViewCommand = new AsyncCommand<bool>(StartLiveView);
            StopLiveViewCommand = new RelayCommand(StopLiveView);
        }

        /// <summary>
        /// Backwards compatible ContentId due to refactoring
        /// </summary>
        public new string ContentId {
            get => typeof(ImagingVM).Name;
        }

        public CameraInfo CameraInfo {
            get {
                return cameraInfo ?? DeviceInfo.CreateDefaultInstance<CameraInfo>();
            }
            set {
                cameraInfo = value;
                RaisePropertyChanged();
            }
        }

        public ICommand CancelSnapCommand { get; private set; }

        private bool isLooping;

        public bool IsLooping {
            get => isLooping;
            set {
                isLooping = value;
                RaisePropertyChanged();
            }
        }

        public bool LiveViewEnabled {
            get {
                return _liveViewEnabled;
            }
            set {
                _liveViewEnabled = value;
                RaisePropertyChanged();
            }
        }

        public bool Loop {
            get {
                return profileService.ActiveProfile.SnapShotControlSettings.Loop;
            }
            set {
                profileService.ActiveProfile.SnapShotControlSettings.Loop = value;
                RaisePropertyChanged();
            }
        }

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

        public IAsyncCommand SnapCommand { get; private set; }

        public double SnapExposureDuration {
            get {
                return profileService.ActiveProfile.SnapShotControlSettings.ExposureDuration;
            }

            set {
                profileService.ActiveProfile.SnapShotControlSettings.ExposureDuration = value;
                RaisePropertyChanged();
            }
        }

        public Model.MyFilterWheel.FilterInfo SnapFilter {
            get {
                return profileService.ActiveProfile.SnapShotControlSettings.Filter;
            }
            set {
                profileService.ActiveProfile.SnapShotControlSettings.Filter = value;
                RaisePropertyChanged();
            }
        }

        public int SnapGain {
            get {
                return profileService.ActiveProfile.SnapShotControlSettings.Gain;
            }
            set {
                profileService.ActiveProfile.SnapShotControlSettings.Gain = value;
                RaisePropertyChanged();
            }
        }

        public bool SnapSave {
            get {
                return profileService.ActiveProfile.SnapShotControlSettings.Save;
            }
            set {
                profileService.ActiveProfile.SnapShotControlSettings.Save = value;
                RaisePropertyChanged();
            }
        }

        public bool SnapSubSample {
            get {
                return _snapSubSample;
            }
            set {
                _snapSubSample = value;
                RaisePropertyChanged();
            }
        }

        public IAsyncCommand StartLiveViewCommand { get; private set; }

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

        public ICommand StopLiveViewCommand { get; private set; }

        private void CancelSnapImage(object o) {
            _captureImageToken?.Cancel();
        }

        private async Task<bool> StartLiveView() {
            _liveViewCts?.Dispose();
            _liveViewCts = new CancellationTokenSource();
            try {
                await this.imagingMediator.StartLiveView(_liveViewCts.Token);
            } catch (OperationCanceledException) {
            }

            return true;
        }

        private void StopLiveView(object o) {
            _liveViewCts?.Cancel();
        }

        public void Dispose() {
            this.cameraMediator.RemoveConsumer(this);
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

                    var renderedImage = await imagingMediator.CaptureAndPrepareImage(seq, new PrepareImageParameters(), _captureImageToken.Token, progress);
                    if (SnapSave) {
                        progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblSavingImage"] });
                        var path = await renderedImage.RawImageData.SaveToDisk(new FileSaveInfo(profileService), _captureImageToken.Token);
                        var imageStatistics = await renderedImage.RawImageData.Statistics.Task;

                        imagingMediator.OnImageSaved(
                            new ImageSavedEventArgs() {
                                PathToImage = new Uri(path),
                                Image = renderedImage.Image,
                                FileType = profileService.ActiveProfile.ImageFileSettings.FileType,
                                Mean = imageStatistics.Mean,
                                HFR = renderedImage.RawImageData.StarDetectionAnalysis.HFR,
                                Duration = renderedImage.RawImageData.MetaData.Image.ExposureTime,
                                IsBayered = renderedImage.RawImageData.Properties.IsBayered,
                                Filter = renderedImage.RawImageData.MetaData.FilterWheel.Filter
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
                //if (_imageProcessingTask != null) {
                //    await _imageProcessingTask;
                //}
                IsLooping = false;
                progress.Report(new ApplicationStatus() { Status = string.Empty });
            }

            return true;
        }

        public void UpdateDeviceInfo(CameraInfo cameraStatus) {
            CameraInfo = cameraStatus;
        }
    }
}