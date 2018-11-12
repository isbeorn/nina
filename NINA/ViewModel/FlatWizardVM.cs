using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Profile;
using Nito.AsyncEx;
using OxyPlot;

namespace NINA.ViewModel {

    internal class FlatWizardVM : DockableVM, ICameraConsumer, IFilterWheelConsumer {
        private readonly IApplicationStatusMediator applicationStatusMediator;
        private readonly ICameraMediator cameraMediator;
        private readonly IFilterWheelMediator filterWheelMediator;
        private readonly IFocuserMediator focuserMediator;
        private readonly IImagingMediator imagingMediator;
        private bool cameraConnected;

        private int flatCount;

        private double meanTarget;

        private double minExposureTime;

        private double tolerance;

        private double calculatedExposureTime;

        private double calculatedHistogramMean;

        private double maxExposureTime;

        private double stepSize;

        private bool isPaused = false;

        private BinningMode binningMode;

        private CancellationTokenSource findExposureCancelToken;
        private PauseTokenSource pauseTokenSource;

        private BitmapSource image;
        private ApplicationStatus _status;

        public IAsyncCommand FindExposureTimeCommand { get; private set; }
        public RelayCommand CancelFlatExposureSequenceCommand { get; }
        public RelayCommand PauseFlatExposureSequenceCommand { get; }
        public RelayCommand ResumeFlatExposureSequenceCommand { get; }
        public IAsyncCommand StartFlatSequenceCommand { get; private set; }

        public FlatWizardVM(IProfileService profileService,
                            ICameraMediator cameraMediator,
                            IFilterWheelMediator filterWheelMediator,
                            IImagingMediator imagingMediator,
                            IFocuserMediator focuserMediator,
                            IApplicationStatusMediator applicationStatusMediator) : base(profileService) {
            Title = "LblFlatsWizard";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["ContrastSVG"];

            this.cameraMediator = cameraMediator;
            this.cameraMediator.RegisterConsumer(this);

            this.filterWheelMediator = filterWheelMediator;
            this.filterWheelMediator.RegisterConsumer(this);

            this.imagingMediator = imagingMediator;

            imagingMediator.SetAutoStretch(false);
            imagingMediator.SetDetectStars(false);

            this.focuserMediator = focuserMediator;
            this.applicationStatusMediator = applicationStatusMediator;

            findExposureCancelToken = new CancellationTokenSource();
            pauseTokenSource = new PauseTokenSource();

            StartFlatSequenceCommand = new AsyncCommand<bool>(() => StartFlatCapture(new Progress<ApplicationStatus>(p => Status = p), findExposureCancelToken.Token, pauseTokenSource.Token));
            CancelFlatExposureSequenceCommand = new RelayCommand(CancelFindExposureTime);
            PauseFlatExposureSequenceCommand = new RelayCommand(new Action<object>((obj) => { IsPaused = true; pauseTokenSource.IsPaused = IsPaused; }));
            ResumeFlatExposureSequenceCommand = new RelayCommand(new Action<object>((obj) => { IsPaused = false; pauseTokenSource.IsPaused = IsPaused; }));

            FlatCount = profileService.ActiveProfile.FlatWizardSettings.FlatCount;
            HistogramMeanTarget = profileService.ActiveProfile.FlatWizardSettings.HistogramMeanTarget;
            Tolerance = profileService.ActiveProfile.FlatWizardSettings.HistogramTolerance;
            GottaGoFast = profileService.ActiveProfile.FlatWizardSettings.NoFlatProcessing;
            MinExposureTime = profileService.ActiveProfile.CameraSettings.MinFlatExposureTime;
            MaxExposureTime = profileService.ActiveProfile.CameraSettings.MaxFlatExposureTime;
            StepSize = profileService.ActiveProfile.FlatWizardSettings.StepSize;
            BinningMode = profileService.ActiveProfile.FlatWizardSettings.BinningMode;

            profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters.CollectionChanged += FilterWheelFilters_CollectionChanged;
            Filters = new ObservableCollection<FilterInfoCheckbox>();

            foreach (FilterInfo item in profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters) {
                Filters.Add(new FilterInfoCheckbox { Filter = item });
            }

            FilterInfoCheckbox filter = Filters.FirstOrDefault();
            if (filter != null) {
                filter.IsChecked = true;
            }
            RaisePropertyChanged("Filters");
        }

