#region "copyright"

/*
    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

/*
 * Copyright 2019 Dale Ghent <daleg@elemental.org>
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