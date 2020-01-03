#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
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