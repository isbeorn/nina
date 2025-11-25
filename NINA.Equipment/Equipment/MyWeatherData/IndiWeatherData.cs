#region "copyright"

/*
    Copyright © 2016 - 2025 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Locale;
using NINA.Equipment.Interfaces;
using NINA.INDI;
using NINA.INDI.Devices;
using NINA.INDI.Interfaces;
using System;

namespace NINA.Equipment.Equipment.MyWeatherData {

    internal class IndiWeatherData : IndiDevice<IINDIWeatherData>, IWeatherData, IDisposable {

        public IndiWeatherData(INDIDeviceInfo info) : base(info) {
        }

        public double AveragePeriod => GetProperty(nameof(IINDIWeatherData.AveragePeriod), double.NaN);

        public double CloudCover => GetProperty(nameof(IINDIWeatherData.CloudCover), double.NaN);

        public double DewPoint => GetProperty(nameof(IINDIWeatherData.DewPoint), double.NaN);

        public double Humidity => GetProperty(nameof(IINDIWeatherData.Humidity), double.NaN);

        public double Pressure => GetProperty(nameof(IINDIWeatherData.Pressure), double.NaN);

        public double RainRate => GetProperty(nameof(IINDIWeatherData.RainRate), double.NaN);

        public double SkyBrightness => GetProperty(nameof(IINDIWeatherData.SkyBrightness), double.NaN);

        public double SkyQuality => GetProperty(nameof(IINDIWeatherData.SkyQuality), double.NaN);

        public double SkyTemperature => GetProperty(nameof(IINDIWeatherData.SkyTemperature), double.NaN);

        public double StarFWHM => GetProperty(nameof(IINDIWeatherData.StarFWHM), double.NaN);

        public double Temperature => GetProperty(nameof(IINDIWeatherData.Temperature), double.NaN);

        public double WindDirection => GetProperty(nameof(IINDIWeatherData.WindDirection), double.NaN);

        public double WindGust => GetProperty(nameof(IINDIWeatherData.WindGust), double.NaN);

        public double WindSpeed => GetProperty(nameof(IINDIWeatherData.WindSpeed), double.NaN);

        protected override string ConnectionLostMessage => Loc.Instance["LblWeatherConnectionLost"];

        protected override IINDIWeatherData GetInstance() {
            return device ??= new INDIWeatherData(_device);
        }
    }
}
