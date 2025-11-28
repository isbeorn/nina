#region "copyright"

/*
    Copyright © 2016 - 2025 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using CommunityToolkit.Mvvm.ComponentModel;
using Namotion.Reflection;
using NCalc.Handlers;
using NINA.Astrometry;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyDome;
using NINA.Equipment.Equipment.MyFlatDevice;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Equipment.MyRotator;
using NINA.Equipment.Equipment.MySafetyMonitor;
using NINA.Equipment.Equipment.MySwitch;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Equipment.MyWeatherData;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.ImageData;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Conditions;
using NINA.WPF.Base.ViewModel;
using Parlot.Fluent;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Input;
using static NINA.Sequencer.Logic.Symbol;

namespace NINA.Sequencer.Logic {
    public class SymbolBroker : DockableVM, ISymbolBroker, ITelescopeConsumer, ISwitchConsumer, IWeatherDataConsumer, IFocuserConsumer, IFilterWheelConsumer,
        IDomeConsumer, ISafetyMonitorConsumer, ICameraConsumer, IFlatDeviceConsumer, IRotatorConsumer {

        public SymbolBroker(IProfileService profileService, ISwitchMediator switchMediator, IWeatherDataMediator weatherDataMediator, ICameraMediator cameraMediator, IDomeMediator domeMediator,
            IFlatDeviceMediator flatMediator, IFilterWheelMediator filterWheelMediator, IRotatorMediator rotatorMediator, ISafetyMonitorMediator safetyMonitorMediator,
            IFocuserMediator focuserMediator, ITelescopeMediator telescopeMediator, IGuiderMediator guiderMediator, IImagingMediator imagingMediator) : base(profileService) {
            SwitchMediator = switchMediator;
            WeatherDataMediator = weatherDataMediator;
            CameraMediator = cameraMediator;
            DomeMediator = domeMediator;
            FlatMediator = flatMediator;
            FilterWheelMediator = filterWheelMediator;
            ProfileService = profileService;
            RotatorMediator = rotatorMediator;
            SafetyMonitorMediator = safetyMonitorMediator;
            FocuserMediator = focuserMediator;
            TelescopeMediator = telescopeMediator;
            GuiderMediator = guiderMediator;
            ImagingMediator = imagingMediator;

            imagingMediator.ImagePrepared += SetImageSymbols;

            ConditionWatchdog = new ConditionWatchdog(UpdateNINASymbols, TimeSpan.FromSeconds(3));
            ConditionWatchdog.Start();

            TelescopeMediator.RegisterConsumer(this);
            SwitchMediator.RegisterConsumer(this);
            WeatherDataMediator.RegisterConsumer(this);
            FocuserMediator.RegisterConsumer(this);
            DomeMediator.RegisterConsumer(this);
            SafetyMonitorMediator.RegisterConsumer(this);
            FilterWheelMediator.RegisterConsumer(this);
            CameraMediator.RegisterConsumer(this);
            FlatMediator.RegisterConsumer(this);
            RotatorMediator.RegisterConsumer(this);

            // Register the default Providers
            foreach (string provider in SymbolProviders) {
                RegisterSymbolProvider(provider);
            }
            // Register basic functions
            RegisterBasicFunctions();
        }

        private ConcurrentDictionary<string, IList<Symbol>> DataSymbols = new ConcurrentDictionary<string, IList<Symbol>>();

        private ConcurrentDictionary<string, IList<Symbol>> HiddenSymbols = new ConcurrentDictionary<string, IList<Symbol>>();

        public static readonly char DELIMITER = '_';

        private static List<string> SymbolProviders =
            new List<string> { "NINA", "Image", "Dome", "Camera", "Mount", "Rotator", "Weather", "Gauge", "Switch", "Focuser", "Safety", "Filter", "FilterWheel" };

        public bool TryGetSymbol(string key, out Symbol symbol) {
            Symbol sym;
            if (GetSymbol(key, out sym)) {
                symbol = sym;
                return true;
            } else if (sym is AmbiguousSymbol) {
                symbol = sym;
                return false;
            }
            symbol = null;
            return false;
        }

        private bool GetSymbol(string key, out Symbol symbol) {
            IList<Symbol> list;
            string prefix = null;

            if (DataSymbols.TryGetValue(key, out list) && list.Count == 1) {
                symbol = list[0];
                return true;
            }

            if (key.IndexOf(DELIMITER) > 0) {
                string[] parts = key.Split(DELIMITER, 2);
                if (parts.Length == 2) {
                    key = parts[1];
                    prefix = parts[0];
                }
            }

            if (!DataSymbols.TryGetValue(key, out list)) {
                symbol = null;
                return false;
            }

            if (prefix != null) {
                foreach (Symbol kvp in list) {
                    if (kvp.Category == prefix) {
                        symbol = kvp;
                        return true;
                    }
                }
            }

            // If the list has one item, we're done
            if (list.Count == 1) {
                symbol = list[0];
                return true;
            }

            // Ambiguous
            symbol = new AmbiguousSymbol(key, list);
            return false;
        }

        public bool TryGetValue(string key, out object value) {
            Symbol d;
            if (GetSymbol(key, out d)) {
                Symbol sym = d as Symbol;
                if (sym != null) {
                    value = sym.Value;
                    return true;
                }
            } else {
                if (d is AmbiguousSymbol a) {
                    value = a;
                    return false;
                }
            }
            value = null;
            return false;
        }

        // DATA SYMBOLS

        private static string[] WeatherData = new string[] { "CloudCover", "DewPoint", "Humidity", "Pressure", "RainRate", "SkyBrightness", "SkyQuality", "SkyTemperature",
            "StarFWHM", "Temperature", "WindDirection", "WindGust", "WindSpeed"};

        public static string RemoveSpecialCharacters(string str) {
            if (str == null) {
                return "__Null__";
            }
            StringBuilder sb = new StringBuilder();
            foreach (char c in str) {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '_') {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static ISwitchMediator SwitchMediator { get; set; }
        private static IWeatherDataMediator WeatherDataMediator { get; set; }
        private static ICameraMediator CameraMediator { get; set; }
        private static IDomeMediator DomeMediator { get; set; }
        private static IFlatDeviceMediator FlatMediator { get; set; }
        private static IFilterWheelMediator FilterWheelMediator { get; set; }
        private static IProfileService ProfileService { get; set; }
        private static IRotatorMediator RotatorMediator { get; set; }
        private static ISafetyMonitorMediator SafetyMonitorMediator { get; set; }
        private static IFocuserMediator FocuserMediator { get; set; }
        private static ITelescopeMediator TelescopeMediator { get; set; }
        private static IGuiderMediator GuiderMediator { get; set; }
        private static IImagingMediator ImagingMediator { get; set; }

        private static ConditionWatchdog ConditionWatchdog { get; set; }

        public static Object SYMBOL_LOCK = new object();

        private static HashSet<string> LoggedOnce = new HashSet<string>();
        public static void LogOnce(string message) {
            if (LoggedOnce.Contains(message)) return;
            Logger.Warning(message);
            LoggedOnce.Add(message);
        }

        private void AddHiddenSymbol(string source, Symbol sym) {
            IList<Symbol> symList;
            if (!HiddenSymbols.TryGetValue(source, out symList)) {
                symList = new List<Symbol>();
                HiddenSymbols.TryAdd(source, symList);
            }
            symList.Add(sym);
        }

        public IList<Symbol> GetHiddenSymbols(string source) {
            IList<Symbol> syms = null;
            HiddenSymbols.TryGetValue(source, out syms);
            return syms;
        }

        public void AddOrUpdateSymbol(string source, string token, object value) {
            AddOrUpdateSymbol(source, token, value, null, SymbolType.SYMBOL_NORMAL);
        }
        public void AddOrUpdateSymbol(string source, string token, object value, SymbolType type) {
            AddOrUpdateSymbol(source, token, value, null, type);
        }
        private void AddOrUpdateSymbol(string source, string token, object value, Symbol[] values) {
            AddOrUpdateSymbol(source, token, value, values, SymbolType.SYMBOL_NORMAL);
        }
        private void AddOrUpdateSymbol(string source, string token, object value, Symbol[] values, SymbolType type) {
            if (!Providers.Contains(source)) {
                Providers.Add(source);
            }

            if (!DataSymbols.TryGetValue(token, out IList<Symbol> list)) {
                list = new List<Symbol>();
                DataSymbols[token] = list;
                Symbol sym = new Symbol(token, value, source, values, type);
                if (type == SymbolType.SYMBOL_HIDDEN) {
                    AddHiddenSymbol(source, sym);
                }
                list.Add(sym);
            } else {
                bool found = false;
                for (int idx = 0; idx < list.Count; idx++) {
                    Symbol s = list[idx];
                    if (s.Category == source) {
                        s.Value = value;
                        found = true;
                        break;
                    }
                }
                if (!found) {
                    Symbol sym = new Symbol(token, value, source, values, type);
                    list.Add(sym);
                }
            }

            // Defined constants...
            if (values != null) {
                foreach (Symbol d in values) {
                    AddOrUpdateSymbol(source, d.Key, d.Value, null, SymbolType.SYMBOL_CONSTANT);
                }
            }
        }

        // For use with registered providers
        private bool RemoveSymbol(string key) {
            IList<Symbol> list;

            if (!DataSymbols.TryGetValue(key, out list)) {
                return false;
            }

            DataSymbols.Remove(key, out _);
            return true;
        }

        private void RemoveAllSymbols(string source) {
            int count = 0;
            foreach (KeyValuePair<string, IList<Symbol>> kvp in DataSymbols) {
                Symbol toRemove = null;
                foreach (Symbol sym in kvp.Value) {
                    if (sym.Category == source) {
                        toRemove = sym;
                        break;
                    }
                }
                if (toRemove != null) {
                    kvp.Value.Remove(toRemove);
                    count++;
                }
            }
            Logger.Info("Removing all symbols from: " + source + " (" + count + ")");
        }

        private bool RemoveSymbol(string source, string key) {
            IList<Symbol> list;

            if (!DataSymbols.TryGetValue(key, out list)) {
                return false;
            }

            if (list.Count == 1) {
                if (list[0].Category == source) {
                    DataSymbols.Remove(key, out _);
                    return true;
                }
                return false;
            }

            Symbol toRemove = null;
            foreach (var sym in list) {
                if (sym.Category == source) {
                    toRemove = sym;
                    break;
                }
            }

            if (toRemove != null) {
                list.Remove(toRemove);
            }

            return true;
        }

        private IList<string> Providers = new List<string>();

        private static Symbol[] PierConstants = new Symbol[] {
            new Symbol("PierUnknown", -1),
            new Symbol("PierEast", 0),
            new Symbol("PierWest", 1)
        };

        private static Symbol[] ShutterConstants = new Symbol[] {
            new Symbol("ShutterUnknown", -1),
            new Symbol("ShutterOpen", 0),
            new Symbol("ShutterClosed", 1),
            new Symbol("ShutterOpening", 2),
            new Symbol("ShutterClosing", 3),
            new Symbol("ShutterError", 4)
        };

        private static Symbol[] CoverConstants = new Symbol[] {
            new Symbol("CoverUnknown", 0),
            new Symbol("CoverNeitherOpenNorClosed", 1),
            new Symbol("CoverClosed", 2),
            new Symbol("CoverOpen", 3),
            new Symbol("CoverError", 4),
            new Symbol("CoverNotPresent", 5)
        };

        public IEnumerable<ConcurrentDictionary<string, object>> GetEquipmentKeys() {
            return (IEnumerable<ConcurrentDictionary<string, object>>)DataSymbols;
        }

        private void AddOptionalImageSymbol(StarDetectionAnalysis a, string name) {
            if (a.HasProperty(name)) {
                var v = a.GetType().GetProperty(name).GetValue(a, null);
                if (v is double vDouble) {
                    AddOrUpdateSymbol("Image", name, Math.Round(vDouble, 2));
                }
            }
        }

        public void SetImageSymbols(object sender, ImagePreparedEventArgs e) {
            TimeSpan time = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();

            StarDetectionAnalysis a = (StarDetectionAnalysis)e.RenderedImage.RawImageData.StarDetectionAnalysis;
            if (double.IsNaN(a.HFR)) {
                a.HFR = 0;
            }

            var imageMetaData = e.RenderedImage.RawImageData.MetaData;

            double rms = 0;
            RMS recordedRMS = imageMetaData.Image.RecordedRMS;
            if (recordedRMS != null) {
                rms = recordedRMS.Total;
            }

            AddOrUpdateSymbol("Image", "HFR", Math.Round(a.HFR, 3));
            AddOrUpdateSymbol("Image", "StarCount", a.DetectedStars);
            AddOrUpdateSymbol("Image", "ImageId", imageMetaData.Image.Id);
            AddOrUpdateSymbol("Image", "ExposureTime", imageMetaData.Image.ExposureTime);
            AddOrUpdateSymbol("Image", "RMS", rms);
            AddOrUpdateSymbol("Image", "Gain", imageMetaData.Camera.Gain);
            AddOrUpdateSymbol("Image", "Offset", imageMetaData.Camera.Offset);
            AddOrUpdateSymbol("Image", "ImageType", imageMetaData.Image.ImageType);

            // Add these if they exist (from Hocus Focus at this time)
            AddOptionalImageSymbol(a, "Eccentricity");
            AddOptionalImageSymbol(a, "FWHM");
        }

        private Task UpdateNINASymbols() {

            var observer = new ObserverInfo() {
                Latitude = ProfileService.ActiveProfile.AstrometrySettings.Latitude,
                Longitude = ProfileService.ActiveProfile.AstrometrySettings.Longitude,
                Elevation = ProfileService.ActiveProfile.AstrometrySettings.Elevation
            };

            NOVAS.SkyPosition sunPos = AstroUtil.GetSunPosition(DateTime.Now, AstroUtil.GetJulianDate(DateTime.Now), observer);
            Coordinates sunCoords = new Coordinates(sunPos.RA, sunPos.Dec, Epoch.JNOW, Coordinates.RAType.Hours);
            TopocentricCoordinates tc = sunCoords.Transform(Angle.ByDegree(observer.Latitude), Angle.ByDegree(observer.Longitude), observer.Elevation);

            AddOrUpdateSymbol("NINA", "MoonAltitude", AstroUtil.GetMoonAltitude(DateTime.UtcNow, observer));
            AddOrUpdateSymbol("NINA", "MoonIllumination", AstroUtil.GetMoonIllumination(DateTime.Now, observer));
            AddOrUpdateSymbol("NINA", "SunAltitude", tc.Altitude.Degree);
            AddOrUpdateSymbol("NINA", "SunAzimuth", tc.Azimuth.Degree);

            double lst = AstroUtil.GetLocalSiderealTimeNow(ProfileService.ActiveProfile.AstrometrySettings.Longitude);
            if (lst < 0) {
                lst = AstroUtil.EuclidianModulus(lst, 24);
            }
            AddOrUpdateSymbol("NINA", "LocalSiderealTime", lst);

            TimeSpan time = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
            double timeSeconds = Math.Floor(time.TotalSeconds);
            AddOrUpdateSymbol("NINA", "ApplicationUptime", timeSeconds);

            return Task.CompletedTask;
        }

        public ISymbolProvider RegisterSymbolProvider(string name) {
            if (Providers.Contains(name)) {
                throw new ArgumentException("Symbol Provider name is already registered.");
            }
            Providers.Add(name);
            return new SymbolProvider(name, this);
        }

        public void AddOrUpdateSymbol(ISymbolProvider provider, string token, object value) {
            if (provider == null) {
                throw new ArgumentNullException(nameof(provider));
            }
            AddOrUpdateSymbol(provider.GetProviderName(), token, value);
        }

        public void AddOrUpdateSymbol(ISymbolProvider provider, string token, object value, Symbol[] values) {
            if (provider == null) {
                throw new ArgumentNullException(nameof(provider));
            }
            AddOrUpdateSymbol(provider.GetProviderName(), token, value, values);
        }

        public bool RemoveSymbol(ISymbolProvider provider, string token) {
            if (provider == null) {
                throw new ArgumentNullException(nameof(provider));
            }

            return RemoveSymbol(provider.GetProviderName(), token);
        }

        public List<Symbol> GetSymbols() {
            IList<Symbol> ss = new List<Symbol>();

            foreach (var kvp in DataSymbols) {
                IList<Symbol> sources = kvp.Value;
                foreach (Symbol ds in sources) {
                    Symbol symCopy = new Symbol(kvp.Key, ds.Value, ds.Category, ds.Constants, ds.Type);
                    ss.Add(symCopy);
                }
            }
            return ss.Where(x => x.Type == SymbolType.SYMBOL_NORMAL).OrderBy(x => x.Category).ThenBy(x => x.Key).ToList();
        }

        public void UpdateDeviceInfo(TelescopeInfo deviceInfo) {
            if (deviceInfo.Connected) {
                AddOrUpdateSymbol("Mount", "Altitude", deviceInfo.Altitude);
                AddOrUpdateSymbol("Mount", "Azimuth", deviceInfo.Azimuth);
                AddOrUpdateSymbol("Mount", "AtPark", deviceInfo.AtPark);

                Coordinates c = deviceInfo.Coordinates.Transform(Epoch.J2000);
                AddOrUpdateSymbol("Mount", "RightAscensionJ2000", c.RA);
                AddOrUpdateSymbol("Mount", "DeclinationJ2000", c.Dec);

                AddOrUpdateSymbol("Mount", "SideOfPier", (int)deviceInfo.SideOfPier, PierConstants);
            } else {
                RemoveSymbol("Mount", "Altitude");
                RemoveSymbol("Mount", "Azimuth");
                RemoveSymbol("Mount", "AtPark");
                RemoveSymbol("Mount", "RightAscensionJ2000");
                RemoveSymbol("Mount", "DeclinationJ2000");
                RemoveSymbol("Mount", "SideOfPier");
            }
        }

        public void Dispose() {
        }

        public void UpdateDeviceInfo(SwitchInfo deviceInfo) {
            if (deviceInfo.Connected) {
                foreach (ISwitch sw in deviceInfo.ReadonlySwitches) {
                    string key = RemoveSpecialCharacters(sw.Name);
                    AddOrUpdateSymbol("Gauge", key, sw.Value);
                }
                foreach (ISwitch sw in deviceInfo.WritableSwitches) {
                    string key = RemoveSpecialCharacters(sw.Name);
                    AddOrUpdateSymbol("Switch", key, sw.Value);
                }
            } else {
                RemoveAllSymbols("Gauge");
                RemoveAllSymbols("Switch");
            }
        }

        public void UpdateDeviceInfo(WeatherDataInfo deviceInfo) {
            if (deviceInfo.Connected) {
                foreach (string dataName in WeatherData) {
                    PropertyInfo info = deviceInfo.GetType().GetProperty(dataName);
                    if (info != null) {
                        object val = info.GetValue(deviceInfo);
                        if (val is double t && !Double.IsNaN(t)) {
                            t = Math.Round(t, 2);
                            string key = RemoveSpecialCharacters(dataName);
                            AddOrUpdateSymbol("Weather", RemoveSpecialCharacters(dataName), t);
                        }
                    }
                }
            } else {
                RemoveAllSymbols("Weather");
            }
        }

        public void UpdateEndAutoFocusRun(AutoFocusInfo info) {
        }
        public void UpdateUserFocused(FocuserInfo info) {
        }

        public void UpdateDeviceInfo(FocuserInfo deviceInfo) {
            if (deviceInfo.Connected) {
                AddOrUpdateSymbol("Focuser", "Position", deviceInfo.Position);
                AddOrUpdateSymbol("Focuser", "Temperature", deviceInfo.Temperature);
            } else {
                RemoveSymbol("Focuser", "Position");
                RemoveSymbol("Focuser", "Temperature");
            }

        }

        public void UpdateDeviceInfo(Equipment.Equipment.MyFilterWheel.FilterWheelInfo deviceInfo) {
            if (deviceInfo.Connected) {
                var f = ProfileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
                foreach (FilterInfo filterInfo in f) {
                    AddOrUpdateSymbol("Filter", RemoveSpecialCharacters(filterInfo.Name), filterInfo.Position);
                }

                if (deviceInfo.SelectedFilter != null) {
                    AddOrUpdateSymbol("FilterWheel", "CurrentFilterIndex", deviceInfo.SelectedFilter.Position);
                }
            } else {
                var f = ProfileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
                foreach (FilterInfo filterInfo in f) {
                    RemoveSymbol("Filter", RemoveSpecialCharacters(filterInfo.Name));
                }
                RemoveSymbol("FilterWheel", "CurrentFilterIndex");
            }
        }

        public void UpdateDeviceInfo(DomeInfo deviceInfo) {
            if (deviceInfo.Connected) {
                AddOrUpdateSymbol("Dome", "ShutterStatus", (int)deviceInfo.ShutterStatus, ShutterConstants);
                AddOrUpdateSymbol("Dome", "DomeAzimuth", deviceInfo.Azimuth);
                AddOrUpdateSymbol("Dome", "DomeAltitude", deviceInfo.Altitude);
            } else {
                RemoveSymbol("Dome", "ShutterStatus");
                RemoveSymbol("Dome", "DomeAzimuth");
                RemoveSymbol("Dome", "DomeAltitude");
            }
        }

        public void UpdateDeviceInfo(SafetyMonitorInfo deviceInfo) {
            if (profileService.ActiveProfile.SafetyMonitorSettings.Id != "No_Device") {
                AddOrUpdateSymbol("Safety", "IsSafe", deviceInfo.Connected && deviceInfo.IsSafe);
            } else {
                RemoveSymbol("Safety", "IsSafe");
            }
        }

        public void UpdateDeviceInfo(CameraInfo deviceInfo) {
            if (deviceInfo.Connected) {
                AddOrUpdateSymbol("Camera", "Temperature", deviceInfo.Temperature);
                // Hidden
                AddOrUpdateSymbol("Camera", "PixelSize", deviceInfo.PixelSize, SymbolType.SYMBOL_HIDDEN);
                AddOrUpdateSymbol("Camera", "XSize", deviceInfo.XSize, SymbolType.SYMBOL_HIDDEN);
                AddOrUpdateSymbol("Camera", "YSize", deviceInfo.YSize, SymbolType.SYMBOL_HIDDEN);
            } else {
                RemoveSymbol("Camera", "Temperature");
                RemoveSymbol("Camera", "PixelSize");
                RemoveSymbol("Camera", "XSize");
                RemoveSymbol("Camera", "YSize");
            }
        }

        public void UpdateDeviceInfo(FlatDeviceInfo deviceInfo) {
            if (deviceInfo.Connected) {
                AddOrUpdateSymbol("FlatPanel", "LightOn", deviceInfo.LightOn);
                AddOrUpdateSymbol("FlatPanel", "Brightness", deviceInfo.Brightness);
                AddOrUpdateSymbol("FlatPanel", "CoverState", (int)deviceInfo.CoverState, CoverConstants);
            } else {
                RemoveSymbol("FlatPanel", "LightOn");
                RemoveSymbol("FlatPanel", "Brightness");
                RemoveSymbol("FlatPanel", "CoverState");
            }
        }

        public void UpdateDeviceInfo(RotatorInfo deviceInfo) {
            if (deviceInfo.Connected) {
                AddOrUpdateSymbol("Rotator", "Position", deviceInfo.Position);
                AddOrUpdateSymbol("Rotator", "MechanicalPosition", deviceInfo.MechanicalPosition);
            } else {
                RemoveSymbol("Rotator", "Position");
                RemoveSymbol("Rotator", "MechanicalPosition");
            }
        }

        private static Random rng = new Random();

        private void RegisterBasicFunctions() {
            static DateTime GetDateTime(FunctionArgs args) {
                DateTime dt;
                if (args.Parameters?.Length > 0) {
                    try {
                        var utc = CoreUtil.UnixTimeStampToDateTime(
                            Convert.ToDouble(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture));
                        dt = utc.ToLocalTime();
                    } catch {
                        dt = DateTime.MinValue;
                    }
                } else {
                    dt = DateTime.Now;
                }
                return dt;
            }

            RegisterFunction(new SymbolFunction(
                "now",
                "Returns the current Unix timestamp in seconds.",
                "now()",
                args => CoreUtil.UnixTimeStampNow(),
                minArgs: 0,
                maxArgs: 0,
                isVolatile: true
            ));

            RegisterFunction(new SymbolFunction(
                "hour",
                "Returns the hour component (0–23) of a given datetime, or of the current time if no argument is supplied.",
                "hour() or hour(someDate)",
                args => (int)GetDateTime(args).Hour,
                minArgs: 0,
                maxArgs: 1
            ));

            RegisterFunction(new SymbolFunction(
                "minute",
                "Returns the minute component (0–59) of a given datetime, or of the current time if no argument is supplied.",
                "minute() or minute(someDate)",
                args => (int)GetDateTime(args).Minute,
                minArgs: 0,
                maxArgs: 1
            ));

            RegisterFunction(new SymbolFunction(
                "day",
                "Returns the day of the month (1–31) of a given datetime, or of the current date if no argument is supplied.",
                "day() or day(someDate)",
                args => (int)GetDateTime(args).Day,
                minArgs: 0,
                maxArgs: 1
            ));

            RegisterFunction(new SymbolFunction(
                "month",
                "Returns the month (1–12) of a given datetime, or of the current date if no argument is supplied.",
                "month() or month(someDate)",
                args => (int)GetDateTime(args).Month,
                minArgs: 0,
                maxArgs: 1
            ));

            RegisterFunction(new SymbolFunction(
                "year",
                "Returns the year component of a given datetime, or of the current date if no argument is supplied.",
                "year() or year(someDate)",
                args => (int)GetDateTime(args).Year,
                minArgs: 0,
                maxArgs: 1
            ));

            RegisterFunction(new SymbolFunction(
                "dow",
                "Returns the day of the week as an integer (0 = Sunday, 1 = Monday, … 6 = Saturday).",
                "dow() or dow(someDate)",
                args => (int)GetDateTime(args).DayOfWeek,
                minArgs: 0,
                maxArgs: 1
            ));

            RegisterFunction(new SymbolFunction(
                "dateString",
                "Formats a datetime value using the specified .NET format string.",
                "dateString(now(), \"yyyy-MM-dd HH:mm:ss\")",
                args => {
                    var dt = GetDateTime(args);
                    var fmt = Convert.ToString(args.Parameters[1].Evaluate(), CultureInfo.InvariantCulture);
                    return dt.ToString(fmt);
                },
                minArgs: 2,
                maxArgs: 2
            ));

            RegisterFunction(new SymbolFunction(
                "defined",
                "Returns whether a symbol name is defined in the symbol table.",
                "defined(\"foo\")",
                args => {
                    var str = Convert.ToString(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture);
                    return TryGetValue(str, out _);
                },
                minArgs: 1,
                maxArgs: 1,
                isVolatile: true // depends on symbol table
            ));

            RegisterFunction(new SymbolFunction(
                "startsWith",
                "Returns whether the string starts with the specified prefix.",
                "startsWith(\"hello\", \"he\")",
                args => {
                    var s = Convert.ToString(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture);
                    var prefix = Convert.ToString(args.Parameters[1].Evaluate(), CultureInfo.InvariantCulture);
                    return s.StartsWith(prefix, StringComparison.Ordinal);
                },
                minArgs: 2,
                maxArgs: 2
            ));

            RegisterFunction(new SymbolFunction(
                "strLength",
                "Returns the length of the given string, or -1 if the argument is not a string.",
                "strLength(\"hello\")",
                args => {
                    var v = args.Parameters[0].Evaluate();
                    return v is string s ? s.Length : -1;
                },
                minArgs: 1,
                maxArgs: 1
            ));

            RegisterFunction(new SymbolFunction(
                "strConcat",
                "Concatenates two strings.",
                "strConcat(\"hello\", \" world\")",
                args => {
                    var a = args.Parameters[0].Evaluate()?.ToString() ?? string.Empty;
                    var b = args.Parameters[1].Evaluate()?.ToString() ?? string.Empty;
                    return string.Concat(a, b);
                },
                minArgs: 2,
                maxArgs: 2
            ));

            RegisterFunction(new SymbolFunction(
                "strAtPos",
                "Returns the character at the specified zero-based index in a string, or an empty string if the index is out of bounds.",
                "strAtPos(\"hello\", 1)",
                args => {
                    var s = args.Parameters[0].Evaluate() as string ?? string.Empty;
                    var idxObj = args.Parameters[1].Evaluate();
                    if (idxObj is int idx && idx >= 0 && idx < s.Length)
                        return s[idx].ToString();
                    return string.Empty;
                },
                minArgs: 2,
                maxArgs: 2
            ));

            RegisterFunction(new SymbolFunction(
                "random",
                "Returns a random double value in the range 0.0–1.0.",
                "random()",
                args => rng.NextDouble(),
                minArgs: 0,
                maxArgs: 0,
                isVolatile: true
            ));
        }

        private readonly Dictionary<string, SymbolFunction> _functions = new(StringComparer.OrdinalIgnoreCase);

        public void RegisterFunction(SymbolFunction function) {
            _functions[function.Name] = function;
        }

        public bool TryInvokeFunction(string name, FunctionArgs args, out object result, out bool isVolatile) {
            result = null;
            isVolatile = false;

            if (!_functions.TryGetValue(name, out var fn)) {
                return false;
            }

            fn.ValidateArgs(name, args);
            result = fn.Implementation(args);
            isVolatile = fn.IsVolatile;
            return true;
        }

        private static readonly IReadOnlyList<SymbolFunction> _ncalcBuiltIns = [
            new SymbolFunction(
                "Abs",
                "Returns the absolute value of a specified number.",
                "Abs(-1)",
                args => Math.Abs(Convert.ToDouble(args.Parameters[0].Evaluate())),
                minArgs: 1, maxArgs: 1),

            new SymbolFunction(
                "Acos",
                "Returns the angle whose cosine is the specified number.",
                "Acos(1)",
                args => Math.Acos(Convert.ToDouble(args.Parameters[0].Evaluate())),
                1, 1),

            new SymbolFunction(
                "Asin",
                "Returns the angle whose sine is the specified number.",
                "Asin(0)",
                args => Math.Asin(Convert.ToDouble(args.Parameters[0].Evaluate())),
                1, 1),

            new SymbolFunction(
                "Atan",
                "Returns the angle whose tangent is the specified number.",
                "Atan(0)",
                args => Math.Atan(Convert.ToDouble(args.Parameters[0].Evaluate())),
                1, 1),

            new SymbolFunction(
                "Ceiling",
                "Returns the smallest integer greater than or equal to the specified number.",
                "Ceiling(1.5)",
                args => Math.Ceiling(Convert.ToDouble(args.Parameters[0].Evaluate())),
                1, 1),

            new SymbolFunction(
                "Cos",
                "Returns the cosine of the specified angle.",
                "Cos(0)",
                args => Math.Cos(Convert.ToDouble(args.Parameters[0].Evaluate())),
                1, 1),

            new SymbolFunction(
                "Exp",
                "Returns e raised to the specified power.",
                "Exp(0)",
                args => Math.Exp(Convert.ToDouble(args.Parameters[0].Evaluate())),
                1, 1),

            new SymbolFunction(
                "Floor",
                "Returns the largest integer less than or equal to the specified number.",
                "Floor(1.5)",
                args => Math.Floor(Convert.ToDouble(args.Parameters[0].Evaluate())),
                1, 1),

            new SymbolFunction(
                "IEEERemainder",
                "Returns the remainder resulting from the division of a specified number by another specified number.",
                "IEEERemainder(3, 2)",
                args => Math.IEEERemainder(
                    Convert.ToDouble(args.Parameters[0].Evaluate()),
                    Convert.ToDouble(args.Parameters[1].Evaluate())),
                2, 2),

            new SymbolFunction(
                "Ln",
                "Returns the natural logarithm of a specified number.",
                "Ln(1)",
                args => Math.Log(Convert.ToDouble(args.Parameters[0].Evaluate())),
                1, 1),

            new SymbolFunction(
                "Log",
                "Returns the logarithm of a specified number.",
                "Log(1, 10)",
                args => Math.Log(
                    Convert.ToDouble(args.Parameters[0].Evaluate()),
                    Convert.ToDouble(args.Parameters[1].Evaluate())),
                2, 2),

            new SymbolFunction(
                "Log10",
                "Returns the base 10 logarithm of a specified number.",
                "Log10(1)",
                args => Math.Log10(Convert.ToDouble(args.Parameters[0].Evaluate())),
                1, 1),

            new SymbolFunction(
                "Max",
                "Returns the larger of two specified numbers.",
                "Max(1, 2)",
                args => Math.Max(
                    Convert.ToDouble(args.Parameters[0].Evaluate()),
                    Convert.ToDouble(args.Parameters[1].Evaluate())),
                2, 2),

            new SymbolFunction(
                "Min",
                "Returns the smaller of two numbers.",
                "Min(1, 2)",
                args => Math.Min(
                    Convert.ToDouble(args.Parameters[0].Evaluate()),
                    Convert.ToDouble(args.Parameters[1].Evaluate())),
                2, 2),

            new SymbolFunction(
                "Pow",
                "Returns a specified number raised to the specified power.",
                "Pow(3, 2)",
                args => Math.Pow(
                    Convert.ToDouble(args.Parameters[0].Evaluate()),
                    Convert.ToDouble(args.Parameters[1].Evaluate())),
                2, 2),

            new SymbolFunction(
                "Round",
                "Rounds a value to the nearest integer or specified number of decimal places.",
                "Round(3.222, 2)",
                args =>
                {
                    // 1 or 2 args: Round(x) or Round(x, decimals)
                    if (args.Parameters.Length == 2)
                    {
                        return Math.Round(
                            Convert.ToDouble(args.Parameters[0].Evaluate()),
                            Convert.ToInt32(args.Parameters[1].Evaluate()));
                    }

                    return Math.Round(Convert.ToDouble(args.Parameters[0].Evaluate()));
                },
                minArgs: 1, maxArgs: 2),

            new SymbolFunction(
                "Sign",
                "Returns a value indicating the sign of a number.",
                "Sign(-10)",
                args => Math.Sign(Convert.ToDouble(args.Parameters[0].Evaluate())),
                1, 1),

            new SymbolFunction(
                "Sin",
                "Returns the sine of the specified angle.",
                "Sin(0)",
                args => Math.Sin(Convert.ToDouble(args.Parameters[0].Evaluate())),
                1, 1),

            new SymbolFunction(
                "Sqrt",
                "Returns the square root of a specified number.",
                "Sqrt(4)",
                args => Math.Sqrt(Convert.ToDouble(args.Parameters[0].Evaluate())),
                1, 1),

            new SymbolFunction(
                "Tan",
                "Returns the tangent of the specified angle.",
                "Tan(0)",
                args => Math.Tan(Convert.ToDouble(args.Parameters[0].Evaluate())),
                1, 1),

            new SymbolFunction(
                "Truncate",
                "Calculates the integral part of a number.",
                "Truncate(1.7)",
                args => Math.Truncate(Convert.ToDouble(args.Parameters[0].Evaluate())),
                1, 1),

            new SymbolFunction(
                "In",
                "Returns whether an element is in a set of values.",
                "in(1 + 1, 1, 2, 3)",
                args =>
                {
                    var value = args.Parameters[0].Evaluate();
                    for (int i = 1; i < args.Parameters.Length; i++)
                    {
                        if (Equals(value, args.Parameters[i].Evaluate()))
                            return true;
                    }
                    return false;
                },
                minArgs: 2, maxArgs: int.MaxValue),

            new SymbolFunction(
                "If",
                "Returns a value based on a condition.",
                "if(3 % 2 = 1, 'value is true', 'value is false')",
                args =>
                {
                    bool condition = Convert.ToBoolean(args.Parameters[0].Evaluate());
                    return condition
                        ? args.Parameters[1].Evaluate()
                        : args.Parameters[2].Evaluate();
                },
                minArgs: 3, maxArgs: 3),

            new SymbolFunction(
                "Ifs",
                "Returns a value based on evaluating a number of conditions, with a default if none are true.",
                "ifs(foo > 50, \"bar\", foo > 75, \"baz\", \"quux\")",
                args =>
                {
                    int count = args.Parameters.Length;

                    // at least condition, value, default
                    if (count < 3)
                        throw new ArgumentException("ifs() requires at least 3 arguments.");

                    // all but last are (condition, value) pairs
                    for (int i = 0; i < count - 1; i += 2)
                    {
                        bool cond = Convert.ToBoolean(args.Parameters[i].Evaluate());
                        if (cond)
                            return args.Parameters[i + 1].Evaluate();
                    }

                    // default value (last argument)
                    return args.Parameters[count - 1].Evaluate();
                },
                minArgs: 3, maxArgs: int.MaxValue),
        ];

        public IReadOnlyCollection<SymbolFunction> GetFunctions() {
            return _functions.Values
                .Concat(_ncalcBuiltIns)
                .ToList();
        }
    }
}
