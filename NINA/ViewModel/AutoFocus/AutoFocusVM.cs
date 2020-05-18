#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyFocuser;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.Profile;
using OxyPlot;
using OxyPlot.Series;
using Accord.Statistics.Models.Regression.Linear;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NINA.Utility.ImageAnalysis;
using NINA.Model.ImageData;
using Accord.Statistics.Models.Regression.Fitting;
using NINA.Utility.Enum;
using NINA.Utility.Mediator;
using System.Text;
using NINA.ViewModel.AutoFocus;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;

namespace NINA.ViewModel {

    internal class AutoFocusVM : DockableVM, ICameraConsumer, IFocuserConsumer, IFilterWheelConsumer {
        private static readonly string ReportDirectory = Path.Combine(Utility.Utility.APPLICATIONTEMPPATH, "AutoFocus");

        static AutoFocusVM() {
            if (!Directory.Exists(ReportDirectory)) {
                Directory.CreateDirectory(ReportDirectory);
            } else {
                Utility.Utility.DirectoryCleanup(ReportDirectory, TimeSpan.FromDays(-30));
            }
        }

        public AutoFocusVM(
            IProfileService profileService,
            IFocuserMediator focuserMediator,
            IGuiderMediator guiderMediator,
            IImagingMediator imagingMediator,
            IApplicationStatusMediator applicationStatusMediator) : this(profileService, null, null, focuserMediator, guiderMediator, imagingMediator, applicationStatusMediator) {
        }

        public AutoFocusVM(
                IProfileService profileService,
                ICameraMediator cameraMediator,
                IFilterWheelMediator filterWheelMediator,
                IFocuserMediator focuserMediator,
                IGuiderMediator guiderMediator,
                IImagingMediator imagingMediator,
                IApplicationStatusMediator applicationStatusMediator
        ) : base(profileService) {
            Title = "LblAutoFocus";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["AutoFocusSVG"];

            if (cameraMediator != null) {
                this.cameraMediator = cameraMediator;
                this.cameraMediator.RegisterConsumer(this);
            }

            if (filterWheelMediator != null) {
                this.filterWheelMediator = filterWheelMediator;
                this.filterWheelMediator.RegisterConsumer(this);
            }

            this.focuserMediator = focuserMediator;
            this.focuserMediator.RegisterConsumer(this);

            this.imagingMediator = imagingMediator;
            this.guiderMediator = guiderMediator;
            this.applicationStatusMediator = applicationStatusMediator;

            FocusPoints = new AsyncObservableCollection<ScatterErrorPoint>();
            PlotFocusPoints = new AsyncObservableCollection<DataPoint>();

            StartAutoFocusCommand = new AsyncCommand<AutoFocusReport>(
                () =>
                    Task.Run(
                        async () => {
                            return await StartAutoFocus(CommandInitializization(), _autoFocusCancelToken.Token, new Progress<ApplicationStatus>(p => Status = p));
                        }
                    ),
                (p) => { return focuserInfo?.Connected == true && cameraInfo?.Connected == true; }
            );
            CancelAutoFocusCommand = new RelayCommand(CancelAutoFocus);
        }

        private CancellationTokenSource _autoFocusCancelToken;
        private AsyncObservableCollection<ScatterErrorPoint> _focusPoints;
        private AsyncObservableCollection<DataPoint> _plotFocusPoints;
        private ICameraMediator cameraMediator;
        private IImagingMediator imagingMediator;
        private IGuiderMediator guiderMediator;
        private IApplicationStatusMediator applicationStatusMediator;
        private List<Accord.Point> brightestStarPositions = new List<Accord.Point>();
        public double AverageContrast { get; private set; }
        public double ContrastStdev { get; private set; }

