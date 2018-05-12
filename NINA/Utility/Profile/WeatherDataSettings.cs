using NINA.Utility.Enum;
using NINA.Utility.Mediator;
using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    public class WeatherDataSettings : IWeatherDataSettings {
        private WeatherDataEnum weatherDataType = WeatherDataEnum.OPENWEATHERMAP;

        [DataMember]
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

        [DataMember]
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

        [DataMember]
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