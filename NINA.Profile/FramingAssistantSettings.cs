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
using NINA.Profile.Interfaces;
using System;
using System.Runtime.Serialization;

namespace NINA.Profile {

    [Serializable()]
    [DataContract]
    public class FramingAssistantSettings : Settings, IFramingAssistantSettings {

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        protected override void SetDefaultValues() {
            cameraHeight = 3500;
            cameraWidth = 4500;
            fieldOfView = 3;
            lastSelectedImageSource = SkySurveySource.HIPS2FITS;
            opacity = 0.2;
            lastRotationAngle = 0;
            saveImageInOfflineCache = false;
            annotateConstellationBoundaries = false;
            annotateConstellations = false;
            annotateDSO = true;
            annotateGrid = true;
        }

        private SkySurveySource lastSelectedImageSource;

        [DataMember]
        public SkySurveySource LastSelectedImageSource {
            get => lastSelectedImageSource;
            set {
                if (lastSelectedImageSource != value) {
                    lastSelectedImageSource = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int cameraHeight;

        [DataMember]
        public int CameraHeight {
            get => cameraHeight;
            set {
                if (cameraHeight != value) {
                    cameraHeight = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int cameraWidth;

        [DataMember]
        public int CameraWidth {
            get => cameraWidth;
            set {
                if (cameraWidth != value) {
                    cameraWidth = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double fieldOfView;

        [DataMember]
        public double FieldOfView {
            get => fieldOfView;
            set {
                if (fieldOfView != value) {
                    fieldOfView = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double opacity;

        [DataMember]
        public double Opacity {
            get => opacity;
            set {
                if (opacity != value) {
                    opacity = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double lastRotationAngle;

        [DataMember]
        public double LastRotationAngle {
            get => lastRotationAngle;
            set {
                if (lastRotationAngle != value) {
                    lastRotationAngle = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool saveImageInOfflineCache;

        [DataMember]
        public bool SaveImageInOfflineCache {
            get => saveImageInOfflineCache;
            set {
                if (saveImageInOfflineCache != value) {
                    saveImageInOfflineCache = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool annotateConstellationBoundaries;

        [DataMember]
        public bool AnnotateConstellationBoundaries {
            get => annotateConstellationBoundaries;
            set {
                if (annotateConstellationBoundaries != value) {
                    annotateConstellationBoundaries = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool annotateConstellations;

        [DataMember]
        public bool AnnotateConstellations {
            get => annotateConstellations;
            set {
                if (annotateConstellations != value) {
                    annotateConstellations = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool annotateDSO;

        [DataMember]
        public bool AnnotateDSO {
            get => annotateDSO;
            set {
                if (annotateDSO != value) {
                    annotateDSO = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool annotateGrid;

        [DataMember]
        public bool AnnotateGrid {
            get => annotateGrid;
            set {
                if (annotateGrid != value) {
                    annotateGrid = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}