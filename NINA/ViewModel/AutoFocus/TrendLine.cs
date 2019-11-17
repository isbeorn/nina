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