        public AsyncObservableCollection<ScatterErrorPoint> FocusPoints {
            get {
                return _focusPoints;
            }
            set {
                _focusPoints = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<DataPoint> PlotFocusPoints {
            get {
                return _plotFocusPoints;
            }
            set {
                _plotFocusPoints = value;
                RaisePropertyChanged();
            }
        }

        private ScatterErrorPoint _minimum;
        private ScatterErrorPoint _maximum;

        private ApplicationStatus _status;

        public ApplicationStatus Status {
            get {
                return _status;
            }
            set {
                _status = value;
                _status.Source = Title;
                RaisePropertyChanged();

                this.applicationStatusMediator.StatusUpdate(_status);
            }
        }

        private TrendLine _leftTrend;

        public TrendLine LeftTrend {
            get {
                return _leftTrend;
            }
            set {
                _leftTrend = value;
                RaisePropertyChanged();
            }
        }

        private TrendLine _rightTrend;

        public TrendLine RightTrend {
            get {
                return _rightTrend;
            }
            set {
                _rightTrend = value;
                RaisePropertyChanged();
            }
        }

        private Func<double, double> _quadraticFitting;

        public Func<double, double> QuadraticFitting {
            get {
                return _quadraticFitting;
            }
            set {
                _quadraticFitting = value;
                RaisePropertyChanged();
            }
        }

        private DataPoint _quadraticMinimum;

        public DataPoint QuadraticMinimum {
            get {
                return _quadraticMinimum;
            }
            set {
                _quadraticMinimum = value;
                RaisePropertyChanged();
            }
        }

        private Func<double, double> _hyperbolicFitting;

        public Func<double, double> HyperbolicFitting {
            get {
                return _hyperbolicFitting;
            }
            set {
                _hyperbolicFitting = value;
                RaisePropertyChanged();
            }
        }

        private DataPoint _hyperbolicMinimum;

        public DataPoint HyperbolicMinimum {
            get {
                return _hyperbolicMinimum;
            }
            set {
                _hyperbolicMinimum = value;
                RaisePropertyChanged();
            }
        }

        private DataPoint _trendLineIntersection;

        public DataPoint TrendLineIntersection {
            get {
                return _trendLineIntersection;
            }
            set {
                _trendLineIntersection = value;
                RaisePropertyChanged();
            }
        }

        private Func<double, double> _gaussianFitting;

        public Func<double, double> GaussianFitting {
            get {
                return _gaussianFitting;
            }
            set {
                _gaussianFitting = value;
                RaisePropertyChanged();
            }
        }

        private DataPoint _gaussianMaximum;

        public DataPoint GaussianMaximum {
            get {
                return _gaussianMaximum;
            }
            set {
                _gaussianMaximum = value;
                RaisePropertyChanged();
            }
        }

        private DataPoint _finalFocusPoint;

        public DataPoint FinalFocusPoint {
            get {
                return _finalFocusPoint;
            }
            set {
                _finalFocusPoint = value;
                RaisePropertyChanged();
            }
        }

        private int _focusPosition;

        private FilterInfo CommandInitializization() {
            _autoFocusCancelToken?.Dispose();
            _autoFocusCancelToken = new CancellationTokenSource();
            FilterInfo filter = null;
            if (this.filterInfo?.SelectedFilter != null) {
                filter = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters.Where(x => x.Position == this.filterInfo.SelectedFilter.Position).FirstOrDefault();
            }
            return filter;
        }

        private async Task GetFocusPoints(FilterInfo filter, int nrOfSteps, IProgress<ApplicationStatus> progress, CancellationToken token, int offset = 0) {
            var stepSize = profileService.ActiveProfile.FocuserSettings.AutoFocusStepSize;

            if (offset != 0) {
                //Move to initial position
                _focusPosition = await focuserMediator.MoveFocuserRelative(offset * stepSize);
            }

            var comparer = new FocusPointComparer();
            var plotComparer = new PlotPointComparer();

            for (int i = 0; i < nrOfSteps; i++) {
                token.ThrowIfCancellationRequested();

                MeasureAndError measurement = await GetAverageMeasurement(filter, profileService.ActiveProfile.FocuserSettings.AutoFocusNumberOfFramesPerPoint, token, progress);

                //If star Measurement is 0, we didn't detect any stars or shapes, and want this point to be ignored by the fitting as much as possible. Setting a very high Stdev will do the trick.
                if (measurement.Measure == 0) {
                    measurement.Stdev = 1000;
                }

                token.ThrowIfCancellationRequested();

                FocusPoints.AddSorted(new ScatterErrorPoint(_focusPosition, measurement.Measure, 0, Math.Max(0.001, measurement.Stdev)), comparer);
                PlotFocusPoints.AddSorted(new DataPoint(_focusPosition, measurement.Measure), plotComparer);
                if (i < nrOfSteps - 1) {
                    Logger.Trace("Moving focuser to next autofocus position");
                    _focusPosition = await focuserMediator.MoveFocuserRelative(-stepSize);
                }

                token.ThrowIfCancellationRequested();
                CalculateTrends();
                if (profileService.ActiveProfile.FocuserSettings.AutoFocusMethod == AFMethodEnum.STARHFR) {
                    if (FocusPoints.Count() >= 3 && (profileService.ActiveProfile.FocuserSettings.AutoFocusCurveFitting == Utility.Enum.AFCurveFittingEnum.PARABOLIC || profileService.ActiveProfile.FocuserSettings.AutoFocusCurveFitting == Utility.Enum.AFCurveFittingEnum.TRENDPARABOLIC)) { CalculateQuadraticFitting(); }
                    if (FocusPoints.Count() >= 3 && (profileService.ActiveProfile.FocuserSettings.AutoFocusCurveFitting == Utility.Enum.AFCurveFittingEnum.HYPERBOLIC || profileService.ActiveProfile.FocuserSettings.AutoFocusCurveFitting == Utility.Enum.AFCurveFittingEnum.TRENDHYPERBOLIC)) { CalculateHyperbolicFitting(); }
                } else if (FocusPoints.Count() >= 3) {
                    CalculateGaussianFitting();
                }
            }
        }

        private async Task<IRenderedImage> TakeExposure(FilterInfo filter, CancellationToken token, IProgress<ApplicationStatus> progress) {
            Logger.Trace("Starting Exposure for autofocus");
            double expTime = profileService.ActiveProfile.FocuserSettings.AutoFocusExposureTime;
            if (filter != null && filter.AutoFocusExposureTime > 0) {
                expTime = filter.AutoFocusExposureTime;
            }
            var seq = new CaptureSequence(expTime, CaptureSequence.ImageTypes.SNAPSHOT, filter, null, 1);
            seq.EnableSubSample = _setSubSample;
            seq.Binning = new BinningMode(profileService.ActiveProfile.FocuserSettings.AutoFocusBinning, profileService.ActiveProfile.FocuserSettings.AutoFocusBinning);

            bool autoStretch = true;
            //If using contrast based statistics, no need to stretch
            if (profileService.ActiveProfile.FocuserSettings.AutoFocusMethod == AFMethodEnum.CONTRASTDETECTION && profileService.ActiveProfile.FocuserSettings.ContrastDetectionMethod == ContrastDetectionMethodEnum.Statistics) {
                autoStretch = false;
            }
            var prepareParameters = new PrepareImageParameters(autoStretch: autoStretch, detectStars: false);
            IRenderedImage image;
            try {
                image = await imagingMediator.CaptureAndPrepareImage(seq, prepareParameters, token, progress);
            } catch (Exception e) {
                if (!_setSubSample) {
                    throw e;
                }

                Logger.Warning("Camera error, trying without subsample");
                Logger.Warning(e.Message);
                _setSubSample = false;
                seq.EnableSubSample = _setSubSample;
                image = await imagingMediator.CaptureAndPrepareImage(seq, prepareParameters, token, progress);
            }

            return image;
        }

        private async Task<MeasureAndError> EvaluateExposure(IRenderedImage image, CancellationToken token, IProgress<ApplicationStatus> progress) {
            Logger.Trace("Evaluating Exposure");

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

            var analysis = new StarDetection(image, pixelFormat, profileService.ActiveProfile.ImageSettings.StarSensitivity, profileService.ActiveProfile.ImageSettings.NoiseReduction);
            if (profileService.ActiveProfile.FocuserSettings.AutoFocusInnerCropRatio < 1 && !_setSubSample) {
                analysis.UseROI = true;
                analysis.InnerCropRatio = profileService.ActiveProfile.FocuserSettings.AutoFocusInnerCropRatio;
                analysis.OuterCropRatio = profileService.ActiveProfile.FocuserSettings.AutoFocusOuterCropRatio;
            }

            if (profileService.ActiveProfile.FocuserSettings.AutoFocusMethod == AFMethodEnum.STARHFR) {
                //Let's set the brightest star list - if it's the first exposure, it's going to be empty
                analysis.BrightestStarPositions = brightestStarPositions;
                analysis.NumberOfAFStars = profileService.ActiveProfile.FocuserSettings.AutoFocusUseBrightestStars;
                await analysis.DetectAsync(progress, token);

                //If current star list is empty, we're doing the first AF point, let's get the brightest star lists from the Star Detector instance
                if (brightestStarPositions.Count() == 0) {
                    brightestStarPositions = analysis.BrightestStarPositions;
                }

                if (profileService.ActiveProfile.ImageSettings.AnnotateImage) {
                    imagingMediator.SetImage(analysis.GetAnnotatedImage());
                }

                Logger.Debug(string.Format("Current Focus: Position: {0}, HRF: {1}", _focusPosition, analysis.AverageHFR));

                return new MeasureAndError() { Measure = analysis.AverageHFR, Stdev = analysis.HFRStdDev };
            } else {
                analysis.ContrastDetectionMethod = profileService.ActiveProfile.FocuserSettings.ContrastDetectionMethod;
                await analysis.MeasureContrastAsync(progress, token);

                if (profileService.ActiveProfile.ImageSettings.AnnotateImage) {
                    imagingMediator.SetImage(analysis.GetAnnotatedImage());
                }

                MeasureAndError ContrastMeasurement = new MeasureAndError() { Measure = analysis.AverageContrast, Stdev = analysis.ContrastStdev };
                return ContrastMeasurement;
            }
        }

        private async Task<bool> ValidateCalculatedFocusPosition(DataPoint focusPoint, FilterInfo filter, CancellationToken token, IProgress<ApplicationStatus> progress, double initialHFR) {
            _focusPosition = await focuserMediator.MoveFocuser((int)focusPoint.X);

            if (profileService.ActiveProfile.FocuserSettings.AutoFocusMethod == AFMethodEnum.STARHFR) {
                double hfr = (await GetAverageMeasurement(filter, profileService.ActiveProfile.FocuserSettings.AutoFocusNumberOfFramesPerPoint, token, progress)).Measure;

                if (hfr > (focusPoint.Y * 1.25)) {
                    Notification.ShowWarning(string.Format(Locale.Loc.Instance["LblFocusPointValidationFailed"], focusPoint.X, focusPoint.Y, hfr));
                }

                if (initialHFR != 0 && hfr > (initialHFR * 1.15)) {
                    Notification.ShowWarning(string.Format(Locale.Loc.Instance["LblAutoFocusNewWorseThanOriginal"], hfr, initialHFR));
                    Logger.Warning(string.Format("New focus point HFR {0} is significantly worse than original HFR {1}", hfr, initialHFR));
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Calculate HFR from position and perfectfocusposition using hyperbola parameters
        /// The HFR of the imaged star disk as function of the focuser position can be described as hyperbola
        /// A hyperbola is defined as:
        /// x=b*sinh(t)
        /// y=a*cosh(t)
        /// Using the arccosh and arsinh functions it is possible to inverse
        /// above calculations and convert x=>t and t->y or y->t and t->x
        /// </summary>
        /// <param name="position">Current focuser position</param>
        /// <param name="perfectFocusPosition">Focuser position where HFR is lowest</param>
        /// <param name="a">Hyperbola parameter a, lowest HFR value at focus position</param>
        /// <param name="b">Hyperbola parameter b, defining the asymptotes, y = +-x*a/b</param>
        /// <returns></returns>
        private double HyperbolicFittingHfrCalc(double position, double perfectFocusPosition, double a, double b) {
            double x = perfectFocusPosition - position;
            double t = MathHelper.HArcsin(x / b); //calculate t-position in hyperbola
            return a * MathHelper.HCos(t); //convert t-position to y/hfd value
        }

        private double ScaledErrorHyperbola(double perfectFocusPosition, double a, double b) {
            return Math.Sqrt(FocusPoints.Sum((dp) => Math.Pow((HyperbolicFittingHfrCalc(dp.X, perfectFocusPosition, a, b) - dp.Y) / dp.ErrorY, 2)));
        }

        /// <summary>
        /// The routine will try to find the best hyperbola curve fit. The focuser position p at the hyperbola minimum is the expected best focuser position
        /// The FocusPoints List will be used as input to the fitting
        /// </summary>
        private void CalculateHyperbolicFitting() {
            double error1, oldError, pRange, aRange, bRange, highestHfr, lowestHfr, highestPosition, lowestPosition, a, b, p, a1, b1, p1, a0, b0, p0;
            double lowestError = double.MaxValue; //scaled RMS (square root of the mean square) of the HFD errors after curve fitting
            int n = FocusPoints.Count();
            ScatterErrorPoint lowestPoint = FocusPoints.Where((dp) => dp.Y >= 0.1).Aggregate((l, r) => l.Y < r.Y ? l : r); // Get lowest non-zero datapoint
            ScatterErrorPoint highestPoint = FocusPoints.Aggregate((l, r) => l.Y > r.Y ? l : r); // Get highest datapoint
            highestPosition = highestPoint.X;
            highestHfr = highestPoint.Y;
            lowestPosition = lowestPoint.X;
            lowestHfr = lowestPoint.Y;
            oldError = double.MaxValue;

            if (highestPosition < lowestPosition) { highestPosition = 2 * lowestPosition - highestPosition; } // Always go up

            //get good starting values for a, b and p
            a = lowestHfr; // a is near the lowest HFR value
            //Alternative hyperbola formula: sqr(y)/sqr(a)-sqr(x)/sqr(b)=1 ==>  sqr(b)=sqr(x)*sqr(a)/(sqr(y)-sqr(a)
            b = Math.Sqrt((highestPosition - lowestPosition) * (highestPosition - lowestPosition) * a * a / (highestHfr * highestHfr - a * a));
            p = lowestPosition;

            int iterationCycles = 0; //how many cycles where used for curve fitting

            //set starting test range
            aRange = a;
            bRange = b;
            pRange = highestPosition - lowestPosition; //large steps since slope could contain some error

            do {
                p0 = p;
                b0 = b;
                a0 = a;

                //Reduce range by 50%
                aRange = aRange * 0.5;
                bRange = bRange * 0.5;
                pRange = pRange * 0.5;

                p1 = p0 - pRange; //Start value

                while (p1 <= p0 + pRange) { //Position loop
                    a1 = a0 - aRange; //Start value
                    while (a1 <= a0 + aRange) { //a loop
                        b1 = b0 - bRange; // Start value
                        while (b1 <= b0 + bRange) { //b loop
                            error1 = ScaledErrorHyperbola(p1, a1, b1);
                            if (error1 < lowestError) { //Better position found
                                oldError = lowestError;
                                lowestError = error1;
                                //Best value up to now
                                a = a1;
                                b = b1;
                                p = p1;
                            }
                            b1 = b1 + bRange * 0.1; //do 20 steps within range, many steps guarantees convergence
                        }
                        a1 = a1 + aRange * 0.1; //do 20 steps within range
                    }
                    p1 = p1 + pRange * 0.1; //do 20 steps within range
                }
                iterationCycles++;
            } while (oldError - lowestError >= 0.0001 && lowestError > 0.0001 && iterationCycles < 30);
            HyperbolicFitting = (x) => a * MathHelper.HCos(MathHelper.HArcsin((p - x) / b));
            HyperbolicMinimum = new DataPoint((int)Math.Round(p), a);
        }

        private void CalculateGaussianFitting() {
            double[][] inputs = Accord.Math.Matrix.ToJagged(FocusPoints.ToList().ConvertAll((dp) => dp.X).ToArray());
            double[] outputs = FocusPoints.ToList().ConvertAll((dp) => dp.Y).ToArray();

            ScatterErrorPoint lowestPoint = FocusPoints.Where((dp) => dp.Y >= 0.1).Aggregate((l, r) => l.Y < r.Y ? l : r); // Get lowest non-zero datapoint
            ScatterErrorPoint highestPoint = FocusPoints.Aggregate((l, r) => l.Y > r.Y ? l : r); // Get highest datapoint
            double highestPosition = highestPoint.X;
            double highestContrast = highestPoint.Y;
            double lowestPosition = lowestPoint.X;
            double lowestContrast = lowestPoint.Y;
            double sigma = Accord.Statistics.Measures.StandardDeviation(FocusPoints.ToList().ConvertAll((dp) => dp.X).ToArray());

            var nls = new NonlinearLeastSquares() {
                NumberOfParameters = 4,
                StartValues = new[] { highestPosition, sigma, highestContrast, lowestContrast },
                Function = (w, x) => w[2] * Math.Exp(-1 * (x[0] - w[0]) * (x[0] - w[0]) / (2 * w[1] * w[1])) + w[3],
                Gradient = (w, x, r) => {
                    r[0] = w[2] * (x[0] - w[0]) * Math.Exp(-1 * (x[0] - w[0]) * (x[0] - w[0]) / (2 * w[1] * w[1])) / (w[1] * w[1]);
                    r[1] = w[2] * (x[0] - w[0]) * (x[0] - w[0]) * Math.Exp(-1 * (x[0] - w[0]) * (x[0] - w[0]) / (2 * w[1] * w[1])) / (w[1] * w[1] * w[1]);
                    r[2] = Math.Exp(-1 * (x[0] - w[0]) * (x[0] - w[0]) / (2 * w[1] * w[1]));
                    r[3] = 1;
                },
                Algorithm = new Accord.Math.Optimization.LevenbergMarquardt() {
                    MaxIterations = 30,
                    Tolerance = 0
                }
            };

            var regression = nls.Learn(inputs, outputs);
            GaussianFitting = (x) => regression.Coefficients[2] * Math.Exp(-1 * (x - regression.Coefficients[0]) * (x - regression.Coefficients[0]) / (2 * regression.Coefficients[1] * regression.Coefficients[1])) + regression.Coefficients[3];
            GaussianMaximum = new DataPoint((int)Math.Round(regression.Coefficients[0]), regression.Coefficients[2] + regression.Coefficients[3]);
        }

        private void CalculateTrendLineIntersection() {
            //Get Trendline Intersection
            TrendLineIntersection = LeftTrend.Intersect(RightTrend);
        }

        private void CalculateQuadraticFitting() {
            var fitting = new PolynomialLeastSquares() { Degree = 2 };
            PolynomialRegression poly = fitting.Learn(FocusPoints.Select((dp) => dp.X).ToArray(), FocusPoints.Select((dp) => dp.Y).ToArray(), FocusPoints.Select((dp) => 1 / (dp.ErrorY * dp.ErrorY)).ToArray());
            QuadraticFitting = (x) => (poly.Weights[0] * x * x + poly.Weights[1] * x + poly.Intercept);
            int minimumX = (int)Math.Round(poly.Weights[1] / (2 * poly.Weights[0]) * -1);
            double minimumY = QuadraticFitting(minimumX);
            QuadraticMinimum = new DataPoint(minimumX, minimumY);
        }

        private void CalculateTrends() {
            if (profileService.ActiveProfile.FocuserSettings.AutoFocusMethod == AFMethodEnum.STARHFR) {
                //Get the minimum based on HFR and Error, rather than just HFR. This ensures 0 HFR is never used, and low HFR / High error numbers are also ignored
                _minimum = FocusPoints.Aggregate((l, r) => l.Y + l.ErrorY < r.Y + r.ErrorY ? l : r);
                IEnumerable<ScatterErrorPoint> leftTrendPoints = FocusPoints.Where((x) => x.X < _minimum.X && x.Y > (_minimum.Y + 0.1));
                IEnumerable<ScatterErrorPoint> rightTrendPoints = FocusPoints.Where((x) => x.X > _minimum.X && x.Y > (_minimum.Y + 0.1));
                LeftTrend = new TrendLine(leftTrendPoints);
                RightTrend = new TrendLine(rightTrendPoints);
            } else {
                _maximum = FocusPoints.Aggregate((l, r) => l.Y - l.ErrorY > r.Y - r.ErrorY ? l : r);
                IEnumerable<ScatterErrorPoint> leftTrendPoints = FocusPoints.Where((x) => x.X < _maximum.X && x.Y < (_maximum.Y - 0.01));
                IEnumerable<ScatterErrorPoint> rightTrendPoints = FocusPoints.Where((x) => x.X > _maximum.X && x.Y < (_maximum.Y - 0.01));
                LeftTrend = new TrendLine(leftTrendPoints);
                RightTrend = new TrendLine(rightTrendPoints);
            }
        }

        private async Task<MeasureAndError> GetAverageMeasurement(FilterInfo filter, int exposuresPerFocusPoint, CancellationToken token, IProgress<ApplicationStatus> progress) {
            //Average HFR  of multiple exposures (if configured this way)
            double sumMeasure = 0;
            double sumVariances = 0;
            for (int i = 0; i < exposuresPerFocusPoint; i++) {
                var image = await TakeExposure(filter, token, progress);
                var partialMeasurement = await EvaluateExposure(image, token, progress);
                sumMeasure = sumMeasure + partialMeasurement.Measure;
                sumVariances = sumVariances + partialMeasurement.Stdev * partialMeasurement.Stdev;
                token.ThrowIfCancellationRequested();
            }

            return new MeasureAndError() { Measure = sumMeasure / exposuresPerFocusPoint, Stdev = Math.Sqrt(sumVariances / exposuresPerFocusPoint) };
        }

        public async Task<AutoFocusReport> StartAutoFocus(FilterInfo filter, CancellationToken token, IProgress<ApplicationStatus> progress) {
            AutoFocusReport report = null;
            Logger.Trace("Starting Autofocus");
            FocusPoints.Clear();
            PlotFocusPoints.Clear();
            LeftTrend = null;
            RightTrend = null;
            _minimum = new ScatterErrorPoint(0, 0, 0, 0);
            TrendLineIntersection = new DataPoint(0, 0);
            QuadraticFitting = null;
            QuadraticMinimum = new DataPoint(0, 0);
            HyperbolicFitting = null;
            HyperbolicMinimum = new DataPoint(0, 0);
            FinalFocusPoint = new DataPoint(0, 0);
            int numberOfAttempts = 0;
            int initialFocusPosition = focuserInfo.Position;
            double initialHFR = 0;
            //Remember imaging filter, and get autofocus filter, if any
            FilterInfo imagingFilter = filter;
            FilterInfo defaultFocusFilter = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters.Where(f => f.AutoFocusFilter == true).FirstOrDefault();

            System.Drawing.Rectangle oldSubSample = new System.Drawing.Rectangle();

            if (profileService.ActiveProfile.FocuserSettings.AutoFocusInnerCropRatio < 1 && profileService.ActiveProfile.FocuserSettings.AutoFocusOuterCropRatio == 1 && cameraInfo.CanSubSample) {
                Logger.Debug("Setting camera subsample");
                oldSubSample = new System.Drawing.Rectangle(cameraInfo.SubSampleX, cameraInfo.SubSampleY, cameraInfo.SubSampleWidth, cameraInfo.SubSampleHeight);
                int subSampleWidth = (int)Math.Round(cameraInfo.XSize * profileService.ActiveProfile.FocuserSettings.AutoFocusInnerCropRatio);
                int subSampleHeight = (int)Math.Round(cameraInfo.YSize * profileService.ActiveProfile.FocuserSettings.AutoFocusInnerCropRatio);
                int subSampleX = (int)Math.Round((cameraInfo.XSize - subSampleWidth) / 2.0d);
                int subSampleY = (int)Math.Round((cameraInfo.YSize - subSampleHeight) / 2.0d);
                try {
                    cameraMediator.SetSubSampleArea(subSampleX, subSampleY, subSampleWidth, subSampleHeight);
                } catch (Exception e) {
                    Logger.Warning("Could not set subsample of rectangle X = " + subSampleX + ", Y = " + subSampleY + ", Width = " + subSampleWidth + ", Height = " + subSampleHeight);
                    Logger.Warning(e.Message);
                    _setSubSample = false;
                }
                _setSubSample = true;
            }

            bool tempComp = false;

            try {
                if (focuserInfo.TempCompAvailable && focuserInfo.TempComp) {
                    tempComp = true;
                    focuserMediator.ToggleTempComp(false);
                }

                if (profileService.ActiveProfile.FocuserSettings.AutoFocusDisableGuiding) {
                    await this.guiderMediator.StopGuiding(token);
                }

                initialFocusPosition = focuserInfo.Position;

                if (profileService.ActiveProfile.FocuserSettings.AutoFocusMethod == AFMethodEnum.STARHFR) {
                    //Get initial position information, as average of multiple exposures, if configured this way
                    initialHFR = (await GetAverageMeasurement(filter, profileService.ActiveProfile.FocuserSettings.AutoFocusNumberOfFramesPerPoint, token, progress)).Measure;
                }

                bool reattempt;
                do {
                    reattempt = false;
                    numberOfAttempts = numberOfAttempts + 1;

                    var offsetSteps = profileService.ActiveProfile.FocuserSettings.AutoFocusInitialOffsetSteps;
                    var offset = offsetSteps;

                    var nrOfSteps = offsetSteps + 1;

                    //Set the filter to the autofocus filter if necessary, and move to it so autofocus X indexing works properly when invoking GetFocusPoints()
                    if (defaultFocusFilter != null && profileService.ActiveProfile.FocuserSettings.UseFilterWheelOffsets) {
                        try {
                            filter = await filterWheelMediator.ChangeFilter(defaultFocusFilter, token, progress);
                        } catch (Exception e) {
                            Logger.Error(e.Message);
                            Notification.ShowWarning(e.Message);
                        }
                    }

                    await GetFocusPoints(filter, nrOfSteps, progress, token, offset);

                    var laststeps = offset;

                    int leftcount = LeftTrend.DataPoints.Count(), rightcount = RightTrend.DataPoints.Count();
                    //When datapoints are not sufficient analyze and take more
                    do {
                        if (leftcount == 0 && rightcount == 0) {
                            Notification.ShowWarning(Locale.Loc.Instance["LblAutoFocusNotEnoughtSpreadedPoints"]);
                            progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblAutoFocusNotEnoughtSpreadedPoints"] });
                            //Reattempting in this situation is very likely meaningless - just move back to initial focus position and call it a day
                            await focuserMediator.MoveFocuser(initialFocusPosition);
                            return null;
                        }

                        // Let's keep moving in, one step at a time, until we have enough left trend points. Then we can think about moving out to fill in the right trend points
                        if (LeftTrend.DataPoints.Count() < offsetSteps && FocusPoints.Where(dp => dp.X < _minimum.X && dp.Y == 0).Count() < offsetSteps) {
                            Logger.Trace("More datapoints needed to the left of the minimum");
                            //Move to the leftmost point - this should never be necessary since we're already there, but just in case
                            if (focuserInfo.Position != (int)Math.Round(FocusPoints.FirstOrDefault().X)) {
                                await focuserMediator.MoveFocuser((int)Math.Round(FocusPoints.FirstOrDefault().X));
                            }
                            //More points needed to the left
                            await GetFocusPoints(filter, 1, progress, token, -1);
                        } else if (RightTrend.DataPoints.Count() < offsetSteps && FocusPoints.Where(dp => dp.X > _minimum.X && dp.Y == 0).Count() < offsetSteps) { //Now we can go to the right, if necessary
                            Logger.Trace("More datapoints needed to the right of the minimum");
                            //More points needed to the right. Let's get to the rightmost point, and keep going right one point at a time
                            if (focuserInfo.Position != (int)Math.Round(FocusPoints.LastOrDefault().X)) {
                                await focuserMediator.MoveFocuser((int)Math.Round(FocusPoints.LastOrDefault().X));
                            }
                            await GetFocusPoints(filter, 1, progress, token, 1);
                        }

                        leftcount = LeftTrend.DataPoints.Count();
                        rightcount = RightTrend.DataPoints.Count();

                        token.ThrowIfCancellationRequested();
                    } while (rightcount + FocusPoints.Where(dp => dp.X > _minimum.X && dp.Y == 0).Count() < offsetSteps || leftcount + FocusPoints.Where(dp => dp.X < _minimum.X && dp.Y == 0).Count() < offsetSteps);

                    token.ThrowIfCancellationRequested();

                    FinalFocusPoint = DetermineFinalFocusPoint();

                    report = GenerateReport(initialFocusPosition, initialHFR);

                    LastAutoFocusPoint = new AutoFocusPoint { Focuspoint = FinalFocusPoint, Temperature = focuserInfo.Temperature, Timestamp = DateTime.Now };

                    //Set the filter to the autofocus filter if necessary, but do not move to it yet (otherwise filter offset will be ignored in final validation). This will be done as part of the capture in ValidateCalculatedFocusPosition
                    if (defaultFocusFilter != null && profileService.ActiveProfile.FocuserSettings.UseFilterWheelOffsets) {
                        filter = imagingFilter;
                    }

                    bool goodAutoFocus = await ValidateCalculatedFocusPosition(FinalFocusPoint, filter, token, progress, initialHFR);

                    if (!goodAutoFocus) {
                        if (numberOfAttempts < profileService.ActiveProfile.FocuserSettings.AutoFocusTotalNumberOfAttempts) {
                            Notification.ShowWarning(Locale.Loc.Instance["LblAutoFocusReattempting"]);
                            await focuserMediator.MoveFocuser(initialFocusPosition);
                            Logger.Warning("Potentially bad auto-focus. Reattempting.");
                            FocusPoints.Clear();
                            PlotFocusPoints.Clear();
                            LeftTrend = null;
                            RightTrend = null;
                            _minimum = new ScatterErrorPoint(0, 0, 0, 0);
                            TrendLineIntersection = new DataPoint(0, 0);
                            QuadraticFitting = null;
                            QuadraticMinimum = new DataPoint(0, 0);
                            HyperbolicFitting = null;
                            HyperbolicMinimum = new DataPoint(0, 0);
                            FinalFocusPoint = new DataPoint(0, 0);
                            reattempt = true;
                        } else {
                            Notification.ShowWarning(Locale.Loc.Instance["LblAutoFocusRestoringOriginalPosition"]);
                            Logger.Warning("Potentially bad auto-focus. Restoring original focus position.");
                            reattempt = false;
                            await focuserMediator.MoveFocuser(initialFocusPosition);
                            return null;
                        }
                    }
                } while (reattempt);

                //_focusPosition = await Mediator.Instance.RequestAsync(new MoveFocuserMessage() { Position = (int)p.X, Absolute = true, Token = token });
            } catch (OperationCanceledException) {
                FocusPoints.Clear();
                PlotFocusPoints.Clear();
                //Get back to original filter, if necessary
                try {
                    await filterWheelMediator.ChangeFilter(imagingFilter);
                } catch (Exception e) {
                    Logger.Error(e.Message);
                    Notification.ShowError(e.Message);
                }
            } catch (Exception ex) {
                Notification.ShowError(ex.Message);
                Logger.Error(ex);
            } finally {
                //Restore original sub-sample rectangle, if appropriate
                if (_setSubSample && oldSubSample.X >= 0 && oldSubSample.Y >= 0 && oldSubSample.Width > 0 && oldSubSample.Height > 0) {
                    try {
                        cameraMediator.SetSubSampleArea((int)oldSubSample.X, (int)oldSubSample.Y, (int)oldSubSample.Width, (int)oldSubSample.Height);
                    } catch (Exception e) {
                        Logger.Warning("Could not set back old sub sample area");
                        Logger.Warning(e.Message);
                        Notification.ShowError(e.Message);
                    }
                }
                //Restore the temperature compensation of the focuser
                if (focuserInfo.TempCompAvailable && tempComp) {
                    focuserMediator.ToggleTempComp(true);
                }

                brightestStarPositions.Clear();
                await this.guiderMediator.StartGuiding(token);
                progress.Report(new ApplicationStatus() { Status = string.Empty });
            }
            return report;
        }

        private DataPoint DetermineFinalFocusPoint() {
            using (MyStopWatch.Measure()) {
                var method = profileService.ActiveProfile.FocuserSettings.AutoFocusMethod;

                CalculateTrendLineIntersection();
                CalculateHyperbolicFitting();
                CalculateQuadraticFitting();
                CalculateGaussianFitting();

                if (method == AFMethodEnum.STARHFR) {
                    var fitting = profileService.ActiveProfile.FocuserSettings.AutoFocusCurveFitting;
                    if (fitting == Utility.Enum.AFCurveFittingEnum.TRENDLINES) {
                        return TrendLineIntersection;
                    }

                    if (fitting == Utility.Enum.AFCurveFittingEnum.HYPERBOLIC) {
                        return HyperbolicMinimum;
                    }

                    if (fitting == Utility.Enum.AFCurveFittingEnum.PARABOLIC) {
                        return QuadraticMinimum;
                    }

                    if (fitting == Utility.Enum.AFCurveFittingEnum.TRENDPARABOLIC) {
                        return new DataPoint(Math.Round((TrendLineIntersection.X + QuadraticMinimum.X) / 2), (TrendLineIntersection.Y + QuadraticMinimum.Y) / 2);
                    }

                    if (fitting == Utility.Enum.AFCurveFittingEnum.TRENDHYPERBOLIC) {
                        return new DataPoint(Math.Round((TrendLineIntersection.X + HyperbolicMinimum.X) / 2), (TrendLineIntersection.Y + HyperbolicMinimum.Y) / 2);
                    }

                    Logger.Error($"Invalid AutoFocus Fitting {fitting} for method {method}");
                    return new DataPoint();
                } else {
                    return GaussianMaximum;
                }
            }
        }

        /// <summary>
        /// Generates a JSON report into %localappdata%\NINA\AutoFocus for the complete autofocus run containing all the measurements
        /// </summary>
        /// <param name="initialFocusPosition"></param>
        /// <param name="initialHFR"></param>
        private AutoFocusReport GenerateReport(double initialFocusPosition, double initialHFR) {
            try {
                var method = profileService.ActiveProfile.FocuserSettings.AutoFocusMethod;

                var report = new AutoFocusReport() {
                    Timestamp = DateTime.Now,
                    Temperature = focuserInfo.Temperature,
                    InitialFocusPoint = new FocusPoint() {
                        Position = initialFocusPosition,
                        Value = initialHFR
                    },
                    CalculatedFocusPoint = new FocusPoint() {
                        Position = FinalFocusPoint.X,
                        Value = FinalFocusPoint.Y
                    },
                    PreviousFocusPoint = new FocusPoint() {
                        Position = LastAutoFocusPoint?.Focuspoint.X ?? double.NaN,
                        Value = LastAutoFocusPoint?.Focuspoint.Y ?? double.NaN
                    },
                    Method = method.ToString(),
                    Fitting = method == AFMethodEnum.STARHFR ? profileService.ActiveProfile.FocuserSettings.AutoFocusCurveFitting.ToString() : "GAUSSIAN",
                    MeasurePoints = FocusPoints.Select(x => new FocusPoint() { Position = x.X, Value = x.Y, Error = x.ErrorY }),
                    Intersections = new Intersections() {
                        TrendLineIntersection = new FocusPoint() { Position = TrendLineIntersection.X, Value = TrendLineIntersection.Y },
                        GaussianMaximum = new FocusPoint() { Position = GaussianMaximum.X, Value = GaussianMaximum.Y },
                        HyperbolicMinimum = new FocusPoint() { Position = HyperbolicMinimum.X, Value = HyperbolicMinimum.Y },
                        QuadraticMinimum = new FocusPoint() { Position = QuadraticMinimum.X, Value = QuadraticMinimum.Y }
                    }
                };

                File.WriteAllText(Path.Combine(ReportDirectory, DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + ".json"), JsonConvert.SerializeObject(report));
                return report;
            } catch (Exception ex) {
                Logger.Error(ex);
                return null;
            }
        }

        private AutoFocusPoint _lastAutoFocusPoint;
        private CameraInfo cameraInfo = DeviceInfo.CreateDefaultInstance<CameraInfo>();
        private FocuserInfo focuserInfo = DeviceInfo.CreateDefaultInstance<FocuserInfo>();
        private IFocuserMediator focuserMediator;
        private IFilterWheelMediator filterWheelMediator;
        private FilterWheelInfo filterInfo;
        private bool _setSubSample = false;

        public AutoFocusPoint LastAutoFocusPoint {
            get {
                return _lastAutoFocusPoint;
            }
            set {
                _lastAutoFocusPoint = value;
                RaisePropertyChanged();
            }
        }

        private void CancelAutoFocus(object obj) {
            _autoFocusCancelToken?.Cancel();
        }

        public void UpdateDeviceInfo(CameraInfo cameraInfo) {
            this.cameraInfo = cameraInfo;
        }

        public void UpdateDeviceInfo(FocuserInfo focuserInfo) {
            this.focuserInfo = focuserInfo;
        }

        public void UpdateDeviceInfo(FilterWheelInfo deviceInfo) {
            this.filterInfo = deviceInfo;
        }

        public void Dispose() {
            this.cameraMediator?.RemoveConsumer(this);
            this.filterWheelMediator?.RemoveConsumer(this);
            this.focuserMediator?.RemoveConsumer(this);
        }

        public ICommand StartAutoFocusCommand { get; private set; }
        public ICommand CancelAutoFocusCommand { get; private set; }
    }
}