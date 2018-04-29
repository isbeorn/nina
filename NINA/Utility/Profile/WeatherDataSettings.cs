using NINA.Utility.Enum;
using NINA.Utility.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NINA.Utility.Profile {
    [Serializable()]
    [XmlRoot(nameof(WeatherDataSettings))]
    public class WeatherDataSettings {

        private WeatherDataEnum weatherDataType = WeatherDataEnum.OPENWEATHERMAP;
        [XmlElement(nameof(WeatherDataType))]
        public WeatherDataEnum WeatherDataType {
            get {
                return weatherDataType;
            }
            set {
                weatherDataType = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        private string openWeatherMapAPIKey = string.Empty;
        [XmlElement(nameof(OpenWeatherMapAPIKey))]
        public string OpenWeatherMapAPIKey {
            get {
                return openWeatherMapAPIKey;
            }
            set {
                openWeatherMapAPIKey = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        private string openWeatherMapUrl = "http://api.openweathermap.org/data/2.5/weather";
        [XmlElement(nameof(OpenWeatherMapUrl))]
        public string OpenWeatherMapUrl {
            get {
                return openWeatherMapUrl;
            }
            set {
                openWeatherMapUrl = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }
    }
}
