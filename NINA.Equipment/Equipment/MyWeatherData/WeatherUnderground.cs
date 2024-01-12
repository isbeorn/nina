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
using Newtonsoft.Json.Converters;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Core.Utility.Http;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyWeatherData {

    public class WeatherUnderground : BaseINPC, IWeatherData {
        private const string _category = "N.I.N.A.";
        private const string _driverId = "NINA.WeatherUnderground.Client";
        private const string _driverName = "WeatherUnderground";
        private const string _driverVersion = "1.0";

        // WU current weather API base URL
        private const string _WUnderBaseURL = "https://api.weather.com/v2/pws/observations/current";

        // WU updates weather data every 10 minutes.
        // Seems to be enought.
        private const double _WUnderQueryPeriod = 600;

        private Task updateWorkerTask;

        private CancellationTokenSource WUnderUpdateWorkerCts;

        public WeatherUnderground(IProfileService profileService) {
            this.profileService = profileService;
        }

        private IProfileService profileService;
        public string Category => _category;
        public string Id => _driverId;
        public string Name => _driverName;
        public string DriverInfo => Loc.Instance["LblWeatherUndergroundClientInfo"];
        public string DriverVersion => _driverVersion;
        public string Description => Loc.Instance["LblWeatherUndergroundClientDescription"];
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

        private double _cloudCover = double.NaN;

        public double CloudCover {
            get => _cloudCover;
            set {
                _cloudCover = value;
                RaisePropertyChanged();
            }
        }

        private double _dewpoint;

        public double DewPoint {
            get => _dewpoint;
            set {
                _dewpoint = value;
                RaisePropertyChanged();
            }
        }

        private double _averagePeriod;
        public double AveragePeriod { get => _averagePeriod; set => _averagePeriod = value; }

        public double RainRate => double.NaN;

        public double SkyBrightness => double.NaN;

        public double SkyQuality => double.NaN;

        public double SkyTemperature => double.NaN;

        public double StarFWHM => double.NaN;

        public double WindGust => double.NaN;

        public string WUAPIKey;

        public string WUStation;

        private bool _connected;

        public bool Connected {
            get => _connected;
            set {
                _connected = value;
                RaisePropertyChanged();
            }
        }

        private async Task WUnderUpdateWorker(CancellationToken ct) {
            try {
                while (true) {
                    var url = _WUnderBaseURL + "?stationId={0}&format=json&units=m&apiKey={1}";

                    var request = new HttpGetRequest(url, WUStation, WUAPIKey);
                    string result = await request.Request(new CancellationToken());
                    // Exit and disconnect if result is empty
                    if (string.IsNullOrEmpty(result)) {
                        Notification.ShowError("Weather Underground API did not respond.");
                        Logger.Warning("WU: API return is empty.");
                        Disconnect();
                        break;
                    }

                    var wunderdata = WUnderData.FromJson(result);

                    // temperature is Centigrade
                    Temperature = wunderdata.Observations[0].metric.Temp;

                    // pressure is hectopascals
                    Pressure = wunderdata.Observations[0].metric.Pressure;

                    // Dew Point is Centigrade
                    DewPoint = wunderdata.Observations[0].metric.Dewpt;

                    // humidity in percent
                    Humidity = wunderdata.Observations[0].Humidity;

                    // wind speed in meters per second from kph
                    WindSpeed = wunderdata.Observations[0].metric.WindSpeed * 0.2778;

                    // wind heading in degrees
                    WindDirection = wunderdata.Observations[0].Winddir;

                    // Sleep thread until the next WU API query
                    await Task.Delay(TimeSpan.FromSeconds(_WUnderQueryPeriod), ct);
                }
            } catch (OperationCanceledException) {
                Logger.Debug("WU: WUnderUpdate task cancelled");
            }
        }

        public Task<bool> Connect(CancellationToken ct) {
            if (string.IsNullOrEmpty(GetWUAPIKey())) {
                Notification.ShowError("There is no Weather Underground API key configured.");
                Logger.Warning("WU: No API key has been set");

                Connected = false;
                return Task.FromResult(false);
            } else if (string.IsNullOrEmpty(GetWUStation())) {
                Notification.ShowError("There is no Weather Underground station configured.");
                Logger.Warning("WU: No Weather Underground station has been set");

                Connected = false;
                return Task.FromResult(false);
            }

            WUAPIKey = GetWUAPIKey();
            WUStation = GetWUStation();
            Logger.Debug("WU: Starting WUnderUpdate task");
            WUnderUpdateWorkerCts?.Dispose();
            WUnderUpdateWorkerCts = new CancellationTokenSource();
            updateWorkerTask = WUnderUpdateWorker(WUnderUpdateWorkerCts.Token);

            Connected = true;
            return Task.FromResult(true);
        }

        public void Disconnect() {
            Logger.Debug("WU: Stopping WUnderUpdate task");

            if (Connected == false)
                return;

            try {
                WUnderUpdateWorkerCts?.Cancel();
                WUnderUpdateWorkerCts?.Dispose();
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

        private string GetWUAPIKey() {
            return profileService.ActiveProfile.WeatherDataSettings.WeatherUndergroundAPIKey;
        }

        private string GetWUStation() {
            return profileService.ActiveProfile.WeatherDataSettings.WeatherUndergroundStation;
        }

        public partial class WUnderData {

            [JsonProperty("observations")]
            public Observation[] Observations { get; set; }
        }

        public partial class Observation {

            [JsonProperty("stationID")]
            public string StationId { get; set; }

            [JsonProperty("obsTimeUtc", NullValueHandling = NullValueHandling.Ignore)]
            public DateTimeOffset? ObsTimeUtc { get; set; }

            [JsonProperty("obsTimeLocal", NullValueHandling = NullValueHandling.Ignore)]
            public DateTimeOffset? ObsTimeLocal { get; set; }

            [JsonProperty("neighborhood", NullValueHandling = NullValueHandling.Ignore)]
            public string Neighborhood { get; set; }

            [JsonProperty("softwareType")]
            public object SoftwareType { get; set; }

            [JsonProperty("country", NullValueHandling = NullValueHandling.Ignore)]
            public string Country { get; set; }

            [JsonProperty("solarRadiation")]
            public object SolarRadiation { get; set; }

            [JsonProperty("lon", NullValueHandling = NullValueHandling.Ignore)]
            public double? Lon { get; set; }

            [JsonProperty("realtimeFrequency")]
            public object RealtimeFrequency { get; set; }

            [JsonProperty("epoch", NullValueHandling = NullValueHandling.Ignore)]
            public long? Epoch { get; set; }

            [JsonProperty("lat", NullValueHandling = NullValueHandling.Ignore)]
            public double? Lat { get; set; }

            [JsonProperty("uv")]
            public object Uv { get; set; }

            [JsonProperty("winddir")]
            public double Winddir { get; set; } = double.NaN;

            [JsonProperty("humidity")]
            public long Humidity { get; set; }

            [JsonProperty("qcStatus", NullValueHandling = NullValueHandling.Ignore)]
            public long? QcStatus { get; set; }

            [JsonProperty("metric")]
            public Metric metric { get; set; }
        }

        public class Metric {

            [JsonProperty("temp")]
            public int Temp { get; set; }

            [JsonProperty("heatIndex")]
            public int HeatIndex { get; set; }

            [JsonProperty("dewpt")]
            public int Dewpt { get; set; }

            [JsonProperty("windChill")]
            public int WindChill { get; set; }

            [JsonProperty("windSpeed")]
            public double WindSpeed { get; set; } = double.NaN;

            [JsonProperty("windGust")]
            public int WindGust { get; set; }

            [JsonProperty("pressure")]
            public double Pressure { get; set; } = double.NaN;

            [JsonProperty("precipRate")]
            public double PrecipRate { get; set; } = double.NaN;

            [JsonProperty("precipTotal")]
            public double PrecipTotal { get; set; } = double.NaN;

            [JsonProperty("elev")]
            public int Elev { get; set; }
        }

        public partial class WUnderData {

            public static WUnderData FromJson(string json) => JsonConvert.DeserializeObject<WUnderData>(json, MyWeatherData.Converter.Settings);
        }
    }

    internal static class Converter {

        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            NullValueHandling = NullValueHandling.Ignore,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
                },
        };
    }
}