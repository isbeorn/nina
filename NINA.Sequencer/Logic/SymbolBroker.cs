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
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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
        }

        private static ConcurrentDictionary<string, IList<Symbol>> DataSymbols = new ConcurrentDictionary<string, IList<Symbol>>();

        private static ConcurrentDictionary<string, IList<Symbol>> HiddenSymbols = new ConcurrentDictionary<string, IList<Symbol>>();

        public static readonly char DELIMITER = '_';

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
                DataSymbols.Remove(key, out _);
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
            return RemoveSymbol(provider.GetProviderName() + DELIMITER + token);
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
    }
}
