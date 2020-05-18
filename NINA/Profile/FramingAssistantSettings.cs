#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility.SkySurvey;
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
            lastSelectedImageSource = 0;
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
            get {
                return cameraHeight;
            }
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
            get {
                return cameraWidth;
            }
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
            get {
                return fieldOfView;
            }
            set {
                if (fieldOfView != value) {
                    fieldOfView = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}
