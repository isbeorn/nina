#region "copyright"

/*
    Copyright © 2016 - 2025 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry;
using NINA.Core.Model.Equipment;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyDome;
using NINA.Equipment.Equipment.MyFlatDevice;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Equipment.MyRotator;
using NINA.Equipment.Equipment.MySafetyMonitor;
using NINA.Equipment.Equipment.MySwitch;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Conditions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Core.Utility;
using NINA.WPF.Base.ViewModel;
using NINA.Equipment.Equipment.MyWeatherData;
using System.Reflection;
using NINA.Equipment.Interfaces;
using System.Linq;

namespace NINA.Sequencer.Logic {
    public class SymbolBrokerVM : DockableVM, ISymbolBrokerVM {

        public SymbolBrokerVM(IProfileService profileService, ISwitchMediator switchMediator, IWeatherDataMediator weatherDataMediator, ICameraMediator cameraMediator, IDomeMediator domeMediator,
            IFlatDeviceMediator flatMediator, IFilterWheelMediator filterWheelMediator, IRotatorMediator rotatorMediator, ISafetyMonitorMediator safetyMonitorMediator,
            IFocuserMediator focuserMediator, ITelescopeMediator telescopeMediator, IGuiderMediator guiderMediator) : base(profileService) {
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


            ConditionWatchdog = new ConditionWatchdog(UpdateEquipmentKeys, TimeSpan.FromSeconds(5));
            ConditionWatchdog.Start();
        }

        private static ConcurrentDictionary<string, List<Symbol>> DataKeys = new ConcurrentDictionary<string, List<Symbol>>();

        private const char DELIMITER = '_';

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
            List<Symbol> list;
            string prefix = null;

            if (DataKeys.TryGetValue(key, out list) && list.Count == 1) {
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

            if (!DataKeys.TryGetValue(key, out list)) {
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

        private static ConditionWatchdog ConditionWatchdog { get; set; }

        private static bool TelescopeConnected = false;
        private static bool DomeConnected = false;
        private static bool SafetyConnected = false;
        private static bool FocuserConnected = false;
        private static bool CameraConnected = false;
        private static bool FlatConnected = false;
        private static bool FilterWheelConnected = false;
        private static bool RotatorConnected = false;
        private static bool SwitchConnected = false;
        private static bool WeatherConnected = false;

        private static ObserverInfo Observer = null;

        public static Object SYMBOL_LOCK = new object();

        private static HashSet<string> LoggedOnce = new HashSet<string>();
        public static void LogOnce(string message) {
            if (LoggedOnce.Contains(message)) return;
            Logger.Warning(message);
            LoggedOnce.Add(message);
        }

        public void AddSymbol(string source, string token, object value) {
            AddSymbol(source, token, value, null, false);
        }
        private void AddSymbol(string source, string token, object value, Symbol[] values) {
            AddSymbol(source, token, value, values, true);
        }
        private void AddSymbol(string source, string token, object value, Symbol[] values, bool silent) {
            List<Symbol> list;
            if (!Providers.Contains(source)) {
                Providers.Add(source);
            }
            if (!DataKeys.ContainsKey(token)) {
                list = new List<Symbol>();
                DataKeys[token] = list;
                list.Add(new Symbol(token, value, source, values, false));
            } else {
                list = DataKeys[token];
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
                    list.Add(new Symbol(token, value, source, values, false));
                }
            }

            // Defined constants...
            if (values != null) {
                foreach (Symbol d in values) {
                    AddSymbol(source, d.Key, d.Value, null, true);
                }
            }
        }

        private bool RemoveSymbol(string key) {
            List<Symbol> list;

            if (!DataKeys.TryGetValue(key, out list)) {
                return false;
            }

            DataKeys.Remove(key, out _);
            return true;
        }

        private IList<string> Providers = new List<string>();

        private static Symbol[] PierConstants = new Symbol[] { new Symbol("PierUnknown", -1), new Symbol("PierEast", 0), new Symbol("PierWest", 1) };

        private static Symbol[] RoofConstants = new Symbol[] { new Symbol("RoofNotOpen", 0), new Symbol("RoofOpen", 1), new Symbol("RoofCannotOpenOrRead", 2) };

        private static Symbol[] ShutterConstants = new Symbol[] { new Symbol("ShutterUnknown", -1), new Symbol("ShutterOpen", 0), new Symbol("ShutterClosed", 1), new Symbol("ShutterOpening", 2), new Symbol("ShutterClosing", 3),
            new Symbol("ShutterError", 4) };

        private static Symbol[] CoverConstants = new Symbol[] { new Symbol("CoverUnknown", 0), new Symbol("CoverNeitherOpenNorClosed", 1), new Symbol("CoverClosed", 2), new Symbol("CoverOpen", 3),
            new Symbol("CoverError", 4), new Symbol("CoverNotPresent", 5) };

        public IEnumerable<ConcurrentDictionary<string, object>> GetEquipmentKeys() {
            return (IEnumerable<ConcurrentDictionary<string, object>>)DataKeys;
        }

        private Task UpdateEquipmentKeys() {

            // For testing ambiguous symbols
            //AddSymbol("Foo", "Altitude", 0);
            //AddSymbol("Bar", "Altitude", 1);

            if (Observer == null) {
                Observer = new ObserverInfo() {
                    Latitude = ProfileService.ActiveProfile.AstrometrySettings.Latitude,
                    Longitude = ProfileService.ActiveProfile.AstrometrySettings.Longitude,
                    Elevation = ProfileService.ActiveProfile.AstrometrySettings.Elevation
                };
            }

            NOVAS.SkyPosition sunPos = AstroUtil.GetSunPosition(DateTime.Now, AstroUtil.GetJulianDate(DateTime.Now), Observer);
            Coordinates sunCoords = new Coordinates(sunPos.RA, sunPos.Dec, Epoch.JNOW, Coordinates.RAType.Hours);
            TopocentricCoordinates tc = sunCoords.Transform(Angle.ByDegree(Observer.Latitude), Angle.ByDegree(Observer.Longitude), Observer.Elevation);

            AddSymbol("NINA", "MoonAltitude", AstroUtil.GetMoonAltitude(DateTime.UtcNow, Observer));
            AddSymbol("NINA", "MoonIllumination", AstroUtil.GetMoonIllumination(DateTime.Now));
            AddSymbol("NINA", "SunAltitude", tc.Altitude.Degree);
            AddSymbol("NINA", "SunAzimuth", tc.Azimuth.Degree);

            double lst = AstroUtil.GetLocalSiderealTimeNow(ProfileService.ActiveProfile.AstrometrySettings.Longitude);
            if (lst < 0) {
                lst = AstroUtil.EuclidianModulus(lst, 24);
            }
            AddSymbol("NINA", "LocalSiderealTime", lst);

            TimeSpan time = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
            double timeSeconds = Math.Floor(time.TotalSeconds);
            AddSymbol("NINA", "TIME", timeSeconds);

            TelescopeInfo telescopeInfo = TelescopeMediator.GetInfo();
            TelescopeConnected = telescopeInfo.Connected;
            if (TelescopeConnected) {
                AddSymbol("Telescope", "Altitude", telescopeInfo.Altitude);
                AddSymbol("Telescope", "Azimuth", telescopeInfo.Azimuth);
                AddSymbol("Telescope", "AtPark", telescopeInfo.AtPark);

                Coordinates c = telescopeInfo.Coordinates.Transform(Epoch.J2000);
                AddSymbol("Telescope", "RightAscension", c.RA); // telescopeInfo.RightAscension);
                AddSymbol("Telescope", "Declination", c.Dec); // telescopeInfo.Declination);

                AddSymbol("Telescope", "SideOfPier", (int)telescopeInfo.SideOfPier, PierConstants);
            }

            SafetyMonitorInfo safetyInfo = SafetyMonitorMediator.GetInfo();
            SafetyConnected = safetyInfo.Connected;
            if (SafetyConnected) {
                AddSymbol("Safety", "IsSafe", safetyInfo.IsSafe);
            }

            FocuserInfo fInfo = FocuserMediator.GetInfo();
            FocuserConnected = fInfo.Connected;
            if (fInfo != null && FocuserConnected) {
                AddSymbol("Focuser", "Position", fInfo.Position);
                AddSymbol("Focuser", "Temperature", fInfo.Temperature);
            }

            // Get SensorTemp
            CameraInfo cameraInfo = CameraMediator.GetInfo();
            CameraConnected = cameraInfo.Connected;
            if (CameraConnected) {
                AddSymbol("Camera", "Temperature", cameraInfo.Temperature);

                // Hidden
                //EquipmentKeys.Add("camera__PixelSize", cameraInfo.PixelSize);
                //EquipmentKeys.Add("camera__XSize", cameraInfo.XSize);
                //EquipmentKeys.Add("camera__YSize", cameraInfo.YSize);
                //EquipmentKeys.Add("camera__CoolerPower", cameraInfo.CoolerPower);
                //EquipmentKeys.Add("camera__CoolerOn", cameraInfo.CoolerOn);
                //EquipmentKeys.Add("telescope__FocalLength", ProfileService.ActiveProfile.TelescopeSettings.FocalLength);
            }

            DomeInfo domeInfo = DomeMediator.GetInfo();
            DomeConnected = domeInfo.Connected;
            if (DomeConnected) {
                AddSymbol("Dome", "ShutterStatus", (int)domeInfo.ShutterStatus, ShutterConstants);
                AddSymbol("Dome", "DomeAzimuth", domeInfo.Azimuth);
            }

            FlatDeviceInfo flatInfo = FlatMediator.GetInfo();
            FlatConnected = flatInfo.Connected;
            if (FlatConnected) {
                AddSymbol("Flat Panel", "CoverState", (int)flatInfo.CoverState, CoverConstants);
            }

            RotatorInfo rotatorInfo = RotatorMediator.GetInfo();
            RotatorConnected = rotatorInfo.Connected;
            if (RotatorConnected) {
                AddSymbol("Rotator", "Position", rotatorInfo.MechanicalPosition);
            }

            FilterWheelInfo filterWheelInfo = FilterWheelMediator.GetInfo();
            FilterWheelConnected = filterWheelInfo.Connected;
            if (FilterWheelConnected) {
                var f = ProfileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
                foreach (FilterInfo filterInfo in f) {
                    AddSymbol("Filter", RemoveSpecialCharacters(filterInfo.Name), filterInfo.Position);
                }

                if (filterWheelInfo.SelectedFilter != null) {
                    AddSymbol("FilterWheel", "CurrentFilter", filterWheelInfo.SelectedFilter.Position);
                }
            }

            // Get switch values
            SwitchInfo switchInfo = SwitchMediator.GetInfo();
            SwitchConnected = switchInfo.Connected;
            if (SwitchConnected) {
                foreach (ISwitch sw in switchInfo.ReadonlySwitches) {
                    string key = RemoveSpecialCharacters(sw.Name);
                    AddSymbol("Gauge", key, sw.Value);
                }
                foreach (ISwitch sw in switchInfo.WritableSwitches) {
                    string key = RemoveSpecialCharacters(sw.Name);
                    AddSymbol("Switch", key, sw.Value);
                }
            }

            // Get weather values
            WeatherDataInfo weatherInfo = WeatherDataMediator.GetInfo();
            WeatherConnected = weatherInfo.Connected;
            if (WeatherConnected) {
                foreach (string dataName in WeatherData) {
                    PropertyInfo info = weatherInfo.GetType().GetProperty(dataName);
                    if (info != null) {
                        object val = info.GetValue(weatherInfo);
                        if (val is double t && !Double.IsNaN(t)) {
                            t = Math.Round(t, 2);
                            string key = RemoveSpecialCharacters(dataName);
                            AddSymbol("Weather", RemoveSpecialCharacters(dataName), t);
                        }
                    }
                }
            }

            return Task.CompletedTask;
        }

        public ISymbolProvider RegisterSymbolProvider(string friendlyName, string prefix) {
            if (Providers.Contains(prefix)) {
                throw new ArgumentException("Symbol Provider code is already registered.");
            }
            return new SymbolProvider(friendlyName, prefix, this);
        }

        public void AddSymbol(ISymbolProvider provider, string token, object value) {
            if (provider == null) {
                throw new ArgumentNullException(nameof(provider));
            }
            AddSymbol(provider.GetProviderFriendlyName(), provider.GetProviderCode() + DELIMITER + token, value);
        }
        public bool RemoveSymbol(ISymbolProvider provider, string token) {
            if (provider == null) {
                throw new ArgumentNullException(nameof(provider));
            }
            return RemoveSymbol(provider.GetProviderCode() + DELIMITER + token);
        }

        public List<Symbol> GetSymbols() {
            IList<Symbol> ss = new List<Symbol>();

            foreach (var kvp in DataKeys) {
                List<Symbol> sources = kvp.Value;
                foreach (Symbol ds in sources) {
                    Symbol symCopy = new Symbol(kvp.Key, ds.Value, ds.Category, ds.Constants, ds.Silent);
                    ss.Add(symCopy);
                }
            }
            return ss.Where(x => !x.Silent).OrderBy(x => x.Category).ThenBy(x => x.Key).ToList();
        }
    }
}
