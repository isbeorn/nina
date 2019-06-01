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

    internal class WeatherDataInfo : DeviceInfo {
        private double averagePeriod = double.NaN;

        /// <summary>
        /// Time period, in hours, over which to average sensor readings
        /// </summary>
        public double AveragePeriod {
            get => averagePeriod;
            set { averagePeriod = value; RaisePropertyChanged(); }
        }

        private double cloudCover = double.NaN;

        /// <summary>
        /// Percent of sky covered by clouds
        /// </summary>
        public double CloudCover {
            get => cloudCover;
            set { cloudCover = value; RaisePropertyChanged(); }
        }

        private double dewPoint = double.NaN;

        /// <summary>
        /// Atmospheric dew point reported in °C
        /// </summary>
        public double DewPoint {
            get => dewPoint;
            set { dewPoint = value; RaisePropertyChanged(); }
        }

        private double humidity = double.NaN;

        /// <summary>
        /// Atmospheric humidity in percent (%)
        /// </summary>
        public double Humidity {
            get => humidity;
            set { humidity = value; RaisePropertyChanged(); }
        }

        private double pressure = double.NaN;

        /// <summary>
        /// Atmospheric presure in hectoPascals (hPa)
        /// </summary>
        public double Pressure {
            get => pressure;
            set { pressure = value; RaisePropertyChanged(); }
        }

        private double rainRate = double.NaN;

        /// <summary>
        /// Rain rate in mm per hour
        /// </summary>
        public double RainRate {
            get => rainRate;
            set { rainRate = value; RaisePropertyChanged(); }
        }

        private double skyBrightness = double.NaN;

        /// <summary>
        /// Sky brightness in Lux
        /// </summary>
        public double SkyBrightness {
            get => skyBrightness;
            set { skyBrightness = value; RaisePropertyChanged(); }
        }

        private double skyQuality = double.NaN;

        /// <summary>
        /// Sky quality measured in magnitudes per square arc second
        /// </summary>
        public double SkyQuality {
            get => skyQuality;
            set { skyQuality = value; RaisePropertyChanged(); }
        }

        private double skyTemperature = double.NaN;

        /// <summary>
        /// Sky temperature in °C
        /// </summary>
        public double SkyTemperature {
            get => skyTemperature;
            set { skyTemperature = value; RaisePropertyChanged(); }
        }

        private double starFWHM = double.NaN;

        /// <summary>
        /// Seeing reported as star full width half maximum (arc seconds)
        /// </summary>
        public double StarFWHM {
            get => starFWHM;
            set { starFWHM = value; RaisePropertyChanged(); }
        }

        private double temperature = double.NaN;

        /// <summary>
        /// Ambient air temperature in °C
        /// </summary>
        public double Temperature {
            get => temperature;
            set { temperature = value; RaisePropertyChanged(); }
        }

        private double windDirection = double.NaN;

        /// <summary>
        /// Wind direction (degrees, 0..360.0)
        /// </summary>
        public double WindDirection {
            get => windDirection;
            set { windDirection = value; RaisePropertyChanged(); }
        }

        private double windGust = double.NaN;

        /// <summary>
        /// Wind gust (m/s) Peak 3 second wind speed over the prior 2 minutes
        /// </summary>
        public double WindGust {
            get => windGust;
            set { windGust = value; RaisePropertyChanged(); }
        }

        private double windSpeed = double.NaN;

        /// <summary>
        /// Wind Speed in meters per second
        /// </summary>
        public double WindSpeed {
            get => windSpeed;
            set { windSpeed = value; RaisePropertyChanged(); }
        }
    }
}