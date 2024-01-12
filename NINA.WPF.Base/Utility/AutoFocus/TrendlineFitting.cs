#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Core.Utility;
using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace NINA.WPF.Base.Utility.AutoFocus {

    public class TrendlineFitting : BaseINPC {

        public TrendlineFitting() {
        }

        private Trendline _leftTrend;

        public Trendline LeftTrend {
            get => _leftTrend;
            set {
                _leftTrend = value;
                RaisePropertyChanged();
            }
        }

        private Trendline _rightTrend;

        public Trendline RightTrend {
            get => _rightTrend;
            set {
                _rightTrend = value;
                RaisePropertyChanged();
            }
        }

        private DataPoint _intersection;

        public DataPoint Intersection {
            get => _intersection;
            set {
                _intersection = value;
                RaisePropertyChanged();
            }
        }

        private ScatterErrorPoint _minimum;

        public ScatterErrorPoint Minimum {
            get => _minimum;
            set {
                _minimum = value;
                RaisePropertyChanged();
            }
        }

        private string _leftExpression;

        public string LeftExpression {
            get => _leftExpression;
            set {
                _leftExpression = value;
                RaisePropertyChanged();
            }
        }

        private string _rightExpression;

        public string RightExpression {
            get => _rightExpression;
            set {
                _rightExpression = value;
                RaisePropertyChanged();
            }
        }

        public TrendlineFitting Calculate(ICollection<ScatterErrorPoint> points, string afMethod) {
            if (afMethod == AFMethodEnum.STARHFR.ToString()) {
                //Get the minimum based on HFR and Error, rather than just HFR. This ensures 0 HFR is never used, and low HFR / High error numbers are also ignored
                Minimum = points.Aggregate((l, r) => l.Y + l.ErrorY < r.Y + r.ErrorY ? l : r);
                IEnumerable<ScatterErrorPoint> leftTrendPoints = points.Where((x) => x.X < Minimum.X && x.Y > (Minimum.Y + 0.1));
                IEnumerable<ScatterErrorPoint> rightTrendPoints = points.Where((x) => x.X > Minimum.X && x.Y > (Minimum.Y + 0.1));
                LeftTrend = new Trendline(leftTrendPoints);
                RightTrend = new Trendline(rightTrendPoints);
                Intersection = LeftTrend.Intersect(RightTrend);

                FormattableString leftExpression = $"y = {LeftTrend.Slope} * x + {LeftTrend.Offset}";
                LeftExpression = leftExpression.ToString(CultureInfo.InvariantCulture);
                FormattableString rightExpression = $"y = {RightTrend.Slope} * x + {RightTrend.Offset}";
                RightExpression = rightExpression.ToString(CultureInfo.InvariantCulture);
            } else {
                var max = points.Aggregate((l, r) => l.Y - l.ErrorY > r.Y - r.ErrorY ? l : r);
                Minimum = max; // trendline minimum is actually Gaussian max
                IEnumerable<ScatterErrorPoint> leftTrendPoints = points.Where((x) => x.X < max.X && x.Y < (max.Y - 0.01));
                IEnumerable<ScatterErrorPoint> rightTrendPoints = points.Where((x) => x.X > max.X && x.Y < (max.Y - 0.01));
                LeftTrend = new Trendline(leftTrendPoints);
                RightTrend = new Trendline(rightTrendPoints);
            }
            return this;
        }

        public override string ToString() {
            return $"{LeftExpression} | {RightExpression}";
        }
    }
}