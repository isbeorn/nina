#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Accord.Statistics.Models.Regression.Linear;
using NINA.Core.Utility;
using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace NINA.WPF.Base.Utility.AutoFocus {

    public class QuadraticFitting : BaseINPC {

        public QuadraticFitting() {
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

        public QuadraticFitting Calculate(ICollection<ScatterErrorPoint> points) {
            var fitting = new PolynomialLeastSquares() { Degree = 2 };

            double[] inputs = points.Select((dp) => dp.X).ToArray();
            double[] outputs = points.Select((dp) => dp.Y).ToArray();
            double[] weights = points.Select((dp) => 1 / (dp.ErrorY * dp.ErrorY)).ToArray();

            PolynomialRegression poly = fitting.Learn(inputs, outputs, weights);
            RSquared = poly.CoefficientOfDetermination(inputs, outputs, weights);

            FormattableString expression = $"y = {poly.Weights[0]} * x^2 + {poly.Weights[1]} * x + {poly.Intercept}";

            Expression = expression.ToString(CultureInfo.InvariantCulture);
            Fitting = (x) => (poly.Weights[0] * x * x + poly.Weights[1] * x + poly.Intercept);
            int minimumX = (int)Math.Round(poly.Weights[1] / (2 * poly.Weights[0]) * -1);
            double minimumY = Fitting(minimumX);
            Minimum = new DataPoint(minimumX, minimumY);
            return this;
        }

        public override string ToString() {
            return $"{Expression}";
        }
    }
}