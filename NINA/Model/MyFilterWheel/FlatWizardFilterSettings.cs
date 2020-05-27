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
using NINA.Utility.Enum;
using System;
using System.Runtime.Serialization;

namespace NINA.Model.MyFilterWheel {

    [Serializable]
    [DataContract]
    public class FlatWizardFilterSettings : BaseINPC {
        private FlatWizardMode flatWizardMode;
        private double histogramMeanTarget;

        private double histogramTolerance;

        private double maxFlatExposureTime;

        private double minFlatExposureTime;

        private double stepSize;

        private double maxFlatDeviceBrightness;

        private double minFlatDeviceBrightness;

        private double flatDeviceStepSize;

        public FlatWizardFilterSettings() {
            flatWizardMode = FlatWizardMode.DYNAMICEXPOSURE;
            HistogramMeanTarget = 0.5;
            HistogramTolerance = 0.1;
            StepSize = 0.1;
            MinFlatExposureTime = 0.01;
            MaxFlatExposureTime = 30;
            MinFlatDeviceBrightness = 0;
            MaxFlatDeviceBrightness = 100;
            FlatDeviceStepSize = 10;
        }

        [DataMember]
        public FlatWizardMode FlatWizardMode {
            get => flatWizardMode;
            set {
                flatWizardMode = value;
                RaisePropertyChanged();
            }
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

        [DataMember]
        public double MaxFlatDeviceBrightness {
            get => maxFlatDeviceBrightness;
            set {
                maxFlatDeviceBrightness = value;
                RaisePropertyChanged();
            }
        }

        [DataMember]
        public double MinFlatDeviceBrightness {
            get => minFlatDeviceBrightness;
            set {
                minFlatDeviceBrightness = value;
                if (MaxFlatDeviceBrightness < minFlatDeviceBrightness) {
                    MaxFlatDeviceBrightness = minFlatDeviceBrightness;
                }

                RaisePropertyChanged();
            }
        }

        [DataMember]
        public double FlatDeviceStepSize {
            get => flatDeviceStepSize;
            set {
                flatDeviceStepSize = value;
                RaisePropertyChanged();
            }
        }
    }
}