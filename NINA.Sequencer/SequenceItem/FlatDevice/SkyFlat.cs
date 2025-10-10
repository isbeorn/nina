using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NINA.Astrometry;
using NINA.Astrometry.Interfaces;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Equipment.MyGuider;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Model;
using NINA.Image.ImageAnalysis;
using NINA.Image.ImageData;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.Logic;
using NINA.Sequencer.SequenceItem.FilterWheel;
using NINA.Sequencer.SequenceItem.Guider;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Sequencer.Utility;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.Mediator;
using NINA.WPF.Base.Model;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace NINA.Sequencer.SequenceItem.FlatDevice {

    [ExportMetadata("Name", "Lbl_SequenceItem_FlatDevice_SkyFlat_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_FlatDevice_SkyFlat_Description")]
    [ExportMetadata("Icon", "FlatWizardSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_FlatDevice")]
    [Export(typeof(ISequenceItem))]
    [Export(typeof(ISequenceContainer))]
    [JsonObject(MemberSerialization.OptIn)]
    public partial class SkyFlat : SequentialContainer, IImmutableContainer {
        private IProfileService profileService;
        private IImagingMediator imagingMediator;
        private IImageSaveMediator imageSaveMediator;
        private ITwilightCalculator twilightCalculator;
        private ITelescopeMediator telescopeMediator;
        private ISymbolBroker symbolBroker;

        private bool cameraIsLinear = true;

        private static List<(DateTime Timestamp, double ADU, double ExposureTime)> exposureHistory = new List<(DateTime, double, double)>();

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            this.Items.Clear();
            this.Conditions.Clear();
            this.Triggers.Clear();
        }

        [ImportingConstructor]
        public SkyFlat(IProfileService profileService,
                       ICameraMediator cameraMediator,
                       ITelescopeMediator telescopeMediator,
                       IImagingMediator imagingMediator,
                       IImageSaveMediator imageSaveMediator,
                       IImageHistoryVM imageHistoryVM,
                       IFilterWheelMediator filterWheelMediator,
                       ITwilightCalculator twilightCalculator) :
            this(
                null,
                profileService,
                telescopeMediator,
                imagingMediator,
                imageSaveMediator,
                twilightCalculator,
                new SwitchFilter(profileService, filterWheelMediator),
                new TakeExposure(profileService, cameraMediator, imagingMediator, imageSaveMediator, imageHistoryVM) { ImageType = CaptureSequence.ImageTypes.FLAT },
                new LoopCondition() { Iterations = 1 }
            ) {
            HistogramTargetPercentage = 0.5;
            HistogramTolerancePercentage = 0.1;
            MaxExposure = 10;
            MinExposure = 0;
            ShouldDither = false;
        }

        private SkyFlat(
            SkyFlat cloneMe,
            IProfileService profileService,
            ITelescopeMediator telescopeMediator,
            IImagingMediator imagingMediator,
            IImageSaveMediator imageSaveMediator,
            ITwilightCalculator twilightCalculator,
            SwitchFilter switchFilter,
            TakeExposure takeExposure,
            LoopCondition loopCondition

        ) {
            this.profileService = profileService;
            this.imagingMediator = imagingMediator;
            this.imageSaveMediator = imageSaveMediator;
            this.twilightCalculator = twilightCalculator;
            this.telescopeMediator = telescopeMediator;
            ditherPixels = profileService.ActiveProfile.GuiderSettings.DitherPixels;
            ditherSettleTime = profileService.ActiveProfile.GuiderSettings.SettleTime;

            this.Add(new Annotation());
            this.Add(new Annotation());
            this.Add(switchFilter);
            this.Add(new Annotation());

            var container = new SequentialContainer();
            container.Add(loopCondition);
            container.Add(takeExposure);
            this.Add(container);

            IsExpanded = false;
            if (cloneMe != null) {
                CopyMetaData(cloneMe);
                DitherPixels = cloneMe.DitherPixels;
                DitherSettleTime = cloneMe.DitherSettleTime;
            }
        }

        private InstructionErrorBehavior errorBehavior = InstructionErrorBehavior.ContinueOnError;

        [JsonProperty]
        public override InstructionErrorBehavior ErrorBehavior {
            get => errorBehavior;
            set {
                errorBehavior = value;
                foreach (var item in Items) {
                    item.ErrorBehavior = errorBehavior;
                }
                RaisePropertyChanged();
            }
        }

        private int attempts = 1;

        [JsonProperty]
        public override int Attempts {
            get => attempts;
            set {
                if (value > 0) {
                    attempts = value;
                    foreach (var item in Items) {
                        item.Attempts = attempts;
                    }
                    RaisePropertyChanged();
                }
            }
        }

        public override object Clone() {
            var clone = new SkyFlat(
                this,
                profileService,
                telescopeMediator,
                imagingMediator,
                imageSaveMediator,
                twilightCalculator,
                (SwitchFilter)this.GetSwitchFilterItem().Clone(),
                (TakeExposure)this.GetExposureItem().Clone(),
                (LoopCondition)this.GetIterations().Clone()
            ) {
                MaxExposure = this.MaxExposure,
                MinExposure = this.MinExposure,
                HistogramTargetPercentage = this.HistogramTargetPercentage,
                HistogramTolerancePercentage = this.HistogramTolerancePercentage,
                ShouldDither = this.ShouldDither
            };
            return clone;
        }

        public SwitchFilter GetSwitchFilterItem() {
            return Items.First(x => x is SwitchFilter) as SwitchFilter;
        }

        public SequentialContainer GetImagingContainer() {
            return Items.First(x => x is SequentialContainer) as SequentialContainer;
        }

        public TakeExposure GetExposureItem() {
            return GetImagingContainer().Items.First(x => x is TakeExposure) as TakeExposure;
        }

        public LoopCondition GetIterations() {
            return GetImagingContainer().Conditions.First(x => x is LoopCondition) as LoopCondition;
        }

        public SequentialContainer ImagingContainer {
            get => GetImagingContainer();
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            try {
                DeterminedHistogramADU = 0;
                var loop = GetIterations();
                if (loop.CompletedIterations >= loop.Iterations) {
                    Logger.Warning($"The {nameof(SkyFlat)} progress is already complete ({loop.CompletedIterations}/{loop.Iterations}). The instruction will be skipped");
                    throw new SequenceItemSkippedException($"The {nameof(SkyFlat)} progress is already complete ({loop.CompletedIterations}/{loop.Iterations}). The instruction will be skipped");
                }

                GetIterations().ResetProgress();

                springTwilight = twilightCalculator.GetTwilightDuration(new DateTime(DateTime.Now.Year, 03, 20), 30.0, 0d, 0d).TotalMilliseconds;
                todayTwilight = twilightCalculator.GetTwilightDuration(DateTime.Now, profileService.ActiveProfile.AstrometrySettings.Latitude, profileService.ActiveProfile.AstrometrySettings.Longitude, 0d).TotalMilliseconds;

                Logger.Info($"Determining Sky Flat Exposure Time. Min {MinExposure}, Max {MaxExposure}, Target {HistogramTargetPercentage * 100}%, Tolerance {HistogramTolerancePercentage * 100}%");
                var exposureDetermination = await DetermineExposureTime(MinExposure, MaxExposure, progress, token);

                if (exposureDetermination is null) {
                    throw new SequenceEntityFailedException("Failed to determine exposure time for sky flats");
                } else {
                    // Exposure time has been successfully determined
                    GetExposureItem().ExposureTime = exposureDetermination.StartExposureTime;
                }

                exposureDetermination.CameraIsLinear = cameraIsLinear;
                await TakeSkyFlats(exposureDetermination, progress, token);
            } finally {
                await CoreUtil.Wait(TimeSpan.FromMilliseconds(500));
                progress?.Report(new ApplicationStatus() { Source = Loc.Instance["Lbl_SequenceItem_FlatDevice_SkyFlat_Name"] });
            }
        }

        private async Task<SkyFlatExposureDetermination> DetermineExposureTime(double initialMin, double initialMax, IProgress<ApplicationStatus> progress, CancellationToken ct, double lastTime = 0) {
            const double TOLERANCE = 0.00001;

            var currentMin = initialMin;
            var currentMax = initialMax;
            var lastExposureTime = 0d;
            var determinedHistogramADUpercentage = 0d;
            cameraIsLinear = true;
            const int MAX_LINEAR_TEST_ATTEMPTS = 3;
            int linearTestAttempts = 0;
            double _targetADU = 0;
            var exposureAduPairs = new List<(double exposure, double adu)>();
            for (var iterations = 0; iterations <= 20; iterations++) {
                var exposureTime = Math.Round((currentMax + currentMin) / 2d, 5);

                // If this is the first iteration and the last time is not 0, use the last time as the initial exposure time
                if (iterations == 0 && lastTime != 0) {
                    exposureTime = lastTime;
                }

                // If the histogram is between 10% and 90%, we can use the last exposure time to calculate the target exposure time
                if (cameraIsLinear) {
                    if (lastExposureTime != 0d && (determinedHistogramADUpercentage >= 0.1 && determinedHistogramADUpercentage <= 0.9) && _targetADU != 0) {
                        double factor = (_targetADU / DeterminedHistogramADU);
                        exposureTime = lastExposureTime * factor;

                        exposureTime = Math.Min(exposureTime, initialMax);
                        exposureTime = Math.Max(exposureTime, initialMin);
                    }
                }

                if (Math.Abs(exposureTime - initialMin) < TOLERANCE || Math.Abs(exposureTime - initialMax) < TOLERANCE) {
                    // If the exposure time is equal to the min/max and not yield a result it will be skip any more unnecessary attempts
                    iterations = 20;
                }

                progress?.Report(new ApplicationStatus() {
                    Status = string.Format(Loc.Instance["Lbl_SequenceItem_FlatDevice_SkyFlat_DetermineTime"], Math.Round(exposureTime,5), iterations, 20),
                    Source = Loc.Instance["Lbl_SequenceItem_FlatDevice_SkyFlat_Name"]
                });

                var sequence = new CaptureSequence(exposureTime, CaptureSequence.ImageTypes.FLAT, GetSwitchFilterItem().Filter, GetExposureItem().Binning, 1) { Gain = GetExposureItem().Gain, Offset = GetExposureItem().Offset };

                var timer = Stopwatch.StartNew();
                var image = await imagingMediator.CaptureImage(sequence, ct, progress);

                var imageData = await image.ToImageData(progress, ct);
                var prepTask = imagingMediator.PrepareImage(imageData, new PrepareImageParameters(true, false), ct);
                await prepTask;
                var statistics = await imageData.Statistics;

                var mean = statistics.Mean;
                DeterminedHistogramADU = mean;
                determinedHistogramADUpercentage = (mean / (Math.Pow(2, imageData.Properties.BitDepth) - 1));
                _targetADU = (Math.Pow(2, imageData.Properties.BitDepth) - 1) * HistogramTargetPercentage;

                if (determinedHistogramADUpercentage > 0.1 && determinedHistogramADUpercentage < 0.9) {
                    exposureAduPairs.Add((exposureTime, mean));
                }

                if (cameraIsLinear && exposureAduPairs.Count >= 2) {
                    var isLinear = TestLinearity(exposureAduPairs);
                    linearTestAttempts++;

                    if (!isLinear && linearTestAttempts >= MAX_LINEAR_TEST_ATTEMPTS) {
                        Logger.Warning("Camera response determined to be non-linear, switching to binary search method");
                        cameraIsLinear = false;
                    }
                }

                var check = HistogramMath.GetExposureAduState(mean, HistogramTargetPercentage, image.BitDepth, HistogramTolerancePercentage);

                lastExposureTime = exposureTime;
                this.GetExposureItem().ExposureTime = exposureTime;

                switch (check) {
                    case HistogramMath.ExposureAduState.ExposureWithinBounds:

                        // Go ahead and save the exposure, as it already fits the parameters
                        FillTargetMetaData(image.MetaData);
                        await imageSaveMediator.Enqueue(imageData, prepTask, progress, ct);

                        Logger.Info($"Found exposure time at {exposureTime}s with histogram ADU {mean}");
                        progress?.Report(new ApplicationStatus() {
                            Status = string.Format(Loc.Instance["Lbl_SequenceItem_FlatDevice_SkyFlat_FoundTime"], Math.Round(exposureTime,5)),
                            Source = Loc.Instance["Lbl_SequenceItem_FlatDevice_SkyFlat_Name"]
                        });

                        if (cameraIsLinear) {
                            exposureTime = exposureTime * (_targetADU / DeterminedHistogramADU);
                        }
                        return new SkyFlatExposureDetermination(timer, exposureTime, springTwilight, todayTwilight);

                    case HistogramMath.ExposureAduState.ExposureBelowLowerBound:
                        Logger.Info($"Exposure too dim at {Math.Round(exposureTime, 5)}s. ADU measured at: {DeterminedHistogramADU}. Retrying with higher exposure time");
                        currentMin = exposureTime;
                        break;

                    case HistogramMath.ExposureAduState:
                        Logger.Info($"Exposure too bright at {Math.Round(exposureTime, 5)}s. ADU measured at: {DeterminedHistogramADU}. Retrying with lower exposure time");
                        currentMax = exposureTime;
                        break;
                }
            }
            if (Math.Abs(initialMax - currentMin) < TOLERANCE) {
                throw new SequenceEntityFailedException(Loc.Instance["Lbl_SequenceItem_FlatDevice_SkyFlat_LightTooDim"]);
            } else {
                throw new SequenceEntityFailedException(Loc.Instance["Lbl_SequenceItem_FlatDevice_SkyFlat_LightTooBright"]);
            }
        }

        private bool TestLinearity(List<(double exposure, double adu)> exposureAduPairs) {
            const double LINEAR_R_SQUARED_THRESHOLD = 0.8;

            if (exposureAduPairs.Count < 2) return true;

            // Calculate linear regression
            double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
            int n = exposureAduPairs.Count;

            foreach (var pair in exposureAduPairs) {
                sumX += pair.exposure;
                sumY += pair.adu;
                sumXY += pair.exposure * pair.adu;
                sumX2 += pair.exposure * pair.exposure;
            }

            double slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            double intercept = (sumY - slope * sumX) / n;

            // Calculate R-squared
            double meanY = sumY / n;
            double totalSumSquares = 0;
            double residualSumSquares = 0;

            foreach (var pair in exposureAduPairs) {
                double predictedY = slope * pair.exposure + intercept;
                residualSumSquares += Math.Pow(pair.adu - predictedY, 2);
                totalSumSquares += Math.Pow(pair.adu - meanY, 2);
            }

            double rSquared = 1 - (residualSumSquares / totalSumSquares);

            Logger.Debug($"Linearity test R-squared: {rSquared}");
            return rSquared >= LINEAR_R_SQUARED_THRESHOLD;
        }

        private void FillTargetMetaData(ImageMetaData metaData) {
            var dsoContainer = RetrieveTarget(this.Parent);
            if (dsoContainer != null) {
                var target = dsoContainer.Target;
                if (target != null) {
                    metaData.Target.Name = target.DeepSkyObject.NameAsAscii;
                    metaData.Target.Coordinates = target.InputCoordinates.Coordinates;
                    metaData.Target.PositionAngle = target.PositionAngle;
                }
            }

            var root = ItemUtility.GetRootContainer(this.Parent);
            if (root != null) {
                metaData.Sequence.Title = root.SequenceTitle;
            }
        }

        /// <summary>
        /// When an inner instruction interrupts this set, it should reroute the interrupt to the real parent set
        /// </summary>
        /// <returns></returns>
        public override Task Interrupt() {
            return this.Parent?.Interrupt();
        }

        [ObservableProperty]
        private double determinedHistogramADU;

        private double minExposure;

        [JsonProperty]
        public double MinExposure {
            get => minExposure;
            set {
                minExposure = value;
                RaisePropertyChanged();
            }
        }

        private double maxExposure;

        [JsonProperty]
        public double MaxExposure {
            get => maxExposure;
            set {
                maxExposure = value;
                RaisePropertyChanged();
            }
        }

        private double histogramTargetPercentage;

        [JsonProperty]
        public double HistogramTargetPercentage {
            get => histogramTargetPercentage;
            set {
                if (value < 0) {
                    value = 0;
                }
                if (value > 1) {
                    value = 1;
                }
                histogramTargetPercentage = value;
                RaisePropertyChanged();
            }
        }

        private double histogramTolerancePercentage;

        [JsonProperty]
        public double HistogramTolerancePercentage {
            get => histogramTolerancePercentage;
            set {
                if (value < 0) {
                    value = 0;
                }
                if (value > 1) {
                    value = 1;
                }
                histogramTolerancePercentage = value;
                RaisePropertyChanged();
            }
        }

        private bool shouldDither;
        private double springTwilight;
        private double todayTwilight;

        [JsonProperty]
        public bool ShouldDither {
            get => shouldDither;
            set {
                shouldDither = value;
                RaisePropertyChanged();
            }
        }

        private double ditherPixels;

        [JsonProperty]
        public double DitherPixels {
            get => ditherPixels;
            set {
                ditherPixels = value;
                RaisePropertyChanged();
            }
        }

        private double ditherSettleTime;

        [JsonProperty]
        public double DitherSettleTime {
            get => ditherSettleTime;
            set {
                ditherSettleTime = value;
                RaisePropertyChanged();
            }
        }

        public override bool Validate() {
            var switchFilter = GetSwitchFilterItem();
            var takeExposure = GetExposureItem();

            var valid = takeExposure.Validate() && switchFilter.Validate();

            var issues = new ObservableCollection<string>();

            if (ShouldDither) {
                var info = telescopeMediator.GetInfo();
                if (!info.Connected) {
                    issues.Add(Loc.Instance["Lbl_SequenceItem_FlatDevice_SkyFlat_Validation_Dither_MountNotConnected"]);
                } else {
                    if (!info.CanPulseGuide) {
                        issues.Add(Loc.Instance["Lbl_SequenceItem_FlatDevice_SkyFlat_Validation_Dither_CannotPulseGuide"]);
                    }
                    if (info.AtPark) {
                        issues.Add(Loc.Instance["Lbl_SequenceItem_FlatDevice_SkyFlat_Validation_Dither_MountParked"]);
                    }
                }
            }

            if (MinExposure > MaxExposure) {
                issues.Add(Loc.Instance["Lbl_SequenceItem_FlatDevice_SkyFlat_Validation_InputRangeInvalid"]);
            }

            Issues = issues.Concat(takeExposure.Issues).Concat(switchFilter.Issues).Distinct().ToList();
            RaisePropertyChanged(nameof(Issues));

            return valid;
        }

        /// <summary>
        /// This method will take twilight sky flat exposures by adjusting the exposure time based on the changing sky conditions during the runtime.
        /// A paper which explains the math behind the algorithm can be found here
        /// https://ui.adsabs.harvard.edu/abs/1993AJ....105.1206T/abstract
        /// </summary>
        /// <param name="skyFlatTimes"></param>
        /// <param name="firstExposureTime"></param>
        /// <param name="filter"></param>
        /// <param name="pt"></param>
        /// <remarks></remarks>
        /// <returns></returns>
        private async Task TakeSkyFlats(SkyFlatExposureDetermination exposureDetermination, IProgress<ApplicationStatus> mainProgress, CancellationToken token) {
            var dsoContainer = RetrieveTarget(this.Parent);

            IProgress<ApplicationStatus> progress = new Progress<ApplicationStatus>(
                x => mainProgress?.Report(new ApplicationStatus() {
                    Status = $"Capturing sky flats.",
                    Progress = GetIterations().CompletedIterations + 1,
                    MaxProgress = GetIterations().Iterations,
                    ProgressType = ApplicationStatus.StatusProgressType.ValueOfMaxValue,
                    Status2 = x.Status,
                    Progress2 = x.Progress,
                    ProgressType2 = x.ProgressType,
                    MaxProgress2 = x.MaxProgress,
                    Source = Loc.Instance["Lbl_SequenceItem_FlatDevice_SkyFlat_Name"]
                })
            );
            GetIterations().ResetProgress();
            // we start at exposure + 1, as DetermineExposureTime is already saving an exposure
            GetIterations().CompletedIterations++;
            for (var i = 1; i < GetIterations().Iterations; i++) {
                GetIterations().CompletedIterations++;

                var filter = GetSwitchFilterItem().Filter;
                var time = exposureDetermination.GetNextExposureTime();

                if (time > MaxExposure) {
                    Notification.ShowWarning(String.Format(Loc.Instance["LblExposureOverMax"], Math.Round(time,5)));
                        
                    Logger.Warning($"Predicted exposure {time} is longer than {MaxExposure} - Stopping");
                    break;
                }
                if (time < MinExposure) {
                    Notification.ShowWarning(String.Format(Loc.Instance["LblExposureUnderMin"], Math.Round(time, 5)));

                    Logger.Warning($"Predicted exposure {time} is shorter than {MinExposure} - Stopping");
                    break;
                }

                var sequence = new CaptureSequence(time, CaptureSequence.ImageTypes.FLAT, filter, GetExposureItem().Binning, GetIterations().Iterations) { Gain = GetExposureItem().Gain, Offset = GetExposureItem().Offset };
                sequence.ProgressExposureCount = i;

                Task ditherTask = null;

                if (ditherTask != null) {
                    await ditherTask;
                }

                var exposureData = await imagingMediator.CaptureImage(sequence, token, progress);

                var imageData = await exposureData.ToImageData(progress, token);

                if (ShouldDither && i < (GetIterations().Iterations - 1)) {
                    ditherTask = Dither(progress, token);
                }

                var prepTask = imagingMediator.PrepareImage(imageData, new PrepareImageParameters(true, false), token);

                var imageStatistics = await imageData.Statistics.Task;
                switch (
                        HistogramMath.GetExposureAduState(
                            imageStatistics.Mean,
                            HistogramTargetPercentage,
                            imageData.Properties.BitDepth,
                            HistogramTolerancePercentage)
                        ) {
                    case HistogramMath.ExposureAduState.ExposureBelowLowerBound:
                    case HistogramMath.ExposureAduState.ExposureAboveUpperBound:
                        Logger.Warning($"Skyflat correction did not work and is outside of tolerance: " +
                                     $"first exposure time {exposureDetermination.StartExposureTime}, " +
                                     $"current exposure time {time}, " +
                                     $"elapsed time: {exposureDetermination.GetElapsedTime().TotalSeconds}, " +
                                     $"current mean adu: {imageStatistics.Mean}. " +
                                     $"The sky flat exposure time will be determined again and the exposure will be repeated.");
                        Notification.ShowWarning(String.Format(Loc.Instance["LblSkyFlatOutsideTolerance"], exposureDetermination.StartExposureTime, time, 
                            exposureDetermination.GetElapsedTime().TotalSeconds, imageStatistics.Mean));

                        cameraIsLinear = false;
                            
                        exposureDetermination = await DetermineExposureTime(MinExposure, MaxExposure, progress, token, time);
                        continue;
                }
                exposureDetermination.TargetADU = (Math.Pow(2, imageData.Properties.BitDepth) - 1) * HistogramTargetPercentage;
                exposureDetermination.LastADU = imageStatistics.Mean;                
                exposureDetermination.PreviousADU = exposureDetermination.LastADU;
                exposureDetermination.CameraIsLinear = cameraIsLinear;

                FillTargetMetaData(imageData.MetaData);
                await imageSaveMediator.Enqueue(imageData, prepTask, progress, token);

                progress?.Report(new ApplicationStatus { Status = Loc.Instance["LblSavingImage"] });
            }
        }

        private Task Dither(IProgress<ApplicationStatus> progress, CancellationToken token) {
            return Task.Run(async () => {
                var info = telescopeMediator.GetInfo();
                if (!info.Connected) {
                    Logger.Error("Dither between flat exposures set but telescope is not connected");
                    return;
                }
                if (!info.CanPulseGuide) {
                    Logger.Error("Dither between flat exposures set but telescope is not capable of pulse guiding");
                    return;
                }
                if (info.AtPark) {
                    Logger.Error("Dither between flat exposures set but telescope is parked");
                    return;
                }
                Logger.Info("Dithering between flat frames");
                using (var directGuider = new DirectGuider(profileService, telescopeMediator)) {
                    await directGuider.Connect(token);
                    await directGuider.StartGuiding(false, null, token);
                    await directGuider.Dither(DitherPixels, TimeSpan.FromSeconds(DitherSettleTime), false, progress, token);
                    await directGuider.StopGuiding(token);
                    directGuider.Disconnect();
                }
            }, token);
        }

        private IDeepSkyObjectContainer RetrieveTarget(ISequenceContainer parent) {
            if (parent != null) {
                var container = parent as IDeepSkyObjectContainer;
                if (container != null) {
                    return container;
                } else {
                    return RetrieveTarget(parent.Parent);
                }
            } else {
                return null;
            }
        }

        public class SkyFlatExposureDetermination {
            private TimeSpan ti;
            private double k;
            private double s;
            private double a;
            private double currentExposureTime;
            private Stopwatch timer;

            private double lastADU;

            public double LastADU {
                get => lastADU;
                set {
                    lastADU = value;
                }
            }

            private double previousADU;

            public double PreviousADU {
                get => previousADU;
                set {
                    previousADU = value;
                }
            }

            private double targetADU;

            public double TargetADU {
                get => targetADU;
                set {
                    targetADU = value;
                }
            }

            private bool cameraIsLinear;

            public bool CameraIsLinear {
                get => cameraIsLinear;
                set {
                    cameraIsLinear = value;
                }
            }

            private ICustomDateTime DateTime { get; }

            public SkyFlatExposureDetermination(Stopwatch timer, double startExposureTime, double springTwilight, double todayTwilight, ICustomDateTime dateTime = null) {
                this.timer = timer;
                this.StartExposureTime = startExposureTime;
                this.currentExposureTime = startExposureTime;
                this.DateTime = dateTime ?? new SystemDateTime();

                var tau = Math.Abs(todayTwilight) / Math.Abs(springTwilight);
                ti = TimeSpan.Zero;

                k = DateTime.Now.Hour < 12 ? 0.091 / 60 : -0.094 / 60;

                s = startExposureTime;
                a = Math.Pow(10, k / tau);

                this.lastADU = 0;
                this.previousADU = 0;
                this.targetADU = 0;
                this.cameraIsLinear = true;
                exposureHistory = new List<(DateTime, double, double)>();
            }

            public double StartExposureTime { get; private set; }

            public TimeSpan GetElapsedTime() {
                return timer.Elapsed;
            }

            public double GetNextExposureTime() {
                if (cameraIsLinear) {
                    return GetNextExposureTimeByADU();
                }
                return GetNextExposureTime(timer.Elapsed);
            }

            public double GetNextExposureTimeByADU() {
                var calibrationFactor = 1.0;
                
                if (lastADU != 0) {
                    exposureHistory.Add((DateTime.Now, lastADU, currentExposureTime));
                    Logger.Info($"Adding ADU={lastADU}, Exposure Time={currentExposureTime}");
                    ManageHistory();
                }

                if (exposureHistory.Count > 1) {
                    currentExposureTime = PredictNextExposureTime(DateTime.Now, lastADU, currentExposureTime);

                    Logger.Info($"Using exponential fit. Estimated next ExposureTime={currentExposureTime}");
                } else {
                    if (lastADU > 0.1) {
                        calibrationFactor *= targetADU / lastADU;
                        currentExposureTime = currentExposureTime * calibrationFactor;
                        Logger.Info($"Using linear calibration. Last ADU={lastADU}, Calibration factor={calibrationFactor}");
                    }
                }

                return currentExposureTime;
            }

            private double PredictNextExposureTime(DateTime currentTime, double lastADU, double currentExposureTime) {
                if (exposureHistory.Count < 2) {                    
                    Logger.Debug("Not enough data to fit a model; falling back to original method");
                    return currentExposureTime * (lastADU / previousADU);
                }

                // Calibrate the history to the target ADU
                var calibratedHistory = exposureHistory.Select(point => {
                    double calibratedExposureTime = point.ExposureTime * (targetADU / point.ADU);
                    return (point.Timestamp, calibratedExposureTime);
                }).ToList();

                // Fit an exponential model to the calibrated exposure times
                // Model: ExposureTime(t) = ExposureTime0 * exp(k * t)
                // Use linear regression on ln(ExposureTime) vs time to estimate k
                double sumT = 0, sumLnExposureTime = 0, sumT2 = 0, sumTLnExposureTime = 0;
                int n = calibratedHistory.Count;

                foreach (var point in calibratedHistory) {
                    double t = (point.Timestamp - calibratedHistory[0].Timestamp).TotalSeconds;
                    double lnExposureTime = Math.Log(point.calibratedExposureTime);
                    sumT += t;
                    sumLnExposureTime += lnExposureTime;
                    sumT2 += t * t;
                    sumTLnExposureTime += t * lnExposureTime;
                }

                // Calculate the growth constant k
                double k = (n * sumTLnExposureTime - sumT * sumLnExposureTime) / (n * sumT2 - sumT * sumT);

                // Predict the next exposure time
                double deltaT = (currentTime - calibratedHistory[calibratedHistory.Count - 1].Timestamp).TotalSeconds;
                //double nextExposureTime = calibratedHistory[calibratedHistory.Count - 1].calibratedExposureTime * Math.Exp(k * deltaT);

                // Estimate the time it will take to complete the next exposure
                double nextExposureDuration = currentExposureTime; // Initial guess
                double nextExposureTime = calibratedHistory[calibratedHistory.Count - 1].calibratedExposureTime * Math.Exp(k * (deltaT + nextExposureDuration));

                // Iteratively refine the estimate of nextExposureDuration
                for (int i = 0; i < 3; i++) { // Run 3 iterations for convergence
                    nextExposureDuration = nextExposureTime;
                    nextExposureTime = calibratedHistory[calibratedHistory.Count - 1].calibratedExposureTime * Math.Exp(k * (deltaT + nextExposureDuration));
                }

                return nextExposureTime;
            }

            private void ManageHistory() {
                while (exposureHistory.Count > 5) {
                    exposureHistory.RemoveAt(0);
                }
            }

            public double GetNextExposureTime(TimeSpan delta) {
                var trot = delta - ti - TimeSpan.FromSeconds(currentExposureTime);
                if (trot.TotalMilliseconds < 0) trot = TimeSpan.FromMilliseconds(0);

                var tiPlus1 = Math.Log(
                                    Math.Pow(a, ti.TotalSeconds + trot.TotalSeconds) + s * Math.Log(a)
                              )
                              / Math.Log(a);
                currentExposureTime = tiPlus1 - (ti + trot).TotalSeconds;

                Logger.Debug($"ti:{ti}, trot:{trot}, tiPlus1:{tiPlus1}, eiPlus1:{currentExposureTime}");

                ti = delta;
                return currentExposureTime;
            }
        }
    }
}