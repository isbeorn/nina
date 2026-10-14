#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Equipment.Hubs;
using NINA.Equipment.Interfaces;
using NINA.Profile;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NINA.Equipment.Equipment.MyWeatherData {
    public class WeatherDataHub : EquipmentHub, IWeatherData {
        public const string DeviceId = "NINA.Weather.Hub";
        private Dictionary<WeatherDataField, string> sources = new Dictionary<WeatherDataField, string>();
        private readonly Dictionary<WeatherDataField, string> fieldErrors = new Dictionary<WeatherDataField, string>();
        private bool sessionConnected;

        public WeatherDataHub(string name) : base(DeviceId, name, "N.I.N.A.") {
        }

        public void Configure(IReadOnlyList<HubDevice> devices, IReadOnlyDictionary<WeatherDataField, string> sources) {
            ConfigureDevices(devices);
            this.sources = sources?.ToDictionary(x => x.Key, x => x.Value) ?? new Dictionary<WeatherDataField, string>();
            fieldErrors.Clear();
        }

        protected override bool IsSupportedDevice(IDevice device) => device is IWeatherData;

        protected override bool AllowPartialConnection => true;

        protected override IEnumerable<HubDevice> DevicesToConnect => Devices.Where(x => x.Device != null
            && sources.Any(source => Enum.IsDefined(source.Key) && source.Value == x.Device.Id));

        protected override void SessionConnected() {
            sessionConnected = true;
        }

        protected override void SessionEnded() {
            sessionConnected = false;
            fieldErrors.Clear();
        }

        public override bool Connected => IsSessionActive && sessionConnected && SessionDevices.Any(x => ReadConnected(x.Device));

        private double Read(WeatherDataField field, Func<IWeatherData, double> read) {
            if (!IsSessionActive || !sessionConnected || !sources.TryGetValue(field, out var id)) {
                return double.NaN;
            }

            var child = SessionDevices.FirstOrDefault(x => x.Device.Id == id);
            if (child == null || !RefreshConnectionStatus(child)) {
                return double.NaN;
            }

            try {
                var result = read((IWeatherData)child.Device);
                fieldErrors.Remove(field);
                Report(child, string.Join(Environment.NewLine, fieldErrors.Where(x => sources[x.Key] == id).Select(x => x.Value)));
                return result;
            } catch (Exception ex) {
                fieldErrors[field] = ex.Message;
                Report(child, string.Join(Environment.NewLine, fieldErrors.Where(x => sources[x.Key] == id).Select(x => x.Value)));
                return double.NaN;
            }
        }

        public double AveragePeriod => Read(WeatherDataField.AveragePeriod, x => x.AveragePeriod);

        public double CloudCover => Read(WeatherDataField.CloudCover, x => x.CloudCover);

        public double DewPoint => Read(WeatherDataField.DewPoint, x => x.DewPoint);

        public double Humidity => Read(WeatherDataField.Humidity, x => x.Humidity);

        public double Pressure => Read(WeatherDataField.Pressure, x => x.Pressure);

        public double RainRate => Read(WeatherDataField.RainRate, x => x.RainRate);

        public double SkyBrightness => Read(WeatherDataField.SkyBrightness, x => x.SkyBrightness);

        public double SkyQuality => Read(WeatherDataField.SkyQuality, x => x.SkyQuality);

        public double SkyTemperature => Read(WeatherDataField.SkyTemperature, x => x.SkyTemperature);

        public double StarFWHM => Read(WeatherDataField.StarFWHM, x => x.StarFWHM);

        public double Temperature => Read(WeatherDataField.Temperature, x => x.Temperature);

        public double WindDirection => Read(WeatherDataField.WindDirection, x => x.WindDirection);

        public double WindGust => Read(WeatherDataField.WindGust, x => x.WindGust);

        public double WindSpeed => Read(WeatherDataField.WindSpeed, x => x.WindSpeed);
    }
}
