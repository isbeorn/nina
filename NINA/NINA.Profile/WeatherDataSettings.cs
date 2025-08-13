#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

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

    [Serializable()]
    [DataContract]
    public class WeatherDataSettings : Settings, IWeatherDataSettings {

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        protected override void SetDefaultValues() {
            Id = "No_Device";
            LastDeviceName = string.Empty;
            OpenWeatherMapAPIKey = string.Empty;
            TheWeatherCompanyAPIKey = string.Empty;
            WeatherUndergroundAPIKey = string.Empty;
            WeatherUndergroundStation = string.Empty;
        }

        private string id = string.Empty;

        [DataMember]
        public string Id {
            get => id;
            set {
                id = value;
                RaisePropertyChanged();
            }
        }

        private string lastDeviceName;

        [DataMember]
        public string LastDeviceName {
            get => lastDeviceName;
            set {
                if (lastDeviceName != value) {
                    lastDeviceName = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string openWeatherMapAPIKey = string.Empty;

        [DataMember]
        public string OpenWeatherMapAPIKey {
            get => openWeatherMapAPIKey;
            set {
                openWeatherMapAPIKey = value;
                RaisePropertyChanged();
            }
        }

        private string theWeatherCompanyAPIKey = string.Empty;

        [DataMember]
        public string TheWeatherCompanyAPIKey {
            get => theWeatherCompanyAPIKey;
            set {
                theWeatherCompanyAPIKey = value;
                RaisePropertyChanged();
            }
        }

        private string weatherUndergroundAPIKey = string.Empty;

        [DataMember]
        public string WeatherUndergroundAPIKey {
            get => weatherUndergroundAPIKey;
            set {
                weatherUndergroundAPIKey = value;
                RaisePropertyChanged();
            }
        }

        private string weatherUndergroundStation = string.Empty;

        [DataMember]
        public string WeatherUndergroundStation {
            get => weatherUndergroundStation;
            set {
                weatherUndergroundStation = value;
                RaisePropertyChanged();
            }
        }
    }
}