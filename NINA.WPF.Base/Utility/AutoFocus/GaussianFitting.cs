#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Accord.Statistics.Models.Regression.Fitting;
using NINA.Core.Utility;
using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace NINA.WPF.Base.Utility.AutoFocus {

    public class GaussianFitting : BaseINPC {

        public GaussianFitting() {
        }

        private Func<double, double> _fitting;

        public Func<double, double> Fitting {
            get => _fitting;
            set {
                _fitting = value;
                RaisePropertyChanged();
            }
        }

        private DataPoint _maximum;

        public DataPoint Maximum {
            get => _maximum;
            set {
                _maximum = value;
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

        public GaussianFitting Calculate(ICollection<ScatterErrorPoint> points) {
            double[][] inputs = Accord.Math.Matrix.ToJagged(points.ToList().ConvertAll((dp) => dp.X).ToArray());
            double[] outputs = points.ToList().ConvertAll((dp) => dp.Y).ToArray();

            ScatterErrorPoint lowestPoint = points.Where((dp) => dp.Y >= 0.1).Aggregate((l, r) => l.Y < r.Y ? l : r); // Get lowest non-zero datapoint
            ScatterErrorPoint highestPoint = points.Aggregate((l, r) => l.Y > r.Y ? l : r); // Get highest datapoint
            double highestPosition = highestPoint.X;
            double highestContrast = highestPoint.Y;
            double lowestPosition = lowestPoint.X;
            double lowestContrast = lowestPoint.Y;
            double sigma = Accord.Statistics.Measures.StandardDeviation(points.ToList().ConvertAll((dp) => dp.X).ToArray());

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
            FormattableString expression = $"y = {regression.Coefficients[2]} * exp(-1 * (x - {regression.Coefficients[0]}) * (x - {regression.Coefficients[0]}) / (2 * {regression.Coefficients[1]} * {regression.Coefficients[1]})) + {regression.Coefficients[3]}";
            Expression = expression.ToString(CultureInfo.InvariantCulture);
            Fitting = (x) => regression.Coefficients[2] * Math.Exp(-1 * (x - regression.Coefficients[0]) * (x - regression.Coefficients[0]) / (2 * regression.Coefficients[1] * regression.Coefficients[1])) + regression.Coefficients[3];
            Maximum = new DataPoint((int)Math.Round(regression.Coefficients[0]), regression.Coefficients[2] + regression.Coefficients[3]);
            return this;
        }

        public override string ToString() {
            return $"{Expression}";
        }
    }
}