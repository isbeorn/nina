#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

    Hyperbolic fitting based on CCDCiel source, also under GPL3
    Copyright (C) 2018 Patrick Chevalley & Han Kleijn (author)

    http://www.ap-i.net
    h@ap-i.net

    http://www.hnsky.org
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

namespace NINA.ViewModel {

    internal class AutoFocusVM : DockableVM, ICameraConsumer, IFocuserConsumer, IFilterWheelConsumer {

        public AutoFocusVM(IProfileService profileService,
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

            StartAutoFocusCommand = new AsyncCommand<bool>(
                () =>
                    Task.Run(
                        async () => {
                            return await StartAutoFocus(CommandInitializization(), _autoFocusCancelToken.Token, new Progress<ApplicationStatus>(p => Status = p));
                        }
                    ),
                (p) => { return focuserInfo?.Connected == true && cameraInfo?.Connected == true; }
            );
            CancelAutoFocusCommand = new RelayCommand(CancelAutoFocus);

            StartBacklashMeasurementCommand = new AsyncCommand<bool>(
                () =>
                    Task.Run(
                        async () => {
                            return await StartBacklashMeasurement(CommandInitializization(), _autoFocusCancelToken.Token, new Progress<ApplicationStatus>(p => Status = p));
                        }
                    ),
                (p) => { return focuserInfo?.Connected == true && cameraInfo?.Connected == true; }
            );
            CancelBacklashMeasurementCommand = new RelayCommand(CancelAutoFocus);
        }

        private CancellationTokenSource _autoFocusCancelToken;
        private AsyncObservableCollection<ScatterErrorPoint> _focusPoints;
        private AsyncObservableCollection<DataPoint> _plotFocusPoints;
        private ICameraMediator cameraMediator;
        private IImagingMediator imagingMediator;
        private IGuiderMediator guiderMediator;
        private IApplicationStatusMediator applicationStatusMediator;
        private List<Accord.Point> brightestStarPositions = new List<Accord.Point>();

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

                HFRAndError measurement = await GetAverageMeasurement(filter, profileService.ActiveProfile.FocuserSettings.AutoFocusNumberOfFramesPerPoint, token, progress);

                //If star HFR is 0, we didn't detect any stars, and want this point to be ignored by the fitting as much as possible. Setting a very high Stdev will do the trick.
                if (measurement.HFR == 0) {
                    measurement.Stdev = 1000;
                }

                token.ThrowIfCancellationRequested();

                FocusPoints.AddSorted(new ScatterErrorPoint(_focusPosition, measurement.HFR, 0, Math.Max(0.001, measurement.Stdev)), comparer);
                PlotFocusPoints.AddSorted(new DataPoint(_focusPosition, measurement.HFR), plotComparer);
                if (i < nrOfSteps - 1) {
                    Logger.Trace("Moving focuser to next autofocus position");
                    _focusPosition = await focuserMediator.MoveFocuserRelative(-stepSize);
                }

