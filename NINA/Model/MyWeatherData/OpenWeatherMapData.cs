using Newtonsoft.Json.Linq;
using NINA.Utility;
using NINA.Utility.Notification;
using NINA.Utility.Profile;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyWeatherData {

    internal class OpenWeatherMapData : BaseINPC, IWeatherData {
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
                return Math.Pow((Humidity / 100), 1.0d / 8.0d) * (112 + 0.9 * Temperature) + 0.1 * Temperature - 112;
            }
        }

        public async Task<bool> Update() {
            var apikey = ProfileManager.Instance.ActiveProfile.WeatherDataSettings.OpenWeatherMapAPIKey;
            var latitude = ProfileManager.Instance.ActiveProfile.AstrometrySettings.Latitude;
            var longitude = ProfileManager.Instance.ActiveProfile.AstrometrySettings.Longitude;

            if (string.IsNullOrEmpty(apikey)) {
                Notification.ShowError("Unable to get weather data! No API Key set");
                return false;
            }

            var url = ProfileManager.Instance.ActiveProfile.WeatherDataSettings.OpenWeatherMapUrl + "?appid={0}&lat={1}&lon={2}";
            string result = await Utility.Utility.HttpGetRequest(new CancellationToken(), url, apikey, latitude, longitude);

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