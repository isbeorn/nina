#region "copyright"

/*
    Copyright (c) 2016 - 2025 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
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

    public class OpenMeteo : BaseINPC, IWeatherData {
        private const string _category = "N.I.N.A.";
        private const string _driverId = "NINA.OpenMeteo.Client";
        private const string _driverName = "OpenMeteo";
        private const string _driverVersion = "1.0";

        private const string _currentWeatherBaseURL = "https://api.open-meteo.com/v1/forecast";

        // updates weather data every 10 minutes.
        // They strongly suggest that the API not be queried more frequent than that.
        private const double _queryPeriod = 600;

        private Task updateWorkerTask;
        private CancellationTokenSource UpdateWorkerCts;

        public OpenMeteo(IProfileService profileService) {
            this.profileService = profileService;
        }

        private IProfileService profileService;

        public string Category => _category;

        public string Id => _driverId;

        public string Name => _driverName;
        public string DisplayName => Name;

        public string DriverInfo => Loc.Instance["LblOpenMeteoClientInfo"];

        public string DriverVersion => _driverVersion;

        public string Description => Loc.Instance["LblOpenMeteoClientDescription"];

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

        private double _windGust;

        public double WindGust {
            get => _windGust;
            set {
                _windGust = value;
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

        private double _rainRate;

        public double RainRate {
            get => _rainRate;
            set {
                _rainRate = value;
                RaisePropertyChanged();
            }
        }

        public double DewPoint => AstroUtil.ApproximateDewPoint(Temperature, Humidity);

        private double _averagePeriod;
        public double AveragePeriod { get => _averagePeriod; set => _averagePeriod = value; }

        public double SkyBrightness => double.NaN;

        public double SkyQuality => double.NaN;

        public double SkyTemperature => double.NaN;

        public double StarFWHM => double.NaN;

        public string OWMAPIKey;
        private bool _connected;

        public bool Connected {
            get => _connected;
            set {
                _connected = value;
                RaisePropertyChanged();
            }
        }

        private async Task UpdateWorker(CancellationToken ct) {
            try 
            {
                while (true) 
                {
                    try 
                    {
                        await Task.Delay(TimeSpan.FromSeconds(_queryPeriod), ct);
                        await QueryWeather();
                    }
                    catch (Exception e) 
                    {
                        Logger.Debug($"OpenMeteo: Exception during update: {e.ToString()}");
                    }
                }
                
            } 
            catch (OperationCanceledException)
            {
                Logger.Debug("OpenMeteo: Update task cancelled");
                throw;
            } 
        }

        private async Task QueryWeather()
        {
            var latitude = profileService.ActiveProfile.AstrometrySettings.Latitude;
            var longitude = profileService.ActiveProfile.AstrometrySettings.Longitude;

            var parameter = "current=temperature_2m,relative_humidity_2m,precipitation,cloud_cover,surface_pressure,wind_direction_10m,wind_speed_10m,wind_gusts_10m";
            var url = $"{_currentWeatherBaseURL}?latitude={latitude}&longitude={longitude}&{parameter}";

            var request = new HttpGetRequest(url);
            string result = await request.Request(new CancellationToken());

            JObject o = JObject.Parse(result);
            var data = o.ToObject<OpenMeteoDataResponse>();

            Temperature = data.Current.Temperature;
            Pressure = data.Current.SurfacePressure;
            Humidity = data.Current.Humidity;
            
            WindDirection = data.Current.WindDirection;
            WindSpeed = ConvertKilometerPerHourToMeterPerSecond(data.Current.WindSpeed);
            WindGust = ConvertKilometerPerHourToMeterPerSecond(data.Current.WindGusts);

            CloudCover = data.Current.CloudCover;

            RainRate = data.Current.Precipitation;
        }

        private double ConvertKilometerPerHourToMeterPerSecond(double kmh)
        {
            return kmh * 0.277778;
        }

        public async Task<bool> Connect(CancellationToken ct) {
            await QueryWeather();

            Logger.Debug("OpenMeteo: Starting Update task");
            UpdateWorkerCts?.Dispose();
            UpdateWorkerCts = new CancellationTokenSource();
            updateWorkerTask = UpdateWorker(UpdateWorkerCts.Token);

            Connected = true;
            return true;
        }

        public void Disconnect() {
            Logger.Debug("OpenMeteo: Stopping Update task");

            if (Connected == false)
                return;

            try {
                UpdateWorkerCts?.Cancel();
                UpdateWorkerCts?.Dispose();
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
        
        public class OpenMeteoDataResponse {

            [JsonProperty(PropertyName = "current")]
            public OpenMeteoDataResponseCurrent Current { get; set; }

            public class OpenMeteoDataResponseCurrent {

                [JsonProperty(PropertyName = "temperature_2m")]
                public double Temperature { get; set; } //degree celcius
                
                [JsonProperty(PropertyName = "surface_pressure")]
                public double SurfacePressure { get; set; } //hPa
                
                [JsonProperty(PropertyName = "relative_humidity_2m")]
                public double Humidity { get; set; } //percentage

                [JsonProperty(PropertyName = "precipitation")]
                public double Precipitation { get; set; } //mm

                [JsonProperty(PropertyName = "cloud_cover")]
                public double CloudCover { get; set; } //percentage
                                
                [JsonProperty(PropertyName = "wind_direction_10m")]
                public double WindDirection { get; set; } // degree

                [JsonProperty(PropertyName = "wind_speed_10m")]
                public double WindSpeed { get; set; } // km/h
                
                [JsonProperty(PropertyName = "wind_gusts_10m")]
                public double WindGusts { get; set; } // km/h
            }
        }
    }
}