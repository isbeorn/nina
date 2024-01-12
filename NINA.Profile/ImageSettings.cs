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
using NINA.Profile.Interfaces;
using System;
using System.IO;
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
            annotateUnlimitedStars = false;
            starSensitivity = StarSensitivityEnum.High;
            noiseReduction = NoiseReductionEnum.None;
            detectStars = true;
            autoStretch = true;
        }

        private double autoStretchFactor;

        [DataMember]
        public double AutoStretchFactor {
            get => autoStretchFactor;
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
            get => blackClipping;
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
            get => annotateImage;
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
                if (DllLoader.IsX86()) {
                    return false;
                } else {
                    return debayerImage;
                }
            }
            set {
                if (!DllLoader.IsX86()) {
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
        }

        private bool debayeredHFR;

        [DataMember]
        public bool DebayeredHFR {
            get {
                if (DllLoader.IsX86()) {
                    return false;
                } else {
                    return debayeredHFR;
                }
            }
            set {
                if (!DllLoader.IsX86()) {
                    if (debayeredHFR != value) {
                        debayeredHFR = value;
                        if (debayeredHFR) {
                            DebayerImage = debayeredHFR;
                        }
                        RaisePropertyChanged();
                    }
                }
            }
        }

        private bool unlinkedStretch;

        [DataMember]
        public bool UnlinkedStretch {
            get {
                if (DllLoader.IsX86()) {
                    return false;
                } else {
                    return unlinkedStretch;
                }
            }
            set {
                if (!DllLoader.IsX86()) {
                    if (unlinkedStretch != value) {
                        unlinkedStretch = value;
                        if (unlinkedStretch) {
                            DebayerImage = unlinkedStretch;
                        }
                        RaisePropertyChanged();
                    }
                }
            }
        }

        private bool annotateUnlimitedStars;

        [DataMember]
        public bool AnnotateUnlimitedStars {
            get => annotateUnlimitedStars;
            set {
                if (annotateUnlimitedStars != value) {
                    annotateUnlimitedStars = value;
                    RaisePropertyChanged();
                }
            }
        }

        private StarSensitivityEnum starSensitivity;

        [DataMember]
        public StarSensitivityEnum StarSensitivity {
            get => starSensitivity;
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
            get => noiseReduction;
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
            get => sharpCapSensorAnalysisFolder;
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
            get => detectStars;
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
            get => autoStretch;
            set {
                if (autoStretch != value) {
                    autoStretch = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}