                token.ThrowIfCancellationRequested();
                CalculateTrends();
                if (FocusPoints.Count() >= 3 && (profileService.ActiveProfile.FocuserSettings.AutoFocusCurveFitting == Utility.Enum.AFCurveFittingEnum.PARABOLIC || profileService.ActiveProfile.FocuserSettings.AutoFocusCurveFitting == Utility.Enum.AFCurveFittingEnum.TRENDPARABOLIC)) { CalculateQuadraticFitting(); }
                if (FocusPoints.Count() >= 3 && (profileService.ActiveProfile.FocuserSettings.AutoFocusCurveFitting == Utility.Enum.AFCurveFittingEnum.HYPERBOLIC || profileService.ActiveProfile.FocuserSettings.AutoFocusCurveFitting == Utility.Enum.AFCurveFittingEnum.TRENDHYPERBOLIC)) { CalculateHyperbolicFitting(); }
            }
        }

        private async Task<IImageData> TakeExposure(FilterInfo filter, CancellationToken token, IProgress<ApplicationStatus> progress) {
            Logger.Trace("Starting Exposure for autofocus");
            double expTime = profileService.ActiveProfile.FocuserSettings.AutoFocusExposureTime;
            if (filter != null && filter.AutoFocusExposureTime > 0) {
                expTime = filter.AutoFocusExposureTime;
            }
            var seq = new CaptureSequence(expTime, CaptureSequence.ImageTypes.SNAPSHOT, filter, null, 1);
            seq.EnableSubSample = _setSubSample;
            seq.Binning = new BinningMode(profileService.ActiveProfile.FocuserSettings.AutoFocusBinning, profileService.ActiveProfile.FocuserSettings.AutoFocusBinning);

            var oldAutoStretch = imagingMediator.SetAutoStretch(true);
            var oldDetectStars = imagingMediator.SetDetectStars(false);
            IImageData image;
            try {
                image = await imagingMediator.CaptureAndPrepareImage(seq, token, progress);
            } catch (Exception e) {
                Logger.Warning("Camera error, trying without subsample");
                Logger.Warning(e.Message);
                _setSubSample = false;
                seq.EnableSubSample = _setSubSample;
                image = await imagingMediator.CaptureAndPrepareImage(seq, token, progress);
            }
            imagingMediator.SetAutoStretch(oldAutoStretch);
            imagingMediator.SetDetectStars(oldDetectStars);

            return image;
        }

        private async Task<HFRAndError> EvaluateExposure(IImageData image, CancellationToken token, IProgress<ApplicationStatus> progress) {
            Logger.Trace("Evaluating Exposure");
            System.Windows.Media.PixelFormat pixelFormat;

            if (image.Statistics.IsBayered && profileService.ActiveProfile.ImageSettings.DebayerImage) {
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

            Logger.Debug(string.Format("Current Focus: Position: {0}, HFR: {1}", _focusPosition, analysis.AverageHFR));

            return new HFRAndError() { HFR = analysis.AverageHFR, Stdev = analysis.HFRStdDev };
        }

        private async Task<bool> ValidateCalculatedFocusPosition(DataPoint focusPoint, FilterInfo filter, CancellationToken token, IProgress<ApplicationStatus> progress, double initialHFR) {
            _focusPosition = await focuserMediator.MoveFocuser((int)focusPoint.X);

            double hfr = (await GetAverageMeasurement(filter, profileService.ActiveProfile.FocuserSettings.AutoFocusNumberOfFramesPerPoint, token, progress)).HFR;

            if (hfr > (focusPoint.Y * 1.25)) {
                Notification.ShowWarning(string.Format(Locale.Loc.Instance["LblFocusPointValidationFailed"], focusPoint.X, focusPoint.Y, hfr));
            }

            if (hfr > (initialHFR * 1.15)) {
                Notification.ShowWarning(string.Format(Locale.Loc.Instance["LblAutoFocusNewWorseThanOriginal"], hfr, initialHFR));
                Logger.Warning(string.Format("New focus point HFR {0} is significantly worse than original HFR {1}", hfr, initialHFR));
                return false;
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
        private double HfrCalc(double position, double perfectFocusPosition, double a, double b) {
            double x = perfectFocusPosition - position;
            double t = MathHelper.HArcsin(x / b); //calculate t-position in hyperbola
            return a * MathHelper.HCos(t); //convert t-position to y/hfd value
        }

        private double ScaledErrorHyperbola(double perfectFocusPosition, double a, double b) {
            return Math.Sqrt(FocusPoints.Sum((dp) => Math.Pow((HfrCalc(dp.X, perfectFocusPosition, a, b) - dp.Y) / dp.ErrorY, 2)));
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

        private void CalculateQuadraticFitting() {
            var fitting = new PolynomialLeastSquares() { Degree = 2 };
            PolynomialRegression poly = fitting.Learn(FocusPoints.Select((dp) => dp.X).ToArray(), FocusPoints.Select((dp) => dp.Y).ToArray(), FocusPoints.Select((dp) => 1 / (dp.ErrorY * dp.ErrorY)).ToArray());
            QuadraticFitting = (x) => (poly.Weights[0] * x * x + poly.Weights[1] * x + poly.Intercept);
            int minimumX = (int)Math.Round(poly.Weights[1] / (2 * poly.Weights[0]) * -1);
            double minimumY = QuadraticFitting(minimumX);
            QuadraticMinimum = new DataPoint(minimumX, minimumY);
        }

        private void CalculateTrends() {
            //Get the minimum based on HFR and Error, rather than just HFR. This ensures 0 HFR is never used, and low HFR / High error numbers are also ignored
            _minimum = FocusPoints.Aggregate((l, r) => l.Y + l.ErrorY < r.Y + r.ErrorY ? l : r);
            IEnumerable<ScatterErrorPoint> leftTrendPoints = FocusPoints.Where((x) => x.X < _minimum.X && x.Y > (_minimum.Y + 0.1));
            IEnumerable<ScatterErrorPoint> rightTrendPoints = FocusPoints.Where((x) => x.X > _minimum.X && x.Y > (_minimum.Y + 0.1));
            LeftTrend = new TrendLine(leftTrendPoints);
            RightTrend = new TrendLine(rightTrendPoints);
        }

        private async Task<HFRAndError> GetAverageMeasurement(FilterInfo filter, int exposuresPerFocusPoint, CancellationToken token, IProgress<ApplicationStatus> progress) {
            //Average HFR  of multiple exposures (if configured this way)
            double sumHfr = 0;
            double sumVariances = 0;
            for (int i = 0; i < exposuresPerFocusPoint; i++) {
                var image = await TakeExposure(filter, token, progress);
                var partialMeasurement = await EvaluateExposure(image, token, progress);
                sumHfr = sumHfr + partialMeasurement.HFR;
                sumVariances = sumVariances + partialMeasurement.Stdev * partialMeasurement.Stdev;
                token.ThrowIfCancellationRequested();
            }

            return new HFRAndError() { HFR = sumHfr / exposuresPerFocusPoint, Stdev = Math.Sqrt(sumVariances / exposuresPerFocusPoint) };
        }

        public enum Direction {
            IN = 1,
            OUT = -1
        }

        public async Task<bool> StartBacklashMeasurement(FilterInfo filter, CancellationToken token, IProgress<ApplicationStatus> progress) {
            Logger.Trace("Starting Backlash Measurement");
            int initialPosition = focuserInfo.Position;
            LeftTrend = null;
            RightTrend = null;

            var startBacklashDiag = MyMessageBox.MyMessageBox.Show(Locale.Loc.Instance["LblStartBacklashMeasurementConfirmation"], Locale.Loc.Instance["LblStartBacklashQuestion"], System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
            if (startBacklashDiag == System.Windows.MessageBoxResult.Cancel) {
                return false;
            }

            //Save previous backlash values
            int oldBacklashIn = profileService.ActiveProfile.FocuserSettings.BacklashIn;
            int oldBacklashOut = profileService.ActiveProfile.FocuserSettings.BacklashOut;
            int backlashIN = 0;
            int backlashOUT = 0;
            try {
                //set previous backlash values to zero, so current backlash settings do not impair measurement
                profileService.ActiveProfile.FocuserSettings.BacklashIn = profileService.ActiveProfile.FocuserSettings.BacklashOut = 0;
                await this.guiderMediator.StopGuiding(token);
                progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblStartingINBacklashMeasurement"] });
                backlashIN = await MeasureBacklash(filter, Direction.IN, token, progress);
                _focusPosition = await focuserMediator.MoveFocuser(initialPosition);
                progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblStartingOUTBacklashMeasurement"] });
                backlashOUT = await MeasureBacklash(filter, Direction.OUT, token, progress);
                progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblAutoFocusRestoringOriginalPosition"] });
                var saveBacklashDiag = MyMessageBox.MyMessageBox.Show(String.Format(Locale.Loc.Instance["LblBacklashMeasurements"], backlashIN, backlashOUT), Locale.Loc.Instance["LblSaveBacklashQuestion"], System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
                if (saveBacklashDiag == System.Windows.MessageBoxResult.OK) {
                    //Set new backlash values
                    profileService.ActiveProfile.FocuserSettings.BacklashIn = backlashIN;
                    profileService.ActiveProfile.FocuserSettings.BacklashOut = backlashOUT;
                } else {
                    //Set back old backlash values
                    profileService.ActiveProfile.FocuserSettings.BacklashIn = oldBacklashIn;
                    profileService.ActiveProfile.FocuserSettings.BacklashOut = oldBacklashOut;
                }
            } catch (OperationCanceledException) {
                FocusPoints.Clear();
                PlotFocusPoints.Clear();
            } catch (Exception e) {
                Logger.Warning(e.Message);
                Notification.ShowError(Locale.Loc.Instance["LblBacklashMeasurementException"]);
            } finally {
                progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblAutoFocusRestoringOriginalPosition"] });
                _focusPosition = await focuserMediator.MoveFocuser(initialPosition);
            }
            return true;
        }

        public async Task<int> MeasureBacklash(FilterInfo filter, Direction direction, CancellationToken token, IProgress<ApplicationStatus> progress) {
            FocusPoints.Clear();
            PlotFocusPoints.Clear();
            int stepSize = profileService.ActiveProfile.FocuserSettings.AutoFocusStepSize;
            int offset = profileService.ActiveProfile.FocuserSettings.AutoFocusInitialOffsetSteps;
            int backlash = 0;
            var comparer = new FocusPointComparer();
            //initial move far in or out of focus
            _focusPosition = await focuserMediator.MoveFocuserRelative((int)Math.Ceiling(offset * stepSize * 2d * (int)direction));
            token.ThrowIfCancellationRequested();
            //get HFR at this point
            double hfr0 = (await GetAverageMeasurement(filter, 3, token, progress)).HFR;
            token.ThrowIfCancellationRequested();
            FocusPoints.AddSorted(new ScatterErrorPoint(_focusPosition, hfr0, 0, 0), comparer);

            int counter = 0;
            double hfr1 = 0;
            do {
                //Move back one step
                _focusPosition = await focuserMediator.MoveFocuserRelative((int)Math.Round(stepSize * (int)direction * -1d));
                token.ThrowIfCancellationRequested();
                //get HFR at this point
                hfr1 = (await GetAverageMeasurement(filter, 3, token, progress)).HFR;
                token.ThrowIfCancellationRequested();
                FocusPoints.AddSorted(new ScatterErrorPoint(_focusPosition, hfr1, 0, 0), comparer);
                counter++;
            } while (Math.Abs((hfr0 - hfr1) / hfr1) < 0.03 && counter < 3); //Slope is almost zero, backlash not cleared yet

            //Move back one more step
            _focusPosition = await focuserMediator.MoveFocuserRelative((int)Math.Round(stepSize * (int)direction * -1d));
            token.ThrowIfCancellationRequested();
            //get HFR at this point
            double hfr2 = (await GetAverageMeasurement(filter, 3, token, progress)).HFR;
            token.ThrowIfCancellationRequested();
            FocusPoints.AddSorted(new ScatterErrorPoint(_focusPosition, hfr2, 0, 0), comparer);

            //This far from focus, hfr0, hfr1, and hfr2 should be on a line, let's get the slopes
            double measuredSlope = Math.Abs((hfr0 - hfr1) / (stepSize * counter));
            double idealSlope = Math.Abs((hfr1 - hfr2) / stepSize);

            if (hfr1 != hfr2 && measuredSlope < idealSlope) {
                backlash = (int)Math.Round((1 - measuredSlope / idealSlope) * stepSize * counter);
            }

            return backlash;
        }

        public async Task<bool> StartAutoFocus(FilterInfo filter, CancellationToken token, IProgress<ApplicationStatus> progress) {
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
            int initialFocusPosition;
            double initialHFR = 0;

            System.Drawing.Rectangle oldSubSample = new System.Drawing.Rectangle();

            if (profileService.ActiveProfile.FocuserSettings.AutoFocusInnerCropRatio < 1 && profileService.ActiveProfile.FocuserSettings.AutoFocusOuterCropRatio == 1 && cameraInfo.CanSubSample) {
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

                //Get initial position information, as average of multiple exposures, if configured this way
                initialHFR = (await GetAverageMeasurement(filter, profileService.ActiveProfile.FocuserSettings.AutoFocusNumberOfFramesPerPoint, token, progress)).HFR;
                initialFocusPosition = focuserInfo.Position;

                bool reattempt = false;
                do {
                    numberOfAttempts = numberOfAttempts + 1;

                    var offsetSteps = profileService.ActiveProfile.FocuserSettings.AutoFocusInitialOffsetSteps;
                    var offset = offsetSteps;

                    var nrOfSteps = offsetSteps + 1;

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
                            return false;
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

                    //Get Trendline Intersection
                    TrendLineIntersection = LeftTrend.Intersect(RightTrend);

                    if (profileService.ActiveProfile.FocuserSettings.AutoFocusCurveFitting == Utility.Enum.AFCurveFittingEnum.TRENDLINES) {
                        FinalFocusPoint = TrendLineIntersection;
                    }

                    if (profileService.ActiveProfile.FocuserSettings.AutoFocusCurveFitting == Utility.Enum.AFCurveFittingEnum.HYPERBOLIC) {
                        CalculateHyperbolicFitting();
                        FinalFocusPoint = HyperbolicMinimum;
                    }

                    if (profileService.ActiveProfile.FocuserSettings.AutoFocusCurveFitting == Utility.Enum.AFCurveFittingEnum.PARABOLIC) {
                        CalculateQuadraticFitting();
                        FinalFocusPoint = QuadraticMinimum;
                    }

                    if (profileService.ActiveProfile.FocuserSettings.AutoFocusCurveFitting == Utility.Enum.AFCurveFittingEnum.TRENDPARABOLIC) {
                        CalculateQuadraticFitting();
                        FinalFocusPoint = new DataPoint(Math.Round((TrendLineIntersection.X + QuadraticMinimum.X) / 2), (TrendLineIntersection.Y + QuadraticMinimum.Y) / 2);
                    }

                    if (profileService.ActiveProfile.FocuserSettings.AutoFocusCurveFitting == Utility.Enum.AFCurveFittingEnum.TRENDHYPERBOLIC) {
                        CalculateHyperbolicFitting();
                        FinalFocusPoint = new DataPoint(Math.Round((TrendLineIntersection.X + HyperbolicMinimum.X) / 2), (TrendLineIntersection.Y + HyperbolicMinimum.Y) / 2);
                    }

                    LastAutoFocusPoint = new AutoFocusPoint { Focuspoint = FinalFocusPoint, Temperature = focuserInfo.Temperature, Timestamp = DateTime.Now };

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
                            return false;
                        }
                    }
                } while (reattempt);
                //_focusPosition = await Mediator.Instance.RequestAsync(new MoveFocuserMessage() { Position = (int)p.X, Absolute = true, Token = token });
            } catch (OperationCanceledException) {
                FocusPoints.Clear();
                PlotFocusPoints.Clear();
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
            return true;
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
        public ICommand StartBacklashMeasurementCommand { get; private set; }
        public ICommand CancelBacklashMeasurementCommand { get; private set; }
    }

    public static class MathHelper {

        // Hyperbolic Sine
        public static double HSin(double x) {
            return (Math.Exp(x) - Math.Exp(-x)) / 2;
        }

        // Hyperbolic Cosine
        public static double HCos(double x) {
            return (Math.Exp(x) + Math.Exp(-x)) / 2;
        }

        // Hyperbolic Tangent
        public static double HTan(double x) {
            return (Math.Exp(x) - Math.Exp(-x)) / (Math.Exp(x) + Math.Exp(-x));
        }

        // Inverse Hyperbolic Sine
        public static double HArcsin(double x) {
            return Math.Log(x + Math.Sqrt(x * x + 1));
        }

        // Inverse Hyperbolic Cosine
        public static double HArccos(double x) {
            return Math.Log(x + Math.Sqrt(x * x - 1));
        }

        // Inverse Hyperbolic Tangent
        public static double HArctan(double x) {
            return Math.Log((1 + x) / (1 - x)) / 2;
        }
    }

    public class AutoFocusPoint {
        public DataPoint Focuspoint { get; set; }
        public DateTime Timestamp { get; set; }
        public double Temperature { get; set; }
    }

    public class FocusPointComparer : IComparer<ScatterErrorPoint> {

        public int Compare(ScatterErrorPoint x, ScatterErrorPoint y) {
            if (x.X < y.X) {
                return -1;
            } else if (x.X > y.X) {
                return 1;
            } else {
                return 0;
            }
        }
    }

    public class PlotPointComparer : IComparer<DataPoint> {

        public int Compare(DataPoint x, DataPoint y) {
            if (x.X < y.X) {
                return -1;
            } else if (x.X > y.X) {
                return 1;
            } else {
                return 0;
            }
        }
    }

    public struct HFRAndError {
        public double HFR { get; set; }
        public double Stdev { get; set; }
    }

    public class TrendLine {

        public TrendLine(IEnumerable<ScatterErrorPoint> l) {
            DataPoints = l;

            if (DataPoints.Count() > 1) {
                double[] inputs = DataPoints.Select((dp) => dp.X).ToArray();
                double[] outputs = DataPoints.Select((dp) => dp.Y).ToArray();
                double[] weights = DataPoints.Select((dp) => 1 / (dp.ErrorY * dp.ErrorY)).ToArray();

                OrdinaryLeastSquares ols = new OrdinaryLeastSquares();
                SimpleLinearRegression regression = ols.Learn(inputs, outputs, weights);

                Slope = regression.Slope;
                Offset = regression.Intercept;
            }
        }

        public double Slope { get; set; }
        public double Offset { get; set; }

        public IEnumerable<ScatterErrorPoint> DataPoints { get; set; }

        public double GetY(double x) {
            return Slope * x + Offset;
        }

        public DataPoint Intersect(TrendLine line) {
            if (this.Slope == line.Slope) {
                //Lines are parallel
                return new DataPoint(0, 0);
            }
            var x = (line.Offset - this.Offset) / (this.Slope - line.Slope);
            var y = this.Slope * x + this.Offset;

            return new DataPoint((int)Math.Round(x), y);
        }
    }
}