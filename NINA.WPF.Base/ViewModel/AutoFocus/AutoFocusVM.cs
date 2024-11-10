#region "copyright"

/*
    Copyright � 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Core.Enum;
using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Core.Utility.Notification;
using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Navigation;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Image.ImageAnalysis;
using NINA.Core.Locale;
using NINA.Image.Interfaces;
using NINA.Equipment.Model;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.Utility.AutoFocus;
using NINA.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NINA.WPF.Base.ViewModel.AutoFocus {

    public partial class AutoFocusVM : BaseVM, IAutoFocusVM {
        private int _focusPosition;
        private ICameraMediator cameraMediator;
        private IFilterWheelMediator filterWheelMediator;
        private IFocuserMediator focuserMediator;
        private IGuiderMediator guiderMediator;
        private IImagingMediator imagingMediator;
        private readonly IPluggableBehaviorSelector<IStarDetection> starDetectionSelector;
        private readonly IPluggableBehaviorSelector<IStarAnnotator> starAnnotatorSelector;
        public static readonly string ReportDirectory = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "AutoFocus");

        static AutoFocusVM() {
            if (!Directory.Exists(ReportDirectory)) {
                Directory.CreateDirectory(ReportDirectory);
            } else {
                CoreUtil.DirectoryCleanup(ReportDirectory, TimeSpan.FromDays(-180));
            }
        }

        public AutoFocusVM(
                IProfileService profileService,
                ICameraMediator cameraMediator,
                IFilterWheelMediator filterWheelMediator,
                IFocuserMediator focuserMediator,
                IGuiderMediator guiderMediator,
                IImagingMediator imagingMediator,
                IPluggableBehaviorSelector<IStarDetection> starDetectionSelector,
                IPluggableBehaviorSelector<IStarAnnotator> starAnnotatorSelector
        ) : base(profileService) {
            this.cameraMediator = cameraMediator;
            this.filterWheelMediator = filterWheelMediator;
            this.focuserMediator = focuserMediator;

            this.imagingMediator = imagingMediator;
            this.guiderMediator = guiderMediator;
            this.starDetectionSelector = starDetectionSelector;
            this.starAnnotatorSelector = starAnnotatorSelector;

            FocusPoints = new AsyncObservableCollection<ScatterErrorPoint>();
            PlotFocusPoints = new AsyncObservableCollection<DataPoint>();
        }

        [ObservableProperty]
        private AFCurveFittingEnum _autoFocusChartCurveFitting;

        [ObservableProperty]
        private AFMethodEnum _autoFocusChartMethod;

        [ObservableProperty]
        private DataPoint _finalFocusPoint;

        [ObservableProperty]
        private AsyncObservableCollection<ScatterErrorPoint> _focusPoints;

        [ObservableProperty]
        private GaussianFitting _gaussianFitting;

        [ObservableProperty]
        private HyperbolicFitting _hyperbolicFitting;

        [ObservableProperty]
        private ReportAutoFocusPoint _lastAutoFocusPoint;

        [ObservableProperty]
        private AsyncObservableCollection<DataPoint> _plotFocusPoints;

        [ObservableProperty]
        private QuadraticFitting _quadraticFitting;

        [ObservableProperty]
        private TrendlineFitting _trendlineFitting;

        [ObservableProperty]
        private TimeSpan _autoFocusDuration;

        public void SetCurveFittings(string method, string fitting) {
            TrendlineFitting = new TrendlineFitting().Calculate(FocusPoints, method);

            if (AFMethodEnum.STARHFR.ToString() == method) {
                if (FocusPoints.Count >= 3
                    && (AFCurveFittingEnum.PARABOLIC.ToString() == fitting || AFCurveFittingEnum.TRENDPARABOLIC.ToString() == fitting)) {
                    QuadraticFitting = new QuadraticFitting().Calculate(FocusPoints);
                }
                if (FocusPoints.Count >= 3
                    && (AFCurveFittingEnum.HYPERBOLIC.ToString() == fitting || AFCurveFittingEnum.TRENDHYPERBOLIC.ToString() == fitting)) {
                    HyperbolicFitting = new HyperbolicFitting().Calculate(FocusPoints);
                }
            } else if (FocusPoints.Count >= 3) {
                GaussianFitting = new GaussianFitting().Calculate(FocusPoints);
            }
        }

        public async Task<AutoFocusReport> StartAutoFocus(FilterInfo imagingFilter, CancellationToken token, IProgress<ApplicationStatus> progress) {
            Logger.Trace("Starting Autofocus");

            ClearCharts();

            AutoFocusReport report = null;

            // Restrict upper limit of points in case of unexpected scenario that would not be interrupted otherwise. E.g. a zigzag pattern would lead to that
            var maximumFocusPoints = profileService.ActiveProfile.FocuserSettings.AutoFocusNumberOfFramesPerPoint * profileService.ActiveProfile.FocuserSettings.AutoFocusInitialOffsetSteps * 10;

            int numberOfAttempts = 0;
            int initialFocusPosition = focuserMediator.GetInfo().Position;
            double initialHFR = double.NaN;

            bool tempComp = false;
            bool guidingStopped = false;
            bool completed = false;
            using (var stopWatch = MyStopWatch.Measure()) {
                try {
                    focuserMediator.BroadcastAutoFocusRunStarting();
                    if (focuserMediator.GetInfo().TempCompAvailable && focuserMediator.GetInfo().TempComp) {
                        tempComp = true;
                        focuserMediator.ToggleTempComp(false);
                    }

                    if (profileService.ActiveProfile.FocuserSettings.AutoFocusDisableGuiding) {
                        guidingStopped = await this.guiderMediator.StopGuiding(token);
                    }

                    FilterInfo autofocusFilter = await SetAutofocusFilter(imagingFilter, token, progress);

                    initialFocusPosition = focuserMediator.GetInfo().Position;

                    if (profileService.ActiveProfile.FocuserSettings.AutoFocusMethod == AFMethodEnum.STARHFR && profileService.ActiveProfile.FocuserSettings.RSquaredThreshold <= 0) {
                        //Get initial position information, as average of multiple exposures, if configured this way
                        initialHFR = (await GetAverageMeasurement(autofocusFilter, profileService.ActiveProfile.FocuserSettings.AutoFocusNumberOfFramesPerPoint, token, progress)).Measure;
                    }

                    var reverse = profileService.ActiveProfile.FocuserSettings.BacklashCompensationModel == BacklashCompensationModel.OVERSHOOT && profileService.ActiveProfile.FocuserSettings.BacklashIn > 0 && profileService.ActiveProfile.FocuserSettings.BacklashOut == 0;

                    bool reattempt;
                    do {
                        reattempt = false;
                        numberOfAttempts = numberOfAttempts + 1;

                        var offsetSteps = profileService.ActiveProfile.FocuserSettings.AutoFocusInitialOffsetSteps;
                        var offset = offsetSteps;

                        var nrOfSteps = offsetSteps + 1;

                        await GetFocusPoints(autofocusFilter, nrOfSteps, progress, token, offset, reverse);

                        var laststeps = offset;

                        int leftcount = TrendlineFitting.LeftTrend.DataPoints.Count(), rightcount = TrendlineFitting.RightTrend.DataPoints.Count();
                        //When datapoints are not sufficient analyze and take more
                        do {
                            if (leftcount == 0 && rightcount == 0) {
                                Notification.ShowWarning(Loc.Instance["LblAutoFocusNotEnoughtSpreadedPoints"]);
                                progress.Report(new ApplicationStatus() { Status = Loc.Instance["LblAutoFocusNotEnoughtSpreadedPoints"] });
                                //Reattempting in this situation is very likely meaningless - just move back to initial focus position and call it a day
                                await focuserMediator.MoveFocuser(initialFocusPosition, token);
                                return null;
                            }

                            // Let's keep moving in, one step at a time, until we have enough left trend points. Then we can think about moving out to fill in the right trend points
                            if (TrendlineFitting.LeftTrend.DataPoints.Count() < offsetSteps && FocusPoints.Where(dp => dp.X < TrendlineFitting.Minimum.X && dp.Y == 0).Count() < offsetSteps) {
                                Logger.Trace("More datapoints needed to the left of the minimum");
                                //Move to the leftmost point - this should never be necessary since we're already there, but just in case
                                if (focuserMediator.GetInfo().Position != (int)Math.Round(FocusPoints.FirstOrDefault().X)) {
                                    await focuserMediator.MoveFocuser((int)Math.Round(FocusPoints.FirstOrDefault().X), token);
                                }
                                //More points needed to the left
                                await GetFocusPoints(autofocusFilter, 1, progress, token, -1, false);
                            } else if (TrendlineFitting.RightTrend.DataPoints.Count() < offsetSteps && FocusPoints.Where(dp => dp.X > TrendlineFitting.Minimum.X && dp.Y == 0).Count() < offsetSteps) { //Now we can go to the right, if necessary
                                Logger.Trace("More datapoints needed to the right of the minimum");
                                //More points needed to the right. Let's get to the rightmost point, and keep going right one point at a time
                                if (focuserMediator.GetInfo().Position != (int)Math.Round(FocusPoints.LastOrDefault().X)) {
                                    await focuserMediator.MoveFocuser((int)Math.Round(FocusPoints.LastOrDefault().X), token);
                                }
                                await GetFocusPoints(autofocusFilter, 1, progress, token, 1, false);
                            }

                            leftcount = TrendlineFitting.LeftTrend.DataPoints.Count();
                            rightcount = TrendlineFitting.RightTrend.DataPoints.Count();

                            if (maximumFocusPoints < FocusPoints.Count) {
                                // Break out when the maximum limit of focus points is reached
                                Notification.ShowError(Loc.Instance["LblAutoFocusPointLimitReached"]);
                                Logger.Error($"Autofocus failed to complete. Maximum number of focus points exceeded ({maximumFocusPoints}).");
                                break;
                            }
                            if (focuserMediator.GetInfo().Position == 0) {
                                // Break out when the focuser hits the zero position. It can't continue from there
                                Notification.ShowError(Loc.Instance["LblAutoFocusZeroPositionReached"]);
                                Logger.Error("Autofocus failed to complete. Focuser Position reached 0.");
                                break;
                            }

                            token.ThrowIfCancellationRequested();
                        } while (rightcount + FocusPoints.Where(dp => dp.X > TrendlineFitting.Minimum.X && dp.Y == 0).Count() < offsetSteps || leftcount + FocusPoints.Where(dp => dp.X < TrendlineFitting.Minimum.X && dp.Y == 0).Count() < offsetSteps);

                        token.ThrowIfCancellationRequested();

                        FinalFocusPoint = DetermineFinalFocusPoint();

                        LastAutoFocusPoint = new ReportAutoFocusPoint { Focuspoint = FinalFocusPoint, Temperature = focuserMediator.GetInfo().Temperature, Timestamp = DateTime.Now, Filter = autofocusFilter?.Name };

                        bool goodAutoFocus = await ValidateCalculatedFocusPosition(FinalFocusPoint, autofocusFilter, token, progress, initialHFR);

                        var duration = stopWatch.Elapsed;
                        AutoFocusDuration = duration;

                        var starDetection = starDetectionSelector.GetBehavior();
                        report = GenerateReport(initialFocusPosition, initialHFR, autofocusFilter?.Name ?? string.Empty, duration, starDetection);

                        if (!goodAutoFocus) {
                            if (numberOfAttempts < profileService.ActiveProfile.FocuserSettings.AutoFocusTotalNumberOfAttempts) {
                                Notification.ShowWarning(Loc.Instance["LblAutoFocusReattempting"]);
                                await focuserMediator.MoveFocuser(initialFocusPosition, token);
                                Logger.Warning("Potentially bad auto-focus. Reattempting.");
                                FocusPoints.Clear();
                                PlotFocusPoints.Clear();
                                TrendlineFitting = null;
                                QuadraticFitting = null;
                                HyperbolicFitting = null;
                                GaussianFitting = null;
                                FinalFocusPoint = new DataPoint(0, 0);
                                AutoFocusDuration = TimeSpan.Zero;
                                reattempt = true;
                            } else {
                                Notification.ShowWarning(Loc.Instance["LblAutoFocusRestoringOriginalPosition"]);
                                Logger.Warning("Potentially bad auto-focus. Restoring original focus position.");
                                reattempt = false;
                                await focuserMediator.MoveFocuser(initialFocusPosition, token);
                                return null;
                            }
                        }
                    } while (reattempt);
                    completed = true;
                    AutoFocusInfo info = new AutoFocusInfo(report.Temperature, report.CalculatedFocusPoint.Position, report.Filter, report.Timestamp);
                    focuserMediator.BroadcastSuccessfulAutoFocusRun(info);
                    Logger.Info("AutoFocus completed");
                } catch (OperationCanceledException) {
                    Logger.Warning("AutoFocus cancelled");
                } catch (Exception ex) {
                    Notification.ShowError(ex.Message);
                    Logger.Error("Failure during AutoFocus", ex);
                } finally {
                    if (!completed) {
                        Logger.Warning($"AutoFocus did not complete successfully, so restoring the focuser position to {initialFocusPosition}");
                        try {
                            await focuserMediator.MoveFocuser(initialFocusPosition, default);
                        } catch (Exception e) {
                            Logger.Error("Failed to restore focuser position after AutoFocus failure", e);
                        }

                        FocusPoints.Clear();
                        PlotFocusPoints.Clear();
                    }

                    //Get back to original filter, if necessary
                    try {
                        await filterWheelMediator.ChangeFilter(imagingFilter);
                    } catch (Exception e) {
                        Logger.Error("Failed to restore previous filter position after AutoFocus", e);
                        Notification.ShowError($"Failed to restore previous filter position: {e.Message}");
                    }

                    //Restore the temperature compensation of the focuser
                    if (focuserMediator.GetInfo().TempCompAvailable && tempComp) {
                        focuserMediator.ToggleTempComp(true);
                    }

                    if (guidingStopped) {
                        var startGuidingTask = this.guiderMediator.StartGuiding(false, progress, default);
                        var completedTask = await Task.WhenAny(Task.Delay(TimeSpan.FromMinutes(1)), startGuidingTask);
                        if (startGuidingTask != completedTask) {
                            Notification.ShowWarning(Loc.Instance["LblStartGuidingFailed"]);
                            Logger.Warning("Failed to restart guiding after AutoFocus");
                        }
                    }
                    Logger.Debug("AutoFocus cleanup complete");
                    progress.Report(new ApplicationStatus() { Status = string.Empty });
                }
                return report;
            }
        }

        private void ClearCharts() {
            AutoFocusChartMethod = profileService.ActiveProfile.FocuserSettings.AutoFocusMethod;
            AutoFocusChartCurveFitting = profileService.ActiveProfile.FocuserSettings.AutoFocusCurveFitting;
            FocusPoints.Clear();
            PlotFocusPoints.Clear();
            TrendlineFitting = null;
            QuadraticFitting = null;
            HyperbolicFitting = null;
            GaussianFitting = null;
            FinalFocusPoint = new DataPoint(0, 0);
            LastAutoFocusPoint = null;
            AutoFocusDuration = TimeSpan.Zero;
        }

        private DataPoint DetermineFinalFocusPoint() {
            using (MyStopWatch.Measure()) {
                var method = profileService.ActiveProfile.FocuserSettings.AutoFocusMethod;

                TrendlineFitting = new TrendlineFitting().Calculate(FocusPoints, method.ToString());

                HyperbolicFitting = new HyperbolicFitting().Calculate(FocusPoints);

                QuadraticFitting = new QuadraticFitting().Calculate(FocusPoints);

                GaussianFitting = new GaussianFitting().Calculate(FocusPoints);

                if (method == AFMethodEnum.STARHFR) {
                    var fitting = profileService.ActiveProfile.FocuserSettings.AutoFocusCurveFitting;
                    if (fitting == AFCurveFittingEnum.TRENDLINES) {
                        return TrendlineFitting.Intersection;
                    }

                    if (fitting == AFCurveFittingEnum.HYPERBOLIC) {
                        return HyperbolicFitting.Minimum;
                    }

                    if (fitting == AFCurveFittingEnum.PARABOLIC) {
                        return QuadraticFitting.Minimum;
                    }

                    if (fitting == AFCurveFittingEnum.TRENDPARABOLIC) {
                        return new DataPoint(Math.Round((TrendlineFitting.Intersection.X + QuadraticFitting.Minimum.X) / 2), (TrendlineFitting.Intersection.Y + QuadraticFitting.Minimum.Y) / 2);
                    }

                    if (fitting == AFCurveFittingEnum.TRENDHYPERBOLIC) {
                        return new DataPoint(Math.Round((TrendlineFitting.Intersection.X + HyperbolicFitting.Minimum.X) / 2), (TrendlineFitting.Intersection.Y + HyperbolicFitting.Minimum.Y) / 2);
                    }

                    Logger.Error($"Invalid AutoFocus Fitting {fitting} for method {method}");
                    return new DataPoint();
                } else {
                    return GaussianFitting.Maximum;
                }
            }
        }

        private async Task<MeasureAndError> EvaluateExposure(IExposureData exposureData, CancellationToken token, IProgress<ApplicationStatus> progress) {
            Logger.Trace("Evaluating Exposure");

            var imageData = await exposureData.ToImageData(progress, token);

            bool autoStretch = true;
            //If using contrast based statistics, no need to stretch
            if (profileService.ActiveProfile.FocuserSettings.AutoFocusMethod == AFMethodEnum.CONTRASTDETECTION && profileService.ActiveProfile.FocuserSettings.ContrastDetectionMethod == ContrastDetectionMethodEnum.Statistics) {
                autoStretch = false;
            }
            var image = await imagingMediator.PrepareImage(imageData, new PrepareImageParameters(autoStretch, false), token);
            
            var imageProperties = image.RawImageData.Properties;
            var imageStatistics = await image.RawImageData.Statistics.Task;

            //Very simple to directly provide result if we use statistics based contrast detection
            if (profileService.ActiveProfile.FocuserSettings.AutoFocusMethod == AFMethodEnum.CONTRASTDETECTION && profileService.ActiveProfile.FocuserSettings.ContrastDetectionMethod == ContrastDetectionMethodEnum.Statistics) {
                return new MeasureAndError() { Measure = 100 * imageStatistics.StDev / imageStatistics.Mean, Stdev = 0.01 };
            }

            System.Windows.Media.PixelFormat pixelFormat;

            if (imageProperties.IsBayered && profileService.ActiveProfile.ImageSettings.DebayerImage) {
                pixelFormat = System.Windows.Media.PixelFormats.Rgb48;
            } else {
                pixelFormat = System.Windows.Media.PixelFormats.Gray16;
            }

            if (profileService.ActiveProfile.FocuserSettings.AutoFocusMethod == AFMethodEnum.STARHFR) {
                var analysisParams = new StarDetectionParams() {
                    IsAutoFocus = true,
                    Sensitivity = profileService.ActiveProfile.ImageSettings.StarSensitivity,
                    NoiseReduction = profileService.ActiveProfile.ImageSettings.NoiseReduction,
                    NumberOfAFStars = profileService.ActiveProfile.FocuserSettings.AutoFocusUseBrightestStars
                };
                if (profileService.ActiveProfile.FocuserSettings.AutoFocusInnerCropRatio < 1 && !IsSubSampleEnabled()) {
                    analysisParams.UseROI = true;
                    analysisParams.InnerCropRatio = profileService.ActiveProfile.FocuserSettings.AutoFocusInnerCropRatio;
                }
                if (profileService.ActiveProfile.FocuserSettings.AutoFocusOuterCropRatio < 1) {
                    analysisParams.UseROI = true;
                    if (IsSubSampleEnabled() && profileService.ActiveProfile.FocuserSettings.AutoFocusOuterCropRatio < 1.0) {
                        // We have subsampled already. Since outer crop is set, the user wants a donut shape
                        // OuterCrop of 0 activates the donut logic without any outside clipping, and we scale the inner ratio accordingly
                        analysisParams.OuterCropRatio = 0.0;
                        analysisParams.InnerCropRatio = profileService.ActiveProfile.FocuserSettings.AutoFocusInnerCropRatio / profileService.ActiveProfile.FocuserSettings.AutoFocusOuterCropRatio;
                    } else {
                        analysisParams.OuterCropRatio = profileService.ActiveProfile.FocuserSettings.AutoFocusOuterCropRatio;
                    }
                }

                var starDetection = starDetectionSelector.GetBehavior();
                var analysisResult = await starDetection.Detect(image, pixelFormat, analysisParams, progress, token);
                image.UpdateAnalysis(analysisParams, analysisResult);

                if (profileService.ActiveProfile.ImageSettings.AnnotateImage) {
                    token.ThrowIfCancellationRequested();
                    var starAnnotator = starAnnotatorSelector.GetBehavior();
                    var annotatedImage = await starAnnotator.GetAnnotatedImage(analysisParams, analysisResult, image.Image, token: token);
                    imagingMediator.SetImage(annotatedImage);
                }

                Logger.Debug($"Current Focus: Position: {_focusPosition}, HFR: {analysisResult.AverageHFR}");
                var stdev = double.IsNaN(analysisResult.HFRStdDev) ? 0 : analysisResult.HFRStdDev;
                return new MeasureAndError() { Measure = analysisResult.AverageHFR, Stdev = stdev };
            } else {
                var analysis = new ContrastDetection();
                var analysisParams = new ContrastDetectionParams() {
                    Sensitivity = profileService.ActiveProfile.ImageSettings.StarSensitivity,
                    NoiseReduction = profileService.ActiveProfile.ImageSettings.NoiseReduction,
                    Method = profileService.ActiveProfile.FocuserSettings.ContrastDetectionMethod
                };
                if (profileService.ActiveProfile.FocuserSettings.AutoFocusInnerCropRatio < 1 && !IsSubSampleEnabled()) {
                    analysisParams.UseROI = true;
                    analysisParams.InnerCropRatio = profileService.ActiveProfile.FocuserSettings.AutoFocusInnerCropRatio;
                }
                var analysisResult = await analysis.Measure(image, analysisParams, progress, token);

                var stdev = double.IsNaN(analysisResult.ContrastStdev) ? 0 : analysisResult.ContrastStdev;
                MeasureAndError ContrastMeasurement = new MeasureAndError() { Measure = analysisResult.AverageContrast, Stdev = stdev };
                return ContrastMeasurement;
            }
        }

        /// <summary>
        /// Generates a JSON report into %localappdata%\NINA\AutoFocus for the complete autofocus run containing all the measurements
        /// </summary>
        /// <param name="initialFocusPosition"></param>
        /// <param name="initialHFR"></param>
        private AutoFocusReport GenerateReport(double initialFocusPosition, double initialHFR, string filter, TimeSpan duration, IStarDetection starDetector) {
            try {
                var report = AutoFocusReport.GenerateReport(
                    profileService,
                    "NINA",
                    starDetector.Name,
                    FocusPoints,
                    initialFocusPosition,
                    initialHFR,
                    FinalFocusPoint,
                    LastAutoFocusPoint,
                    TrendlineFitting,
                    QuadraticFitting,
                    HyperbolicFitting,
                    GaussianFitting,
                    focuserMediator.GetInfo().Temperature,
                    filter,
                    duration
                );

                string path = Path.Combine(ReportDirectory, $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}--{profileService.ActiveProfile.Id}.json");
                File.WriteAllText(path, JsonConvert.SerializeObject(report));
                return report;
            } catch (Exception ex) {
                Logger.Error(ex);
                return null;
            }
        }

        private async Task<MeasureAndError> GetAverageMeasurement(FilterInfo filter, int exposuresPerFocusPoint, CancellationToken token, IProgress<ApplicationStatus> progress) {
            var task = await GetAverageMeasurementTask(filter, exposuresPerFocusPoint, token, progress);
            return await task;
        }

        private async Task<Task<MeasureAndError>> GetAverageMeasurementTask(FilterInfo filter, int exposuresPerFocusPoint, CancellationToken token, IProgress<ApplicationStatus> progress) {
            List<Task<MeasureAndError>> measurements = new List<Task<MeasureAndError>>();

            for (int i = 0; i < exposuresPerFocusPoint; i++) {
                var image = await TakeExposure(filter, token, progress);

                measurements.Add(EvaluateExposure(image, token, progress));

                token.ThrowIfCancellationRequested();
            }

            return EvaluateAllExposures(measurements, exposuresPerFocusPoint, token);
        }

        private async Task<MeasureAndError> EvaluateAllExposures(List<Task<MeasureAndError>> measureTasks, int exposuresPerFocusPoint, CancellationToken token) {
            var measures = await Task.WhenAll(measureTasks);

            //Average HFR  of multiple exposures (if configured this way)
            double sumMeasure = 0;
            double sumVariances = 0;
            foreach (var partialMeasurement in measures) {
                sumMeasure += partialMeasurement.Measure;
                sumVariances += partialMeasurement.Stdev * partialMeasurement.Stdev;
            }
            return new MeasureAndError() { Measure = sumMeasure / exposuresPerFocusPoint, Stdev = Math.Sqrt(sumVariances / exposuresPerFocusPoint) };
        }


        private async Task GetFocusPoints(FilterInfo filter, int nrOfSteps, IProgress<ApplicationStatus> progress, CancellationToken token, int offset, bool reverse) {
            var stepSize = profileService.ActiveProfile.FocuserSettings.AutoFocusStepSize;
            var sign = 1;
            
            if (reverse) {
                sign = -1;
            }

            if (offset != 0) {
                //Move to initial position
                Logger.Trace($"Moving focuser from {_focusPosition} to initial position by moving {offset * stepSize} steps");
                _focusPosition = await focuserMediator.MoveFocuserRelative(sign * offset * stepSize, token);
            } 
            
            var comparer = new FocusPointComparer();
            var plotComparer = new PlotPointComparer();

            for (int i = 0; i < nrOfSteps; i++) {
                token.ThrowIfCancellationRequested();

                var currentFocusPosition = _focusPosition;
                Task<MeasureAndError> measurementTask = await GetAverageMeasurementTask(filter, profileService.ActiveProfile.FocuserSettings.AutoFocusNumberOfFramesPerPoint, token, progress);
                if (i < nrOfSteps - 1) {
                    Logger.Trace($"Moving focuser from {_focusPosition} to the next autofocus position using step size: {-stepSize}");
                    _focusPosition = await focuserMediator.MoveFocuserRelative(sign * -stepSize, token);
                }

                MeasureAndError measurement = await measurementTask;

                //If star Measurement is 0, we didn't detect any stars or shapes, and want this point to be ignored by the fitting as much as possible. Setting a very high Stdev will do the trick.
                if (measurement.Measure == 0) {
                    Logger.Warning($"No stars detected in step {i + 1}. Setting a high stddev to ignore the point.");
                    measurement.Stdev = 1000;
                }

                token.ThrowIfCancellationRequested();

                FocusPoints.AddSorted(new ScatterErrorPoint(currentFocusPosition, measurement.Measure, 0, Math.Max(0.001, measurement.Stdev)), comparer);
                PlotFocusPoints.AddSorted(new DataPoint(currentFocusPosition, measurement.Measure), plotComparer);

                token.ThrowIfCancellationRequested();

                SetCurveFittings(profileService.ActiveProfile.FocuserSettings.AutoFocusMethod.ToString(), profileService.ActiveProfile.FocuserSettings.AutoFocusCurveFitting.ToString());
            }
        }

        private async Task<FilterInfo> SetAutofocusFilter(FilterInfo imagingFilter, CancellationToken token, IProgress<ApplicationStatus> progress) {
            if (profileService.ActiveProfile.FocuserSettings.UseFilterWheelOffsets) {
                var filter = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters.Where(f => f.AutoFocusFilter == true).FirstOrDefault();
                if (filter == null) {
                    return imagingFilter;
                }

                //Set the filter to the autofocus filter if necessary, and move to it so autofocus X indexing works properly when invoking GetFocusPoints()
                try {
                    return await filterWheelMediator.ChangeFilter(filter, token, progress);
                } catch (Exception e) {
                    Logger.Error("Failed to change filter during AutoFocus", e);
                    Notification.ShowWarning($"Failed to change filter: {e.Message}");
                    return imagingFilter;
                }
            } else {
                return imagingFilter;
            }
        }

        private ObservableRectangle GetSubSampleRectangle() {
            var cameraInfo = cameraMediator.GetInfo();
            var innerCropRatio = profileService.ActiveProfile.FocuserSettings.AutoFocusInnerCropRatio;
            var outerCropRatio = profileService.ActiveProfile.FocuserSettings.AutoFocusOuterCropRatio;
            if (innerCropRatio < 1 || outerCropRatio < 1 && cameraInfo.CanSubSample) {
                // if only inner crop is set, then it is the outer boundary. Otherwise we use the outer crop
                var outsideCropRatio = outerCropRatio >= 1.0 ? innerCropRatio : outerCropRatio;

                int subSampleWidth = (int)Math.Round(cameraInfo.XSize * outsideCropRatio);
                int subSampleHeight = (int)Math.Round(cameraInfo.YSize * outsideCropRatio);
                int subSampleX = (int)Math.Round((cameraInfo.XSize - subSampleWidth) / 2.0d);
                int subSampleY = (int)Math.Round((cameraInfo.YSize - subSampleHeight) / 2.0d);
                return new ObservableRectangle(subSampleX, subSampleY, subSampleWidth, subSampleHeight);
            }
            return null;
        }

        private async Task<IExposureData> TakeExposure(FilterInfo filter, CancellationToken token, IProgress<ApplicationStatus> progress) {
            IExposureData image;
            var retries = 0;
            do {
                Logger.Trace("Starting Exposure for autofocus");
                double expTime = profileService.ActiveProfile.FocuserSettings.AutoFocusExposureTime;
                if (filter != null && filter.AutoFocusExposureTime > -1) {
                    expTime = filter.AutoFocusExposureTime;
                }
                var seq = new CaptureSequence(expTime, CaptureSequence.ImageTypes.SNAPSHOT, filter, null, 1);

                var subSampleRectangle = GetSubSampleRectangle();
                if (subSampleRectangle != null) {
                    seq.EnableSubSample = true;
                    seq.SubSambleRectangle = subSampleRectangle;
                }

                if (filter?.AutoFocusBinning != null) {
                    seq.Binning = filter.AutoFocusBinning;
                } else {
                    seq.Binning = new BinningMode(profileService.ActiveProfile.FocuserSettings.AutoFocusBinning, profileService.ActiveProfile.FocuserSettings.AutoFocusBinning);
                }

                if (filter?.AutoFocusOffset > -1) {
                    seq.Offset = filter.AutoFocusOffset;
                }

                if (filter?.AutoFocusGain > -1) {
                    seq.Gain = filter.AutoFocusGain;
                }

                try {
                    image = await imagingMediator.CaptureImage(seq, token, progress);
                } catch (Exception e) {
                    if (!IsSubSampleEnabled()) {
                        throw;
                    }

                    Logger.Warning("Camera error, trying without subsample");
                    Logger.Error(e);
                    seq.EnableSubSample = false;
                    seq.SubSambleRectangle = null;
                    image = await imagingMediator.CaptureImage(seq, token, progress);
                }
                retries++;
                if (image == null && retries < 3) {
                    Logger.Warning($"Image acquisition failed - Retrying {retries}/2");
                }
            } while (image == null && retries < 3);

            return image;
        }

        private bool IsSubSampleEnabled() {
            var cameraInfo = cameraMediator.GetInfo();
            return (profileService.ActiveProfile.FocuserSettings.AutoFocusInnerCropRatio < 1 || profileService.ActiveProfile.FocuserSettings.AutoFocusOuterCropRatio < 1) && cameraInfo.CanSubSample;
        }

        private async Task<bool> ValidateCalculatedFocusPosition(DataPoint focusPoint, FilterInfo filter, CancellationToken token, IProgress<ApplicationStatus> progress, double initialHFR) {
            var rSquaredThreshold = profileService.ActiveProfile.FocuserSettings.RSquaredThreshold;
            if (profileService.ActiveProfile.FocuserSettings.AutoFocusMethod == AFMethodEnum.STARHFR) {
                // Evaluate R� for Fittings to be above threshold

                if (rSquaredThreshold > 0) {
                    var hyperbolicBad = HyperbolicFitting.RSquared < rSquaredThreshold;
                    var quadraticBad = QuadraticFitting.RSquared < rSquaredThreshold;
                    var trendlineBad = TrendlineFitting.LeftTrend.RSquared < rSquaredThreshold || TrendlineFitting.RightTrend.RSquared < rSquaredThreshold;

                    var fitting = profileService.ActiveProfile.FocuserSettings.AutoFocusCurveFitting;

                    if ((fitting == AFCurveFittingEnum.HYPERBOLIC || fitting == AFCurveFittingEnum.TRENDHYPERBOLIC) && hyperbolicBad) {
                        Logger.Error($"Auto Focus Failed! R� (Coefficient of determination) for Hyperbolic Fitting is below threshold. {Math.Round(HyperbolicFitting.RSquared, 2)} / {rSquaredThreshold}");
                        Notification.ShowError(string.Format(Loc.Instance["LblAutoFocusCurveCorrelationCoefficientLow"], Math.Round(HyperbolicFitting.RSquared, 2), rSquaredThreshold));
                        return false;
                    }

                    if ((fitting == AFCurveFittingEnum.PARABOLIC || fitting == AFCurveFittingEnum.TRENDPARABOLIC) && quadraticBad) {
                        Logger.Error($"Auto Focus Failed! R� (Coefficient of determination) for Parabolic Fitting is below threshold. {Math.Round(QuadraticFitting.RSquared, 2)} / {rSquaredThreshold}");
                        Notification.ShowError(string.Format(Loc.Instance["LblAutoFocusCurveCorrelationCoefficientLow"], Math.Round(QuadraticFitting.RSquared, 2), rSquaredThreshold));
                        return false;
                    }

                    if ((fitting == AFCurveFittingEnum.TRENDLINES || fitting == AFCurveFittingEnum.TRENDHYPERBOLIC || fitting == AFCurveFittingEnum.TRENDPARABOLIC) && trendlineBad) {
                        Logger.Error($"Auto Focus Failed! R� (Coefficient of determination) for Trendline Fitting is below threshold. Left: {Math.Round(TrendlineFitting.LeftTrend.RSquared, 2)} / {rSquaredThreshold}; Right: {Math.Round(TrendlineFitting.RightTrend.RSquared, 2)} / {rSquaredThreshold}");
                        Notification.ShowError(string.Format(Loc.Instance["LblAutoFocusCurveCorrelationCoefficientLow"], Math.Round(TrendlineFitting.LeftTrend.RSquared, 2), Math.Round(TrendlineFitting.RightTrend.RSquared, 2), rSquaredThreshold));
                        return false;
                    }
                }
            }

            var min = FocusPoints.Min(x => x.X);
            var max = FocusPoints.Max(x => x.X);

            if (focusPoint.X < min || focusPoint.X > max) {
                Logger.Error($"Determined focus point position is outside of the overall measurement points of the curve. Fitting is incorrect and autofocus settings are incorrect. FocusPosition {focusPoint.X}; Min: {min}; Max:{max}");
                Notification.ShowError(Loc.Instance["LblAutoFocusPointOutsideOfBounds"]);
                return false;
            }

            _focusPosition = await focuserMediator.MoveFocuser((int)focusPoint.X, token);
            double hfr = (await GetAverageMeasurement(filter, profileService.ActiveProfile.FocuserSettings.AutoFocusNumberOfFramesPerPoint, token, progress)).Measure;

            if (profileService.ActiveProfile.FocuserSettings.AutoFocusMethod == AFMethodEnum.STARHFR && rSquaredThreshold <= 0) {
                if (initialHFR != 0 && hfr > (initialHFR * 1.15)) {
                    Logger.Warning(string.Format("New focus point HFR {0} is significantly worse than original HFR {1}", hfr.ToString("F3"), initialHFR.ToString("F3")));
                    Notification.ShowWarning(string.Format(Loc.Instance["LblAutoFocusNewWorseThanOriginal"], hfr.ToString("F3"), initialHFR.ToString("F3")));
                    return false;
                }
            }
            return true;
        }
    }
}