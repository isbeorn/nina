#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility;
using System;
using System.Runtime.Serialization;

namespace NINA.Model.MyFilterWheel {

    [Serializable]
    [DataContract]
    public class FlatWizardFilterSettings : BaseINPC {
        private double histogramMeanTarget;

        private double histogramTolerance;

        private double maxFlatExposureTime;

        private double minFlatExposureTime;

        private double stepSize;

        public FlatWizardFilterSettings() {
            HistogramMeanTarget = 0.5;
            HistogramTolerance = 0.1;
            StepSize = 0.1;
            MinFlatExposureTime = 0.01;
            MaxFlatExposureTime = 30;
        }

        [DataMember]
        public double HistogramMeanTarget {
            get => histogramMeanTarget;
            set {
                histogramMeanTarget = value;
                RaisePropertyChanged();
            }
        }

        [DataMember]
        public double HistogramTolerance {
            get => histogramTolerance;
            set {
                histogramTolerance = value;
                RaisePropertyChanged();
            }
        }

        [DataMember]
        public double MaxFlatExposureTime {
            get => maxFlatExposureTime;
            set {
                maxFlatExposureTime = value;
                RaisePropertyChanged();
            }
        }

        [DataMember]
        public double MinFlatExposureTime {
            get => minFlatExposureTime;
            set {
                minFlatExposureTime = value;
                if (MaxFlatExposureTime < minFlatExposureTime) {
                    MaxFlatExposureTime = minFlatExposureTime;
                }

                RaisePropertyChanged();
            }
        }

        [DataMember]
        public double StepSize {
            get => stepSize;
            set {
                stepSize = value;
                RaisePropertyChanged();
            }
        }
    }
}
