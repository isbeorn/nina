#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace NINA.Model.MyWeatherData {

    internal interface IWeatherData : IDevice {

        /// <summary>
        /// Time period, in hours, over which to average sensor readings
        /// </summary>
        double AveragePeriod { get; set; }

        /// <summary>
        /// Percent of sky covered by clouds
        /// </summary>
        double CloudCover { get; }

        /// <summary>
        /// Atmospheric dew point reported in °C
        /// </summary>
        double DewPoint { get; }

        /// <summary>
        /// Atmospheric humidity in percent (%)
        /// </summary>
        double Humidity { get; }

        /// <summary>
        /// Atmospheric presure in hectoPascals (hPa)
        /// </summary>
        double Pressure { get; }

        /// <summary>
        /// Rain rate in mm per hour
        /// </summary>
        double RainRate { get; }

        /// <summary>
        /// Sky brightness in Lux
        /// </summary>
        double SkyBrightness { get; }

        /// <summary>
        /// Sky quality measured in magnitudes per square arc second
        /// </summary>
        double SkyQuality { get; }

        /// <summary>
        /// Sky temperature in °C
        /// </summary>
        double SkyTemperature { get; }

        /// <summary>
        /// Seeing reported as star full width half maximum (arc seconds)
        /// </summary>
        double StarFWHM { get; }

        /// <summary>
        /// Ambient air temperature in °C
        /// </summary>
        double Temperature { get; }

        /// <summary>
        /// Wind direction (degrees, 0..360.0)
        /// </summary>
        double WindDirection { get; }

        /// <summary>
        /// Wind gust (m/s) Peak 3 second wind speed over the prior 2 minutes
        /// </summary>
        double WindGust { get; }

        /// <summary>
        /// Wind Speed in meters per second
        /// </summary>
        double WindSpeed { get; }
    }
}