        private void FilterWheelFilters_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            Filters.Clear();
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove) {
                foreach (FilterInfo item in e.OldItems) {
                    Filters.Remove(Filters.Single(f => f.Filter == item));
                }
            } else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add) {
                foreach (FilterInfo item in e.NewItems) {
                    Filters.Add(new FilterInfoCheckbox { Filter = item });
                }
            }
            RaisePropertyChanged("Filters");
        }

        public FlatWizardVM(IProfileService profileService,
                            ICameraMediator cameraMediator,
                            IFilterWheelMediator filterWheelMediator,
                            IImagingMediator imagingMediator,
                            IFocuserMediator focuserMediator,
                            IApplicationStatusMediator applicationStatusMediator,
                            List<FilterInfo> filtersToFocus,
                            int flatCount,
                            string targetName) : this(profileService, cameraMediator, filterWheelMediator, imagingMediator, focuserMediator, applicationStatusMediator) {
            Filters.Clear();
            foreach (FilterInfoCheckbox filter in filtersToFocus.Select(f => new FilterInfoCheckbox { Filter = f, IsChecked = true })) {
                Filters.Add(filter);
            }

            this.targetName = targetName;
            FlatCount = flatCount;
        }

        private void CancelFindExposureTime(object obj) {
            findExposureCancelToken?.Cancel();
        }

        private async Task<bool> StartFlatCapture(IProgress<ApplicationStatus> progress, CancellationToken ct, PauseToken pt) {
            foreach (FilterInfoCheckbox filter in Filters.Where(f => f.IsChecked)) {
                await StartFindingExposureTimeSequence(progress, ct, pt, filter.Filter);
                await StartFlatCaptureSequence(progress, ct, pt, filter.Filter, targetName);
                filter.IsChecked = false;
            }
            return true;
        }

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

        private async Task<bool> StartFlatCaptureSequence(IProgress<ApplicationStatus> progress, CancellationToken ct, PauseToken pt, FilterInfo filter, string targetName = "") {
            try {
                CaptureSequence sequence = new CaptureSequence(CalculatedExposureTime, "FLAT", filter, BinningMode, FlatCount);
                sequence.Gain = Gain;
                while (sequence.ProgressExposureCount < sequence.TotalExposureCount) {
                    await imagingMediator.CaptureImageWithoutSavingToHistoryAndThumbnail(sequence, ct, progress, true, false, targetName);

                    sequence.ProgressExposureCount++;

                    if (pt.IsPaused) {
                        IsPaused = true;
                        progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblPaused"] });
                        await pt.WaitWhilePausedAsync(ct);
                        IsPaused = false;
                    }

                    ct.ThrowIfCancellationRequested();
                }
            } catch (OperationCanceledException) {
            }
            return true;
        }

        private async Task<bool> StartFindingExposureTimeSequence(IProgress<ApplicationStatus> progress, CancellationToken ct, PauseToken pt, FilterInfo filter) {
            bool finished = false;
            double exposureTime = MinExposureTime;

            var status = new ApplicationStatus { Status = "Starting Exposure Time calculation at " + MinExposureTime, Source = Title };
            progress.Report(status);
            ImageArray iarr = null;
            List<DataPoint> datapoints = new List<DataPoint>();

            try {
                do {
                    var sequence = new CaptureSequence(exposureTime, "FLAT", filter, BinningMode, 1);
                    sequence.Gain = Gain;
                    iarr = await imagingMediator.CaptureImageWithoutSavingToHistoryAndThumbnail(sequence, ct, progress, false, true);
                    Image = await ImageControlVM.StretchAsync(
                        iarr.Statistics.Mean,
                        ImageAnalysis.CreateSourceFromArray(
                            iarr,
                            System.Windows.Media.PixelFormats.Gray16),
                        profileService.ActiveProfile.ImageSettings.AutoStretchFactor);

                    var currentMean = iarr.Statistics.Mean;
                    datapoints.Add(new DataPoint(exposureTime, currentMean));
                    var histogramMeanAdu = HistogramMeanTarget * Math.Pow(2, profileService.ActiveProfile.CameraSettings.BitDepth);

                    if (histogramMeanAdu - histogramMeanAdu * Tolerance <= currentMean && histogramMeanAdu + histogramMeanAdu * Tolerance >= currentMean) {
                        CalculatedExposureTime = exposureTime;
                        CalculatedHistogramMean = currentMean;
                        progress.Report(new ApplicationStatus() { Status = "Mean ADU is " + CalculatedHistogramMean + ", target Exposure Time is " + CalculatedExposureTime, Source = Title });
                        finished = true;
                    } else if (currentMean > histogramMeanAdu + histogramMeanAdu * Tolerance) {
                        // issue with too bright flats
                        // display current mean ADU of max ADU of the camera
                        // prompt user to either:
                        // - dim the light
                        // - adjust tolerance/mean
                        // - reset exposure counter should be optional
                        // - accept current exposure
                        // - cancel flat wizard sequence
                        progress.Report(new ApplicationStatus() { Status = "Could not find histogram center, last result: " + currentMean + ", last target exposure time is " + exposureTime, Source = Title });
                        finished = true;
                    } else {
                        exposureTime += StepSize;
                        progress.Report(new ApplicationStatus() { Status = "Mean ADU was " + currentMean + ", starting Exposure Time calculation at " + exposureTime, Source = Title });
                    }

                    if (datapoints.Count > 3 && !finished) {
                        TrendLine line = new TrendLine(datapoints);
                        exposureTime = (histogramMeanAdu - line.Offset) / line.Slope;
                    }

                    if (exposureTime > MaxExposureTime) {
                        // issue with too long flats
                        // display current mean ADU of max ADU of the camera
                        // display estimated flat exposure time
                        // prompt user to either:
                        // - brighten the light
                        // - adjust tolerance/mean
                        // - reset exposure counter should be optional
                        // - cancel flat wizard sequence
                        finished = true;
                    }

                    if (pt.IsPaused) {
                        IsPaused = true;
                        progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblPaused"] });
                        await pt.WaitWhilePausedAsync(ct);
                        IsPaused = false;
                    }

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    ct.ThrowIfCancellationRequested();
                } while (finished == false);
            } catch (OperationCanceledException) {
            }

            imagingMediator.DestroyImage();
            iarr = null;
            Image = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();

            return finished;
        }

        public bool CameraConnected {
            get {
                return cameraConnected;
            }
            set {
                cameraConnected = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<FilterInfoCheckbox> filters;
        private FilterWheelInfo filterWheelInfo;

        public ObservableCollection<FilterInfoCheckbox> Filters {
            get {
                return filters;
            }
            set {
                filters = value;
                RaisePropertyChanged();
            }
        }

        private string targetName;
        private short gain;

        public int FlatCount {
            get {
                return flatCount;
            }
            set {
                flatCount = value;
                profileService.ActiveProfile.FlatWizardSettings.FlatCount = flatCount;
                RaisePropertyChanged();
            }
        }

        public double StepSize {
            get {
                return stepSize;
            }
            set {
                stepSize = value;
                profileService.ActiveProfile.FlatWizardSettings.StepSize = stepSize;
                RaisePropertyChanged();
            }
        }

        public double CalculatedExposureTime {
            get {
                return calculatedExposureTime;
            }
            set {
                calculatedExposureTime = value;
                RaisePropertyChanged();
            }
        }

        public double CalculatedHistogramMean {
            get {
                return calculatedHistogramMean;
            }
            set {
                calculatedHistogramMean = value;
                RaisePropertyChanged();
            }
        }

        public double HistogramMeanTarget {
            get {
                return meanTarget;
            }
            set {
                meanTarget = value;
                profileService.ActiveProfile.FlatWizardSettings.HistogramMeanTarget = meanTarget;
                RaisePropertyChanged();
            }
        }

        public double MinExposureTime {
            get {
                return minExposureTime;
            }
            set {
                minExposureTime = value;
                profileService.ActiveProfile.CameraSettings.MinFlatExposureTime = minExposureTime;
                RaisePropertyChanged();
            }
        }

        public double MaxExposureTime {
            get {
                return maxExposureTime;
            }
            set {
                maxExposureTime = value;
                profileService.ActiveProfile.CameraSettings.MaxFlatExposureTime = maxExposureTime;
                RaisePropertyChanged();
            }
        }

        public double Tolerance {
            get {
                return tolerance;
            }
            set {
                tolerance = value;
                RaisePropertyChanged();
            }
        }

        public BitmapSource Image {
            get {
                return image;
            }
            set {
                image = value;
                RaisePropertyChanged();
            }
        }

        public BinningMode BinningMode {
            get {
                return binningMode;
            }
            set {
                binningMode = value;
                RaisePropertyChanged();
            }
        }

        public bool IsPaused {
            get {
                return isPaused;
            }
            set {
                isPaused = value;
                RaisePropertyChanged();
            }
        }

        public short Gain {
            get {
                return gain;
            }
            set {
                gain = value;
                RaisePropertyChanged();
            }
        }

        public void UpdateDeviceInfo(CameraInfo deviceInfo) {
            CameraConnected = deviceInfo.Connected;
            if (CameraConnected) {
                MinExposureTime = profileService.ActiveProfile.CameraSettings.MinFlatExposureTime;
                MaxExposureTime = profileService.ActiveProfile.CameraSettings.MaxFlatExposureTime;
            }
        }

        public void UpdateDeviceInfo(FilterWheelInfo deviceInfo) {
            filterWheelInfo = deviceInfo;
        }

        public class FilterInfoCheckbox : BaseINPC {
            public FilterInfo Filter { get; set; }

            private bool isChecked = false;

            public bool IsChecked {
                get {
                    return isChecked;
                }

                set {
                    isChecked = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}