#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model.MyCamera;
using System;
using System.Runtime.Serialization;

namespace NINA.Profile {

    [Serializable()]
    [DataContract]
    public class FlatWizardSettings : Settings, IFlatWizardSettings {

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        protected override void SetDefaultValues() {
            flatCount = 10;
            histogramTolerance = 0.1;
            histogramMeanTarget = 0.5;
            stepSize = 0.5;
            binningMode = new BinningMode(1, 1);
            darkFlatCount = 0;
        }

        private int flatCount;

        [DataMember]
        public int FlatCount {
            get {
                return flatCount;
            }
            set {
                if (flatCount != value) {
                    flatCount = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double histogramMeanTarget;

        [DataMember]
        public double HistogramMeanTarget {
            get {
                return histogramMeanTarget;
            }
            set {
                if (histogramMeanTarget != value) {
                    histogramMeanTarget = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double histogramTolerance;

        [DataMember]
        public double HistogramTolerance {
            get {
                return histogramTolerance;
            }
            set {
                if (histogramTolerance != value) {
                    histogramTolerance = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double stepSize;

        [DataMember]
        public double StepSize {
            get {
                return stepSize;
            }
            set {
                if (stepSize != value) {
                    stepSize = value;
                    RaisePropertyChanged();
                }
            }
        }

        private BinningMode binningMode;

        [DataMember]
        public BinningMode BinningMode {
            get {
                return binningMode;
            }
            set {
                if (binningMode != value) {
                    binningMode = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int darkFlatCount;

        [DataMember]
        public int DarkFlatCount {
            get => darkFlatCount;
            set {
                if (darkFlatCount != value) {
                    darkFlatCount = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}
