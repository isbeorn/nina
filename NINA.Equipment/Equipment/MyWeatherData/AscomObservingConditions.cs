#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using ASCOM.Common.DeviceInterfaces;
using ASCOM.Com.DriverAccess;
using NINA.Core.Locale;
using NINA.Equipment.Interfaces;
using System;

namespace NINA.Equipment.Equipment.MyWeatherData {

    internal class AscomObservingConditions : AscomDevice<ObservingConditions>, IWeatherData, IDisposable {

        public AscomObservingConditions(string weatherDataId, string weatherDataName) : base(weatherDataId, weatherDataName) {
        }

        public double AveragePeriod => GetProperty(nameof(ObservingConditions.AveragePeriod), double.NaN);

        public double CloudCover => GetProperty(nameof(ObservingConditions.CloudCover), double.NaN);

        public double DewPoint => GetProperty(nameof(ObservingConditions.DewPoint), double.NaN);

        public double Humidity => GetProperty(nameof(ObservingConditions.Humidity), double.NaN);

        public double Pressure => GetProperty(nameof(ObservingConditions.Pressure), double.NaN);

        public double RainRate => GetProperty(nameof(ObservingConditions.RainRate), double.NaN);

        public double SkyBrightness => GetProperty(nameof(ObservingConditions.SkyBrightness), double.NaN);

        public double SkyQuality => GetProperty(nameof(ObservingConditions.SkyQuality), double.NaN);

        public double SkyTemperature => GetProperty(nameof(ObservingConditions.SkyTemperature), double.NaN);

        public double StarFWHM => GetProperty(nameof(ObservingConditions.StarFWHM), double.NaN);

        public double Temperature => GetProperty(nameof(ObservingConditions.Temperature), double.NaN);

        public double WindDirection => GetProperty(nameof(ObservingConditions.WindDirection), double.NaN);

        public double WindGust => GetProperty(nameof(ObservingConditions.WindGust), double.NaN);

        public double WindSpeed => GetProperty(nameof(ObservingConditions.WindSpeed), double.NaN);

        protected override string ConnectionLostMessage => Loc.Instance["LblWeatherConnectionLost"];

        protected override ObservingConditions GetInstance(string id) {
            return new ObservingConditions(id);
        }
    }
}