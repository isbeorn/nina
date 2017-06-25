using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Model.MyWeatherData {
    interface IWeatherData {

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
