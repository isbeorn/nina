#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using System;

namespace NINA.Core.Model {

    public class RMS : BaseINPC {
        private int datapoints;
        private double meanRA = 0, meanDec = 0;
        private double m2RA = 0, m2Dec = 0; // Variance accumulator
        private double ra;
        private double dec;
        private double total;
        private double peakRA = 0;
        private double peakDec = 0;

        public double RA {
            get => ra;

            set {
                ra = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(RAText));
            }
        }

        public double Dec {
            get => dec;

            set {
                dec = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(DecText));
            }
        }

        public double Total {
            get => total;

            set {
                total = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(TotalText));
            }
        }

        public string RAText => string.Format(Locale.Loc.Instance["LblPHD2RARMS"], RA.ToString("0.00"), (RA * Scale).ToString("0.00"));

        public string DecText => string.Format(Locale.Loc.Instance["LblPHD2DecRMS"], Dec.ToString("0.00"), (Dec * Scale).ToString("0.00"));

        public string TotalText => string.Format(Locale.Loc.Instance["LblPHD2TotalRMS"], Total.ToString("0.00"), (Total * Scale).ToString("0.00"));
        public string PeakRAText => string.Format(Locale.Loc.Instance["LblPHD2PeakRA"], PeakRA.ToString("0.00"), (PeakRA * Scale).ToString("0.00"));
        public string PeakDecText => string.Format(Locale.Loc.Instance["LblPHD2PeakDec"], PeakDec.ToString("0.00"), (PeakDec * Scale).ToString("0.00"));

        public double Scale { get; private set; } = 1;

        public double PeakRA { 
            get => peakRA; 
            set {
                peakRA = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(PeakRAText));
            }
        }

        public double PeakDec { 
            get => peakDec; 
            set {
                peakDec = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(PeakDecText));
            }
        }

        public int DataPoints { get => datapoints; }

        public void AddDataPoint(double raDistance, double decDistance) {
            datapoints++;
            double deltaRA = raDistance - meanRA;
            meanRA += deltaRA / datapoints;
            m2RA += deltaRA * (raDistance - meanRA);

            double deltaDec = decDistance - meanDec;
            meanDec += deltaDec / datapoints;
            m2Dec += deltaDec * (decDistance - meanDec);

            PeakRA = Math.Max(PeakRA, Math.Abs(raDistance));
            PeakDec = Math.Max(PeakDec, Math.Abs(decDistance));

            CalculateRMS();
        }

        public void RemoveDataPoint(double raDistance, double decDistance) {
            if (datapoints <= 0) {
                return;
            }

            if (datapoints == 1) {
                Clear();
                return;
            }

            int newDataPoints = datapoints - 1;

            double previousMeanRA = meanRA;
            meanRA = ((meanRA * datapoints) - raDistance) / newDataPoints;
            m2RA -= (raDistance - previousMeanRA) * (raDistance - meanRA);

            double previousMeanDec = meanDec;
            meanDec = ((meanDec * datapoints) - decDistance) / newDataPoints;
            m2Dec -= (decDistance - previousMeanDec) * (decDistance - meanDec);

            datapoints = newDataPoints;
            if (m2RA < 0 && m2RA > -1e-9) {
                m2RA = 0;
            }
            if (m2Dec < 0 && m2Dec > -1e-9) {
                m2Dec = 0;
            }

            CalculateRMS();
        }

        private void CalculateRMS() {
            RA = (datapoints > 0) ? Math.Sqrt(m2RA / datapoints) : 0;
            Dec = (datapoints > 0) ? Math.Sqrt(m2Dec / datapoints) : 0;
            Total = Math.Sqrt(RA * RA + Dec * Dec);
        }

        public void Clear() {
            datapoints = 0;
            meanRA = 0.0d;
            meanDec = 0.0d;
            m2RA = 0.0d;
            m2Dec = 0.0d;
            RA = 0;
            Dec = 0;
            Total = 0;
            PeakRA = 0;
            PeakDec = 0;
            RaiseAllPropertiesChanged();
        }

        public void SetScale(double scale) {
            this.Scale = scale;
            RaiseAllPropertiesChanged();
        }
    }
}