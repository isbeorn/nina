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
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
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

            this.Add(new Annotation());
            this.Add(new Annotation());
            this.Add(switchFilter);

            var container = new SequentialContainer();
            container.Add(loopCondition);
            container.Add(takeExposure);
            this.Add(container);

            IsExpanded = false;
            if (cloneMe != null) {
                CopyMetaData(cloneMe);
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
            return (Items[2] as SwitchFilter);
        }

        public SequentialContainer GetImagingContainer() {
            return (Items[1] as SequentialContainer);
        }

        public TakeExposure GetExposureItem() {
            return ((Items[3] as SequentialContainer).Items[0] as TakeExposure);
        }

        public LoopCondition GetIterations() {
            return ((Items[3] as IConditionable).Conditions[0] as LoopCondition);
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

                Logger.Info($"Determining Sky Flat Exposure Time. Min {MinExposure}, Max {MaxExposure}, Target {HistogramTargetPercentage * 100}%, Tolerance {HistogramTolerancePercentage * 100}%");
                var exposureTime = await DetermineExposureTime(MinExposure, MaxExposure, MinExposure, MaxExposure, 0, progress, token);

                if (double.IsNaN(exposureTime)) {
                    throw new SequenceEntityFailedException("Failed to determine exposure time for sky flats");
                } else {
                    // Exposure time has been successfully determined
                    GetExposureItem().ExposureTime = exposureTime;
                }

                await TakeSkyFlats(exposureTime, progress, token);
            } finally {
                await CoreUtil.Wait(TimeSpan.FromMilliseconds(500));
                progress?.Report(new ApplicationStatus() { Source = Loc.Instance["Lbl_SequenceItem_FlatDevice_SkyFlat_Name"] });
            }
        }

        private async Task<double> DetermineExposureTime(double initialMin, double initialMax, double currentMin, double currentMax, int iterations, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            const double TOLERANCE = 0.00001;
            if (iterations >= 20) {
                if (Math.Abs(initialMax - currentMin) < TOLERANCE) {
                    throw new SequenceEntityFailedException(Loc.Instance["Lbl_SequenceItem_FlatDevice_SkyFlat_LightTooDim"]);
                } else {
                    throw new SequenceEntityFailedException(Loc.Instance["Lbl_SequenceItem_FlatDevice_SkyFlat_LightTooBright"]);
                }
            }

            var exposureTime = Math.Round((currentMax + currentMin) / 2d, 5);

            if (Math.Abs(exposureTime - initialMin) < TOLERANCE || Math.Abs(exposureTime - initialMax) < TOLERANCE) {
                // If the exposure time is equal to the min/max and not yield a result it will be skip any more unnecessary attempts
                iterations = 20;
            }

            progress?.Report(new ApplicationStatus() {
                Status = string.Format(Loc.Instance["Lbl_SequenceItem_FlatDevice_SkyFlat_DetermineTime"], exposureTime, iterations, 20),
                Source = Loc.Instance["Lbl_SequenceItem_FlatDevice_SkyFlat_Name"]
            });

            var sequence = new CaptureSequence(exposureTime, CaptureSequence.ImageTypes.FLAT, GetSwitchFilterItem().Filter, GetExposureItem().Binning, 1) { Gain = GetExposureItem().Gain, Offset = GetExposureItem().Offset };

            var image = await imagingMediator.CaptureImage(sequence, ct, progress);

            var imageData = await image.ToImageData(progress, ct);
            await imagingMediator.PrepareImage(imageData, new PrepareImageParameters(true, false), ct);
            var statistics = await imageData.Statistics;

            var mean = statistics.Mean;

            var check = HistogramMath.GetExposureAduState(mean, HistogramTargetPercentage, image.BitDepth, HistogramTolerancePercentage);

            switch (check) {
                case HistogramMath.ExposureAduState.ExposureWithinBounds:
                    DeterminedHistogramADU = mean;
                    Logger.Info($"Found exposure time at {exposureTime}s with histogram ADU {mean}");
                    progress?.Report(new ApplicationStatus() {
                        Status = string.Format(Loc.Instance["Lbl_SequenceItem_FlatDevice_SkyFlat_FoundTime"], exposureTime)
                    });
                    return exposureTime;
                case HistogramMath.ExposureAduState.ExposureBelowLowerBound:
                    Logger.Info($"Exposure too dim at {exposureTime}s. Retrying with higher exposure time");
                    return await DetermineExposureTime(initialMin, initialMax, exposureTime, currentMax, ++iterations, progress, ct);
                case HistogramMath.ExposureAduState:
                    Logger.Info($"Exposure too bright at {exposureTime}s. Retrying with lower exposure time");
                    return await DetermineExposureTime(initialMin, initialMax, currentMin, exposureTime, ++iterations, progress, ct);
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
                if (value >= MaxExposure) {
                    value = MaxExposure;
                }
                minExposure = value;
                RaisePropertyChanged();
            }
        }

        private double maxExposure;

        [JsonProperty]
        public double MaxExposure {
            get => maxExposure;
            set {
                if (value <= MinExposure) {
                    value = MinExposure;
                }
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
        [JsonProperty]
        public bool ShouldDither {
            get => shouldDither;
            set {
                shouldDither = value;
                RaisePropertyChanged();
            }
        }

        public override bool Validate() {
            var switchFilter = GetSwitchFilterItem();
            var takeExposure = GetExposureItem();

            var valid = takeExposure.Validate() && switchFilter.Validate();

            var issues = new ObservableCollection<string>();

            if(ShouldDither) {
                var info = telescopeMediator.GetInfo();
                if (!info.Connected) {
                    issues.Add("Dither between sky flat exposures enabled but telescope is not connected");
                }
                if (!info.CanPulseGuide) {
                    issues.Add("Dither between sky flat exposures enabled but telescope is not capable of pulse guiding");
                }
                if (info.AtPark) {
                    issues.Add("Dither between sky flat exposures enabled but telescope is parked");
                }
            }

            Issues = issues.Concat(takeExposure.Issues).Concat(switchFilter.Issues).Distinct().ToList();
            RaisePropertyChanged(nameof(Issues));

            return valid;
        }


        /// <summary>
        /// This method will take twilight sky flat exposures by adjusting the exposure time based on the changing sky conditions during the runtime.
        /// A paper which explains the math behind the algorithm can be found here
        /// http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.56.3178&rep=rep1&type=pdf
        /// </summary>
        /// <param name="skyFlatTimes"></param>
        /// <param name="firstExposureTime"></param>
        /// <param name="filter"></param>
        /// <param name="pt"></param>
        /// <remarks></remarks>
        /// <returns></returns>
        private async Task TakeSkyFlats(double firstExposureTime, IProgress<ApplicationStatus> mainProgress, CancellationToken token) {
            var dsoContainer = RetrieveTarget(this.Parent);

            var stopWatch = Stopwatch.StartNew();

            var springTwilight = twilightCalculator.GetTwilightDuration(new DateTime(DateTime.Now.Year, 03, 20), 30.0, 0d).TotalMilliseconds;
            var todayTwilight = twilightCalculator.GetTwilightDuration(DateTime.Now, profileService.ActiveProfile.AstrometrySettings.Latitude, profileService.ActiveProfile.AstrometrySettings.Longitude).TotalMilliseconds;

            var tau = todayTwilight / springTwilight;
            var k = DateTime.Now.Hour < 12 ? 0.094 / 60 : -0.094 / 60;

            var s = firstExposureTime;
            var a = Math.Pow(10, k / tau);

            var calculatedExposureTime = firstExposureTime;
            var time = firstExposureTime;

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
            for (var i = 0; i < GetIterations().Iterations; i++) {
                GetIterations().CompletedIterations++;

                var filter = GetSwitchFilterItem().Filter;
                var sequence = new CaptureSequence(time, CaptureSequence.ImageTypes.FLAT, filter, GetExposureItem().Binning, GetIterations().Iterations) { Gain = GetExposureItem().Gain, Offset = GetExposureItem().Offset };
                sequence.ProgressExposureCount = i;
                var ti = stopWatch.Elapsed;

                Task ditherTask = null;

                if(ditherTask != null) {
                    await ditherTask;
                }

                var exposureData =  await imagingMediator.CaptureImage(sequence, token, progress);

                var imageData = await exposureData.ToImageData(progress, token);

                if(ShouldDither && i < (GetIterations().Iterations - 1)) {
                    ditherTask = Dither(progress, token);
                }

                var prepTask = imagingMediator.PrepareImage(imageData, new PrepareImageParameters(true, false), token);

                if (dsoContainer != null) {
                    var target = dsoContainer.Target;
                    if (target != null) {
                        imageData.MetaData.Target.Name = target.DeepSkyObject.NameAsAscii;
                        imageData.MetaData.Target.Coordinates = target.InputCoordinates.Coordinates;
                        imageData.MetaData.Target.PositionAngle = target.PositionAngle;
                    }
                }

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
                                     $"first exposure time {firstExposureTime}, " +
                                     $"current exposure time {time}, " +
                                     $"elapsed time: {stopWatch.ElapsedMilliseconds / 1000d}, " +
                                     $"current mean adu: {imageStatistics.Mean}. " +
                                     $"The sky flat exposure time will be determined again and the exposure will be repeated.");
                        Notification.ShowWarning($"Skyflat correction did not work and is outside of tolerance:" + Environment.NewLine +
                                     $"first exposure time {firstExposureTime:#.####}" + Environment.NewLine +
                                     $"current exposure time {time:#.####}, " + Environment.NewLine +
                                     $"elapsed time: {stopWatch.ElapsedMilliseconds / 1000d:#.##}" + Environment.NewLine +
                                     $"mean adu: {imageStatistics.Mean:#.##}." + Environment.NewLine +
                                     $"The sky flat exposure time will be determined again and the exposure will be repeated.");

                        firstExposureTime = await DetermineExposureTime(MinExposure, MaxExposure, MinExposure, MaxExposure, 0, progress, token);
                        k = DateTime.Now.Hour < 12 ? 0.094 / 60 : -0.094 / 60;
                        a = Math.Pow(10, k / tau);
                        s = firstExposureTime;
                        calculatedExposureTime = firstExposureTime;
                        time = firstExposureTime;
                        Logger.Info($"New exposure time for sky flat determined - {time}");
                        stopWatch = Stopwatch.StartNew();
                        i--;
                        continue;
                }


                await imageSaveMediator.Enqueue(imageData, prepTask, progress, token);

                progress.Report(new ApplicationStatus { Status = Loc.Instance["LblSavingImage"] });
                var trot = stopWatch.Elapsed - ti - TimeSpan.FromMilliseconds(time * 1000d);
                if (trot.TotalMilliseconds < 0) trot = TimeSpan.FromMilliseconds(0);

                var tiPlus1 = Math.Log(Math.Pow(a, ti.TotalMilliseconds / 1000d + trot.TotalMilliseconds / 1000d) + s * Math.Log(a))
                              / Math.Log(a);
                time = tiPlus1 - (ti + trot).TotalMilliseconds / 1000d;

                //TODO: Move this to Trace, once confirmed working well
                Logger.Info($"ti:{ti}, trot:{trot}, tiPlus1:{tiPlus1}, eiPlus1:{time}");
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
                    await directGuider.Dither(progress, token);
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
    }
}