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

using NINA.Locale;
using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyTelescope;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Enum;
using NINA.Utility.Mediator.Interfaces;
using NINA.Profile;
using NINA.ViewModel.Interfaces;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using NINA.Model.ImageData;
using NINA.Model.MyFlatDevice;
using NINA.Utility.Notification;
using NINA.Utility.Mediator;
using NINA.ViewModel.Equipment.FlatDevice;

namespace NINA.ViewModel.FlatWizard {

    public class FlatWizardVM : DockableVM, IFlatWizardVM {
        private readonly IApplicationStatusMediator applicationStatusMediator;
        private BinningMode binningMode;
        private double calculatedExposureTime;
        private double calculatedHistogramMean;
        private bool cameraConnected;
        private CameraInfo cameraInfo;
        private FilterWheelInfo filterWheelInfo;
        private ObservableCollection<FlatWizardFilterSettingsWrapper> filters = new ObservableCollection<FlatWizardFilterSettingsWrapper>();
        private int flatCount;
        private CancellationTokenSource flatSequenceCts;
        private short gain;
        private BitmapSource image;
        private bool isPaused;
        private int mode;
        private FlatWizardFilterSettingsWrapper singleFlatWizardFilterSettings;
        private ApplicationStatus status;
        private bool pauseBetweenFilters;
        private IFlatDeviceVM _flatDeviceVM;
        private IFlatDeviceMediator _flatDeviceMediator;
        private FlatDeviceInfo _flatDevice;

        public FlatWizardVM(IProfileService profileService,
                            IImagingVM imagingVM,
                            ICameraMediator cameraMediator,
                            IFilterWheelMediator filterWheelMediator,
                            ITelescopeMediator telescopeMediator,
                            IFlatDeviceVM flatDeviceVM,
                            IFlatDeviceMediator flatDeviceMediator,
                            IApplicationResourceDictionary resourceDictionary,
                            IApplicationStatusMediator applicationStatusMediator) : base(profileService) {
            Title = "LblFlatWizard";
            ImageGeometry = (System.Windows.Media.GeometryGroup)resourceDictionary["FlatWizardSVG"];

            ImagingVM = imagingVM;

            this.applicationStatusMediator = applicationStatusMediator;

            flatSequenceCts?.Dispose();
            flatSequenceCts = new CancellationTokenSource();
            var pauseTokenSource = new PauseTokenSource();

            StartFlatSequenceCommand = new AsyncCommand<bool>(
                () => StartSingleFlatCapture(new Progress<ApplicationStatus>(p => Status = p), pauseTokenSource.Token),
                (object o) => cameraInfo.Connected
            );
            StartMultiFlatSequenceCommand = new AsyncCommand<bool>(
                () => StartMultiFlatCapture(new Progress<ApplicationStatus>(p => Status = p), pauseTokenSource.Token),
                (object o) => cameraInfo.Connected && filterWheelInfo.Connected
            );
            SlewToZenithCommand = new AsyncCommand<bool>(
                () => SlewToZenith(),
                (object o) => telescopeInfo.Connected
            );

            CancelFlatExposureSequenceCommand = new RelayCommand(CancelFindExposureTime);
            PauseFlatExposureSequenceCommand = new RelayCommand(obj => { IsPaused = true; pauseTokenSource.IsPaused = IsPaused; });
            ResumeFlatExposureSequenceCommand = new RelayCommand(obj => { IsPaused = false; pauseTokenSource.IsPaused = IsPaused; });

            FlatCount = profileService.ActiveProfile.FlatWizardSettings.FlatCount;
            DarkFlatCount = profileService.ActiveProfile.FlatWizardSettings.DarkFlatCount;
            BinningMode = profileService.ActiveProfile.FlatWizardSettings.BinningMode;

            Filters = new ObservableCollection<FlatWizardFilterSettingsWrapper>();

            profileService.ProfileChanged += (sender, args) => {
                UpdateSingleFlatWizardFilterSettings(profileService);
                watchedFilterList.CollectionChanged -= FiltersCollectionChanged;
                watchedFilterList = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
                watchedFilterList.CollectionChanged += FiltersCollectionChanged;
                UpdateFilterWheelsSettings();
            };

            watchedFilterList = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
            watchedFilterList.CollectionChanged += FiltersCollectionChanged;

            // first update filters

            UpdateSingleFlatWizardFilterSettings(profileService);
            UpdateFilterWheelsSettings();

            // then register consumer and get the cameraInfo so it's populated to all filters including the singleflatwizardfiltersettings
            this.cameraMediator = cameraMediator;
            cameraMediator.RegisterConsumer(this);
            this.filterWheelMediator = filterWheelMediator;
            filterWheelMediator.RegisterConsumer(this);
            this.telescopeMediator = telescopeMediator;
            this.telescopeMediator.RegisterConsumer(this);

            // register the flat panel mediator and VM
            _flatDeviceVM = flatDeviceVM;
            _flatDeviceMediator = flatDeviceMediator;
            _flatDeviceMediator.RegisterConsumer(this);
        }

