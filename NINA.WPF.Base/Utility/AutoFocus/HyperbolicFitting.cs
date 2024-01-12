#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Accord.Math.Optimization.Losses;
using NINA.Core.Utility;
using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace NINA.WPF.Base.Utility.AutoFocus {

    public class HyperbolicFitting : BaseINPC {

        public HyperbolicFitting() {
        }

        private Func<double, double> _fitting;

        public Func<double, double> Fitting {
            get => _fitting;
            set {
                _fitting = value;
                RaisePropertyChanged();
            }
        }

        private DataPoint _minimum;

        public DataPoint Minimum {
            get => _minimum;
            set {
                _minimum = value;
                RaisePropertyChanged();
            }
        }

        private string _expression;

        public string Expression {
            get => _expression;
            set {
                _expression = value;
                RaisePropertyChanged();
            }
        }

        private double rSquared;

        public double RSquared {
            get => rSquared;
            set {
                rSquared = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// The routine will try to find the best hyperbola curve fit. The focuser position p at the hyperbola minimum is the expected best focuser position
        /// The FocusPoints List will be used as input to the fitting
        /// </summary>
        public HyperbolicFitting Calculate(ICollection<ScatterErrorPoint> points) {
            double error1, oldError, pRange, aRange, bRange, highestHfr, lowestHfr, highestPosition, lowestPosition, a, b, p, a1, b1, p1, a0, b0, p0;
            double lowestError = double.MaxValue; //scaled RMS (square root of the mean square) of the HFD errors after curve fitting

            var nonZeroPoints = points.Where((dp) => dp.Y >= 0.1);
            if (!nonZeroPoints.Any()) {
                // No non zero points in curve. No fit can be calculated.
                return this;
            }
            int n = nonZeroPoints.Count();

            ScatterErrorPoint lowestPoint = nonZeroPoints.Aggregate((l, r) => l.Y < r.Y ? l : r); // Get lowest non-zero datapoint
            ScatterErrorPoint highestPoint = nonZeroPoints.Aggregate((l, r) => l.Y > r.Y ? l : r); // Get highest datapoint
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

            if (double.IsNaN(aRange) || double.IsNaN(bRange) || aRange == 0 || bRange == 0 || pRange == 0) {
                //Not enough valid data points to fit a curve
                return this;
            }

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
                            error1 = ScaledErrorHyperbola(nonZeroPoints, p1, a1, b1);
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

            FormattableString expression = $"y = {a} * cosh(asinh(({p} - x) / {b}))";
            Expression = expression.ToString(CultureInfo.InvariantCulture);
            Fitting = (x) => a * MathHelper.HCos(MathHelper.HArcsin((p - x) / b));
            Minimum = new DataPoint((int)Math.Round(p), a);

            var inputs = nonZeroPoints.Select((dp) => dp.X).ToArray();
            var outputs = nonZeroPoints.Select((dp) => dp.Y).ToArray();
            //var weights = nonZeroPoints.Select((dp) => 1 / (dp.ErrorY * dp.ErrorY)).ToArray();
            var transformed = new double[inputs.Length];
            var rSquared = new RSquaredLoss(n, outputs);
            //rSquared.Weights = weights;
            for (var i = 0; i < n; i++) {
                transformed[i] = Fitting(inputs[i]);
            }
            RSquared = rSquared.Loss(transformed);

            return this;
        }

        private double ScaledErrorHyperbola(IEnumerable<ScatterErrorPoint> points, double perfectFocusPosition, double a, double b) {
            return Math.Sqrt(points.Sum((dp) => Math.Pow((HyperbolicFittingHfrCalc(dp.X, perfectFocusPosition, a, b) - dp.Y) / dp.ErrorY, 2)));
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

        public override string ToString() {
            return $"{Expression}";
        }
    }
}