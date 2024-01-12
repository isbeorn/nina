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
    public class GnssSettings : Settings, IGnssSettings {

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        protected override void SetDefaultValues() {
            gnssSource = GnssSourceEnum.NmeaSerial;
        }

        private GnssSourceEnum gnssSource;

        [DataMember]
        public GnssSourceEnum GnssSource {
            get => gnssSource;
            set {
                if (gnssSource != value) {
                    gnssSource = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}