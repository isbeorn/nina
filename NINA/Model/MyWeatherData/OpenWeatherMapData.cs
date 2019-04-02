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

using Newtonsoft.Json.Linq;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Http;
using NINA.Utility.Notification;
using NINA.Utility.Profile;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyWeatherData {

    internal class OpenWeatherMapData : BaseINPC, IWeatherData {

        public OpenWeatherMapData(IProfileService profileService) {
            this.profileService = profileService;
        }

        private IProfileService profileService;

        public double Latitude { get; private set; }

        public double Longitude { get; private set; }

        public double Temperature { get; private set; }

        public double Pressure { get; private set; }

        public double Humidity { get; private set; }

        public double WindSpeed { get; private set; }

        public double WindDirection { get; private set; }

        public double CloudCoverage { get; private set; }

        public DateTime Sunrise { get; private set; }

        public DateTime Sunset { get; private set; }

        public double Dewpoint {
            get {
                return Astrometry.ApproximateDewPoint(Temperature, Humidity);
            }
        }

        public async Task<bool> Update() {
            var apikey = profileService.ActiveProfile.WeatherDataSettings.OpenWeatherMapAPIKey;
            var latitude = profileService.ActiveProfile.AstrometrySettings.Latitude;
            var longitude = profileService.ActiveProfile.AstrometrySettings.Longitude;

            if (string.IsNullOrEmpty(apikey)) {
                Notification.ShowError("Unable to get weather data! No API Key set");
                return false;
            }

            var url = profileService.ActiveProfile.WeatherDataSettings.OpenWeatherMapUrl + "?appid={0}&lat={1}&lon={2}";

            var request = new HttpGetRequest(url, apikey, latitude, longitude);
            string result = await request.Request(new CancellationToken());

            JObject o = JObject.Parse(result);
            var openweatherdata = o.ToObject<OpenWeatherDataResponse>();

            this.Latitude = openweatherdata.coord.lat;
            this.Longitude = openweatherdata.coord.lon;
            this.Temperature = openweatherdata.main.temp - 273.15;
            this.Pressure = openweatherdata.main.pressure;
            this.Humidity = openweatherdata.main.humidity;
            this.WindSpeed = openweatherdata.wind.speed;
            this.WindDirection = openweatherdata.wind.deg;
            this.CloudCoverage = openweatherdata.clouds.all;
            this.Sunrise = Utility.Utility.UnixTimeStampToDateTime(openweatherdata.sys.sunrise);
            this.Sunset = Utility.Utility.UnixTimeStampToDateTime(openweatherdata.sys.sunset);

            RaiseAllPropertiesChanged();
            return true;
        }

        public class OpenWeatherDataResponse {
            public OpenWeatherDataResponseCoord coord { get; set; }
            public OpenWeatherDataResponseMain main { get; set; }
            public OpenWeatherDataResponseWind wind { get; set; }
            public OpenWeatherDataResponseClouds clouds { get; set; }
            public OpenWeatherDataResponseSys sys { get; set; }
            public int id { get; set; }
            public string name { get; set; }

            public class OpenWeatherDataResponseMain {
                public double temp { get; set; }
                public double pressure { get; set; }
                public double humidity { get; set; }
                public double temp_min { get; set; }
                public double temp_max { get; set; }
            }

            public class OpenWeatherDataResponseCoord {
                public double lon { get; set; }
                public double lat { get; set; }
            }

            public class OpenWeatherDataResponseSys {
                public double sunrise { get; set; }
                public double sunset { get; set; }
            }

            public class OpenWeatherDataResponseClouds {
                public double all { get; set; }
            }

            public class OpenWeatherDataResponseWind {
                public double speed { get; set; }
                public double deg { get; set; }
            }
        }
    }
}