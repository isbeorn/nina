#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility.Enum;
using System;
using System.Runtime.Serialization;

namespace NINA.Profile {

    [Serializable()]
    [DataContract]
    public class ImageSettings : Settings, IImageSettings {

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        protected override void SetDefaultValues() {
            autoStretchFactor = 0.2;
            blackClipping = -2.8;
            annotateImage = false;
            debayerImage = true;
            debayeredHFR = true;
            unlinkedStretch = true;
            starSensitivity = StarSensitivityEnum.Normal;
            noiseReduction = NoiseReductionEnum.None;
            detectStars = false;
            autoStretch = true;
        }

        private double autoStretchFactor;

        [DataMember]
        public double AutoStretchFactor {
            get {
                return autoStretchFactor;
            }
            set {
                if (autoStretchFactor != value) {
                    autoStretchFactor = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double blackClipping;

        [DataMember]
        public double BlackClipping {
            get {
                return blackClipping;
            }
            set {
                if (blackClipping != value) {
                    blackClipping = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool annotateImage;

        [DataMember]
        public bool AnnotateImage {
            get {
                return annotateImage;
            }
            set {
                if (annotateImage != value) {
                    annotateImage = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool debayerImage;

        [DataMember]
        public bool DebayerImage {
            get {
                return debayerImage;
            }
            set {
                if (debayerImage != value) {
                    debayerImage = value;
                    if (!debayerImage) {
                        UnlinkedStretch = false;
                        DebayeredHFR = false;
                    }
                    RaisePropertyChanged();
                }
            }
        }

        private bool debayeredHFR;

        [DataMember]
        public bool DebayeredHFR {
            get {
                return debayeredHFR;
            }
            set {
                if (debayeredHFR != value) {
                    debayeredHFR = value;
                    if (debayeredHFR) {
                        DebayerImage = debayeredHFR;
                    }
                    RaisePropertyChanged();
                }
            }
        }

        private bool unlinkedStretch;

        [DataMember]
        public bool UnlinkedStretch {
            get {
                return unlinkedStretch;
            }
            set {
                if (unlinkedStretch != value) {
                    unlinkedStretch = value;
                    if (unlinkedStretch) {
                        DebayerImage = unlinkedStretch;
                    }
                    RaisePropertyChanged();
                }
            }
        }

        private StarSensitivityEnum starSensitivity;

        [DataMember]
        public StarSensitivityEnum StarSensitivity {
            get {
                return starSensitivity;
            }
            set {
                if (starSensitivity != value) {
                    starSensitivity = value;
                    RaisePropertyChanged();
                }
            }
        }

        private NoiseReductionEnum noiseReduction;

        [DataMember]
        public NoiseReductionEnum NoiseReduction {
            get {
                return noiseReduction;
            }
            set {
                if (noiseReduction != value) {
                    noiseReduction = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string sharpCapSensorAnalysisFolder;

        [DataMember]
        public string SharpCapSensorAnalysisFolder {
            get {
                return sharpCapSensorAnalysisFolder;
            }
            set {
                if (sharpCapSensorAnalysisFolder != value) {
                    sharpCapSensorAnalysisFolder = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool detectStars;

        [DataMember]
        public bool DetectStars {
            get {
                return detectStars;
            }
            set {
                if (detectStars != value) {
                    detectStars = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool autoStretch;

        [DataMember]
        public bool AutoStretch {
            get {
                return autoStretch;
            }
            set {
                if (autoStretch != value) {
                    autoStretch = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}