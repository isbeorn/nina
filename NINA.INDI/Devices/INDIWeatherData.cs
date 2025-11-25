#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.INDI.Enums;
using NINA.INDI.Protocol;
using NINA.INDI.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Utility;

namespace NINA.INDI.Devices {

    public class INDIWeatherData : INDIDevice, IINDIWeatherData {






        public override void OnTextPropertyUpdated(INDITextProperty p) {
            base.OnTextPropertyUpdated(p);
        }

        public override void OnNumberPropertyUpdated(INDINumberProperty p) {
            base.OnNumberPropertyUpdated(p);
        }

        public override void OnSwitchPropertyUpdated(INDISwitchProperty p) {
            base.OnSwitchPropertyUpdated(p);
        }

        public override void OnBlobPropertyUpdated(INDIBlobProperty p) {
            base.OnBlobPropertyUpdated(p);
        }





        public INDIWeatherData(INDIDeviceInfo device) : base(device) {
        }

        /// <summary>
        /// Specify critical properties that must arrive before Connect() completes
        /// </summary>
        protected override string[] GetRequiredConnectionProperties() {
            return ["WEATHER_PARAMETERS"];
        }

        public double AveragePeriod => GetNumberPropertyValue("WEATHER_PARAMETERS", "WEATHER_AVERAGE_PERIOD") ?? double.NaN;
        public double CloudCover => GetNumberPropertyValue("WEATHER_PARAMETERS", "WEATHER_CLOUD_COVER") ?? double.NaN;
        public double DewPoint => GetNumberPropertyValue("WEATHER_PARAMETERS", "WEATHER_DEW_POINT") ?? double.NaN;
        public double Humidity => GetNumberPropertyValue("WEATHER_PARAMETERS", "WEATHER_HUMIDITY") ?? double.NaN;
        public double Pressure => GetNumberPropertyValue("WEATHER_PARAMETERS", "WEATHER_PRESSURE") ?? double.NaN;
        public double RainRate => GetNumberPropertyValue("WEATHER_PARAMETERS", "WEATHER_RAIN_RATE") ?? double.NaN;
        public double SkyBrightness => GetNumberPropertyValue("WEATHER_PARAMETERS", "WEATHER_SKY_BRIGHTNESS") ?? double.NaN;
        public double SkyQuality => GetNumberPropertyValue("WEATHER_PARAMETERS", "WEATHER_SKY_QUALITY") ?? double.NaN;
        public double SkyTemperature => GetNumberPropertyValue("WEATHER_PARAMETERS", "WEATHER_SKY_TEMPERATURE") ?? double.NaN;
        public double StarFWHM => GetNumberPropertyValue("WEATHER_PARAMETERS", "WEATHER_STAR_FWHM") ?? double.NaN;
        public double Temperature => GetNumberPropertyValue("WEATHER_PARAMETERS", "WEATHER_TEMPERATURE") ?? double.NaN;
        public double WindDirection => GetNumberPropertyValue("WEATHER_PARAMETERS", "WEATHER_WIND_DIRECTION") ?? double.NaN;
        public double WindGust => GetNumberPropertyValue("WEATHER_PARAMETERS", "WEATHER_WIND_GUST") ?? double.NaN;
        public double WindSpeed => GetNumberPropertyValue("WEATHER_PARAMETERS", "WEATHER_WIND_SPEED") ?? double.NaN;

        #region Unsupported

        public IList<string> SupportedActions { get; }

        public string Action(string actionName, string actionParameters) {
            throw new NotImplementedException();
        }

        public void CommandBlind(string command, bool raw = false) {
            throw new NotImplementedException();
        }

        public bool CommandBool(string command, bool raw = false) {
            throw new NotImplementedException();
        }

        public string CommandString(string command, bool raw = false) {
            throw new NotImplementedException();
        }

        #endregion
    }
}
