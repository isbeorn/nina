#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using Accord;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Model;
using NINA.Image.Interfaces;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Profile.Interfaces;
using NINA.ViewModel.ImageHistory;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static NINA.Equipment.Model.CaptureSequence;
using NINA.Equipment.Equipment;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.ViewModel;
using NINA.Astrometry;
using NINA.WPF.Base.Behaviors;
using System.ComponentModel;
using NINA.Equipment.Exceptions;
using System.Windows.Media.Imaging;
using NINA.Equipment.Equipment.MyFilterWheel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NINA.ViewModel.Imaging {

    internal partial class AnchorableSnapshotVM : DockableVM, ICameraConsumer, IFilterWheelConsumer, IAnchorableSnapshotVM {
        private CancellationTokenSource _captureImageToken;
        private CancellationTokenSource _liveViewCts;
        private bool _liveViewEnabled;
        private BinningMode _snapBin;
        private bool _snapSubSample;

        private ApplicationStatus _status;

        private IApplicationStatusMediator applicationStatusMediator;
        private CameraInfo cameraInfo;
        private FilterWheelInfo filterWheelInfo;
        private ICameraMediator cameraMediator;
        private IImagingMediator imagingMediator;
        private IImageSaveMediator imageSaveMediator;
        private IProgress<ApplicationStatus> progress;
        private IImageHistoryVM imageHistoryVM;
        private IFilterWheelMediator filterWheelMediator;

        public AnchorableSnapshotVM(
                IProfileService profileService,
                IImagingMediator imagingMediator,
                ICameraMediator cameraMediator,
                IApplicationStatusMediator applicationStatusMediator,
                IImageSaveMediator imageSaveMediator,
                IImageHistoryVM imageHistoryVM,
                IFilterWheelMediator filterWheelMediator) : base(profileService) {
            Title = Loc.Instance["LblImaging"];
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["ImagingSVG"];
            this.applicationStatusMediator = applicationStatusMediator;
            this.cameraMediator = cameraMediator;
            this.cameraMediator.RegisterConsumer(this);
            this.imagingMediator = imagingMediator;
            this.imageSaveMediator = imageSaveMediator;
            this.imageHistoryVM = imageHistoryVM;
            this.filterWheelMediator = filterWheelMediator;
            this.filterWheelMediator.RegisterConsumer(this);
            progress = new Progress<ApplicationStatus>(p => Status = p);
            SnapCommand = new AsyncCommand<bool>(async () => {
                cameraMediator.RegisterCaptureBlock(this);
                try {
                    var result = await SnapImage(progress);
                    return result;
                } finally {
                    cameraMediator.ReleaseCaptureBlock(this);
                }
            }, (o) => cameraMediator.IsFreeToCapture(this) && !LiveViewEnabled);
            CancelSnapCommand = new RelayCommand(CancelSnapImage);
            StartLiveViewCommand = new AsyncCommand<bool>(async () => {
                cameraMediator.RegisterCaptureBlock(this);
                try {
                    var result = await StartLiveView();
                    return result;
                } finally {
                    cameraMediator.ReleaseCaptureBlock(this);
                }
            }, (o) => cameraMediator.IsFreeToCapture(this) && !IsLooping);
            StopLiveViewCommand = new RelayCommand(StopLiveView);
            SnapFilter = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters?.FirstOrDefault(x => x.Name == profileService.ActiveProfile.SnapShotControlSettings.Filter?.Name);

            profileService.ProfileChanged += (object sender, EventArgs e) => {
                SnapFilter = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters?.FirstOrDefault(x => x.Name == profileService.ActiveProfile.SnapShotControlSettings.Filter?.Name);
            };
            SubSampleRectangleMoveCommand = new RelayCommand(SubSampleRectangleMove);
        }

        /// <summary>
        /// Backwards compatible ContentId due to refactoring
        /// </summary>
        public new string ContentId => typeof(ImagingVM).Name;

        public CameraInfo CameraInfo {
            get => cameraInfo ?? DeviceInfo.CreateDefaultInstance<CameraInfo>();
            set {
                cameraInfo = value;
                RaisePropertyChanged();
            }
        }

        public FilterWheelInfo FilterWheelInfo {
            get => filterWheelInfo ?? DeviceInfo.CreateDefaultInstance<FilterWheelInfo>();
            set {
                filterWheelInfo = value;
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
            get => _liveViewEnabled;
            set {
                _liveViewEnabled = value;
                RaisePropertyChanged();
            }
        }

        public bool Loop {
            get => profileService.ActiveProfile.SnapShotControlSettings.Loop;
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
            get => profileService.ActiveProfile.SnapShotControlSettings.ExposureDuration;

            set {
                profileService.ActiveProfile.SnapShotControlSettings.ExposureDuration = value;
                RaisePropertyChanged();
            }
        }

        private FilterInfo snapFilter;

        public FilterInfo SnapFilter {
            get => snapFilter;
            set {
                snapFilter = value;
                profileService.ActiveProfile.SnapShotControlSettings.Filter = value;
                RaisePropertyChanged();
            }
        }

        public int SnapGain {
            get => profileService.ActiveProfile.SnapShotControlSettings.Gain;
            set {
                profileService.ActiveProfile.SnapShotControlSettings.Gain = value;
                RaisePropertyChanged();
            }
        }

        public bool SnapSave {
            get => profileService.ActiveProfile.SnapShotControlSettings.Save;
            set {
                profileService.ActiveProfile.SnapShotControlSettings.Save = value;
                RaisePropertyChanged();
            }
        }

        [ObservableProperty]
        private string snapTargetName = "Snapshot";

        public bool SnapSubSample {
            get => _snapSubSample;
            set {
                _snapSubSample = value;
                RaisePropertyChanged();
            }
        }

        private ObservableRectangle subSampleRectangle;

        public ObservableRectangle SubSampleRectangle {
            get => subSampleRectangle;
            set {
                if (subSampleRectangle != null) {
                    subSampleRectangle.PropertyChanged -= SubSampleRectangle_PropertyChangedSizeValidation;
                }
                subSampleRectangle = value;
                if (subSampleRectangle != null) {
                    subSampleRectangle.PropertyChanged += SubSampleRectangle_PropertyChangedSizeValidation;
                }
                RaisePropertyChanged();
            }
        }

        private void SubSampleRectangleMove(object obj) {
            var dragResult = (DragResult)obj;
            if (SubSampleRectangle != null) {
                var mode = dragResult.Mode;
                var delta = dragResult.Delta;
                if (mode == DragMode.Move) {
                    if (SubSampleRectangle.X + delta.X < 0) {
                        delta.X = 0;
                    } else if (SubSampleRectangle.X + SubSampleRectangle.Width + delta.X > CameraInfo.XSize) {
                        delta.X = (int)(CameraInfo.XSize - SubSampleRectangle.Width - SubSampleRectangle.X);
                    }

                    if (SubSampleRectangle.Y + delta.Y < 0) {
                        delta.Y = 0;
                    } else if (SubSampleRectangle.Y + SubSampleRectangle.Height + delta.Y > CameraInfo.YSize) {
                        delta.Y = (int)(CameraInfo.YSize - SubSampleRectangle.Height - SubSampleRectangle.Y);
                    }

                    var x = (int)(SubSampleRectangle.X + delta.X);
                    var y = (int)(SubSampleRectangle.Y + delta.Y);
                    SubSampleRectangle = new ObservableRectangle(x, y, SubSampleRectangle.Width, SubSampleRectangle.Height);
                } else {
                    var x = (int)SubSampleRectangle.X;
                    var y = (int)SubSampleRectangle.Y;
                    var width = (int)SubSampleRectangle.Width;
                    var height = (int)SubSampleRectangle.Height;

                    if (mode == DragMode.Resize_Top_Left) {
                        x += (int)delta.X;
                        y += (int)delta.Y;
                        width -= (int)delta.X;
                        height -= (int)delta.Y;
                    } else if (mode == DragMode.Resize_Top_Right) {
                        y += (int)delta.Y;
                        width += (int)delta.X;
                        height -= (int)delta.Y;
                    } else if (mode == DragMode.Resize_Bottom_Left) {
                        x += (int)delta.X;
                        width -= (int)delta.X;
                        height += (int)delta.Y;
                    } else if (mode == DragMode.Resize_Left) {
                        x += (int)delta.X;
                        width -= (int)delta.X;
                    } else if (mode == DragMode.Resize_Right) {
                        width += (int)delta.X;
                    } else if (mode == DragMode.Resize_Top) {
                        y += (int)delta.Y;
                        height -= (int)delta.Y;
                    } else if (mode == DragMode.Resize_Bottom) {
                        height += (int)delta.Y;
                    } else {
                        width += (int)delta.X;
                        height += (int)delta.Y;
                    }
                    /* Validate and adjust total boundaries */
                    if (x < 0) { x = 0; }
                    if (y < 0) { y = 0; }
                    if (width < 1) { width = 1; }
                    if (height < 1) { height = 1; }
                    if (x >= CameraInfo.XSize) { x = CameraInfo.XSize - 1; }
                    if (y >= CameraInfo.YSize) { y = CameraInfo.YSize - 1; }

                    if (x + width > CameraInfo.XSize) { width = CameraInfo.XSize - x; }
                    if (y + height > CameraInfo.YSize) { height = CameraInfo.YSize - y; }

                    SubSampleRectangle = new ObservableRectangle(x, y, width, height);
                }
            }
        }

        private void SubSampleRectangle_PropertyChangedSizeValidation(object sender, PropertyChangedEventArgs e) {
            SubSampleRectangleMove(new DragResult() { Delta = new System.Windows.Vector(), Mode = DragMode.Move });
        }

        public ICommand SubSampleRectangleMoveCommand { get; private set; }

        public IAsyncCommand StartLiveViewCommand { get; private set; }

        public ApplicationStatus Status {
            get => _status;
            set {
                _status = value;
                _status.Source = Title;
                RaisePropertyChanged();

                applicationStatusMediator.StatusUpdate(_status);
            }
        }

        public ICommand StopLiveViewCommand { get; private set; }

        private void CancelSnapImage(object o) {
            try { _captureImageToken?.Cancel(); } catch { }
        }

        private async Task<bool> StartLiveView() {
            _liveViewCts?.Dispose();
            LiveViewEnabled = true;
            _liveViewCts = new CancellationTokenSource();
            try {
                var seq = new CaptureSequence(SnapExposureDuration, ImageTypes.SNAPSHOT, SnapFilter, SnapBin, 1);
                seq.EnableSubSample = SnapSubSample;
                seq.SubSambleRectangle = SubSampleRectangle;
                seq.Gain = SnapGain;

                await this.imagingMediator.StartLiveView(seq, _liveViewCts.Token);
            } catch (OperationCanceledException) {
            } finally {
                LiveViewEnabled = false;
            }

            return true;
        }

        private void StopLiveView(object o) {
            try { _liveViewCts?.Cancel(); } catch { }
            LiveViewEnabled = false;
        }

        private BitmapSource _image;

        public BitmapSource Image {
            get => _image;
            set {
                _image = value;
                RaisePropertyChanged();
            }
        }

        public async Task<bool> SnapImage(IProgress<ApplicationStatus> progress) {
            _captureImageToken?.Dispose();
            _captureImageToken = new CancellationTokenSource();

            try {
                var success = true;
                if (Loop) IsLooping = true;
                Task<IRenderedImage> prepareTask = null;
                var savedFullImageWhileLooping = false;
                var count = 0;
                do {
                    var seq = new CaptureSequence(SnapExposureDuration, ImageTypes.SNAPSHOT, SnapFilter, SnapBin, 1);
                    seq.EnableSubSample = SnapSubSample;
                    seq.SubSambleRectangle = SubSampleRectangle;
                    seq.Gain = SnapGain;
                    seq.ProgressExposureCount = count;
                    seq.TotalExposureCount = count + 1;

                    var exposureData = await imagingMediator.CaptureImage(seq, _captureImageToken.Token, progress, snapTargetName);
                    count++;

                    if (exposureData == null) {
                        return false;
                    }

                    var imageData = await exposureData.ToImageData(progress, _captureImageToken.Token);

                    if (prepareTask?.Status < TaskStatus.RanToCompletion) {
                        await prepareTask;
                    }
                    prepareTask = imagingMediator.PrepareImage(imageData, new PrepareImageParameters(), _captureImageToken.Token);
                    if (SnapSave) {
                        progress.Report(new ApplicationStatus() { Status = Loc.Instance["LblSavingImage"] });
                        await imageSaveMediator.Enqueue(imageData, prepareTask, progress, _captureImageToken.Token);
                        imageHistoryVM.Add(imageData.MetaData.Image.Id, await imageData.Statistics, ImageTypes.SNAPSHOT);
                    }

                    if (!SnapSubSample && !savedFullImageWhileLooping) {
                        var renderedImage = await prepareTask;
                        Image = renderedImage.Image;
                        savedFullImageWhileLooping = IsLooping;
                    }

                    _captureImageToken.Token.ThrowIfCancellationRequested();
                } while (Loop && success);
            } catch (CameraExposureFailedException) {
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

        public void UpdateDeviceInfo(FilterWheelInfo deviceInfo) {
            FilterWheelInfo = deviceInfo;
        }

        public void UpdateDeviceInfo(CameraInfo cameraStatus) {
            CameraInfo = cameraStatus;
            if (cameraInfo.Connected && SubSampleRectangle == null) {
                SubSampleRectangle = new ObservableRectangle(cameraInfo.SubSampleX, cameraInfo.SubSampleY, cameraInfo.SubSampleWidth, cameraInfo.SubSampleHeight);
            } else if (!cameraInfo.Connected && SubSampleRectangle != null) {
                SnapSubSample = false;
                SubSampleRectangle = null;
            }
        }

        public void Dispose() {
            this.cameraMediator.RemoveConsumer(this);
            this.filterWheelMediator.RemoveConsumer(this);
        }
    }
}