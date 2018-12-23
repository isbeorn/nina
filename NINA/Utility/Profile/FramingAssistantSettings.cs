#region "copyright"

/*
    Copyright © 2016 - 2018 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    public class FramingAssistantSettings : Settings, IFramingAssistantSettings {

        [OnDeserializing]
        public void OnDeserialization(StreamingContext context) {
            SetDefaultValues();
        }

        private void SetDefaultValues() {
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
                lastSelectedImageSource = value;
                RaisePropertyChanged();
            }
        }

        private int cameraHeight = 3500;

        [DataMember]
        public int CameraHeight {
            get {
                return cameraHeight;
            }
            set {
                cameraHeight = value;
                RaisePropertyChanged();
            }
        }

        private int cameraWidth = 4500;

        [DataMember]
        public int CameraWidth {
            get {
                return cameraWidth;
            }
            set {
                cameraWidth = value;
                RaisePropertyChanged();
            }
        }

        private double fieldOfView = 3;

        [DataMember]
        public double FieldOfView {
            get {
                return fieldOfView;
            }
            set {
                fieldOfView = value;
                RaisePropertyChanged();
            }
        }
    }
}