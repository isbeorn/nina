#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using NINA.Core.Locale;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Equipment.MyFlatDevice;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Profile.Interfaces;
using NINA.Utility;
using NINA.Astrometry;
using NINA.ViewModel.Interfaces;
using Nito.AsyncEx;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using NINA.Core.Enum;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Core.Utility;
using NINA.Core.Interfaces;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Equipment.Model;
using NINA.Image.ImageAnalysis;
using NINA.Core.Utility.Notification;
using NINA.Image.FileFormat;
using NINA.Core.Utility.WindowService;
using NINA.Profile;
using NINA.Astrometry.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Equipment.Equipment;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.ViewModel;
using NINA.WPF.Base.Model;
using NINA.WPF.Base.Utility.AutoFocus;
using NINA.Sequencer.SequenceItem.FlatDevice;
using NINA.Sequencer.SequenceItem;
using NINA.ViewModel.ImageHistory;
using NINA.WPF.Base.Mediator;
using System.Reflection;
using NINA.Sequencer.Container;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NINA.ViewModel.FlatWizard {
    
    internal partial class FlatWizardVM : DockableVM, IFlatWizardVM {
        private readonly IImageSaveMediator imageSaveMediator;
        private readonly IApplicationStatusMediator applicationStatusMediator;
        private readonly IFlatDeviceMediator flatDeviceMediator;
        private readonly IImagingVM imagingVM;
        private readonly IFilterWheelMediator filterWheelMediator;
        private readonly ICameraMediator cameraMediator;
        private readonly ITelescopeMediator telescopeMediator;
        private readonly IProgress<ApplicationStatus> progress;
        private readonly IMyMessageBoxVM messageBox;
        private readonly ITwilightCalculator twilightCalculator;
        private readonly INighttimeCalculator nighttimeCalculator;
        private ObserveAllCollection<FilterInfo> watchedFilterList;

        public FlatWizardVM(IProfileService profileService,
                            IImagingVM imagingVM,
                            ICameraMediator cameraMediator,
                            IFilterWheelMediator filterWheelMediator,
                            ITelescopeMediator telescopeMediator,
                            IFlatDeviceMediator flatDeviceMediator,
                            IImageGeometryProvider imageGeometryProvider,
                            IApplicationStatusMediator applicationStatusMediator,
                            IMyMessageBoxVM messageBox,
                            INighttimeCalculator nighttimeCalculator,
                            ITwilightCalculator twilightCalculator,
                            IImageSaveMediator imageSaveMediator) : base(profileService) {
            Title = Loc.Instance["LblFlatWizard"];
            ImageGeometry = imageGeometryProvider.GetImageGeometry("FlatWizardSVG");

            var pauseTokenSource = new PauseTokenSource();

            progress = new Progress<ApplicationStatus>(p => Status = p);
            StartFlatSequenceCommand = new AsyncCommand<bool>(() => CaptureForSingleFilter(pauseTokenSource.Token), (object o) => CameraInfo.Connected && cameraMediator.IsFreeToCapture(this));
            StartMultiFlatSequenceCommand = new AsyncCommand<bool>(() => CaptureForSelectedFilters(pauseTokenSource.Token), (object o) => CameraInfo.Connected && filterWheelInfo.Connected && cameraMediator.IsFreeToCapture(this));
            SlewToZenithCommand = new AsyncCommand<bool>(() => SlewToZenith(CancellationToken.None), (object o) => telescopeInfo.Connected);

            CancelFlatExposureSequenceCommand = new RelayCommand(CancelFindExposureTime);
            PauseFlatExposureSequenceCommand = new RelayCommand(obj => { IsPaused = true; pauseTokenSource.IsPaused = IsPaused; });
            ResumeFlatExposureSequenceCommand = new RelayCommand(obj => { IsPaused = false; pauseTokenSource.IsPaused = IsPaused; });

            FlatCount = profileService.ActiveProfile.FlatWizardSettings.FlatCount;
            DarkFlatCount = profileService.ActiveProfile.FlatWizardSettings.DarkFlatCount;
            AltitudeSite = profileService.ActiveProfile.FlatWizardSettings.AltitudeSite;
            FlatWizardMode = profileService.ActiveProfile.FlatWizardSettings.FlatWizardMode;

            profileService.ProfileChanged += (sender, args) => {
                UpdateSingleFlatWizardFilterSettings();
                watchedFilterList.CollectionChanged -= FiltersCollectionChanged;
                watchedFilterList = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
                watchedFilterList.CollectionChanged += FiltersCollectionChanged;
                UpdateFilterWheelsSettings();
            };

            watchedFilterList = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
            watchedFilterList.CollectionChanged += FiltersCollectionChanged;

            // first update filters
            UpdateSingleFlatWizardFilterSettings();
            UpdateFilterWheelsSettings();

            // then register consumers and get the cameraInfo so it's populated to all filters including the singleflatwizardfiltersettings
            this.cameraMediator = cameraMediator;
            this.cameraMediator.RegisterConsumer(this);
            this.filterWheelMediator = filterWheelMediator;
            this.filterWheelMediator.RegisterConsumer(this);
            this.telescopeMediator = telescopeMediator;
            this.telescopeMediator.RegisterConsumer(this);
            this.flatDeviceMediator = flatDeviceMediator;
            this.flatDeviceMediator.RegisterConsumer(this);
            this.imageSaveMediator = imageSaveMediator;

            this.applicationStatusMediator = applicationStatusMediator;
            this.imagingVM = imagingVM;
            this.messageBox = messageBox;
            this.twilightCalculator = twilightCalculator;
            this.nighttimeCalculator = nighttimeCalculator;

            TargetName = "FlatWizard";
        }

        public void Dispose() {
            imagingVM.Dispose();
            cameraMediator.RemoveConsumer(this);
            filterWheelMediator.RemoveConsumer(this);
            telescopeMediator.RemoveConsumer(this);
            flatDeviceMediator.RemoveConsumer(this);
        }

 

        public AltitudeSite AltitudeSite {
            get => profileService.ActiveProfile.FlatWizardSettings.AltitudeSite;
            set {
                if (profileService.ActiveProfile.FlatWizardSettings.AltitudeSite != value) {
                    profileService.ActiveProfile.FlatWizardSettings.AltitudeSite = value;
                    RaisePropertyChanged();
                }
            }
        }

        private async Task<bool> SlewToZenith(CancellationToken token) {
            var latitude = Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Latitude);
            var longitude = Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Longitude);
            var elevation = profileService.ActiveProfile.AstrometrySettings.Elevation;
            var azimuth = AltitudeSite == AltitudeSite.WEST ? Angle.ByDegree(90) : Angle.ByDegree(270);
            await telescopeMediator.SlewToCoordinatesAsync(new TopocentricCoordinates(azimuth, Angle.ByDegree(89), latitude, longitude, elevation), token);
            telescopeMediator.SetTrackingEnabled(false);
            return true;
        }

        private void UpdateSingleFlatWizardFilterSettings() {
            if (SingleFlatWizardFilterSettings != null) {
                SingleFlatWizardFilterSettings.Settings.PropertyChanged -= UpdateProfileValues;
            }

            var bitDepth = GetBitDepth();

            SingleFlatWizardFilterSettings = new FlatWizardFilterSettingsWrapper(null, new FlatWizardFilterSettings {
                HistogramMeanTarget = profileService.ActiveProfile.FlatWizardSettings.HistogramMeanTarget,
                HistogramTolerance = profileService.ActiveProfile.FlatWizardSettings.HistogramTolerance,
                MaxFlatExposureTime = profileService.ActiveProfile.CameraSettings.MaxFlatExposureTime,
                MinFlatExposureTime = profileService.ActiveProfile.CameraSettings.MinFlatExposureTime                
            }, bitDepth, CameraInfo, flatDeviceInfo);
            SingleFlatWizardFilterSettings.Settings.PropertyChanged += UpdateProfileValues;
        }

        private int GetBitDepth() {
            var bitDepth = CameraInfo?.BitDepth ?? (int)profileService.ActiveProfile.CameraSettings.BitDepth;

            return bitDepth;
        }

        public IImagingVM ImagingVM => imagingVM;


        private double calculatedExposureTime;

        public double CalculatedExposureTime {
            get => calculatedExposureTime;
            set {
                calculatedExposureTime = value;
                RaisePropertyChanged();
            }
        }

        private double calculatedHistogramMean;

        public double CalculatedHistogramMean {
            get => calculatedHistogramMean;
            set {
                calculatedHistogramMean = value;
                RaisePropertyChanged();
            }
        }

        private bool cameraConnected;

        public bool CameraConnected {
            get => cameraConnected;
            set {
                cameraConnected = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(StartFlatExposureTooltip));
                RaisePropertyChanged(nameof(StartFlatExposureMultiTooltip));
            }
        }

        public string StartFlatExposureTooltip {
            get {
                if (!CameraConnected) return Loc.Instance["LblCameraNotConnected"];
                if (!cameraMediator.IsFreeToCapture(this)) return Loc.Instance["LblCameraBusy"];
                return Loc.Instance["LblStartSequence"];
            }
        }
        public string StartFlatExposureMultiTooltip {
            get {
                if (!CameraConnected) return Loc.Instance["LblCameraNotConnected"];
                if (!cameraMediator.IsFreeToCapture(this)) return Loc.Instance["LblCameraBusy"];
                if (!filterWheelInfo.Connected) return Loc.Instance["LblFilterWheelNotConnected"];
                return Loc.Instance["LblStartSequence"];
            }
        }

        public string SlewToZenithTooltip => !telescopeInfo.Connected ? Loc.Instance["LblTelescopeNotConnected"] : "";

        public bool SlewToZenithTooltipEnabled => !telescopeInfo.Connected;

        private string targetName;

        public string TargetName {
            get => targetName;
            set {
                targetName = value;
                RaisePropertyChanged();
            }
        }
        
        public ObservableCollection<FilterInfo> FilterInfos => new ObservableCollection<FilterInfo>(Filters.Select(f => f.Filter).ToList());

        private ObservableCollection<FlatWizardFilterSettingsWrapper> filters;

        public ObservableCollection<FlatWizardFilterSettingsWrapper> Filters {
            get => filters ?? (filters = new ObservableCollection<FlatWizardFilterSettingsWrapper>());
            set {
                filters = value;
                RaisePropertyChanged();
            }
        }

        private int flatCount;

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

        private bool isPaused;

        public bool IsPaused {
            get => isPaused;
            set {
                isPaused = value;
                RaisePropertyChanged();
            }
        }

        private int mode;

        public int Mode {
            get => mode;
            set {
                mode = value;
                RaisePropertyChanged();
            }
        }

        public FilterInfo SelectedFilter {
            get => singleFlatWizardFilterSettings.Filter;
            set {
                singleFlatWizardFilterSettings.Filter = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(SingleFlatWizardFilterSettings));
            }
        }

        private CameraInfo cameraInfo;
        public CameraInfo CameraInfo {
            get => cameraInfo ?? DeviceInfo.CreateDefaultInstance<CameraInfo>();
            set {
                cameraInfo = value;
                RaisePropertyChanged();
            }
        }

        private FlatWizardFilterSettingsWrapper singleFlatWizardFilterSettings;

        public FlatWizardFilterSettingsWrapper SingleFlatWizardFilterSettings {
            get => singleFlatWizardFilterSettings;
            set {
                singleFlatWizardFilterSettings = value;
                value.IsChecked = true;
                RaisePropertyChanged();
            }
        }

        private ApplicationStatus status;
        private ApplicationStatus prevStatus;

        public ApplicationStatus Status {
            get => status;
            set {
                status = value;
                if (status.Source == null) {
                    status.Source = Loc.Instance["LblFlatWizardCapture"];
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

        private bool pauseBetweenFilters;

        public bool PauseBetweenFilters {
            get => pauseBetweenFilters;
            set {
                pauseBetweenFilters = value;
                RaisePropertyChanged();
            }
        }

        public FlatWizardMode FlatWizardMode {
            get => profileService.ActiveProfile.FlatWizardSettings.FlatWizardMode;
            set {
                if (profileService.ActiveProfile.FlatWizardSettings.FlatWizardMode != value) {
                    profileService.ActiveProfile.FlatWizardSettings.FlatWizardMode = value;
                    RaisePropertyChanged();
                }
            }
        }

        private FlatDeviceInfo flatDeviceInfo;

        public FlatDeviceInfo FlatDeviceInfo {
            get => flatDeviceInfo;
            private set {
                flatDeviceInfo = value;
                RaisePropertyChanged();
            }
        }

        private CancellationTokenSource flatSequenceCts = new CancellationTokenSource();

        private void CancelFindExposureTime(object obj) {
            try { flatSequenceCts?.Cancel(); } catch { }
        }

        private async Task<bool> CaptureForSelectedFilters(PauseToken pt) {
            try {
                cameraMediator.RegisterCaptureBlock(this);
                return await StartFlatMagic(Filters, pt);
            } finally {
                cameraMediator.ReleaseCaptureBlock(this);
            }
        }

        private async Task<bool> CaptureForSingleFilter(PauseToken pt) {
            try {
                cameraMediator.RegisterCaptureBlock(this);      

                return await StartFlatMagic(new List<FlatWizardFilterSettingsWrapper> { SingleFlatWizardFilterSettings }, pt);
            } finally {
                cameraMediator.ReleaseCaptureBlock(this);
            }
        }

        private SequenceContainer GetInstructionForMode(FlatWizardFilterSettingsWrapper settings) {
            IImagingMediator imagingMediator = (IImagingMediator)typeof(ImagingVM).GetField("imagingMediator", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(imagingVM);
            IImageHistoryVM imageHistoryVM = (IImageHistoryVM)typeof(ImagingVM).GetField("imageHistoryVM", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(imagingVM);

            SequenceContainer sequenceItem;
            switch (FlatWizardMode) {
                case FlatWizardMode.DYNAMICBRIGHTNESS:
                    sequenceItem = new AutoBrightnessFlat(profileService, cameraMediator, imagingMediator, imageSaveMediator, imageHistoryVM, filterWheelMediator, flatDeviceMediator);
                    var autoBrightnessFlat = sequenceItem as AutoBrightnessFlat;
                    autoBrightnessFlat.GetSwitchFilterItem().Filter = settings.Filter;
                    autoBrightnessFlat.MaxBrightness = settings.Settings.MaxAbsoluteFlatDeviceBrightness;
                    autoBrightnessFlat.MinBrightness = settings.Settings.MinAbsoluteFlatDeviceBrightness;
                    autoBrightnessFlat.GetExposureItem().Binning = settings.Settings.Binning;
                    autoBrightnessFlat.GetExposureItem().Gain = settings.Settings.Gain;
                    autoBrightnessFlat.GetExposureItem().Offset = settings.Settings.Offset;
                    autoBrightnessFlat.GetExposureItem().ExposureTime = settings.Settings.MaxFlatExposureTime;
                    autoBrightnessFlat.HistogramTargetPercentage = settings.Settings.HistogramMeanTarget;
                    autoBrightnessFlat.HistogramTolerancePercentage = settings.Settings.HistogramTolerance;
                    autoBrightnessFlat.GetIterations().Iterations = FlatCount;
                    break;
                case FlatWizardMode.SKYFLAT:
                    sequenceItem = new SkyFlat(profileService, cameraMediator, telescopeMediator, imagingMediator, imageSaveMediator, imageHistoryVM, filterWheelMediator, twilightCalculator);
                    var skyflat = sequenceItem as SkyFlat;
                    skyflat.GetSwitchFilterItem().Filter = settings.Filter;
                    skyflat.GetExposureItem().Binning = settings.Settings.Binning;
                    skyflat.GetExposureItem().Gain = settings.Settings.Gain;
                    skyflat.GetExposureItem().Offset = settings.Settings.Offset;
                    skyflat.MaxExposure = settings.Settings.MaxFlatExposureTime;
                    skyflat.MinExposure = settings.Settings.MinFlatExposureTime;
                    skyflat.HistogramTargetPercentage = settings.Settings.HistogramMeanTarget;
                    skyflat.HistogramTolerancePercentage = settings.Settings.HistogramTolerance;
                    skyflat.GetIterations().Iterations = FlatCount;
                    break;
                case FlatWizardMode.DYNAMICEXPOSURE:
                default:
                    sequenceItem = new AutoExposureFlat(profileService, cameraMediator, imagingMediator, imageSaveMediator, imageHistoryVM, filterWheelMediator, flatDeviceMediator);
                    var autoExposureFlat = sequenceItem as AutoExposureFlat;
                    autoExposureFlat.GetSwitchFilterItem().Filter = settings.Filter;
                    autoExposureFlat.GetExposureItem().Binning = settings.Settings.Binning;
                    autoExposureFlat.GetExposureItem().Gain = settings.Settings.Gain;
                    autoExposureFlat.GetExposureItem().Offset = settings.Settings.Offset;
                    autoExposureFlat.MaxExposure = settings.Settings.MaxFlatExposureTime;
                    autoExposureFlat.MinExposure = settings.Settings.MinFlatExposureTime;
                    autoExposureFlat.GetSetBrightnessItem().Brightness = settings.Settings.MaxAbsoluteFlatDeviceBrightness;
                    autoExposureFlat.HistogramTargetPercentage = settings.Settings.HistogramMeanTarget;
                    autoExposureFlat.HistogramTolerancePercentage = settings.Settings.HistogramTolerance;
                    autoExposureFlat.GetIterations().Iterations = FlatCount;
                    break;
            }

            // Wrap the item into a dso container, for the target name to apply
            var container = new DeepSkyObjectContainer(profileService, nighttimeCalculator, null, null, null, cameraMediator, filterWheelMediator);
            container.Target = new InputTarget(Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Latitude), Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Longitude), profileService.ActiveProfile.AstrometrySettings.Horizon) {
                TargetName = TargetName,                
                InputCoordinates = new InputCoordinates() {
                    Coordinates = telescopeInfo.Coordinates
                }
            };
            container.Add(sequenceItem);
            ActiveFlatInstruction = sequenceItem;

            return sequenceItem;
        }

        [ObservableProperty]
        private ISequenceItem activeFlatInstruction;

        public async Task<bool> StartFlatMagic(IEnumerable<FlatWizardFilterSettingsWrapper> filterSettingsWrappers, PauseToken pt) {
            try {
                if (!HasWritePermission(profileService.ActiveProfile.ImageFileSettings.FilePath)) return false;
                flatSequenceCts?.Dispose();
                flatSequenceCts = new CancellationTokenSource();

                var filterCount = 0;
                var timesForDarks = new Dictionary<FlatWizardFilterSettingsWrapper, (double time, double brightness)>();
                foreach (var filterSettings in filterSettingsWrappers) {
                    if (!filterSettings.IsChecked) {
                        continue;
                    }
                    var totalCount = filterSettingsWrappers.Where(x => x.IsChecked).Count();
                    filterCount++;
                    if (PauseBetweenFilters) {
                        var dialogResult = messageBox.Show(
                            string.Format(Loc.Instance["LblPrepFlatFilterMsgBox"], filterSettings.Filter?.Name ?? string.Empty),
                            Loc.Instance["LblFlatWizard"], MessageBoxButton.OKCancel, MessageBoxResult.OK);
                        if (dialogResult == MessageBoxResult.Cancel)
                            throw new OperationCanceledException();
                    }

                    progress.Report(new ApplicationStatus {
                        Status = $"{Loc.Instance["LblFilter"]}: {filterSettings.Filter?.Name ?? string.Empty}",
                        Progress = filterCount,
                        MaxProgress = totalCount,
                        ProgressType = ApplicationStatus.StatusProgressType.ValueOfMaxValue,
                        Source = Title
                    });

                    SequenceContainer flatInstruction = GetInstructionForMode(filterSettings);


                    // Keep the panel closed when there are more filters to take flats with or if the user specified to take darks and the setting to open for darks is off
                    bool shouldKeepPanelClosed = (filterCount < totalCount) || !profileService.ActiveProfile.FlatWizardSettings.OpenWhenDone;
                    if ((filterCount == totalCount) && profileService.ActiveProfile.FlatWizardSettings.OpenForDarkFlats && DarkFlatCount > 0) {
                        shouldKeepPanelClosed = false;
                    }

                    if (flatInstruction is AutoExposureFlat aef) {
                        aef.KeepPanelClosed = shouldKeepPanelClosed;
                    }
                    if (flatInstruction is AutoBrightnessFlat abf) {
                        abf.KeepPanelClosed = shouldKeepPanelClosed;
                    }

                    var instructionProgress = new Progress<ApplicationStatus>(x => this.applicationStatusMediator.StatusUpdate(x));

                    try {
                        if (!flatInstruction.Validate()) {
                            throw new SequenceEntityFailedValidationException(string.Join(",", flatInstruction.Issues));
                        }

                        await flatInstruction.Execute(instructionProgress, flatSequenceCts.Token);

                        if (flatInstruction is AutoExposureFlat aef1) {
                            timesForDarks.Add(filterSettings, (aef1.GetExposureItem().ExposureTime, aef1.GetSetBrightnessItem().Brightness));
                        }
                        if (flatInstruction is AutoBrightnessFlat abf1) {
                            timesForDarks.Add(filterSettings, (abf1.GetExposureItem().ExposureTime, abf1.GetSetBrightnessItem().Brightness));
                        }
                    } catch(OperationCanceledException) {
                        throw;                    
                    } catch(Exception ex) {
                        Logger.Error(ex);
                        if(string.IsNullOrEmpty(filterSettings.Filter?.Name)) {
                            Notification.ShowError(string.Format(Loc.Instance["LblFlatWizardFailed"], ex.Message));
                        } else {
                            Notification.ShowError(string.Format(Loc.Instance["LblFlatWizardFailedForFilter"], filterSettings.Filter?.Name, ex.Message));
                        }
                    }

                    await WaitWhilePaused(pt);
                }

                await TakeDarkFlats(timesForDarks, pt);

                if (profileService.ActiveProfile.FlatWizardSettings.OpenWhenDone) {
                    await flatDeviceMediator.OpenCover(progress, flatSequenceCts.Token);
                } else {
                    await flatDeviceMediator.CloseCover(progress, flatSequenceCts.Token);
                }
            } catch (OperationCanceledException) { 
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(ex.Message);
                return false;
            } finally {                
                imagingVM.DestroyImage();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                progress.Report(new ApplicationStatus());
                progress.Report(new ApplicationStatus { Source = Title });
            }

            return true;
        }

        private async Task TakeDarkFlats(Dictionary<FlatWizardFilterSettingsWrapper, (double time, double brightness)> exposureTimes, PauseToken pt) {
            if ((exposureTimes.Count > 0) && DarkFlatCount > 0) {
                progress.Report(new ApplicationStatus { Status = Loc.Instance["LblPreparingDarkFlatSequence"], Source = Title });
                if (flatDeviceInfo?.Connected == true) { await flatDeviceMediator.ToggleLight(false, null, flatSequenceCts.Token); }
                if ((flatDeviceInfo?.Connected & flatDeviceInfo?.SupportsOpenClose) == true && profileService.ActiveProfile.FlatWizardSettings.OpenForDarkFlats) { await flatDeviceMediator.OpenCover(null, flatSequenceCts.Token); }
                var dialogResult = messageBox.Show(Loc.Instance["LblCoverScopeMsgBox"],
                    Loc.Instance["LblCoverScopeMsgBoxTitle"], MessageBoxButton.OKCancel, MessageBoxResult.OK);
                if (dialogResult == MessageBoxResult.OK) {
                    var filterCount = 0;
                    foreach (var keyValuePair in exposureTimes) {
                        filterCount++;
                        var filterName = keyValuePair.Key.Filter?.Name ?? string.Empty;
                        progress.Report(new ApplicationStatus {
                            Status2 = $"{Loc.Instance["LblFilter"]} {filterName}",
                            Progress2 = filterCount,
                            MaxProgress2 = exposureTimes.Count,
                            ProgressType2 = ApplicationStatus.StatusProgressType.ValueOfMaxValue,
                            Status3 = Loc.Instance["LblExposures"],
                            Progress3 = 0,
                            MaxProgress3 = 0,
                            ProgressType3 = ApplicationStatus.StatusProgressType.ValueOfMaxValue,
                            Source = Title
                        });

                        var darkFlatsSequence = new CaptureSequence(keyValuePair.Value.time,
                                                                    CaptureSequence.ImageTypes.DARK,
                                                                    keyValuePair.Key.Filter,
                                                                    keyValuePair.Key.Settings.Binning,
                                                                    DarkFlatCount) { Gain = keyValuePair.Key.Settings.Gain, Offset = keyValuePair.Key.Settings.Offset };
                        await CaptureImages(darkFlatsSequence, pt);
                    }
                }
            }
        }

        private async Task CaptureImages(CaptureSequence sequence, PauseToken pt) {            
            while (sequence.ProgressExposureCount < sequence.TotalExposureCount) {
                progress.Report(new ApplicationStatus {
                    Status3 = Loc.Instance["LblExposures"],
                    Progress3 = sequence.ProgressExposureCount + 1,
                    MaxProgress3 = sequence.TotalExposureCount,
                    ProgressType3 = ApplicationStatus.StatusProgressType.ValueOfMaxValue,
                    Source = Title
                });

                var captureImageTask = imagingVM.CaptureImage(sequence, flatSequenceCts.Token, progress);

                await captureImageTask;

                var imageData = await (await captureImageTask).ToImageData(progress, flatSequenceCts.Token);
                imageData.MetaData.Target.Name = TargetName;

                var prepTask = imagingVM.PrepareImage(imageData, new PrepareImageParameters(true, false), flatSequenceCts.Token);

                await imageSaveMediator.Enqueue(imageData, prepTask, progress, flatSequenceCts.Token);

                sequence.ProgressExposureCount++;

                await WaitWhilePaused(pt);

                flatSequenceCts.Token.ThrowIfCancellationRequested();
            }

            progress.Report(new ApplicationStatus { Status = Loc.Instance["LblSavingImage"] });            
        }

        public IWindowService WindowService { get; set; } = new WindowService();

        private void FiltersCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            if (watchedFilterList.Count != filters.Count) {
                UpdateFilterWheelsSettings();
            }
        }

        private void UpdateFilterWheelsSettings() {
            var selectedFilter = SelectedFilter?.Position;
            Filters.Clear();
            foreach (var filter in profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters
                .Select(s => new FlatWizardFilterSettingsWrapper(s, s.FlatWizardFilterSettings, GetBitDepth(),
                    CameraInfo, flatDeviceInfo))) {
                Filters.Add(filter);
            }

            SelectedFilter = Filters.FirstOrDefault(f => f.Filter?.Position == selectedFilter)?.Filter;

            RaisePropertyChanged(nameof(Filters));
            RaisePropertyChanged(nameof(FilterInfos));
        }

        private void UpdateProfileValues(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (Math.Abs(profileService.ActiveProfile.FlatWizardSettings.HistogramMeanTarget - SingleFlatWizardFilterSettings.Settings.HistogramMeanTarget) > 0.1) {
                profileService.ActiveProfile.FlatWizardSettings.HistogramMeanTarget = SingleFlatWizardFilterSettings.Settings.HistogramMeanTarget;
            }

            if (Math.Abs(profileService.ActiveProfile.FlatWizardSettings.HistogramTolerance - SingleFlatWizardFilterSettings.Settings.HistogramTolerance) > 0.1) {
                profileService.ActiveProfile.FlatWizardSettings.HistogramTolerance = SingleFlatWizardFilterSettings.Settings.HistogramTolerance;
            }

            if (Math.Abs(profileService.ActiveProfile.CameraSettings.MaxFlatExposureTime - SingleFlatWizardFilterSettings.Settings.MaxFlatExposureTime) > 0.0001) {
                profileService.ActiveProfile.CameraSettings.MaxFlatExposureTime = SingleFlatWizardFilterSettings.Settings.MaxFlatExposureTime;
            }

            if (Math.Abs(profileService.ActiveProfile.CameraSettings.MinFlatExposureTime - SingleFlatWizardFilterSettings.Settings.MinFlatExposureTime) > 0.0001) {
                profileService.ActiveProfile.CameraSettings.MinFlatExposureTime = SingleFlatWizardFilterSettings.Settings.MinFlatExposureTime;
            }
        }

        public bool HasWritePermission(string dir) {
            if (!Directory.Exists(dir)) {
                Notification.ShowError(Loc.Instance["LblDirectoryNotFound"]);
                return false;
            }

            try {
                using (var fs = File.Create(Path.Combine(dir, Path.GetRandomFileName()), 1,
                    FileOptions.DeleteOnClose)) {
                }
                return true;
            } catch (UnauthorizedAccessException) {
                Notification.ShowError(Loc.Instance["LblDirectoryNotWritable"]);
                return false;
            }
        }

        private async Task WaitWhilePaused(PauseToken pt) {
            if (!pt.IsPaused) return;
            IsPaused = true;
            progress.Report(new ApplicationStatus { Status = Loc.Instance["LblPaused"], Source = Title });
            await pt.WaitWhilePausedAsync(flatSequenceCts.Token);
            IsPaused = false;
        }

        public void UpdateDeviceInfo(CameraInfo deviceInfo) {
            var prevBitDepth = CameraInfo?.BitDepth ?? 0;
            CameraInfo = deviceInfo;
            CameraConnected = CameraInfo?.Connected ?? false;

            if (prevBitDepth == CameraInfo?.BitDepth) return;
            var bitDepth = GetBitDepth();
            SingleFlatWizardFilterSettings.BitDepth = bitDepth;
            SingleFlatWizardFilterSettings.CameraInfo = CameraInfo;
            foreach (var filter in Filters) {
                filter.BitDepth = bitDepth;
                filter.CameraInfo = deviceInfo;
            }
            RaisePropertyChanged(nameof(StartFlatExposureTooltip));
            RaisePropertyChanged(nameof(StartFlatExposureMultiTooltip));
        }

        private FilterWheelInfo filterWheelInfo;

        public void UpdateDeviceInfo(FilterWheelInfo deviceInfo) {
            filterWheelInfo = deviceInfo;
            RaisePropertyChanged(nameof(StartFlatExposureTooltip));
            RaisePropertyChanged(nameof(StartFlatExposureMultiTooltip));
        }

        private TelescopeInfo telescopeInfo;

        public void UpdateDeviceInfo(TelescopeInfo deviceInfo) {
            telescopeInfo = deviceInfo;
            RaisePropertyChanged(nameof(SlewToZenithTooltip));
            RaisePropertyChanged(nameof(SlewToZenithTooltipEnabled));
        }

        public void UpdateDeviceInfo(FlatDeviceInfo deviceInfo) {
            FlatDeviceInfo = deviceInfo;
            if (FlatDeviceInfo.Connected) {
                AdjustSettingForBrightness(SingleFlatWizardFilterSettings);

                foreach (var setting in Filters) {
                    AdjustSettingForBrightness(setting);
                }
            }
        }

        private void AdjustSettingForBrightness(FlatWizardFilterSettingsWrapper setting) {
            if (setting.Settings.MinAbsoluteFlatDeviceBrightness < FlatDeviceInfo.MinBrightness) {
                setting.Settings.MinAbsoluteFlatDeviceBrightness = FlatDeviceInfo.MinBrightness;
            }
            if (setting.Settings.MaxAbsoluteFlatDeviceBrightness < FlatDeviceInfo.MinBrightness) {
                setting.Settings.MaxAbsoluteFlatDeviceBrightness = FlatDeviceInfo.MinBrightness;
            }

            if (setting.Settings.MinAbsoluteFlatDeviceBrightness > FlatDeviceInfo.MaxBrightness) {
                setting.Settings.MinAbsoluteFlatDeviceBrightness = FlatDeviceInfo.MaxBrightness;
            }
            if (setting.Settings.MaxAbsoluteFlatDeviceBrightness > FlatDeviceInfo.MaxBrightness) {
                setting.Settings.MaxAbsoluteFlatDeviceBrightness = FlatDeviceInfo.MaxBrightness;
            }
        }

        public RelayCommand CancelFlatExposureSequenceCommand { get; }
        public RelayCommand PauseFlatExposureSequenceCommand { get; }
        public RelayCommand ResumeFlatExposureSequenceCommand { get; }
        public IAsyncCommand StartFlatSequenceCommand { get; }
        public IAsyncCommand StartMultiFlatSequenceCommand { get; }
        public IAsyncCommand SlewToZenithCommand { get; }
    }
}