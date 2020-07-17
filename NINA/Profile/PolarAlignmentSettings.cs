#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
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