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

using NINA.Utility.Astrometry;
using System;
using System.Runtime.Serialization;

namespace NINA.Profile {

    [Serializable()]
    [DataContract]
    public class AstrometrySettings : Settings, IAstrometrySettings {

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        protected override void SetDefaultValues() {
            epochType = Epoch.JNOW;
            hemisphereType = Hemisphere.NORTHERN;
            latitude = 0;
            longitude = 0;
        }

        private Epoch epochType;

        [DataMember]
        public Epoch EpochType {
            get {
                return epochType;
            }
            set {
                if (epochType != value) {
                    epochType = value;
                    RaisePropertyChanged();
                }
            }
        }

        private Hemisphere hemisphereType;

        [DataMember]
        public Hemisphere HemisphereType {
            get {
                return hemisphereType;
            }
            set {
                if (hemisphereType != value) {
                    hemisphereType = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double latitude;

        [DataMember]
        public double Latitude {
            get {
                return latitude;
            }
            set {
                if (latitude != value) {
                    latitude = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double longitude;

        [DataMember]
        public double Longitude {
            get {
                return longitude;
            }
            set {
                if (longitude != value) {
                    longitude = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}