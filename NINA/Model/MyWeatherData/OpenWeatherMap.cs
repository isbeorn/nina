#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json.Linq;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Http;
using NINA.Utility.Notification;
using NINA.Profile;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyWeatherData {

    internal class OpenWeatherMap : BaseINPC, IWeatherData {
        private const string _category = "N.I.N.A.";
        private const string _driverId = "NINA.OpenWeatherMap.Client";
        private const string _driverName = "OpenWeatherMap";
        private const string _driverVersion = "1.0";

        // OWM current weather API base URL
        private const string _owmCurrentWeatherBaseURL = "https://api.openweathermap.org/data/2.5/weather";

        // OWM updates weather data every 10 minutes.
        // They strongly suggest that the API not be queried more frequent than that.
        private const double _owmQueryPeriod = 600;

        private Task updateWorkerTask;
        private CancellationTokenSource OWMUpdateWorkerCts;

        public OpenWeatherMap(IProfileService profileService) {
            this.profileService = profileService;
        }

        private IProfileService profileService;

        public string Category => _category;

        public string Id => _driverId;

        public string Name => _driverName;

        public string DriverInfo => Locale.Loc.Instance["LblOpenWeatherMapClientInfo"];

        public string DriverVersion => _driverVersion;

        public string Description => Locale.Loc.Instance["LblOpenWeatherMapClientDescription"];

        public bool HasSetupDialog => false;

        private double _temperature;

        public double Temperature {
            get => _temperature;
            set {
                _temperature = value;
                RaisePropertyChanged();
            }
        }

        private double _pressure;

        public double Pressure {
            get => _pressure;
            set {
                _pressure = value;
                RaisePropertyChanged();
            }
        }

        private double _humidity;

        public double Humidity {
            get => _humidity;
            set {
                _humidity = value;
                RaisePropertyChanged();
            }
        }

        private double _windDirection;

        public double WindDirection {
            get => _windDirection;
            set {
                _windDirection = value;
                RaisePropertyChanged();
            }
        }

        private double _windSpeed;

        public double WindSpeed {
            get => _windSpeed;
            set {
                _windSpeed = value;
                RaisePropertyChanged();
            }
        }

        private double _cloudCover;

        public double CloudCover {
            get => _cloudCover;
            set {
                _cloudCover = value;
                RaisePropertyChanged();
            }
        }

        public double DewPoint {
            get {
                return Astrometry.ApproximateDewPoint(Temperature, Humidity);
            }
        }

        private double _averagePeriod;
        public double AveragePeriod { get => double.NaN; set => _averagePeriod = value; }

        public double RainRate { get => double.NaN; }

        public double SkyBrightness { get => double.NaN; }

        public double SkyQuality { get => double.NaN; }

        public double SkyTemperature { get => double.NaN; }

        public double StarFWHM { get => double.NaN; }

        public double WindGust { get => double.NaN; }

        private bool _connected;

        public bool Connected {
            get => _connected;
            set {
                _connected = value;
                RaisePropertyChanged();
            }
        }

        private async Task OWMUpdateWorker(CancellationToken ct) {
            try {
                while (true) {
                    if (string.IsNullOrEmpty(GetOWMAPIKey())) {
                        Notification.ShowError("There is no OpenWeatherMap API key configured.");
                        Logger.Warning("OWM: No API key has been set. Sleeping for 30 seconds until next try");

                        // Sleep for 30 seconds before trying again
                        await Task.Delay(TimeSpan.FromSeconds(30), ct);

                        return;
                    }

                    var latitude = profileService.ActiveProfile.AstrometrySettings.Latitude;
                    var longitude = profileService.ActiveProfile.AstrometrySettings.Longitude;

                    var url = _owmCurrentWeatherBaseURL + "?appid={0}&lat={1}&lon={2}";

                    var request = new HttpGetRequest(url, GetOWMAPIKey(), latitude, longitude);
                    string result = await request.Request(new CancellationToken());

                    JObject o = JObject.Parse(result);
                    var openweatherdata = o.ToObject<OpenWeatherDataResponse>();

                    // temperature is provided in Kelvin
                    Temperature = openweatherdata.main.temp - 273.15;

                    // pressure is hectopascals
                    Pressure = openweatherdata.main.pressure;

                    // humidity in percent
                    Humidity = openweatherdata.main.humidity;

                    // wind speed in meters per second
                    WindSpeed = openweatherdata.wind.speed;

                    // wind heading in degrees
                    WindDirection = openweatherdata.wind.deg;

                    // cloudiness in percent
                    CloudCover = openweatherdata.clouds.all;

                    // Sleep thread until the next OWM API query
                    await Task.Delay(TimeSpan.FromSeconds(_owmQueryPeriod), ct);
                }
            } catch (OperationCanceledException) {
                Logger.Debug("OWM: OWMUpdate task cancelled");
            }
        }

        public Task<bool> Connect(CancellationToken ct) {
            if (string.IsNullOrEmpty(GetOWMAPIKey())) {
                Notification.ShowError("There is no OpenWeatherMap API key configured.");
                Logger.Warning("OWM: No API key has been set");

                Connected = false;
                return Task.FromResult(false);
            }

            Logger.Debug("OWM: Starting OWMUpdate task");
            OWMUpdateWorkerCts?.Dispose();
            OWMUpdateWorkerCts = new CancellationTokenSource();
            updateWorkerTask = OWMUpdateWorker(OWMUpdateWorkerCts.Token);

            Connected = true;
            return Task.FromResult(true);
        }

        public void Disconnect() {
            Logger.Debug("OWM: Stopping OWMUpdate task");

            if (Connected == false)
                return;

            OWMUpdateWorkerCts.Cancel();
            OWMUpdateWorkerCts.Dispose();

            Connected = false;
        }

        public void SetupDialog() {
        }

        private string GetOWMAPIKey() {
            return profileService.ActiveProfile.WeatherDataSettings.OpenWeatherMapAPIKey;
        }

        public class OpenWeatherDataResponse {
            public OpenWeatherDataResponseCoord coord { get; set; }
            public OpenWeatherDataResponseMain main { get; set; }
            public OpenWeatherDataResponseWind wind { get; set; }
            public OpenWeatherDataResponseClouds clouds { get; set; }
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