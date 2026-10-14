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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace NINA.Profile {
    public enum WeatherDataField {
        AveragePeriod,
        CloudCover,
        DewPoint,
        Humidity,
        Pressure,
        RainRate,
        SkyBrightness,
        SkyQuality,
        SkyTemperature,
        StarFWHM,
        Temperature,
        WindDirection,
        WindGust,
        WindSpeed
    }

    [Serializable]
    [DataContract]
    public class WeatherHubSettings : DeviceHubSettings, IWeatherHubSettings {
        private Dictionary<WeatherDataField, string> sources;

        protected override void SetDefaultValues() {
            base.SetDefaultValues();
            sources = new Dictionary<WeatherDataField, string>();
        }

        [DataMember(Name = "Sources")]
        private Dictionary<WeatherDataField, string> PersistedSources {
            get => sources;
            set => sources = value ?? new Dictionary<WeatherDataField, string>();
        }

        public IReadOnlyDictionary<WeatherDataField, string> Sources => new ReadOnlyDictionary<WeatherDataField, string>(sources);

        public void SetSource(WeatherDataField field, string deviceId) {
            if (sources.TryGetValue(field, out var existing) && existing == deviceId) {
                return;
            }

            if (string.IsNullOrEmpty(deviceId)) {
                if (!sources.Remove(field)) {
                    return;
                }
            } else {
                sources[field] = deviceId;
            }

            RaisePropertyChanged(nameof(Sources));
        }
    }
}
