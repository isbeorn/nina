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

using NINA.Model.MyWeatherData;
using NINA.Utility;
using NINA.Utility.Enum;
using NINA.Profile;
using System;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace NINA.ViewModel {

    internal class WeatherDataVM : DockableVM {

        public WeatherDataVM(IProfileService profileService) : base(profileService) {
            this.Title = "LblWeather";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["CloudSVG"];

            _updateWeatherDataTimer = new DispatcherTimer();
            _updateWeatherDataTimer.Interval = TimeSpan.FromSeconds(60);
            _updateWeatherDataTimer.Tick += _updateWeatherDataTimer_Tick;

            _doUpdate = false;

            this.UpdateWeatherDataCommand = new AsyncCommand<bool>(() => UpdateWeatherData());
        }

        private async void _updateWeatherDataTimer_Tick(object sender, EventArgs e) {
            await UpdateWeatherDataCommand.ExecuteAsync(null);
        }

        private DispatcherTimer _updateWeatherDataTimer;

        private bool _doUpdate;

        public bool DoUpdate {
            get {
                return _doUpdate;
            }
            set {
                _doUpdate = value;
                if (_doUpdate) {
                    _updateWeatherDataTimer_Tick(null, null);
                    _updateWeatherDataTimer.Start();
                } else {
                    _updateWeatherDataTimer.Stop();
                }
                RaisePropertyChanged();
            }
        }

        private async Task<bool> UpdateWeatherData() {
            return await WeatherData.Update();
        }

        private IWeatherData _weatherData;

        public IWeatherData WeatherData {
            get {
                if (_weatherData == null) {
                    if (profileService.ActiveProfile.WeatherDataSettings.WeatherDataType == WeatherDataEnum.OPENWEATHERMAP) {
                        WeatherData = new OpenWeatherMapData(profileService);
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