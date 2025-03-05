using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Model;
using NINA.Image.ImageAnalysis;
using NINA.Image.ImageData;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.Generators;
using NINA.Sequencer.Logic;
using NINA.Sequencer.SequenceItem.FilterWheel;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.Utility;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.SequenceItem.FlatDevice {

    [ExportMetadata("Name", "Lbl_SequenceItem_FlatDevice_AutoExposureFlat_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_FlatDevice_AutoExposureFlat_Description")]
    [ExportMetadata("Icon", "FlatWizardSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_FlatDevice")]
    [Export(typeof(ISequenceItem))]
    [Export(typeof(ISequenceContainer))]
    [JsonObject(MemberSerialization.OptIn)]
    [UsesExpressions]

    public partial class AutoExposureFlat : SequentialContainer, IImmutableContainer {
        private IProfileService profileService;
        private IImagingMediator imagingMediator;
        private IImageSaveMediator imageSaveMediator;
        private ISymbolBrokerVM symbolBroker;


        private bool cameraIsLinear = true;

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            this.Items.Clear();
            this.Conditions.Clear();
            this.Triggers.Clear();
        }

        [ImportingConstructor]
        public AutoExposureFlat(IProfileService profileService, ICameraMediator cameraMediator, IImagingMediator imagingMediator,
            IImageSaveMediator imageSaveMediator, IImageHistoryVM imageHistoryVM, IFilterWheelMediator filterWheelMediator, IFlatDeviceMediator flatDeviceMediator) :
            this(
                null,
                profileService,
                imagingMediator,
                imageSaveMediator,
                new CloseCover(flatDeviceMediator),
                new ToggleLight(flatDeviceMediator) { OnOff = true },
                new SwitchFilter(profileService, filterWheelMediator),
                new SetBrightness(flatDeviceMediator) { Brightness = 50 },
                new TakeExposure(profileService, cameraMediator, imagingMediator, imageSaveMediator, imageHistoryVM) { ImageType = CaptureSequence.ImageTypes.FLAT },
                new LoopCondition() { Iterations = 1 },
                new ToggleLight(flatDeviceMediator) { OnOff = false },
                new OpenCover(flatDeviceMediator)

            ) {

            HistogramTargetPercentage = 0.5;
            HistogramTolerancePercentage = 0.1;
        }

        private AutoExposureFlat(
            AutoExposureFlat cloneMe,
            IProfileService profileService,
            IImagingMediator imagingMediator,
            IImageSaveMediator imageSaveMediator,
            CloseCover closeCover,
            ToggleLight toggleLightOn,
            SwitchFilter switchFilter,
            SetBrightness setBrightness,
            TakeExposure takeExposure,
            LoopCondition loopCondition,
            ToggleLight toggleLightOff,
            OpenCover openCover
        ) {
            this.profileService = profileService;
            this.imagingMediator = imagingMediator;
            this.imageSaveMediator = imageSaveMediator;

            this.Add(closeCover);
            this.Add(toggleLightOn);
            this.Add(switchFilter);
            this.Add(setBrightness);

            var container = new SequentialContainer();
            container.Add(loopCondition);
            container.Add(takeExposure);
            this.Add(container);

            this.Add(toggleLightOff);
            this.Add(openCover);

            IsExpanded = false;
            if (cloneMe != null) {
                CopyMetaData(cloneMe);
            }
        }

        partial void AfterClone(AutoExposureFlat clone) {
            // The order of these matters!
            clone.profileService = profileService;
            clone.imagingMediator = imagingMediator;
            clone.imageSaveMediator = imageSaveMediator;

            clone.Add((CloseCover)GetCloseCoverItem().Clone());
            clone.Add((ToggleLight)GetToggleLightItem().Clone());
            clone.Add((SwitchFilter)GetSwitchFilterItem().Clone());
            clone.Add((SetBrightness)GetSetBrightnessItem().Clone());
            clone.Add((SequenceContainer)GetImagingContainer().Clone());
            clone.Add((ToggleLight)GetToggleLightOffItem().Clone());
            clone.Add(GetOpenCoverItem());

            clone.HistogramTargetPercentage = HistogramTargetPercentage;
            clone.HistogramTolerancePercentage = HistogramTolerancePercentage;
            clone.KeepPanelClosed = KeepPanelClosed;
            clone.IsExpanded = false;
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

        private AutoExposureFlat(AutoExposureFlat cloneMe) {
            if (cloneMe != null) {
                CopyMetaData(cloneMe);
            }
        }

        public CloseCover GetCloseCoverItem() {
            return (Items[0] as CloseCover);
        }

        public ToggleLight GetToggleLightItem() {
            return (Items[1] as ToggleLight);
        }

        public SwitchFilter GetSwitchFilterItem() {
            return (Items[2] as SwitchFilter);
        }

        public SetBrightness GetSetBrightnessItem() {
            return (Items[3] as SetBrightness);
        }

        public SequentialContainer GetImagingContainer() {
            return (Items[4] as SequentialContainer);
        }

        public TakeExposure GetExposureItem() {
            return ((Items[4] as SequentialContainer).Items[0] as TakeExposure);
        }

        public LoopCondition GetIterations() {
            return ((Items[4] as IConditionable).Conditions[0] as LoopCondition);
        }

        public ToggleLight GetToggleLightOffItem() {
            return (Items[5] as ToggleLight);
        }

        public OpenCover GetOpenCoverItem() {
            return (Items[6] as OpenCover);
        }

        public SequentialContainer ImagingContainer {
            get => GetImagingContainer();
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            IProgress<ApplicationStatus> localProgress = new Progress<ApplicationStatus>(x => {
                x.Source = Loc.Instance["Lbl_SequenceItem_FlatDevice_AutoExposureFlat_Name"];
                progress?.Report(x);
            });
            try {

                DeterminedHistogramADU = 0;
                var loop = GetIterations();
                if (loop.CompletedIterations >= loop.Iterations) {
                    Logger.Warning($"The {nameof(AutoExposureFlat)} progress is already complete ({loop.CompletedIterations}/{loop.Iterations}). The instruction will be skipped");
                    throw new SequenceItemSkippedException($"The {nameof(AutoExposureFlat)} progress is already complete ({loop.CompletedIterations}/{loop.Iterations}). The instruction will be skipped");
                }

                var closeItem = GetCloseCoverItem();
                if (!closeItem.Validate()) {
                    /* Panel most likely cannot open/close so it should just be skipped */
                    closeItem.Skip();
                } else {
                    closeItem.ResetProgress();
                    await closeItem.Run(localProgress, token);
                }

                var openItem = GetOpenCoverItem();
                if (KeepPanelClosed || !openItem.Validate()) {
                    openItem.Skip();
                } else {
                    GetOpenCoverItem().ResetProgress();
                }

                var toggleLight = GetToggleLightItem();
                var setBrightness = GetSetBrightnessItem();
                if (!toggleLight.Validate()) {
                    toggleLight.Skip();
                    setBrightness.Skip();
                } else {
                    toggleLight.ResetProgress();
                    setBrightness.ResetProgress();
                    await toggleLight.Run(localProgress, token);
                    await setBrightness.Run(localProgress, token);
                }

                var toggleLightOff = GetToggleLightOffItem();
                if (!toggleLightOff.Validate()) {
                    toggleLightOff.Skip();
                } else {
                    toggleLightOff.ResetProgress();
                }

                GetIterations().ResetProgress();

                Logger.Info($"Determining Dynamic Exposure Time. Min {MinExposure}, Max {MaxExposure}, Brightness {setBrightness.Brightness}, Target {HistogramTargetPercentage * 100}%, Tolerance {HistogramTolerancePercentage * 100}%");
                var exposureTime = await DetermineExposureTime(MinExposure, MaxExposure, localProgress, token);

                if (double.IsNaN(exposureTime)) {
                    throw new SequenceEntityFailedException("Failed to determine expsoure time for flats");
                } else {
                    // Exposure time has been successfully determined. Set the time and record it for the trained flats
                    GetExposureItem().ExposureTime = exposureTime;

                    if (setBrightness.Validate()) {
                        // Only add the trained setting when the flat device is connected and operational
                        profileService.ActiveProfile.FlatDeviceSettings.AddTrainedFlatExposureSetting(
                            GetSwitchFilterItem().Filter?.Position,
                            GetExposureItem().Binning,
                            GetExposureItem().Gain,
                            GetExposureItem().Offset,
                            setBrightness.Brightness,
                            GetExposureItem().ExposureTime);
                    }
                }

                // we start at exposure + 1, as DetermineExposureTime is already saving an exposure
                GetIterations().CompletedIterations++;
                GetExposureItem().ExposureCount = 1;

                await base.Execute(
                    new Progress<ApplicationStatus>(
                        x => localProgress?.Report(new ApplicationStatus() {
                            Status = string.Format(Loc.Instance["Lbl_SequenceItem_FlatDevice_AutoExposureFlat_FoundTime"], exposureTime),
                            Progress = GetIterations().CompletedIterations + 1,
                            MaxProgress = GetIterations().Iterations,
                            ProgressType = ApplicationStatus.StatusProgressType.ValueOfMaxValue,
                            Status2 = x.Status,
                            Progress2 = x.Progress,
                            ProgressType2 = x.ProgressType,
                            MaxProgress2 = x.MaxProgress
                        })
                    ),
                    token
                );
            } finally {
                await CoreUtil.Wait(TimeSpan.FromMilliseconds(500));
                localProgress?.Report(new ApplicationStatus() { });
            }
        }

        private async Task<double> DetermineExposureTime(double initialMin, double initialMax, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            const double TOLERANCE = 0.00001;

            var currentMin = initialMin;
            var currentMax = initialMax;
            var lastExposureTime = 0d;
            var determinedHistogramADUpercentage = 0d;
            cameraIsLinear = true;
            const int MAX_LINEAR_TEST_ATTEMPTS = 3;
            int linearTestAttempts = 0;
            var exposureAduPairs = new List<(double exposure, double adu)>();
            for (var iterations = 0; iterations <= 20; iterations++) {
                var exposureTime = Math.Round((currentMax + currentMin) / 2d, 5);

                if (cameraIsLinear) {
                    if (lastExposureTime != 0d && (determinedHistogramADUpercentage >= 0.1 && determinedHistogramADUpercentage <= 0.9)) {
                        exposureTime = Math.Round(lastExposureTime * (HistogramTargetPercentage / determinedHistogramADUpercentage), 5);

                        exposureTime = Math.Min(exposureTime, initialMax);
                        exposureTime = Math.Max(exposureTime, initialMin);
                    }
                }     

                if (Math.Abs(exposureTime - initialMin) < TOLERANCE || Math.Abs(exposureTime - initialMax) < TOLERANCE) {
                    // If the exposure time is equal to the min/max and not yield a result it will be skip any more unnecessary attempts
                    iterations = 20;
                }

                progress?.Report(new ApplicationStatus() {
                    Status = string.Format(Loc.Instance["Lbl_SequenceItem_FlatDevice_AutoExposureFlat_DetermineTime"], exposureTime, iterations, 20),
                    Source = Loc.Instance["Lbl_SequenceItem_FlatDevice_AutoExposureFlat_Name"]
                });

                var sequence = new CaptureSequence(exposureTime, CaptureSequence.ImageTypes.FLAT, GetSwitchFilterItem().Filter, GetExposureItem().Binning, 1) { Gain = GetExposureItem().Gain, Offset = GetExposureItem().Offset };

                var image = await imagingMediator.CaptureImage(sequence, ct, progress);

                var imageData = await image.ToImageData(progress, ct);
                var prepTask = imagingMediator.PrepareImage(imageData, new PrepareImageParameters(true, false), ct);
                await prepTask;
                var statistics = await imageData.Statistics;

                var mean = statistics.Mean;

                var check = HistogramMath.GetExposureAduState(mean, HistogramTargetPercentage, image.BitDepth, HistogramTolerancePercentage);

                DeterminedHistogramADU = mean;
                determinedHistogramADUpercentage = (mean / (Math.Pow(2, imageData.Properties.BitDepth) - 1));
                lastExposureTime = exposureTime;
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
                this.GetExposureItem().ExposureTime = exposureTime;

                switch (check) {
                    case HistogramMath.ExposureAduState.ExposureWithinBounds:
                        // Go ahead and save the exposure, as it already fits the parameters
                        FillTargetMetaData(imageData.MetaData);
                        await imageSaveMediator.Enqueue(imageData, prepTask, progress, ct);

                        Logger.Info($"Found exposure time at {exposureTime}s with histogram ADU {mean}");
                        progress?.Report(new ApplicationStatus() {
                            Status = string.Format(Loc.Instance["Lbl_SequenceItem_FlatDevice_AutoExposureFlat_FoundTime"], exposureTime)
                        });
                        return exposureTime;
                    case HistogramMath.ExposureAduState.ExposureBelowLowerBound:
                        Logger.Info($"Exposure too dim at {exposureTime}s. ADU measured at: {DeterminedHistogramADU}. Retrying with higher exposure time");
                        currentMin = exposureTime;
                        break;
                    case HistogramMath.ExposureAduState:
                        Logger.Info($"Exposure too bright at {exposureTime}s. ADU measured at: {DeterminedHistogramADU}. Retrying with lower exposure time");
                        currentMax = exposureTime;
                        break;
                }
            }

            if (Math.Abs(initialMax - currentMin) < TOLERANCE) {
                throw new SequenceEntityFailedException(Loc.Instance["Lbl_SequenceItem_FlatDevice_AutoExposureFlat_LightTooDim"]);
            } else {
                throw new SequenceEntityFailedException(Loc.Instance["Lbl_SequenceItem_FlatDevice_AutoExposureFlat_LightTooBright"]);
            }

        }

        private bool TestLinearity(List<(double exposure, double adu)> exposureAduPairs) {
            const double LINEAR_R_SQUARED_THRESHOLD = 0.95;

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

        /// <summary>
        /// When an inner instruction interrupts this set, it should reroute the interrupt to the real parent set
        /// </summary>
        /// <returns></returns>
        public override Task Interrupt() {
            return this.Parent?.Interrupt();
        }

        [ObservableProperty]
        private double determinedHistogramADU;

        private bool keepPanelClosed;

        [JsonProperty]
        public bool KeepPanelClosed {
            get => keepPanelClosed;
            set {
                keepPanelClosed = value;

                RaisePropertyChanged();
            }
        }

        [IsExpression (Default = 10)]
        private double minExposure;

        [IsExpression (Default = 20)]
        private double maxExposure;

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

        public override bool Validate() {
            var switchFilter = GetSwitchFilterItem();
            var takeExposure = GetExposureItem();

            var valid = takeExposure.Validate() && switchFilter.Validate();


            var issues = new ObservableCollection<string>();

            if (MinExposure > MaxExposure) {
                issues.Add(Loc.Instance["Lbl_SequenceItem_FlatDevice_AutoExposureFlat_Validation_InputRangeInvalid"]);
            }

            Issues = issues.Concat(takeExposure.Issues).Concat(switchFilter.Issues).Distinct().ToList();
            Expression.ValidateExpressions(Issues, MinExposureExpression, MaxExposureExpression);
            RaisePropertyChanged(nameof(Issues));

            return Issues.Count == 0;
        }
    }
}