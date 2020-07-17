#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using OxyPlot;
using OxyPlot.Series;
using Accord.Statistics.Models.Regression.Linear;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NINA.ViewModel {

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

        public override string ToString() {
            var sb = new StringBuilder();
            sb.AppendLine($"    Slope: {Slope} Offset: {Offset}");
            sb.AppendLine($"    Datapoints:");
            var sortedList = DataPoints.OrderBy(x => x.X);
            foreach (var point in sortedList) {
                sb.AppendLine($"        X: {point.X} Y: {point.Y} Error: {point.ErrorY}");
            }
            return sb.ToString();
        }
    }
}