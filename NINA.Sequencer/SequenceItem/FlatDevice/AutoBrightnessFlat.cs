using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Model;
using NINA.Image.ImageAnalysis;
using NINA.Image.ImageData;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem.FilterWheel;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.Utility;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.SequenceItem.FlatDevice {

    [ExportMetadata("Name", "Lbl_SequenceItem_FlatDevice_AutoBrightnessFlat_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_FlatDevice_AutoBrightnessFlat_Description")]
    [ExportMetadata("Icon", "FlatWizardSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_FlatDevice")]
    [Export(typeof(ISequenceItem))]
    [Export(typeof(ISequenceContainer))]
    [JsonObject(MemberSerialization.OptIn)]
    public partial class AutoBrightnessFlat : SequentialContainer, IImmutableContainer {
        private IProfileService profileService;
        private IImagingMediator imagingMediator;

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            this.Items.Clear();
            this.Conditions.Clear();
            this.Triggers.Clear();
        }

        [ImportingConstructor]
        public AutoBrightnessFlat(IProfileService profileService, ICameraMediator cameraMediator, IImagingMediator imagingMediator, IImageSaveMediator imageSaveMediator, IImageHistoryVM imageHistoryVM, IFilterWheelMediator filterWheelMediator, IFlatDeviceMediator flatDeviceMediator) :
            this(
                null,
                profileService,
                imagingMediator,
                imageSaveMediator,
                new CloseCover(flatDeviceMediator),
                new ToggleLight(flatDeviceMediator) { OnOff = true },
                new SwitchFilter(profileService, filterWheelMediator),
                new SetBrightness(flatDeviceMediator),
                new TakeExposure(profileService, cameraMediator, imagingMediator, imageSaveMediator, imageHistoryVM) { ImageType = CaptureSequence.ImageTypes.FLAT },
                new LoopCondition() { Iterations = 1 },
                new ToggleLight(flatDeviceMediator) { OnOff = false },
                new OpenCover(flatDeviceMediator)

            ) {

            HistogramTargetPercentage = 0.5;
            HistogramTolerancePercentage = 0.1;
            MaxBrightness = 100;
            MinBrightness = 20;
            GetExposureItem().ExposureTime = 1;
        }

        private AutoBrightnessFlat(
            AutoBrightnessFlat cloneMe,
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
            var clone = new AutoBrightnessFlat(
                this,
                profileService,
                imagingMediator,
                imageSaveMediator,
                (CloseCover)this.GetCloseCoverItem().Clone(),
                (ToggleLight)this.GetToggleLightItem().Clone(),
                (SwitchFilter)this.GetSwitchFilterItem().Clone(),
                (SetBrightness)this.GetSetBrightnessItem().Clone(),
                (TakeExposure)this.GetExposureItem().Clone(),
                (LoopCondition)this.GetIterations().Clone(),
                (ToggleLight)this.GetToggleLightOffItem().Clone(),
                (OpenCover)this.GetOpenCoverItem().Clone()
            ) {
                MaxBrightness = this.MaxBrightness,
                MinBrightness = this.MinBrightness,
                HistogramTargetPercentage = this.HistogramTargetPercentage,
                HistogramTolerancePercentage = this.HistogramTolerancePercentage,
                KeepPanelClosed = this.KeepPanelClosed
            };
            return clone;
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

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            IProgress<ApplicationStatus> localProgress = new Progress<ApplicationStatus>(x => {
                x.Source = Loc.Instance["Lbl_SequenceItem_FlatDevice_AutoBrightnessFlat_Name"];
                progress?.Report(x);
            });
            try {
                DeterminedHistogramADU = 0;
                var loop = GetIterations();
                if (loop.CompletedIterations >= loop.Iterations) {
                    Logger.Warning($"The {nameof(AutoBrightnessFlat)} progress is already complete ({loop.CompletedIterations}/{loop.Iterations}). The instruction will be skipped");
                    throw new SequenceItemSkippedException($"The {nameof(AutoBrightnessFlat)} progress is already complete ({loop.CompletedIterations}/{loop.Iterations}). The instruction will be skipped");
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
                    openItem.ResetProgress();
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
                }

                var toggleLightOff = GetToggleLightOffItem();
                if (!toggleLightOff.Validate()) {
                    toggleLightOff.Skip();
                } else {
                    toggleLightOff.ResetProgress();
                }

                GetIterations().ResetProgress();

                Logger.Info($"Determining Dynamic Exposure Time. Min {MinBrightness}, Max {MaxBrightness}, Exposure {GetExposureItem().ExposureTime}, Target {HistogramTargetPercentage * 100}%, Tolerance {HistogramTolerancePercentage * 100}%");
                var brightness = await DetermineBrightness(MinBrightness, MaxBrightness, localProgress, token);

                if (brightness < 0) {
                    throw new SequenceEntityFailedException("Failed to determine brightness for flats");
                } else {
                    // Exposure time has been successfully determined. Set the time and record it for the trained flats
                    setBrightness.Brightness = brightness;

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

                // we start at exposure + 1, as DetermineBrightness is already saving an exposure
                GetIterations().CompletedIterations++;
                GetExposureItem().ExposureCount = 1;

                await base.Execute(
                    new Progress<ApplicationStatus>(
                        x => localProgress?.Report(new ApplicationStatus() {
                            Status = string.Format(Loc.Instance["Lbl_SequenceItem_FlatDevice_AutoBrightnessFlat_FoundBrightness"], brightness),
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
            } catch (SequenceEntityFailedException) {
                // Ensure the light is turned off when it fails
                await GetToggleLightOffItem().Run(localProgress, token);
                throw;            
            } finally {
                await CoreUtil.Wait(TimeSpan.FromMilliseconds(500));
                localProgress?.Report(new ApplicationStatus() { });
            }
        }

        private async Task<int> DetermineBrightness(int initialMin, int initialMax, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            var currentMin = initialMin;
            var currentMax = initialMax;
            for (var iterations = 0; iterations <= 20; iterations++) {
                var brightness = (int)Math.Round((currentMax + currentMin) / 2d);

                if (Math.Abs(brightness - initialMin) <= 0 || Math.Abs(brightness - initialMax) <= 0) {
                    // If the exposure time is equal to the min/max and not yield a result it will be skip any more unnecessary attempts
                    iterations = 20;
                }

                progress?.Report(new ApplicationStatus() {
                    Status = string.Format(Loc.Instance["Lbl_SequenceItem_FlatDevice_AutoBrightnessFlat_DetermineBrightness"], brightness, iterations, 20),
                    Source = Loc.Instance["Lbl_SequenceItem_FlatDevice_AutoBrightnessFlat_Name"]
                });

                var setBrightnes = GetSetBrightnessItem();
                setBrightnes.ResetProgress();
                setBrightnes.Brightness = brightness;
                await setBrightnes.Run(progress, ct);

                var sequence = new CaptureSequence(GetExposureItem().ExposureTime, CaptureSequence.ImageTypes.FLAT, GetSwitchFilterItem().Filter, GetExposureItem().Binning, 1) { Gain = GetExposureItem().Gain, Offset = GetExposureItem().Offset };

                var image = await imagingMediator.CaptureImage(sequence, ct, progress);

                var imageData = await image.ToImageData(progress, ct);
                var prepTask = imagingMediator.PrepareImage(imageData, new PrepareImageParameters(true, false), ct);
                await prepTask;
                var statistics = await imageData.Statistics;

                var mean = statistics.Mean;

                var check = HistogramMath.GetExposureAduState(mean, HistogramTargetPercentage, image.BitDepth, HistogramTolerancePercentage);

                DeterminedHistogramADU = mean;

                switch (check) {
                    case HistogramMath.ExposureAduState.ExposureWithinBounds:
                        // Go ahead and save the exposure, as it already fits the parameters
                        FillTargetMetaData(imageData.MetaData);
                        await imageSaveMediator.Enqueue(imageData, prepTask, progress, ct);

                        Logger.Info($"Found brightness at panel brightness {brightness} with histogram ADU {mean}");
                        progress?.Report(new ApplicationStatus() {
                            Status = string.Format(Loc.Instance["Lbl_SequenceItem_FlatDevice_AutoBrightnessFlat_FoundBrightness"], brightness),
                        });
                        return brightness;
                    case HistogramMath.ExposureAduState.ExposureBelowLowerBound:
                        Logger.Info($"Exposure too dim at panel brightness {brightness}. ADU measured at: {DeterminedHistogramADU}. Retrying with higher brightness");
                        currentMin = brightness;
                        break;
                    case HistogramMath.ExposureAduState:
                        Logger.Info($"Exposure too bright at panel brightness {brightness}. ADU measured at: {DeterminedHistogramADU}. Retrying with lower brightness");
                        currentMax = brightness;
                        break;
                }
            }

            if (Math.Abs(initialMax - currentMin) <= 0) {
                throw new SequenceEntityFailedException(Loc.Instance["Lbl_SequenceItem_FlatDevice_AutoBrightnessFlat_LightTooDim"]);
            } else {
                throw new SequenceEntityFailedException(Loc.Instance["Lbl_SequenceItem_FlatDevice_AutoBrightnessFlat_LightTooBright"]);
            }

            
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

        private int minBrightness;

        [JsonProperty]
        public int MinBrightness {
            get => minBrightness;
            set {
                minBrightness = value;
                RaisePropertyChanged();
            }
        }

        private int maxBrightness;

        [JsonProperty]
        public int MaxBrightness {
            get => maxBrightness;
            set {
                maxBrightness = value;
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
        private readonly IImageSaveMediator imageSaveMediator;

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
            var setBrightness = GetSetBrightnessItem();

            var valid = takeExposure.Validate() && switchFilter.Validate() && setBrightness.Validate();

            var issues = new ObservableCollection<string>();

            if (MinBrightness > MaxBrightness) {
                issues.Add(Loc.Instance["Lbl_SequenceItem_FlatDevice_AutoBrightnessFlat_Validation_InputRangeInvalid"]);
            }

            Issues = issues.Concat(takeExposure.Issues).Concat(switchFilter.Issues).Concat(setBrightness.Issues).Distinct().ToList();
            RaisePropertyChanged(nameof(Issues));

            return valid;
        }
    }
}