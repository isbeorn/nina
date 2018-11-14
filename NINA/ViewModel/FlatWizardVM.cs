using NINA.Locale;
using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Profile;
using NINA.Utility.WindowService;
using Nito.AsyncEx;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.ViewModel {

    internal class FlatWizardVM : DockableVM, ICameraConsumer, IFilterWheelConsumer {
        private readonly IApplicationStatusMediator applicationStatusMediator;
        private readonly ICameraMediator cameraMediator;
        private readonly IFilterWheelMediator filterWheelMediator;
        private readonly IFocuserMediator focuserMediator;
        private readonly IImagingMediator imagingMediator;
        private ApplicationStatus _status;
        private BinningMode binningMode;
        private double calculatedExposureTime;
        private double calculatedHistogramMean;
        private bool cameraConnected;
        private ObservableCollection<FilterInfoCheckbox> filters;
        private FilterWheelInfo filterWheelInfo;
        private CancellationTokenSource flatSequenceCts;
        private int flatCount;
        private short gain;
        private BitmapSource image;
        private bool isPaused = false;
        private double maxExposureTime;
        private double meanTarget;
        private double minExposureTime;
        private PauseTokenSource pauseTokenSource;
        private double stepSize;
        private string targetName;
        private double tolerance;

        public FlatWizardVM(IProfileService profileService,
                            ICameraMediator cameraMediator,
                            IFilterWheelMediator filterWheelMediator,
                            IImagingMediator imagingMediator,
                            IFocuserMediator focuserMediator,
                            IApplicationStatusMediator applicationStatusMediator) : base(profileService) {
            Title = "LblFlatWizard";
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

            flatSequenceCts = new CancellationTokenSource();
            pauseTokenSource = new PauseTokenSource();

            StartFlatSequenceCommand = new AsyncCommand<bool>(() => StartFlatCapture(new Progress<ApplicationStatus>(p => Status = p), pauseTokenSource.Token));
            CancelFlatExposureSequenceCommand = new RelayCommand(CancelFindExposureTime);
            PauseFlatExposureSequenceCommand = new RelayCommand(new Action<object>((obj) => { IsPaused = true; pauseTokenSource.IsPaused = IsPaused; }));
            ResumeFlatExposureSequenceCommand = new RelayCommand(new Action<object>((obj) => { IsPaused = false; pauseTokenSource.IsPaused = IsPaused; }));

            FlatCount = profileService.ActiveProfile.FlatWizardSettings.FlatCount;
            HistogramMeanTarget = profileService.ActiveProfile.FlatWizardSettings.HistogramMeanTarget;
            Tolerance = profileService.ActiveProfile.FlatWizardSettings.HistogramTolerance;
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

        public BinningMode BinningMode {
            get {
                return binningMode;
            }
            set {
                binningMode = value;
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

        public bool CameraConnected {
            get {
                return cameraConnected;
            }
            set {
                cameraConnected = value;
                RaisePropertyChanged();
            }
        }

        public RelayCommand CancelFlatExposureSequenceCommand { get; }

        public ObservableCollection<FilterInfoCheckbox> Filters {
            get {
                return filters;
            }
            set {
                filters = value;
                RaisePropertyChanged();
            }
        }

        public IAsyncCommand FindExposureTimeCommand { get; private set; }

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

        public short Gain {
            get {
                return gain;
            }
            set {
                gain = value;
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

        public BitmapSource Image {
            get {
                return image;
            }
            set {
                image = value;
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

        public RelayCommand PauseFlatExposureSequenceCommand { get; }
        public RelayCommand ResumeFlatExposureSequenceCommand { get; }
        public IAsyncCommand StartFlatSequenceCommand { get; private set; }

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

        public double Tolerance {
            get {
                return tolerance;
            }
            set {
                tolerance = value;
                RaisePropertyChanged();
            }
        }

        public IWindowService WindowService { get; set; } = new WindowService();

        private void CancelFindExposureTime(object obj) {
            flatSequenceCts?.Cancel();
        }

        private void FilterWheelFilters_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
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

        private async Task<bool> StartFindingExposureTimeSequence(IProgress<ApplicationStatus> progress, CancellationToken ct, PauseToken pt, FilterInfo filter) {
            bool finished = false;
            double exposureTime = MinExposureTime;

            var status = new ApplicationStatus { Status = "Starting Exposure Time calculation at " + MinExposureTime, Source = Title };
            progress.Report(status);
            ImageArray iarr = null;
            List<DataPoint> datapoints = new List<DataPoint>();

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
                    TrendLine line = new TrendLine(datapoints);
                    var expectedExposureTime = (histogramMeanAdu - line.Offset) / line.Slope;

                    if (expectedExposureTime < exposureTime && datapoints.Count >= 3) {
                        exposureTime = expectedExposureTime;
                    } else {
                        var flatsWizardUserPrompt = new FlatWizardUserPromptVM(Loc.Instance["LblFlatUserPromptFlatTooBright"],
                            currentMean, Math.Pow(2, profileService.ActiveProfile.CameraSettings.BitDepth),
                            Tolerance, HistogramMeanTarget, MinExposureTime, MaxExposureTime, expectedExposureTime
                        );
                        finished = EvaluateDialogResult(ref exposureTime, ref datapoints, flatsWizardUserPrompt);
                    }
                } else {
                    exposureTime += StepSize;
                    progress.Report(new ApplicationStatus() { Status = "Mean ADU was " + currentMean + ", starting Exposure Time calculation at " + exposureTime, Source = Title });
                }

                if (datapoints.Count >= 3 && !finished) {
                    TrendLine line = new TrendLine(datapoints);
                    exposureTime = (histogramMeanAdu - line.Offset) / line.Slope;
                }

                if ((exposureTime > MaxExposureTime || exposureTime < MinExposureTime) && !finished) {
                    var flatsWizardUserPrompt = new FlatWizardUserPromptVM(Loc.Instance["LblFlatUserPromptFlatTooDim"],
                        currentMean, Math.Pow(2, profileService.ActiveProfile.CameraSettings.BitDepth),
                        Tolerance, HistogramMeanTarget, MinExposureTime, MaxExposureTime, exposureTime
                    );
                    finished = EvaluateDialogResult(ref exposureTime, ref datapoints, flatsWizardUserPrompt);
                }

                if (pt.IsPaused) {
                    IsPaused = true;
                    progress.Report(new ApplicationStatus() { Status = Loc.Instance["LblPaused"] });
                    await pt.WaitWhilePausedAsync(ct);
                    IsPaused = false;
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                ct.ThrowIfCancellationRequested();
            } while (finished == false);

            return finished;
        }

        private bool EvaluateDialogResult(ref double exposureTime, ref List<DataPoint> dataPoints, FlatWizardUserPromptVM flatsWizardUserPrompt) {
            WindowService.ShowDialog(flatsWizardUserPrompt, Loc.Instance["LblFlatUserPromptFailure"], System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.ToolWindow);
            if (!flatsWizardUserPrompt.Continue) {
                flatSequenceCts.Cancel();
                return true;
            } else {
                HistogramMeanTarget = flatsWizardUserPrompt.HistogramMean;
                Tolerance = flatsWizardUserPrompt.Tolerance;
                MinExposureTime = flatsWizardUserPrompt.MinimumTime;
                MaxExposureTime = flatsWizardUserPrompt.MaximumTime;
                if (flatsWizardUserPrompt.Reset) {
                    exposureTime = MinExposureTime;
                    dataPoints = new List<DataPoint>();
                }
                return false;
            }
        }

        private async Task<bool> StartFlatCapture(IProgress<ApplicationStatus> progress, PauseToken pt) {
            if (flatSequenceCts.IsCancellationRequested) {
                flatSequenceCts = new CancellationTokenSource();
            }
            try {
                if (filters.Count == 0) {
                    await StartFindingExposureTimeSequence(progress, flatSequenceCts.Token, pt, null);
                    await StartFlatCaptureSequence(progress, flatSequenceCts.Token, pt, null, targetName);
                } else {
                    foreach (FilterInfoCheckbox filter in Filters.Where(f => f.IsChecked)) {
                        await StartFindingExposureTimeSequence(progress, flatSequenceCts.Token, pt, filter.Filter);
                        await StartFlatCaptureSequence(progress, flatSequenceCts.Token, pt, filter.Filter, targetName);
                        filter.IsChecked = false;
                        CalculatedExposureTime = 0;
                        CalculatedHistogramMean = 0;
                    }
                }
            } catch (OperationCanceledException) {
                Utility.Notification.Notification.ShowError(Loc.Instance["LblFlatSequenceCancelled"]);
                progress.Report(new ApplicationStatus { Status = Loc.Instance["LblFlatSequenceCancelled"] });
                CalculatedExposureTime = 0;
                CalculatedHistogramMean = 0;
            }

            imagingMediator.DestroyImage();
            Image = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();

            return true;
        }

        private async Task<bool> StartFlatCaptureSequence(IProgress<ApplicationStatus> progress, CancellationToken ct, PauseToken pt, FilterInfo filter, string targetName = "") {
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

            return true;
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
            private bool isChecked = false;
            public FilterInfo Filter { get; set; }

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