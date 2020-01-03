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

using System;
using System.Runtime.Serialization;

namespace NINA.Profile {

    [Serializable()]
    [DataContract]
    public class PolarAlignmentSettings : Settings, IPolarAlignmentSettings {

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        protected override void SetDefaultValues() {
            altitudeDeclination = 0;
            altitudeMeridianOffset = 90;
            azimuthDeclination = 0;
            azimuthMeridianOffset = 0;
        }

        private double altitudeDeclination;

        [DataMember]
        public double AltitudeDeclination {
            get {
                return altitudeDeclination;
            }
            set {
                if (altitudeDeclination != value) {
                    altitudeDeclination = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double altitudeMeridianOffset;

        [DataMember]
        public double AltitudeMeridianOffset {
            get {
                return altitudeMeridianOffset;
            }
            set {
                if (altitudeMeridianOffset != value) {
                    altitudeMeridianOffset = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double azimuthDeclination;

        [DataMember]
        public double AzimuthDeclination {
            get {
                return azimuthDeclination;
            }
            set {
                if (azimuthDeclination != value) {
                    azimuthDeclination = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double azimuthMeridianOffset;

        [DataMember]
        public double AzimuthMeridianOffset {
            get {
                return azimuthMeridianOffset;
            }
            set {
                if (azimuthMeridianOffset != value) {
                    azimuthMeridianOffset = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}