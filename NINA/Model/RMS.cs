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
*/

#endregion "copyright"

using NINA.Utility;
using System;

namespace NINA.Model {

    public class RMS : BaseINPC {
        private int datapoints;
        private double sum_RA;
        private double sum_RA2;
        private double sum_Dec;
        private double sum_Dec2;
        private double ra;
        private double dec;
        private double total;

        public double RA {
            get {
                return ra;
            }

            set {
                ra = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(RAText));
            }
        }

        public double Dec {
            get {
                return dec;
            }

            set {
                dec = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(DecText));
            }
        }

        public double Total {
            get {
                return total;
            }

            set {
                total = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(TotalText));
            }
        }

        public string RAText {
            get {
                return string.Format(Locale.Loc.Instance["LblPHD2RARMS"], RA.ToString("0.00"), (RA * Scale).ToString("0.00"));
            }
        }

        public string DecText {
            get {
                return string.Format(Locale.Loc.Instance["LblPHD2DecRMS"], Dec.ToString("0.00"), (Dec * Scale).ToString("0.00"));
            }
        }

        public string TotalText {
            get {
                return string.Format(Locale.Loc.Instance["LblPHD2TotalRMS"], Total.ToString("0.00"), (Total * Scale).ToString("0.00"));
            }
        }

        public double Scale { get; private set; } = 1;

        public void AddDataPoint(double raDistance, double decDistance) {
            datapoints++;
            sum_RA += raDistance;
            sum_RA2 += (raDistance * raDistance);
            sum_Dec += decDistance;
            sum_Dec2 += (decDistance * decDistance);

            CalculateRMS();
        }

        public void RemoveDataPoint(double raDistance, double decDistance) {
            datapoints--;
            sum_RA -= raDistance;
            sum_RA2 -= (raDistance * raDistance);
            sum_Dec -= decDistance;
            sum_Dec2 -= (decDistance * decDistance);

            CalculateRMS();
        }

        private void CalculateRMS() {
            var ra = Math.Sqrt(datapoints * sum_RA2 - sum_RA * sum_RA) / datapoints;
            var dec = Math.Sqrt(datapoints * sum_Dec2 - sum_Dec * sum_Dec) / datapoints;
            RA = ra;
            Dec = dec;
            Total = Math.Sqrt((Math.Pow(dec, 2) + Math.Pow(ra, 2)));
        }

        public void Clear() {
            datapoints = 0;
            sum_RA = 0.0d;
            sum_RA2 = 0.0d;
            sum_Dec = 0.0d;
            sum_Dec2 = 0.0d;
            RA = 0;
            Dec = 0;
            Total = 0;
            RaiseAllPropertiesChanged();
        }

        public void SetScale(double scale) {
            this.Scale = scale;
            RaiseAllPropertiesChanged();
        }
    }
}