        public void Dispose() {
            flatSequenceCts?.Dispose();
            ImagingVM.Dispose();
            this.cameraMediator.RemoveConsumer(this);
            this.filterWheelMediator.RemoveConsumer(this);
            this.telescopeMediator.RemoveConsumer(this);
            this._flatDeviceMediator.RemoveConsumer(this);
        }

        private AltitudeSite altitudeSite;

        public AltitudeSite AltitudeSite {
            get => altitudeSite;
            set {
                altitudeSite = value;
                RaisePropertyChanged();
            }
        }

        private Task<bool> SlewToZenith() {
            var latitude = Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Latitude);
            var longitude = Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Longitude);
            var azimuth = AltitudeSite == AltitudeSite.EAST ? Angle.ByDegree(90) : Angle.ByDegree(270);
            return telescopeMediator.SlewToCoordinatesAsync(new TopocentricCoordinates(azimuth, Angle.ByDegree(89), latitude, longitude));
        }

        private void UpdateSingleFlatWizardFilterSettings(IProfileService profileService) {
            if (SingleFlatWizardFilterSettings != null) {
                SingleFlatWizardFilterSettings.Settings.PropertyChanged -= UpdateProfileValues;
            }

            SingleFlatWizardFilterSettings = new FlatWizardFilterSettingsWrapper(null, new FlatWizardFilterSettings {
                HistogramMeanTarget = profileService.ActiveProfile.FlatWizardSettings.HistogramMeanTarget,
                HistogramTolerance = profileService.ActiveProfile.FlatWizardSettings.HistogramTolerance,
                MaxFlatExposureTime = profileService.ActiveProfile.CameraSettings.MaxFlatExposureTime,
                MinFlatExposureTime = profileService.ActiveProfile.CameraSettings.MinFlatExposureTime,
                StepSize = profileService.ActiveProfile.FlatWizardSettings.StepSize
            }, cameraInfo?.BitDepth ?? (int)profileService.ActiveProfile.CameraSettings.BitDepth);
            SingleFlatWizardFilterSettings.Settings.PropertyChanged += UpdateProfileValues;
        }

        private ObserveAllCollection<FilterInfo> watchedFilterList;

        public BinningMode BinningMode {
            get => binningMode;
            set {
                binningMode = value;
                if (binningMode != profileService.ActiveProfile.FlatWizardSettings.BinningMode) {
                    profileService.ActiveProfile.FlatWizardSettings.BinningMode = binningMode;
                }

                RaisePropertyChanged();
            }
        }

        public double CalculatedExposureTime {
            get => calculatedExposureTime;
            set {
                calculatedExposureTime = value;
                RaisePropertyChanged();
            }
        }

        public double CalculatedHistogramMean {
            get => calculatedHistogramMean;
            set {
                calculatedHistogramMean = value;
                RaisePropertyChanged();
            }
        }

        public bool CameraConnected {
            get => cameraConnected;
            set {
                cameraConnected = value;
                RaisePropertyChanged();
            }
        }

        public RelayCommand CancelFlatExposureSequenceCommand { get; }
        public ObservableCollection<FilterInfo> FilterInfos => new ObservableCollection<FilterInfo>(Filters.Select(f => f.Filter).ToList());

        public ObservableCollection<FlatWizardFilterSettingsWrapper> Filters {
            get => filters;
            set {
                filters = value;
                RaisePropertyChanged();
            }
        }

        public int FlatCount {
            get => flatCount;
            set {
                flatCount = value;
                if (flatCount != profileService.ActiveProfile.FlatWizardSettings.FlatCount) {
                    profileService.ActiveProfile.FlatWizardSettings.FlatCount = flatCount;
                }

                RaisePropertyChanged();
            }
        }

        private int darkFlatCount;

        public int DarkFlatCount {
            get => darkFlatCount;
            set {
                darkFlatCount = value;
                if (darkFlatCount != profileService.ActiveProfile.FlatWizardSettings.DarkFlatCount) {
                    profileService.ActiveProfile.FlatWizardSettings.DarkFlatCount = darkFlatCount;
                }
                RaisePropertyChanged();
            }
        }

        public IFlatWizardExposureTimeFinderService FlatWizardExposureTimeFinderService { get; set; } = new FlatWizardExposureTimeFinderService();

        public short Gain {
            get => gain;
            set {
                gain = value;
                RaisePropertyChanged();
            }
        }

        public BitmapSource Image {
            get => image;
            set {
                image = value;
                RaisePropertyChanged();
            }
        }

        public IImagingVM ImagingVM { get; }

        public bool IsPaused {
            get => isPaused;
            set {
                isPaused = value;
                RaisePropertyChanged();
            }
        }

        public ILoc Locale { get; set; } = Loc.Instance;

        public int Mode {
            get => mode;
            set {
                mode = value;
                RaisePropertyChanged();
            }
        }

        public RelayCommand PauseFlatExposureSequenceCommand { get; }

        public RelayCommand ResumeFlatExposureSequenceCommand { get; }

        public FilterInfo SelectedFilter {
            get => singleFlatWizardFilterSettings.Filter;
            set {
                singleFlatWizardFilterSettings.Filter = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(SingleFlatWizardFilterSettings));
            }
        }

        public FlatWizardFilterSettingsWrapper SingleFlatWizardFilterSettings {
            get => singleFlatWizardFilterSettings;
            set {
                singleFlatWizardFilterSettings = value;
                RaisePropertyChanged();
            }
        }

        public IAsyncCommand StartFlatSequenceCommand { get; }
        public IAsyncCommand StartMultiFlatSequenceCommand { get; }
        public IAsyncCommand SlewToZenithCommand { get; }

        private ApplicationStatus prevStatus;

        public ApplicationStatus Status {
            get => status;
            set {
                status = value;
                if (status.Source == null) {
                    status.Source = Locale["LblFlatWizardCapture"];
                } else if (status.Source == Title) {
                    if (prevStatus != null) {
                        if (string.IsNullOrWhiteSpace(status.Status) && (status.Status2 != null || status.Status3 != null)) {
                            status.Status = prevStatus.Status;
                        }

                        if (status.Status2 == null && prevStatus.Status2 != null) {
                            status.Status2 = prevStatus.Status2;
                            status.Progress2 = prevStatus.Progress2;
                            status.MaxProgress2 = prevStatus.MaxProgress2;
                            status.ProgressType2 = prevStatus.ProgressType2;
                        }
                        if (status.Status3 == null && prevStatus.Status3 != null) {
                            status.Status3 = prevStatus.Status3;
                            status.Progress3 = prevStatus.Progress3;
                            status.MaxProgress3 = prevStatus.MaxProgress3;
                            status.ProgressType3 = prevStatus.ProgressType3;
                        }
                    }
                    prevStatus = status;
                }

                RaisePropertyChanged();

                applicationStatusMediator.StatusUpdate(status);
            }
        }

        public bool PauseBetweenFilters {
            get => pauseBetweenFilters;
            set {
                pauseBetweenFilters = value;
                RaisePropertyChanged();
            }
        }

        private void CancelFindExposureTime(object obj) {
            flatSequenceCts?.Cancel();
        }

        private async Task<bool> StartFindingExposureTimeSequence(IProgress<ApplicationStatus> progress, CancellationToken ct, PauseToken pt, FlatWizardFilterSettingsWrapper wrapper) {
            var exposureTime = wrapper.Settings.MinFlatExposureTime;
            IRenderedImage renderedImage = null;

            if (_flatDevice != null && _flatDevice.Connected) {
                _flatDeviceVM.Brightness = 1.0;
                _flatDeviceVM.SetBrightnessCommand.Execute(null);
            }

            progress.Report(new ApplicationStatus { Status = string.Format(Locale["LblFlatExposureCalcStart"], wrapper.Settings.MinFlatExposureTime), Source = Title });

            var exposureAduState = FlatWizardExposureAduState.ExposureAduBelowMean;

            while (exposureAduState != FlatWizardExposureAduState.ExposureFinished) {
                // check for exposure time state first
                var exposureTimeState = FlatWizardExposureTimeFinderService.GetNextFlatExposureState(exposureTime, wrapper);

                switch (exposureTimeState) {
                    case FlatWizardExposureTimeState.ExposureTimeBelowMinTime:
                        flatSequenceCts.Cancel();
                        Notification.ShowWarning(Locale["LblFlatSequenceCancelled"]);
                        ct.ThrowIfCancellationRequested();
                        break;

                    case FlatWizardExposureTimeState.ExposureTimeAboveMaxTime:
                        exposureTime = FlatWizardExposureTimeFinderService.GetNextExposureTime(exposureTime, wrapper);
                        if (renderedImage == null) {
                            flatSequenceCts.Cancel();
                            Notification.ShowWarning(Locale["LblFlatSequenceCancelled"]);
                            ct.ThrowIfCancellationRequested();
                            break;
                        }

                        var result = await FlatWizardExposureTimeFinderService.EvaluateUserPromptResultAsync(renderedImage.RawImageData, exposureTime, Locale["LblFlatUserPromptFlatTooDim"], wrapper);
                        var stats = await renderedImage.RawImageData.Statistics.Task;

                        if (!result.Continue) {
                            flatSequenceCts.Cancel();
                        } else {
                            exposureTime = result.NextExposureTime;
                            progress.Report(new ApplicationStatus() { Status = string.Format(Locale["LblFlatExposureCalcContinue"], Math.Round(stats.Mean), exposureTime), Source = Title });
                        }
                        break;
                }

                // capture a flat
                var sequence = new CaptureSequence(exposureTime, "FLAT", wrapper.Filter, BinningMode, 1) { Gain = Gain };

                var prepareParameters = new PrepareImageParameters(autoStretch: false, detectStars: false);
                renderedImage = await ImagingVM.CaptureAndPrepareImage(sequence, prepareParameters, ct, progress);
                renderedImage = await renderedImage.Stretch(
                    factor: profileService.ActiveProfile.ImageSettings.AutoStretchFactor,
                    blackClipping: profileService.ActiveProfile.ImageSettings.BlackClipping,
                    unlinked: false);
                Image = renderedImage.Image;

                // check for exposure ADU state
                exposureAduState = await FlatWizardExposureTimeFinderService.GetFlatExposureState(renderedImage.RawImageData, exposureTime, wrapper);
                var imageStatistics = await renderedImage.RawImageData.Statistics.Task;
                FlatWizardExposureTimeFinderService.AddDataPoint(exposureTime, imageStatistics.Mean);

                switch (exposureAduState) {
                    case FlatWizardExposureAduState.ExposureFinished:
                        CalculatedHistogramMean = imageStatistics.Mean;
                        CalculatedExposureTime = exposureTime;
                        progress.Report(new ApplicationStatus { Status = string.Format(Locale["LblFlatExposureCalcFinished"], Math.Round(CalculatedHistogramMean, 2), CalculatedExposureTime), Source = Title });
                        break;

                    case FlatWizardExposureAduState.ExposureAduBelowMean:
                        exposureTime = FlatWizardExposureTimeFinderService.GetNextExposureTime(exposureTime, wrapper);
                        progress.Report(new ApplicationStatus { Status = string.Format(Locale["LblFlatExposureCalcContinue"], Math.Round(imageStatistics.Mean, 2), exposureTime), Source = Title });
                        break;

                    case FlatWizardExposureAduState.ExposureAduAboveMean:
                        if (_flatDevice != null && _flatDevice.Connected && _flatDeviceVM.Brightness >= 0.05) {
                            _flatDeviceVM.Brightness /= 2.0;
                            _flatDeviceVM.SetBrightnessCommand.Execute(null);
                            exposureTime = wrapper.Settings.MinFlatExposureTime;
                            FlatWizardExposureTimeFinderService.ClearDataPoints();
                        } else {
                            exposureTime =
                                FlatWizardExposureTimeFinderService.GetNextExposureTime(exposureTime, wrapper);

                            var result = await FlatWizardExposureTimeFinderService.EvaluateUserPromptResultAsync(
                                renderedImage.RawImageData, exposureTime, Locale["LblFlatUserPromptFlatTooBright"],
                                wrapper);

                            if (!result.Continue) {
                                flatSequenceCts.Cancel();
                            } else {
                                if (_flatDevice != null && _flatDevice.Connected) {
                                    _flatDeviceVM.Brightness = 1.0;
                                    _flatDeviceVM.SetBrightnessCommand.Execute(null);
                                }

                                exposureTime = result.NextExposureTime;
                            }
                        }

                        break;
                }

                await WaitWhilePaused(progress, pt, ct);

                // collect garbage to reduce ram usage
                GC.Collect();
                GC.WaitForPendingFinalizers();
                // throw a cancellation if user requested a cancel as well
                ct.ThrowIfCancellationRequested();
            }

            return true;
        }

        private Task<bool> StartMultiFlatCapture(IProgress<ApplicationStatus> progress, PauseToken pt) {
            return StartFlatMagic(Filters.Where(f => f.IsChecked), progress, pt);
        }

        private Task<bool> StartSingleFlatCapture(IProgress<ApplicationStatus> progress, PauseToken pt) {
            return StartFlatMagic(new List<FlatWizardFilterSettingsWrapper>() { SingleFlatWizardFilterSettings }, progress, pt);
        }

        private async Task<bool> StartFlatMagic(IEnumerable<FlatWizardFilterSettingsWrapper> filters, IProgress<ApplicationStatus> progress, PauseToken pt) {
            try {
                if (flatSequenceCts.IsCancellationRequested) {
                    flatSequenceCts?.Dispose();
                    flatSequenceCts = new CancellationTokenSource();
                }
                var filterCount = 0;
                progress.Report(new ApplicationStatus() {
                    Status2 = string.Empty,
                    Progress2 = 0,
                    MaxProgress2 = 0,
                    ProgressType2 = ApplicationStatus.StatusProgressType.ValueOfMaxValue,
                    Source = Title
                });

                if (_flatDevice != null && _flatDevice.Connected && _flatDevice.SupportsOpenClose) { await _flatDeviceVM.CloseCover(); }
                if (_flatDevice != null && _flatDevice.Connected) { _flatDeviceVM.ToggleLightCommand.Execute((object)true); }

                var totalFilterCount = filters.Count();
                foreach (var filterSettings in filters) {
                    filterCount++;
                    var filterName = filterSettings?.Filter?.Name ?? string.Empty;

                    if (PauseBetweenFilters) {
                        var dialogResult = MyMessageBox.MyMessageBox.Show(
                            string.Format(Locale["LblPrepFlatFilterMsgBox"], filterName),
                            Locale["LblFlatWizard"], MessageBoxButton.OKCancel, MessageBoxResult.OK);
                        if (dialogResult == MessageBoxResult.Cancel)
                            throw new OperationCanceledException();
                    }

                    progress.Report(new ApplicationStatus() {
                        Status2 = $"{Locale["LblFilter"]} {filterName}",
                        Progress2 = filterCount,
                        MaxProgress2 = totalFilterCount,
                        ProgressType2 = ApplicationStatus.StatusProgressType.ValueOfMaxValue,
                        Source = Title
                    });

                    await FindExposureTimesAndTakeImages(filterSettings, progress, pt);
                }

                await TakeDarkFlats(progress, pt);
            } catch (Exception ex) {
                Logger.Error(ex);
                return false;
            } finally {
                Cleanup(progress);
                if (_flatDevice != null && _flatDevice.Connected) { _flatDeviceVM.ToggleLightCommand.Execute((object)false); }
            }

            return true;
        }

        private Dictionary<FlatWizardFilterSettingsWrapper, double> filterToExposureTime = new Dictionary<FlatWizardFilterSettingsWrapper, double>();
        private IFilterWheelMediator filterWheelMediator;
        private ICameraMediator cameraMediator;
        private ITelescopeMediator telescopeMediator;
        private TelescopeInfo telescopeInfo;

        private async Task<bool> FindExposureTimesAndTakeImages(FlatWizardFilterSettingsWrapper filterSettings, IProgress<ApplicationStatus> progress, PauseToken pt) {
            try {
                progress.Report(new ApplicationStatus() {
                    Status3 = Locale["LblExposures"],
                    Progress3 = 0,
                    MaxProgress3 = 0,
                    ProgressType3 = ApplicationStatus.StatusProgressType.ValueOfMaxValue,
                    Source = Title
                });
                await StartFindingExposureTimeSequence(progress, flatSequenceCts.Token, pt, filterSettings);
                filterToExposureTime.Add(filterSettings, CalculatedExposureTime);
                for (var i = 0; i < FlatCount; i++) {
                    progress.Report(new ApplicationStatus() {
                        Status3 = Locale["LblExposures"],
                        Progress3 = i,
                        MaxProgress3 = FlatCount,
                        ProgressType3 = ApplicationStatus.StatusProgressType.ValueOfMaxValue,
                        Source = Title
                    });

                    var captureSequence = new CaptureSequence(CalculatedExposureTime, CaptureSequence.ImageTypes.FLAT, filterSettings.Filter, BinningMode, 1) { Gain = Gain };
                    await StartCaptureSequence(captureSequence, progress, flatSequenceCts.Token, pt);
                }
                filterSettings.IsChecked = false;
                CalculatedExposureTime = 0;
                CalculatedHistogramMean = 0;
            } catch (OperationCanceledException) {
            } finally {
                CalculatedExposureTime = 0;
                CalculatedHistogramMean = 0;
                FlatWizardExposureTimeFinderService.ClearDataPoints();
            }
            return true;
        }

        private async Task TakeDarkFlats(IProgress<ApplicationStatus> progress, PauseToken pt) {
            if (filterToExposureTime.Count > 0 && DarkFlatCount > 0) {
                progress.Report(new ApplicationStatus() { Status = Locale["LblPreparingDarkFlatSequence"], Source = Title });
                if (_flatDevice != null && _flatDevice.Connected && _flatDevice.SupportsOpenClose) { await _flatDeviceVM.OpenCover(); }
                var dialogResult = MyMessageBox.MyMessageBox.Show(
                    Locale["LblCoverScopeMsgBox"],
                    Locale["LblCoverScopeMsgBoxTitle"], MessageBoxButton.OKCancel, MessageBoxResult.OK);
                if (dialogResult == MessageBoxResult.OK) {
                    progress.Report(new ApplicationStatus() {
                        Status2 = string.Empty,
                        Progress2 = 0,
                        MaxProgress2 = 0,
                        ProgressType2 = ApplicationStatus.StatusProgressType.ValueOfMaxValue,
                        Source = Title
                    });

                    var filterCount = 0;
                    var totalFilterCount = filterToExposureTime.Count;
                    foreach (var kvp in filterToExposureTime) {
                        filterCount++;
                        var filterName = kvp.Key.Filter?.Name ?? string.Empty;
                        progress.Report(new ApplicationStatus() {
                            Status2 = $"{Locale["LblFilter"]} {filterName}",
                            Progress2 = filterCount,
                            MaxProgress2 = totalFilterCount,
                            ProgressType2 = ApplicationStatus.StatusProgressType.ValueOfMaxValue,
                            Source = Title
                        });
                        await TakeDarkFlatsForFilter(kvp.Value, kvp.Key.Filter, progress, pt);
                    }
                }
            }
        }

        private async Task TakeDarkFlatsForFilter(double exposureTime, FilterInfo filter, IProgress<ApplicationStatus> progress, PauseToken pt) {
            progress.Report(new ApplicationStatus() {
                Status3 = Locale["LblExposures"],
                Progress3 = 0,
                MaxProgress3 = 0,
                ProgressType3 = ApplicationStatus.StatusProgressType.ValueOfMaxValue,
                Source = Title
            });
            for (var i = 0; i < DarkFlatCount; i++) {
                progress.Report(new ApplicationStatus() {
                    Status3 = Locale["LblExposures"],
                    Progress3 = i,
                    MaxProgress3 = DarkFlatCount,
                    ProgressType3 = ApplicationStatus.StatusProgressType.ValueOfMaxValue,
                    Source = Title
                });
                var captureSequence = new CaptureSequence(exposureTime, CaptureSequence.ImageTypes.DARKFLAT, filter, BinningMode, 1) { Gain = Gain };
                await StartCaptureSequence(captureSequence, progress, flatSequenceCts.Token, pt);
            }
        }

        private void Cleanup(IProgress<ApplicationStatus> progress) {
            filterToExposureTime.Clear();
            ImagingVM.DestroyImage();
            Image = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();

            progress.Report(new ApplicationStatus());
            progress.Report(new ApplicationStatus() { Source = Title });
            FlatWizardExposureTimeFinderService.ClearDataPoints();
        }

        private async Task<bool> StartCaptureSequence(CaptureSequence sequence, IProgress<ApplicationStatus> progress,
            CancellationToken ct, PauseToken pt) {
            Task saveTask = null;
            while (sequence.ProgressExposureCount < sequence.TotalExposureCount) {
                var exposureData = await ImagingVM.CaptureImage(sequence, ct, progress);
                var imageData = await exposureData.ToImageData();
                var prepareParameters = new PrepareImageParameters(autoStretch: false, detectStars: false);
                var prepareTask = ImagingVM.PrepareImage(imageData, prepareParameters, ct);

                if (saveTask != null && !saveTask.IsCompleted) {
                    progress.Report(new ApplicationStatus() { Status = Locale["LblWaitForImageSaving"] });
                    await saveTask;
                }

                saveTask = imageData.SaveToDisk(
                    profileService.ActiveProfile.ImageFileSettings.FilePath,
                    profileService.ActiveProfile.ImageFileSettings.FilePattern,
                    profileService.ActiveProfile.ImageFileSettings.FileType,
                    ct
                );

                await prepareTask;

                sequence.ProgressExposureCount++;

                await WaitWhilePaused(progress, pt, ct);

                ct.ThrowIfCancellationRequested();
            }
            if (saveTask != null && !saveTask.IsCompleted) {
                progress.Report(new ApplicationStatus() { Status = Locale["LblWaitForImageSaving"] });
                await saveTask;
            }

            return true;
        }

        private void FiltersCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            if (watchedFilterList.Count != filters.Count) {
                UpdateFilterWheelsSettings();
            }
        }

        private void UpdateFilterWheelsSettings() {
            using (MyStopWatch.Measure()) {
                var selectedFilter = SelectedFilter;
                var newList = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters
                    .Select(s => new FlatWizardFilterSettingsWrapper(s, s.FlatWizardFilterSettings, cameraInfo?.BitDepth ?? (int)profileService.ActiveProfile.CameraSettings.BitDepth)).ToList();
                var tempList = new FlatWizardFilterSettingsWrapper[Filters.Count];
                Filters.CopyTo(tempList, 0);
                foreach (var item in tempList) {
                    var newListItem = newList.SingleOrDefault(f => f.Filter.Name == item.Filter.Name);
                    Filters[Filters.IndexOf(item)] = newListItem;
                    newList.Remove(newListItem);
                }

                foreach (var item in newList) {
                    Filters.Add(item);
                }

                while (Filters.Contains(null)) {
                    Filters.Remove(null);
                }

                if (selectedFilter != null) {
                    SelectedFilter = Filters.FirstOrDefault(f => f.Filter.Name == selectedFilter.Name)?.Filter;
                }

                RaisePropertyChanged(nameof(Filters));
                RaisePropertyChanged(nameof(FilterInfos));
            }
        }

        private void UpdateProfileValues(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (profileService.ActiveProfile.FlatWizardSettings.HistogramMeanTarget != SingleFlatWizardFilterSettings.Settings.HistogramMeanTarget) {
                profileService.ActiveProfile.FlatWizardSettings.HistogramMeanTarget = SingleFlatWizardFilterSettings.Settings.HistogramMeanTarget;
            }

            if (profileService.ActiveProfile.FlatWizardSettings.HistogramTolerance != SingleFlatWizardFilterSettings.Settings.HistogramTolerance) {
                profileService.ActiveProfile.FlatWizardSettings.HistogramTolerance = SingleFlatWizardFilterSettings.Settings.HistogramTolerance;
            }

            if (profileService.ActiveProfile.CameraSettings.MaxFlatExposureTime != SingleFlatWizardFilterSettings.Settings.MaxFlatExposureTime) {
                profileService.ActiveProfile.CameraSettings.MaxFlatExposureTime = SingleFlatWizardFilterSettings.Settings.MaxFlatExposureTime;
            }

            if (profileService.ActiveProfile.CameraSettings.MinFlatExposureTime != SingleFlatWizardFilterSettings.Settings.MinFlatExposureTime) {
                profileService.ActiveProfile.CameraSettings.MinFlatExposureTime = SingleFlatWizardFilterSettings.Settings.MinFlatExposureTime;
            }

            if (profileService.ActiveProfile.FlatWizardSettings.StepSize != SingleFlatWizardFilterSettings.Settings.StepSize) {
                profileService.ActiveProfile.FlatWizardSettings.StepSize = SingleFlatWizardFilterSettings.Settings.StepSize;
            }
        }

        private async Task<PauseToken> WaitWhilePaused(IProgress<ApplicationStatus> progress, PauseToken pt, CancellationToken ct) {
            if (pt.IsPaused) {
                // if paused we'll wait until user unpaused here
                IsPaused = true;
                progress.Report(new ApplicationStatus() { Status = Locale["LblPaused"], Source = Title });
                await pt.WaitWhilePausedAsync(ct);
                IsPaused = false;
            }

            return pt;
        }

        public void UpdateDeviceInfo(CameraInfo deviceInfo) {
            var prevBitDepth = cameraInfo?.BitDepth ?? 0;
            cameraInfo = deviceInfo;
            CameraConnected = cameraInfo.Connected;

            if (prevBitDepth != cameraInfo.BitDepth) {
                SingleFlatWizardFilterSettings.BitDepth = cameraInfo.BitDepth;
                foreach (var filter in Filters) {
                    filter.BitDepth = cameraInfo.BitDepth;
                }
            }
        }

        public void UpdateDeviceInfo(FilterWheelInfo deviceInfo) {
            this.filterWheelInfo = deviceInfo;
        }

        public void UpdateDeviceInfo(TelescopeInfo deviceInfo) {
            this.telescopeInfo = deviceInfo;
        }

        public void UpdateDeviceInfo(FlatDeviceInfo deviceInfo) {
            this._flatDevice = deviceInfo;
        }
    }
}