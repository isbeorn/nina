#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Core.Enum;
using NINA.Core.Utility;
using System;
using System.Runtime.Serialization;

namespace NINA.Core.Model.Equipment {

    [JsonObject(MemberSerialization.OptIn)]
    [Serializable]
    [DataContract]
    public class FlatWizardFilterSettings : SerializableINPC {
        private FlatWizardMode flatWizardMode;
        private double histogramMeanTarget;

        private double histogramTolerance;

        private double maxFlatExposureTime;

        private double minFlatExposureTime;

        private int maxAbsoluteFlatDeviceBrightness;

        private int minAbsoluteFlatDeviceBrightness;
        private int gain;
        private int offset;
        private BinningMode binning;

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        public FlatWizardFilterSettings() {
            SetDefaultValues();
        }

        private void SetDefaultValues() {
            flatWizardMode = FlatWizardMode.DYNAMICEXPOSURE;
            HistogramMeanTarget = 0.5;
            HistogramTolerance = 0.1;
            MinFlatExposureTime = 0.01;
            MaxFlatExposureTime = 30;
            MinAbsoluteFlatDeviceBrightness = 0;
            MaxAbsoluteFlatDeviceBrightness = 32767;
            Binning = new BinningMode(1, 1);
            Gain = -1;
            Offset = -1;
        }

        [DataMember]
        [JsonProperty]
        public FlatWizardMode FlatWizardMode {
            get => flatWizardMode;
            set {
                flatWizardMode = value;
                RaisePropertyChanged();
            }
        }

        [DataMember]
        [JsonProperty]
        public double HistogramMeanTarget {
            get => histogramMeanTarget;
            set {
                histogramMeanTarget = value;
                RaisePropertyChanged();
            }
        }

        [DataMember]
        [JsonProperty]
        public double HistogramTolerance {
            get => histogramTolerance;
            set {
                histogramTolerance = value;
                RaisePropertyChanged();
            }
        }

        [DataMember]
        [JsonProperty]
        public double MaxFlatExposureTime {
            get => maxFlatExposureTime;
            set {
                maxFlatExposureTime = value;
                RaisePropertyChanged();
            }
        }

        [DataMember]
        [JsonProperty]
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
        [JsonProperty]
        public int MaxAbsoluteFlatDeviceBrightness {
            get => maxAbsoluteFlatDeviceBrightness;
            set {
                maxAbsoluteFlatDeviceBrightness = value;
                RaisePropertyChanged();
            }
        }

        [DataMember]
        [JsonProperty]
        public int MinAbsoluteFlatDeviceBrightness {
            get => minAbsoluteFlatDeviceBrightness;
            set {
                minAbsoluteFlatDeviceBrightness = value;
                if (MaxAbsoluteFlatDeviceBrightness < minAbsoluteFlatDeviceBrightness) {
                    MaxAbsoluteFlatDeviceBrightness = minAbsoluteFlatDeviceBrightness;
                }

                RaisePropertyChanged();
            }
        }

        [DataMember]
        [JsonProperty]
        public int Gain {
            get => gain;
            set {
                if(value == gain) { return; }
                gain = value;
                RaisePropertyChanged();
            }
        }

        [DataMember]
        [JsonProperty]
        public int Offset {
            get => offset;
            set {
                if (value == offset) { return; }
                offset = value;
                RaisePropertyChanged();
            }
        }

        [DataMember]
        [JsonProperty]
        public BinningMode Binning {
            get => binning;
            set {
                if (value == null) { value = new BinningMode(1, 1); }
                if (value == binning) { return; }
                binning = value;
                RaisePropertyChanged();
            }
        }
    }
}