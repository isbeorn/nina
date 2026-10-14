#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Profile.Interfaces;
using System;
using System.Runtime.Serialization;

namespace NINA.Profile {
    [Serializable]
    [DataContract]
    public class HubDeviceSettings : Settings, IHubDeviceSettings {
        private string id;
        private string lastDeviceName;
        private int sourceNumber;

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        protected override void SetDefaultValues() {
            id = string.Empty;
            lastDeviceName = string.Empty;
            sourceNumber = 0;
        }

        [DataMember]
        public string Id {
            get => id;
            set {
                if (id == value) {
                    return;
                }

                id = value;
                RaisePropertyChanged();
            }
        }

        [DataMember]
        public string LastDeviceName {
            get => lastDeviceName;
            set {
                if (lastDeviceName == value) {
                    return;
                }

                lastDeviceName = value;
                RaisePropertyChanged();
            }
        }

        [DataMember]
        public int SourceNumber {
            get => sourceNumber;
            set {
                if (sourceNumber == value) {
                    return;
                }

                sourceNumber = value;
                RaisePropertyChanged();
            }
        }
    }
}
