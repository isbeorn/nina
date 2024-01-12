#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json.Linq;
using NINA.Core.Utility;
using NINA.Astrometry;
using NINA.Core.Utility.Http;
using NINA.Core.Utility.Notification;
using NINA.Profile.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Locale;
using NINA.Equipment.Interfaces;
using System.Collections.Generic;

namespace NINA.Equipment.Equipment.MyWeatherData {

    public class TheWeatherCompany : BaseINPC, IWeatherData {
        private const string _category = "N.I.N.A.";
        private const string _driverId = "NINA.WeatherCompany.Client";
        private const string _driverName = "TheWeatherCompany";
        private const string _driverVersion = "1.0";

        // TWC current weather API base URL
        private const string _twcCurrentWeatherBaseURL = "https://api.weather.com/v1/";

        // TWC updates weather data every 10 minutes.
        // They strongly suggest that the API not be queried more frequent than that.
        private const double _twcQueryPeriod = 600;

        private Task updateWorkerTask;
        private CancellationTokenSource TWCUpdateWorkerCts;

        public TheWeatherCompany(IProfileService profileService) {
            this.profileService = profileService;
        }

        private IProfileService profileService;

        public string Category => _category;

        public string Id => _driverId;

        public string Name => _driverName;

        public string DriverInfo => Loc.Instance["LblTheWeatherCompanyClientInfo"];

        public string DriverVersion => _driverVersion;

        public string Description => Loc.Instance["LblTheWeatherCompanyClientDescription"];

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

        public double DewPoint => AstroUtil.ApproximateDewPoint(Temperature, Humidity);

        private double _averagePeriod = double.NaN;
        public double AveragePeriod { get => _averagePeriod; set => _averagePeriod = value; }

        public double RainRate => double.NaN;

        public double SkyBrightness => double.NaN;

        public double SkyQuality => double.NaN;

        public double SkyTemperature => double.NaN;

        public double StarFWHM => double.NaN;

        public double WindGust => double.NaN;

        public string TWCAPIKey;

        private bool _connected;

        public bool Connected {
            get => _connected;
            set {
                _connected = value;
                RaisePropertyChanged();
            }
        }

        private async Task TWCUpdateWorker(CancellationToken ct) {
            try {
                while (true) {
                    var latitude = profileService.ActiveProfile.AstrometrySettings.Latitude;
                    var longitude = profileService.ActiveProfile.AstrometrySettings.Longitude;

                    // var url = _twcCurrentWeatherBaseURL + "?appid={0}&lat={1}&lon={2}";
                    var url = _twcCurrentWeatherBaseURL + "/geocode/{1}/{2}/observations.json?language=en-US&units=m&apiKey={0}";

                    var request = new HttpGetRequest(url, TWCAPIKey, latitude, longitude);
                    string result = await request.Request(new CancellationToken());

                    JObject o = JObject.Parse(result);
                    var WeatherCompanydata = o.ToObject<WeatherCompanyDataResponse>();

                    Temperature = WeatherCompanydata.observation.temp;

                    // pressure is hectopascals
                    Pressure = WeatherCompanydata.observation.pressure;

                    // humidity in percent
                    Humidity = WeatherCompanydata.observation.rh;

                    // wind speed in meters per second
                    WindSpeed = WeatherCompanydata.observation.wspd;

                    // wind heading in degrees
                    WindDirection = WeatherCompanydata.observation.wdir;

                    // convert METAR codes to a percentage
                    switch (WeatherCompanydata.observation.clds) {
                        case "SKC": // Sky Clear
                            CloudCover = 0;
                            break;

                        case "CLR": // Clear below 12000AGL
                            CloudCover = 20;
                            break;

                        case "SCT": // Scattered Clouds
                            CloudCover = 40;
                            break;

                        case "FEW": // Few Clouds
                            CloudCover = 60;
                            break;

                        case "BKN": // Broken Clouds
                            CloudCover = 80;
                            break;

                        case "OVC": // Overcast
                            CloudCover = 100;
                            break;

                        default:
                            // Should never reach here
                            CloudCover = 100;
                            break;
                    }

                    // Sleep thread until the next TWC API query
                    await Task.Delay(TimeSpan.FromSeconds(_twcQueryPeriod), ct);
                }
            } catch (OperationCanceledException) {
                Logger.Debug("TWC: TWCUpdate task cancelled");
            }
        }

        public Task<bool> Connect(CancellationToken ct) {
            if (string.IsNullOrEmpty(GetTWCAPIKey())) {
                Notification.ShowError("There is no TheWeatherCompany API key configured.");
                Logger.Warning("TWC: No API key has been set");

                Connected = false;
                return Task.FromResult(false);
            }

            TWCAPIKey = GetTWCAPIKey();
            Logger.Debug("TWC: Starting TWCUpdate task");
            TWCUpdateWorkerCts?.Dispose();
            TWCUpdateWorkerCts = new CancellationTokenSource();
            updateWorkerTask = TWCUpdateWorker(TWCUpdateWorkerCts.Token);

            Connected = true;
            return Task.FromResult(true);
        }

        public void Disconnect() {
            Logger.Debug("TWC: Stopping TWCUpdate task");

            if (Connected == false)
                return;

            try {
                TWCUpdateWorkerCts?.Cancel();
                TWCUpdateWorkerCts?.Dispose();
            } catch { }

            Connected = false;
        }

        public void SetupDialog() {
        }

        public IList<string> SupportedActions => new List<string>();

        public string Action(string actionName, string actionParameters) {
            throw new NotImplementedException();
        }

        public string SendCommandString(string command, bool raw) {
            throw new NotImplementedException();
        }

        public bool SendCommandBool(string command, bool raw) {
            throw new NotImplementedException();
        }

        public void SendCommandBlind(string command, bool raw) {
            throw new NotImplementedException();
        }

        private string GetTWCAPIKey() {
            return profileService.ActiveProfile.WeatherDataSettings.TheWeatherCompanyAPIKey;
        }

        public class WeatherCompanyDataResponse {
            public WeatherCompanyDataResponseObs observation { get; set; }
            public int id { get; set; }
            public string name { get; set; }

            public class WeatherCompanyDataResponseObs {
                public double temp { get; set; }
                public double pressure { get; set; }
                public double rh { get; set; }
                public double wspd { get; set; }
                public double wdir { get; set; }
                public string clds { get; set; }
            }
        }
    }
}