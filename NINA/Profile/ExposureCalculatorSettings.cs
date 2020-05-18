#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Profile {

    public class ExposureCalculatorSettings : Settings, IExposureCalculatorSettings {

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        protected override void SetDefaultValues() {
            gain = -1;
            exposureDuration = 30;
            fullWellCapacity = 0;
            readNoise = 0;
            biasMean = 0;
        }

        private int gain;

        [DataMember]
        public int Gain {
            get {
                return gain;
            }
            set {
                if (gain != value) {
                    gain = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double exposureDuration;

        [DataMember]
        public double ExposureDuration {
            get {
                return exposureDuration;
            }
            set {
                if (exposureDuration != value) {
                    exposureDuration = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double fullWellCapacity;

        [DataMember]
        public double FullWellCapacity {
            get {
                return fullWellCapacity;
            }
            set {
                if (fullWellCapacity != value) {
                    fullWellCapacity = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double readNoise;

        [DataMember]
        public double ReadNoise {
            get {
                return readNoise;
            }
            set {
                if (readNoise != value) {
                    readNoise = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double biasMean;

        [DataMember]
        public double BiasMedian {
            get {
                return biasMean;
            }
            set {
                if (biasMean != value) {
                    biasMean = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}
