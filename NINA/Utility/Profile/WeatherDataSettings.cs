#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

#endregion "copyright"

using NINA.Utility.Enum;
using System;
using System.Runtime.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    public class WeatherDataSettings : Settings, IWeatherDataSettings {
        private WeatherDataEnum weatherDataType = WeatherDataEnum.OPENWEATHERMAP;

        [DataMember]
        public WeatherDataEnum WeatherDataType {
            get {
                return weatherDataType;
            }
            set {
                weatherDataType = value;
                RaisePropertyChanged();
            }
        }

        private string openWeatherMapAPIKey = string.Empty;

        [DataMember]
        public string OpenWeatherMapAPIKey {
            get {
                return openWeatherMapAPIKey;
            }
            set {
                openWeatherMapAPIKey = value;
                RaisePropertyChanged();
            }
        }

        private string openWeatherMapUrl = "http://api.openweathermap.org/data/2.5/weather";

        [DataMember]
        public string OpenWeatherMapUrl {
            get {
                return openWeatherMapUrl;
            }
            set {
                openWeatherMapUrl = value;
                RaisePropertyChanged();
            }
        }
    }
}