using System;
using System.Threading.Tasks;

namespace NINA.Model.MyWeatherData {

    internal interface IWeatherData {
        double Latitude { get; }
        double Longitude { get; }
        double Temperature { get; }
        double Pressure { get; }
        double Humidity { get; }
        double WindSpeed { get; }
        double WindDirection { get; }
        double CloudCoverage { get; }

        DateTime Sunrise { get; }
        DateTime Sunset { get; }

        double Dewpoint { get; }

        Task<bool> Update();
    }
}