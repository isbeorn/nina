using NINA.Utility.Enum;

namespace NINA.Utility.Profile {

    public interface IWeatherDataSettings : ISettings {
        string OpenWeatherMapAPIKey { get; set; }
        string OpenWeatherMapUrl { get; set; }
        WeatherDataEnum WeatherDataType { get; set; }
    }
}