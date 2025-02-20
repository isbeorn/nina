#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Astrometry;
using NINA.Core.Utility;
using NINA.Core.Utility.Http;
using NINA.Core.Utility.Notification;
using NINA.Core.Locale;
using NINA.Equipment.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyWeatherData {

    public partial class OpenWeatherMap : BaseINPC, IWeatherData {
        private const string _category = "N.I.N.A.";
        private const string _driverId = "NINA.OpenWeatherMap.Client";
        private const string _driverName = "OpenWeatherMap";
        private const string _driverVersion = "1.0";

        // OWM updates weather data every 10 minutes.
        // They strongly suggest that the API not be queried more frequent than that.
        private const double _owmQueryPeriod = 600;

        private Task updateWorkerTask;
        private CancellationTokenSource OWMUpdateWorkerCts;

        public OpenWeatherMap(IProfileService profileService) {
            this.profileService = profileService;
        }

        private readonly IProfileService profileService;

        public string Category => _category;

        public string Id => _driverId;

        public string Name => _driverName;
        public string DisplayName => Name;

        public string DriverInfo => Loc.Instance["LblOpenWeatherMapClientInfo"];

        public string DriverVersion => _driverVersion;

        public string Description => Loc.Instance["LblOpenWeatherMapClientDescription"];

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

        private double _rainRate;

        public double RainRate {
            get => _rainRate;
            set {
                _rainRate = value;
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

        public double DewPoint => AstroUtil.ApproximateDewPoint(Temperature, Humidity);

        private double _averagePeriod;
        public double AveragePeriod { get => _averagePeriod; set => _averagePeriod = value; }

        public double SkyBrightness => double.NaN;

        public double SkyQuality => double.NaN;

        public double SkyTemperature => double.NaN;

        public double StarFWHM => double.NaN;

        private bool _connected;

        public bool Connected {
            get => _connected;
            set {
                _connected = value;
                RaisePropertyChanged();
            }
        }

        private string ApiKey { get; set; } = string.Empty;
        private double Latitude { get; set; } = double.NaN;
        private double Longitude { get; set; } = double.NaN;

        private async Task OWMUpdateWorker(CancellationToken ct) {
            var failedQueries = 0;

            try {
                while (!ct.IsCancellationRequested) {
                    // Sleep thread until the next OWM API query
                    await Task.Delay(TimeSpan.FromSeconds(_owmQueryPeriod), ct).ConfigureAwait(false);

                    var resposne = await QueryOwm(ct).ConfigureAwait(false);
                    SetOwmResponse(resposne);

                    failedQueries = 0;
                }
            } catch (OperationCanceledException) {
                Logger.Debug("OWM: OWMUpdate task cancelled");
            } catch (Exception ex) {
                failedQueries++;
                Logger.Error("OWM: OWMUpdate connection failed", ex);

                // Show error notification once for every 3 consequtive failed queries
                if (failedQueries <= 3) {
                    Notification.ShowError(ex.Message);
                }
            }
        }

        public async Task<bool> Connect(CancellationToken ct) {
            ApiKey = profileService.ActiveProfile.WeatherDataSettings.OpenWeatherMapAPIKey.Trim();
            Latitude = profileService.ActiveProfile.AstrometrySettings.Latitude;
            Longitude = profileService.ActiveProfile.AstrometrySettings.Longitude;

            // Validate API key and check if we can connect to OpenWeatherMap
            try {
                ValidateInputs();

                var resposne = await QueryOwm(ct).ConfigureAwait(false);
                SetOwmResponse(resposne);

                Logger.Debug($"OWM: Connected to OpenWeatherMap. API data location: {resposne.Name}, {resposne.Sys.Country} (ID: {resposne.Id}");
            } catch (Exception ex) {
                Logger.Error("OWM: Failed to connect to OpenWeatherMap", ex);
                throw;
            }

            Logger.Debug("OWM: Starting OWMUpdate task");
            OWMUpdateWorkerCts?.Dispose();
            OWMUpdateWorkerCts = new CancellationTokenSource();
            updateWorkerTask = OWMUpdateWorker(OWMUpdateWorkerCts.Token);

            Connected = true;
            return true;
        }

        public void Disconnect() {
            Logger.Debug("OWM: Stopping OWMUpdate task");

            if (Connected == false)
                return;

            try {
                OWMUpdateWorkerCts?.Cancel();
                updateWorkerTask?.Wait();

                updateWorkerTask?.Dispose();
                OWMUpdateWorkerCts?.Dispose();
            } catch { }

            ApiKey = string.Empty;
            Latitude = double.NaN;
            Longitude = double.NaN;

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

        private async Task<OpenWeatherDataResponse> QueryOwm(CancellationToken ct) {
            var url = CreateOwmUrl();

            var request = new HttpGetRequest(url, true);
            string result = await request.Request(ct).ConfigureAwait(false);
            Logger.Trace($"OWM: Received response from OWM API: {result}");

            return JsonConvert.DeserializeObject<OpenWeatherDataResponse>(result);
        }

        private void SetOwmResponse(OpenWeatherDataResponse response) {
            // temperature is provided in Kelvin
            Temperature = response.Main.Temp - 273.15;

            // pressure is hectopascals
            var siteElevation = profileService.ActiveProfile.AstrometrySettings.Elevation;

            // The supplied air pressure is the estimated mean sea level pressure. Convert it to the local (QFE) air pressure
            if (double.IsNormal(siteElevation)) {
                if (double.IsNormal(response.Main.SeaLevel)) {
                    Pressure = AstroUtil.MslToLocalPressure(response.Main.SeaLevel, siteElevation);
                } else if (double.IsNormal(response.Main.Pressure)) {
                    Pressure = AstroUtil.MslToLocalPressure(response.Main.Pressure, siteElevation);
                }
            } else {
                if (double.IsNormal(response.Main.GrndLevel)) {
                    Pressure = response.Main.GrndLevel;
                }
            }

            // humidity in percent
            Humidity = response.Main.Humidity;

            // rain rate in mm per hour
            RainRate = response.Rain?.Rain1h ?? 0;

            // wind speed in meters per second
            WindSpeed = response.Wind.Speed;

            // wind heading in degrees
            WindDirection = response.Wind.Heading;

            // wind gust in meters per second
            WindGust = response.Wind.Gust;

            // cloudiness in percent
            CloudCover = response.Clouds.All;
        }

        private string CreateOwmUrl() {
            const string owmCurrentWeatherBaseURL = "https://api.openweathermap.org/data/2.5/weather";
            return string.Format("{0}?appid={1}&lat={2}&lon={3}", owmCurrentWeatherBaseURL, ApiKey, Latitude, Longitude);
        }

        private bool ValidateInputs() {
            // Is there an API key at all?
            if (string.IsNullOrEmpty(ApiKey)) {
                string error = "No OpenWeatherMap API key is configured.";
                throw new Exception(error);
            }

            // Is the API key string alphanumeric?
            var regex = IsAlphanumeric();
            if (!regex.IsMatch(ApiKey)) {
                string error = "The OpenWeatherMap API key contains invalid characters.";
                throw new Exception(error);
            }

            if (double.IsNaN(Latitude) || double.IsNaN(Longitude)) {
                string error = "Site Latitude and/or Longitude are not set.";
                throw new Exception(error);
            }

            return true;
        }

        public class OpenWeatherDataResponse {
            [JsonProperty("coord")]
            public OpenWeatherDataResponseCoord Coord { get; set; }

            [JsonProperty("main")]
            public OpenWeatherDataResponseMain Main { get; set; }

            [JsonProperty("wind")]
            public OpenWeatherDataResponseWind Wind { get; set; }

            [JsonProperty("clouds")]
            public OpenWeatherDataResponseClouds Clouds { get; set; }

            [JsonProperty("rain")]
            public OpenWeatherDataResponseRain Rain { get; set; }

            [JsonProperty("snow")]
            public OpenWeatherDataResponseSnow Snow { get; set; }

            [JsonProperty("sys")]
            public OpenWeatherDataResponseSys Sys { get; set; }

            [JsonProperty("visibility")]
            public double Visibility { get; set; }

            [JsonProperty("timezone")]
            public int Timezone { get; set; }

            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("dt")]
            public long Dt { get; set; }

            [JsonProperty("cod")]
            public int Cod { get; set; }

            [JsonProperty("base")]
            public string Base { get; set; }

            public class OpenWeatherDataResponseMain {
                [JsonProperty("temp")]
                public double Temp { get; set; }

                [JsonProperty("pressure")]
                public double Pressure { get; set; } = double.NaN;

                [JsonProperty("sea_level")]
                public double SeaLevel { get; set; } = double.NaN;

                [JsonProperty("grnd_level")]
                public double GrndLevel { get; set; } = double.NaN;

                [JsonProperty("humidity")]
                public double Humidity { get; set; }

                [JsonProperty("temp_min")]
                public double TempMin { get; set; }

                [JsonProperty("temp_max")]
                public double TempMax { get; set; }
            }

            public class OpenWeatherDataResponseCoord {
                [JsonProperty("lon")]
                public double Longitude { get; set; }

                [JsonProperty("lat")]
                public double Latitude { get; set; }
            }

            public class OpenWeatherDataResponseClouds {
                [JsonProperty("all")]
                public double All { get; set; }
            }

            public class OpenWeatherDataResponseRain {
                [JsonProperty("1h")]
                public double Rain1h { get; set; }
            }

            public class OpenWeatherDataResponseSnow {
                [JsonProperty("1h")]
                public double Snow1h { get; set; }
            }

            public class OpenWeatherDataResponseWind {
                [JsonProperty("speed")]
                public double Speed { get; set; }

                [JsonProperty("deg")]
                public double Heading { get; set; }

                [JsonProperty("gust")]
                public double Gust { get; set; }
            }

            public class OpenWeatherDataResponseSys {
                [JsonProperty("type")]
                public int Type { get; set; }

                [JsonProperty("id")]
                public int Id { get; set; }

                [JsonProperty("message")]
                public double Message { get; set; }

                [JsonProperty("country")]
                public string Country { get; set; }

                [JsonProperty("sunrise")]
                public long Sunrise { get; set; }

                [JsonProperty("sunset")]
                public long Sunset { get; set; }
            }
        }

        [GeneratedRegex("^[a-zA-Z0-9]*$")]
        private static partial Regex IsAlphanumeric();
    }
}