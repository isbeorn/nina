#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Core.Enum;
using NINA.Image.ImageAnalysis;
using NINA.Profile.Interfaces;
using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NINA.WPF.Base.Utility.AutoFocus {

    public class AutoFocusReport {

        [JsonProperty]
        public int Version { get; set; } = 2;

        [JsonProperty]
        public string Filter { get; set; }

        [JsonProperty]
        public string AutoFocuserName { get; set; }

        [JsonProperty]
        public string StarDetectorName { get; set; }

        [JsonProperty]
        public DateTime Timestamp { get; set; }

        [JsonProperty]
        public double Temperature { get; set; }

        [JsonProperty]
        public string Method { get; set; }

        [JsonProperty]
        public string Fitting { get; set; }

        [JsonProperty]
        public FocusPoint InitialFocusPoint { get; set; } = new FocusPoint();

        [JsonProperty]
        public FocusPoint CalculatedFocusPoint { get; set; } = new FocusPoint();

        [JsonProperty]
        public FocusPoint PreviousFocusPoint { get; set; } = new FocusPoint();

        [JsonProperty]
        public IEnumerable<FocusPoint> MeasurePoints { get; set; }

        [JsonProperty]
        public Intersections Intersections { get; set; }

        [JsonProperty]
        public Fittings Fittings { get; set; }

        [JsonProperty]
        public RSquares RSquares { get; set; }

        [JsonProperty]
        public BacklashCompensation BacklashCompensation { get; set; }

        [JsonProperty]
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Generates a JSON report into %localappdata%\NINA\AutoFocus for the complete autofocus run containing all the measurements
        /// </summary>
        /// <param name="initialFocusPosition"></param>
        /// <param name="initialHFR"></param>
        public static AutoFocusReport GenerateReport(
            IProfileService profileService,
            ICollection<ScatterErrorPoint> FocusPoints,
            double initialFocusPosition,
            double initialHFR,
            DataPoint focusPoint,
            ReportAutoFocusPoint lastFocusPoint,
            TrendlineFitting trendlineFitting,
            QuadraticFitting quadraticFitting,
            HyperbolicFitting hyperbolicFitting,
            GaussianFitting gaussianFitting,
            double temperature,
            string filter,
            TimeSpan duration) {
            return GenerateReport(
                profileService,
                "Unknown",
                "Unknown",
                FocusPoints,
                initialFocusPosition,
                initialHFR,
                focusPoint,
                lastFocusPoint,
                trendlineFitting,
                quadraticFitting,
                hyperbolicFitting,
                gaussianFitting,
                temperature,
                filter,
                duration);
        }

        public static AutoFocusReport GenerateReport(
            IProfileService profileService,
            string autoFocuserName,
            string starDetectorName,
            ICollection<ScatterErrorPoint> FocusPoints,
            double initialFocusPosition,
            double initialHFR,
            DataPoint focusPoint,
            ReportAutoFocusPoint lastFocusPoint,
            TrendlineFitting trendlineFitting,
            QuadraticFitting quadraticFitting,
            HyperbolicFitting hyperbolicFitting,
            GaussianFitting gaussianFitting,
            double temperature,
            string filter,
            TimeSpan duration) {
            var report = new AutoFocusReport() {
                AutoFocuserName = autoFocuserName,
                StarDetectorName = starDetectorName,
                Filter = filter,
                Timestamp = DateTime.Now,
                Temperature = temperature,
                InitialFocusPoint = new FocusPoint() {
                    Position = initialFocusPosition,
                    Value = initialHFR
                },
                CalculatedFocusPoint = new FocusPoint() {
                    Position = focusPoint.X,
                    Value = focusPoint.Y
                },
                PreviousFocusPoint = new FocusPoint() {
                    Position = lastFocusPoint?.Focuspoint.X ?? double.NaN,
                    Value = lastFocusPoint?.Focuspoint.Y ?? double.NaN
                },
                Method = profileService.ActiveProfile.FocuserSettings.AutoFocusMethod.ToString(),
                Fitting = profileService.ActiveProfile.FocuserSettings.AutoFocusMethod == AFMethodEnum.STARHFR ? profileService.ActiveProfile.FocuserSettings.AutoFocusCurveFitting.ToString() : "GAUSSIAN",
                MeasurePoints = FocusPoints.Select(x => new FocusPoint() { Position = x.X, Value = x.Y, Error = x.ErrorY }),
                Intersections = new Intersections() {
                    TrendLineIntersection = new FocusPoint() { Position = trendlineFitting.Intersection.X, Value = trendlineFitting.Intersection.Y },
                    GaussianMaximum = new FocusPoint() { Position = gaussianFitting.Maximum.X, Value = gaussianFitting.Maximum.Y },
                    HyperbolicMinimum = new FocusPoint() { Position = hyperbolicFitting.Minimum.X, Value = hyperbolicFitting.Minimum.Y },
                    QuadraticMinimum = new FocusPoint() { Position = quadraticFitting.Minimum.X, Value = quadraticFitting.Minimum.Y }
                },
                Fittings = new Fittings() {
                    Gaussian = gaussianFitting.Expression,
                    Hyperbolic = hyperbolicFitting.Expression,
                    Quadratic = quadraticFitting.Expression,
                    LeftTrend = trendlineFitting.LeftExpression,
                    RightTrend = trendlineFitting.RightExpression
                },
                RSquares = new RSquares() {
                    Hyperbolic = hyperbolicFitting.RSquared,
                    Quadratic = quadraticFitting.RSquared,
                    LeftTrend = trendlineFitting.LeftTrend?.RSquared ?? double.NaN,
                    RightTrend = trendlineFitting.RightTrend?.RSquared ?? double.NaN
                },
                BacklashCompensation = new BacklashCompensation() {
                    BacklashCompensationModel = profileService.ActiveProfile.FocuserSettings.BacklashCompensationModel.ToString(),
                    BacklashIN = profileService.ActiveProfile.FocuserSettings.BacklashIn,
                    BacklashOUT = profileService.ActiveProfile.FocuserSettings.BacklashOut,
                },
                Duration = duration
            };

            return report;
        }
    }

    public class Intersections {

        [JsonProperty]
        public FocusPoint TrendLineIntersection { get; set; }

        [JsonProperty]
        public FocusPoint HyperbolicMinimum { get; set; }

        [JsonProperty]
        public FocusPoint QuadraticMinimum { get; set; }

        [JsonProperty]
        public FocusPoint GaussianMaximum { get; set; }
    }

    public class Fittings {

        [JsonProperty]
        public string Quadratic { get; set; }

        [JsonProperty]
        public string Hyperbolic { get; set; }

        [JsonProperty]
        public string Gaussian { get; set; }

        [JsonProperty]
        public string LeftTrend { get; set; }

        [JsonProperty]
        public string RightTrend { get; set; }
    }

    public class RSquares {

        [JsonProperty]
        public double Quadratic { get; set; }

        [JsonProperty]
        public double Hyperbolic { get; set; }

        [JsonProperty]
        public double LeftTrend { get; set; }

        [JsonProperty]
        public double RightTrend { get; set; }
    }

    public class FocusPoint {

        [JsonProperty]
        public double Position { get; set; }

        [JsonProperty]
        public double Value { get; set; }

        [JsonProperty]
        public double Error { get; set; }
    }

    public class BacklashCompensation {

        [JsonProperty]
        public string BacklashCompensationModel { get; set; }

        [JsonProperty]
        public int BacklashIN { get; set; }

        [JsonProperty]
        public int BacklashOUT { get; set; }
    }
}