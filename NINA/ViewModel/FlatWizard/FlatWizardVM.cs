#region "copyright"

/*
    Copyright © 2016 - 2018 Stefan Berg <isbeorn86+NINA@googlemail.com>

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
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Profile;
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

namespace NINA.ViewModel.FlatWizard {

    public class FlatWizardVM : DockableVM, IFlatWizardVM {
        private readonly IApplicationStatusMediator applicationStatusMediator;
        private BinningMode binningMode;
        private double calculatedExposureTime;
        private double calculatedHistogramMean;
        private bool cameraConnected;
        private CameraInfo cameraInfo;
        private ObservableCollection<FlatWizardFilterSettingsWrapper> filters = new ObservableCollection<FlatWizardFilterSettingsWrapper>();
        private int flatCount;
        private CancellationTokenSource flatSequenceCts;
        private short gain;
        private BitmapSource image;
        private bool isPaused;
        private int mode;
        private FlatWizardFilterSettingsWrapper singleFlatWizardFilterSettings;
        private ApplicationStatus status;

        public FlatWizardVM(IProfileService profileService,
                            IImagingVM imagingVM,
                            ICameraMediator cameraMediator,
                            IApplicationResourceDictionary resourceDictionary,
                            IApplicationStatusMediator applicationStatusMediator) : base(profileService) {
            Title = "LblFlatWizard";
            ImageGeometry = (System.Windows.Media.GeometryGroup)resourceDictionary["FlatWizardSVG"];

            ImagingVM = imagingVM;

            ImagingVM.SetAutoStretch(false);
            ImagingVM.SetDetectStars(false);

            this.applicationStatusMediator = applicationStatusMediator;

            flatSequenceCts = new CancellationTokenSource();
            var pauseTokenSource = new PauseTokenSource();

            StartFlatSequenceCommand = new AsyncCommand<bool>(() => StartFlatCapture(new Progress<ApplicationStatus>(p => Status = p), pauseTokenSource.Token));
            CancelFlatExposureSequenceCommand = new RelayCommand(CancelFindExposureTime);
            PauseFlatExposureSequenceCommand = new RelayCommand(obj => { IsPaused = true; pauseTokenSource.IsPaused = IsPaused; });
            ResumeFlatExposureSequenceCommand = new RelayCommand(obj => { IsPaused = false; pauseTokenSource.IsPaused = IsPaused; });

            SingleFlatWizardFilterSettings = new FlatWizardFilterSettingsWrapper(null, new FlatWizardFilterSettings {
                HistogramMeanTarget = profileService.ActiveProfile.FlatWizardSettings.HistogramMeanTarget,
                HistogramTolerance = profileService.ActiveProfile.FlatWizardSettings.HistogramTolerance,
                MaxFlatExposureTime = profileService.ActiveProfile.CameraSettings.MaxFlatExposureTime,
                MinFlatExposureTime = profileService.ActiveProfile.CameraSettings.MinFlatExposureTime,
                StepSize = profileService.ActiveProfile.FlatWizardSettings.StepSize
            }, cameraInfo?.BitDepth ?? (int)profileService.ActiveProfile.CameraSettings.BitDepth);

            FlatCount = profileService.ActiveProfile.FlatWizardSettings.FlatCount;
            DarkFlatCount = profileService.ActiveProfile.FlatWizardSettings.DarkFlatCount;
            BinningMode = profileService.ActiveProfile.FlatWizardSettings.BinningMode;

            Filters = new ObservableCollection<FlatWizardFilterSettingsWrapper>();

            profileService.ProfileChanged += (sender, args) => {
                watchedFilterList.CollectionChanged -= FiltersCollectionChanged;
                watchedFilterList = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
                watchedFilterList.CollectionChanged += FiltersCollectionChanged;
                UpdateFilterWheelsSettings();
            };

            watchedFilterList = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
            watchedFilterList.CollectionChanged += FiltersCollectionChanged;
            SingleFlatWizardFilterSettings.Settings.PropertyChanged += UpdateProfileValues;

            // first update filters

            UpdateFilterWheelsSettings();

            // then register consumer and get the cameraInfo so it's populated to all filters including the singleflatwizardfiltersettings

            cameraMediator.RegisterConsumer(this);
        }

        private ObserveAllCollection<FilterInfo> watchedFilterList;

        private enum FilterCaptureMode {
            SINGLE = 0,
            MULTI = 1
        }

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

        public IAsyncCommand FindExposureTimeCommand { get; private set; }

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

        public ApplicationStatus Status {
            get => status;
            set {
                status = value;
                if (status.Source == null) {
                    status.Source = Locale["LblFlatWizardCapture"];
                }

                RaisePropertyChanged();

                applicationStatusMediator.StatusUpdate(status);
            }
        }

        private void CancelFindExposureTime(object obj) {
            flatSequenceCts?.Cancel();
        }

        private async Task<bool> StartFindingExposureTimeSequence(IProgress<ApplicationStatus> progress, CancellationToken ct, PauseToken pt, FlatWizardFilterSettingsWrapper wrapper) {
            var exposureTime = wrapper.Settings.MinFlatExposureTime;
            ImageArray imageArray = null;

            progress.Report(new ApplicationStatus { Status = string.Format(Locale["LblFlatExposureCalcStart"], wrapper.Settings.MinFlatExposureTime), Source = Title });

            var exposureAduState = FlatWizardExposureAduState.ExposureAduBelowMean;

            while (exposureAduState != FlatWizardExposureAduState.ExposureFinished) {
                // check for exposure time state first
                var exposureTimeState = FlatWizardExposureTimeFinderService.GetNextFlatExposureState(exposureTime, wrapper);

                switch (exposureTimeState) {
                    case FlatWizardExposureTimeState.ExposureTimeBelowMinTime:
                        flatSequenceCts.Cancel();
                        Utility.Notification.Notification.ShowWarning(Locale["LblFlatSequenceCancelled"]);
                        ct.ThrowIfCancellationRequested();
                        break;

                    case FlatWizardExposureTimeState.ExposureTimeAboveMaxTime:
                        exposureTime = FlatWizardExposureTimeFinderService.GetNextExposureTime(exposureTime, wrapper);

                        var result = await FlatWizardExposureTimeFinderService.EvaluateUserPromptResultAsync(imageArray, exposureTime, Locale["LblFlatUserPromptFlatTooDim"], wrapper);

                        if (!result.Continue) {
                            flatSequenceCts.Cancel();
                        } else {
                            exposureTime = result.NextExposureTime;
                            progress.Report(new ApplicationStatus() { Status = string.Format(Locale["LblFlatExposureCalcContinue"], imageArray?.Statistics.Mean, exposureTime), Source = Title });
                        }
                        break;
                }

                // capture a flat
                var sequence = new CaptureSequence(exposureTime, "FLAT", wrapper.Filter, BinningMode, 1) { Gain = Gain };

                imageArray = await ImagingVM.CaptureImageWithoutHistoryAndThumbnail(sequence, ct, progress, true);
                Image = await ImageControlVM.StretchAsync(
                    imageArray.Statistics,
                    ImageAnalysis.CreateSourceFromArray(imageArray, System.Windows.Media.PixelFormats.Gray16),
                    profileService.ActiveProfile.ImageSettings.AutoStretchFactor, profileService.ActiveProfile.ImageSettings.BlackClipping);

                // check for exposure ADU state
                exposureAduState = FlatWizardExposureTimeFinderService.GetFlatExposureState(imageArray, exposureTime, wrapper);
                FlatWizardExposureTimeFinderService.AddDataPoint(exposureTime, imageArray.Statistics.Mean);

                switch (exposureAduState) {
                    case FlatWizardExposureAduState.ExposureFinished:
                        CalculatedHistogramMean = imageArray.Statistics.Mean;
                        CalculatedExposureTime = exposureTime;
                        progress.Report(new ApplicationStatus { Status = string.Format(Locale["LblFlatExposureCalcFinished"], CalculatedHistogramMean, CalculatedExposureTime), Source = Title });
                        break;

                    case FlatWizardExposureAduState.ExposureAduBelowMean:
                        exposureTime = FlatWizardExposureTimeFinderService.GetNextExposureTime(exposureTime, wrapper);
                        progress.Report(new ApplicationStatus { Status = string.Format(Locale["LblFlatExposureCalcContinue"], imageArray.Statistics.Mean, exposureTime), Source = Title });
                        break;

                    case FlatWizardExposureAduState.ExposureAduAboveMean:
                        exposureTime = FlatWizardExposureTimeFinderService.GetNextExposureTime(exposureTime, wrapper);

                        var result = await FlatWizardExposureTimeFinderService.EvaluateUserPromptResultAsync(imageArray, exposureTime, Locale["LblFlatUserPromptFlatTooBright"], wrapper);

                        if (!result.Continue) {
                            flatSequenceCts.Cancel();
                        } else {
                            exposureTime = result.NextExposureTime;
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

        private async Task<bool> StartFlatCapture(IProgress<ApplicationStatus> progress, PauseToken pt) {
            if (flatSequenceCts.IsCancellationRequested) {
                flatSequenceCts = new CancellationTokenSource();
            }

            Dictionary<FlatWizardFilterSettingsWrapper, double> filterToExposureTime = new Dictionary<FlatWizardFilterSettingsWrapper, double>();
            CaptureSequence captureSequence;

            try {
                if ((FilterCaptureMode)Mode == FilterCaptureMode.SINGLE) {
                    await StartFindingExposureTimeSequence(progress, flatSequenceCts.Token, pt, SingleFlatWizardFilterSettings);
                    captureSequence = new CaptureSequence(CalculatedExposureTime, CaptureSequence.ImageTypes.FLAT, SingleFlatWizardFilterSettings.Filter, BinningMode, FlatCount) { Gain = Gain };
                    await StartCaptureSequence(captureSequence, progress, flatSequenceCts.Token, pt);
                    filterToExposureTime.Add(SingleFlatWizardFilterSettings, CalculatedExposureTime);
                } else {
                    foreach (var filterSettings in Filters.Where(f => f.IsChecked)) {
                        await StartFindingExposureTimeSequence(progress, flatSequenceCts.Token, pt, filterSettings);
                        captureSequence = new CaptureSequence(CalculatedExposureTime, CaptureSequence.ImageTypes.FLAT, filterSettings.Filter, BinningMode, FlatCount) { Gain = Gain };
                        await StartCaptureSequence(captureSequence, progress, flatSequenceCts.Token, pt);
                        filterToExposureTime.Add(filterSettings, CalculatedExposureTime);
                        filterSettings.IsChecked = false;
                        CalculatedExposureTime = 0;
                        CalculatedHistogramMean = 0;
                    }
                }
            } catch (OperationCanceledException) {
            } finally {
                CalculatedExposureTime = 0;
                CalculatedHistogramMean = 0;
                FlatWizardExposureTimeFinderService.ClearDataPoints();
            }

            try {
                if (filterToExposureTime.Count > 0 && DarkFlatCount > 0) {
                    progress.Report(new ApplicationStatus() { Status = Locale["LblPreparingDarkFlatSequence"], Source = Title });
                    var dialogResult = MyMessageBox.MyMessageBox.Show(
                        Locale["LblCoverScopeMsgBox"],
                        Locale["LblCoverScopeMsgBoxTitle"], MessageBoxButton.OKCancel, MessageBoxResult.OK);
                    if (dialogResult == MessageBoxResult.OK) {
                        flatSequenceCts = new CancellationTokenSource();
                        foreach (var kvp in filterToExposureTime) {
                            captureSequence = new CaptureSequence(kvp.Value, CaptureSequence.ImageTypes.DARKFLAT, kvp.Key.Filter, BinningMode, DarkFlatCount) { Gain = Gain };
                            await StartCaptureSequence(captureSequence, progress, flatSequenceCts.Token, pt);
                        }
                    }
                }
            } catch (OperationCanceledException) {
            }

            ImagingVM.DestroyImage();
            Image = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();

            progress.Report(new ApplicationStatus());
            progress.Report(new ApplicationStatus() { Source = Title });

            return true;
        }

        private async Task<bool> StartCaptureSequence(CaptureSequence sequence, IProgress<ApplicationStatus> progress,
            CancellationToken ct, PauseToken pt) {
            while (sequence.ProgressExposureCount < sequence.TotalExposureCount) {
                if (sequence.ProgressExposureCount != sequence.TotalExposureCount - 1) {
                    await ImagingVM.CaptureImageWithoutProcessingAndSaveAsync(sequence, ct, progress);
                } else {
                    await ImagingVM.CaptureImageWithoutProcessingAndSaveSync(sequence, ct, progress);
                }

                sequence.ProgressExposureCount++;

                await WaitWhilePaused(progress, pt, ct);

                ct.ThrowIfCancellationRequested();
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
                foreach (var filter in Filters) {
                    filter.BitDepth = cameraInfo.BitDepth;
                }
            }
        }
    }
}