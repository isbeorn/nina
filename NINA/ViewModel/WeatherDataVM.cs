using NINA.Model.MyWeatherData;
using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.ViewModel {
    class WeatherDataVM : DockableVM {
        public WeatherDataVM() {
            this.Title = "LblWeather";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["CloudSVG"];

            this.ContentId = nameof(WeatherDataVM);

            this.UpdateWeatherDataCommand = new AsyncCommand<bool>(() => UpdateWeatherData());
        }

        private async Task<bool> UpdateWeatherData() {
            return await WeatherData.Update();
        }

        IWeatherData _weatherData;
        public IWeatherData WeatherData {
            get {
                if (_weatherData == null) {
                    if (Settings.WeatherDataType == WeatherDataEnum.OPENWEATHERMAP) {
                        WeatherData = new OpenWeatherMapData();
                    }
                }

                return _weatherData;
            }
            set {
                _weatherData = value;
                RaisePropertyChanged();
            }
        }

        public IAsyncCommand UpdateWeatherDataCommand { get; private set; }
    }
}
