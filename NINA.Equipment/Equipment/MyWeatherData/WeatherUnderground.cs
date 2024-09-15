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
        private const string _driverName = "Weather Underground";
        private const string _driverVersion = "1.0";

        private readonly IProfileService profileService;

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

        public string Category => _category;
        public string Id => _driverId;
        public string Name => _driverName;
        public string DisplayName => Name;
        public string DriverInfo => Loc.Instance["LblWeatherUndergroundClientInfo"];
        public string DriverVersion => _driverVersion;
        public string Description => Loc.Instance["LblWeatherUndergroundClientDescription"];
        public bool HasSetupDialog => false;

        private double _temperature = double.NaN;

        public double Temperature {
            get => _temperature;
            set {
                _temperature = value;
                RaisePropertyChanged();
            }
        }

        private double _pressure = double.NaN;

        public double Pressure {
            get => _pressure;
            set {
                _pressure = value;
                RaisePropertyChanged();
            }
        }

        private double _humidity = double.NaN;

        public double Humidity {
            get => _humidity;
            set {
                _humidity = value;
                RaisePropertyChanged();
            }
        }

        private double _windDirection = double.NaN;

        public double WindDirection {
            get => _windDirection;
            set {
                _windDirection = value;
                RaisePropertyChanged();
            }
        }

        private double _windSpeed = double.NaN;

        public double WindSpeed {
            get => _windSpeed;
            set {
                _windSpeed = value;
                RaisePropertyChanged();
            }
        }

        private double _windGust = double.NaN;

        public double WindGust {
            get => _windGust;
            set {
                _windGust = value;
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

        private double _dewpoint = double.NaN;

        public double DewPoint {
            get => _dewpoint;
            set {
                _dewpoint = value;
                RaisePropertyChanged();
            }
        }

        private double _rainRate = double.NaN;

        public double RainRate {
            get => _rainRate;
            set {
                _rainRate = value;
                RaisePropertyChanged();
            }
        }

        private double _averagePeriod;
        public double AveragePeriod { get => _averagePeriod; set => _averagePeriod = value; }

        public double SkyBrightness => double.NaN;
        public double SkyQuality => double.NaN;
        public double SkyTemperature => double.NaN;
        public double StarFWHM => double.NaN;

        private string WUAPIKey;
        private string WUStation;

        private bool _connected = false;

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
                    // Sleep thread until the next WU API query
                    await Task.Delay(TimeSpan.FromSeconds(_WUnderQueryPeriod), ct);

                    string result = await QueryWunderground(WUStation, WUAPIKey, ct);

                    // Exit and disconnect if result is empty
                    if (string.IsNullOrEmpty(result)) {
                        Notification.ShowError(Loc.Instance["LblWeatherUndergroundErrNoResponse"]);
                        Logger.Error("WU: API return is empty.");
                        Disconnect();
                        break;
                    }

                    UpdateWeatherData(result);
                }
            } catch (OperationCanceledException) {
                Logger.Debug("WU: WUnderUpdate task cancelled");
            } catch (Exception ex) {
                Notification.ShowError(string.Format(Loc.Instance["LblWeatherUndergroundErrReqFailed"], ex.Message));
                Logger.Error($"WU: API query failed: {ex.Message}");
            }
        }

        public async Task<bool> Connect(CancellationToken ct) {
            Connected = false;

            WUAPIKey = profileService.ActiveProfile.WeatherDataSettings.WeatherUndergroundAPIKey;
            WUStation = profileService.ActiveProfile.WeatherDataSettings.WeatherUndergroundStation;

            if (string.IsNullOrEmpty(WUAPIKey)) {
                Notification.ShowError(Loc.Instance["LblWeatherUndergroundErrNoAPIKey"]);
                Logger.Error("WU: No API key has been set");

                return Connected;
            }

            if (string.IsNullOrEmpty(WUStation)) {
                Notification.ShowError(Loc.Instance["LblWeatherUndergroundErrNoStationID"]);
                Logger.Error("WU: No Weather Underground station has been set");

                return Connected;
            }

            // Test our ability to use Weather Underground
            try {
                var result = await QueryWunderground(WUStation, WUAPIKey, ct);

                if (string.IsNullOrEmpty(result)) {
                    Notification.ShowError(Loc.Instance["LblWeatherUndergroundErrNoResponse"]);
                    Logger.Error("WU: API return is empty.");
                    return Connected;
                }

                UpdateWeatherData(result);
            } catch (Exception ex) {
                Notification.ShowError(string.Format(Loc.Instance["LblWeatherUndergroundErrReqFailed"], ex.Message));
                Logger.Error($"WU: API query failed: {ex}");
                return Connected;
            }

            Logger.Debug("WU: Starting WUnderUpdate task");
            WUnderUpdateWorkerCts?.Dispose();
            WUnderUpdateWorkerCts = new CancellationTokenSource();
            updateWorkerTask = WUnderUpdateWorker(WUnderUpdateWorkerCts.Token);

            Connected = true;
            return Connected;
        }

        public void Disconnect() {
            Logger.Debug("WU: Stopping WUnderUpdate task");

            if (Connected == false)
                return;

            try {
                WUnderUpdateWorkerCts?.Cancel();
                WUnderUpdateWorkerCts?.Dispose();
                updateWorkerTask?.Dispose();
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

        private static Task<string> QueryWunderground(string stationId, string apiKey, CancellationToken ct) {
            var url = $"{_WUnderBaseURL}?stationId={stationId}&format=json&units=m&apiKey={apiKey}&numericPrecision=decimal";

            var request = new HttpGetRequest(url, true);
            return request.Request(ct);
        }

        private void UpdateWeatherData(string wuQueryJson) {
            try {
                var wunderdata = WUnderData.FromJson(wuQueryJson);

                // temperature is Centigrade
                Temperature = wunderdata.Observations[0].metric.Temp;

                // pressure is hectopascals
                Pressure = wunderdata.Observations[0].metric.Pressure;

                // Dew Point is Centigrade
                DewPoint = wunderdata.Observations[0].metric.Dewpt;

                // humidity in percent
                Humidity = wunderdata.Observations[0].Humidity;

                // rain rate in mm/h
                RainRate = wunderdata.Observations[0].metric.PrecipRate;

                // wind speed in meters per second from kph
                WindSpeed = wunderdata.Observations[0].metric.WindSpeed * 0.2778;

                // wind heading in degrees
                WindDirection = wunderdata.Observations[0].Winddir;

                // wind gust in meters per second from kph
                WindGust = wunderdata.Observations[0].metric.WindGust * 0.2778;

            } catch (Exception ex) {
                Logger.Error($"WU: API query failed: {ex}");
                throw new Exception(Loc.Instance["LblUnexpectedError"]);
            }
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
            public double Lon { get; set; } = double.NaN;

            [JsonProperty("realtimeFrequency")]
            public object RealtimeFrequency { get; set; }

            [JsonProperty("epoch", NullValueHandling = NullValueHandling.Ignore)]
            public double Epoch { get; set; } = double.NaN;

            [JsonProperty("lat", NullValueHandling = NullValueHandling.Ignore)]
            public double Lat { get; set; } = double.NaN;

            [JsonProperty("uv")]
            public object Uv { get; set; }

            [JsonProperty("winddir")]
            public double Winddir { get; set; } = double.NaN;

            [JsonProperty("humidity")]
            public double Humidity { get; set; } = double.NaN;

            [JsonProperty("qcStatus", NullValueHandling = NullValueHandling.Ignore)]
            public long? QcStatus { get; set; }

            [JsonProperty("metric")]
            public Metric metric { get; set; }
        }

        public class Metric {

            [JsonProperty("temp")]
            public double Temp { get; set; } = double.NaN;

            [JsonProperty("heatIndex")]
            public double HeatIndex { get; set; } = double.NaN;

            [JsonProperty("dewpt")]
            public double Dewpt { get; set; } = double.NaN;

            [JsonProperty("windChill")]
            public double WindChill { get; set; } = double.NaN;

            [JsonProperty("windSpeed")]
            public double WindSpeed { get; set; } = double.NaN;

            [JsonProperty("windGust")]
            public double WindGust { get; set; } = double.NaN;

            [JsonProperty("pressure")]
            public double Pressure { get; set; } = double.NaN;

            [JsonProperty("precipRate")]
            public double PrecipRate { get; set; } = double.NaN;

            [JsonProperty("precipTotal")]
            public double PrecipTotal { get; set; } = double.NaN;

            [JsonProperty("elev")]
            public double Elev { get; set; } = double.NaN;
        }

        public partial class WUnderData {

            public static WUnderData FromJson(string json) => JsonConvert.DeserializeObject<WUnderData>(json, MyWeatherData.Converter.Settings);
        }
    }

    internal static class Converter {

        public static readonly JsonSerializerSettings Settings = new() {
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