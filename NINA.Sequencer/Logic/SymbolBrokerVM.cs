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
using NINA.Equipment.Interfaces;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Conditions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NINA.Sequencer.Logic.Symbol;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Core.Utility;
using NINA.WPF.Base.ViewModel;
using NINA.Equipment.Equipment.MyWeatherData;
using System.Reflection;

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

        public Keys EquipmentKeys { get; set; } = new Keys();

        public Keys GetEquipmentKeys() {
            lock (SYMBOL_LOCK) {
                return EquipmentKeys;
            }
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
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_') {
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
        private static IList<string> EquipmentDataList { get; set; } = new List<string>();

        public static IList<string> GetSwitches() {
            lock (SYMBOL_LOCK) {
                return EquipmentDataList;
            }
        }

        public void AddSymbolData(string id, double value) {
            if (GetEquipmentKeys().ContainsKey(id)) {

            }
        }

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

        private void AddSymbol(List<string> i, string token, object value) {
            AddSymbol(i, token, value, null, false);
        }
        private void AddSymbol(List<string> i, string token, object value, string[] values) {
            AddSymbol(i, token, value, values, false);
        }

        private void AddSymbol(List<string> i, string token, object value, string[] values, bool silent) {
            EquipmentKeys.TryAdd(token, value);
            if (silent) {
                return;
            }
            StringBuilder sb = new StringBuilder(token);
            try {
                sb.Append(": ");
                if (values != null) {
                    sb.Append(values[(int)value + 1]);
                } else if (value is double d) {
                    sb.Append(Math.Round(d, 2));
                //} else if (value is long l) {
                //    sb.Append(Expression.ExprValueString(l));
                } else if (value is int n) {
                    sb.Append(n);
                } else {
                    sb.Append("'" + value.ToString() + "'");
                }
                //sb.Append(')');
                i.Add(sb.ToString());

                if (values != null) {
                    for (int v = 0; v < values.Length; v++) {
                        if (values[v] != null) {
                            EquipmentKeys.TryAdd(values[v], v - 1);
                        }
                    }
                }
            } catch (Exception e) {
                i.Add("Error adding " + token);
                Logger.Warning("Exception (" + e.Message + "): " + token + ", " + value + ", " + values);
            }
        }

        private static string[] PierConstants = new string[] { "PierUnknown", "PierEast", "PierWest" };

        private static string[] RoofConstants = new string[] { null, "RoofNotOpen", "RoofOpen", "RoofCannotOpenOrRead" };

        private static string[] ShutterConstants = new string[] { "ShutterNone", "ShutterOpen", "ShutterClosed", "ShutterOpening", "ShutterClosing", "ShutterError" };

        private static string[] CoverConstants = new string[] { null, "CoverUnknown", "CoverNeitherOpenNorClosed", "CoverClosed", "CoverOpen", "CoverError", "CoverNotPresent" };

        public Task UpdateEquipmentKeys() {

            lock (SYMBOL_LOCK) {
                var i = new List<string>();
                EquipmentKeys = new Keys();

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

                AddSymbol(i, "MoonAltitude", AstroUtil.GetMoonAltitude(DateTime.UtcNow, Observer));
                AddSymbol(i, "MoonIllumination", AstroUtil.GetMoonIllumination(DateTime.Now));
                AddSymbol(i, "SunAltitude", tc.Altitude.Degree);
                AddSymbol(i, "SunAzimuth", tc.Azimuth.Degree);

                double lst = AstroUtil.GetLocalSiderealTimeNow(ProfileService.ActiveProfile.AstrometrySettings.Longitude);
                if (lst < 0) {
                    lst = AstroUtil.EuclidianModulus(lst, 24);
                }
                AddSymbol(i, "LocalSiderealTime", lst);

                TimeSpan time = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
                double timeSeconds = Math.Floor(time.TotalSeconds);
                AddSymbol(i, "TIME", timeSeconds);

                TelescopeInfo telescopeInfo = TelescopeMediator.GetInfo();
                TelescopeConnected = telescopeInfo.Connected;
                if (TelescopeConnected) {
                    AddSymbol(i, "Altitude", telescopeInfo.Altitude);
                    AddSymbol(i, "Azimuth", telescopeInfo.Azimuth);
                    AddSymbol(i, "AtPark", telescopeInfo.AtPark);

                    Coordinates c = telescopeInfo.Coordinates.Transform(Epoch.J2000);
                    AddSymbol(i, "RightAscension", c.RA); // telescopeInfo.RightAscension);
                    AddSymbol(i, "Declination", c.Dec); // telescopeInfo.Declination);

                    AddSymbol(i, "SideOfPier", (int)telescopeInfo.SideOfPier, PierConstants);
                }

                SafetyMonitorInfo safetyInfo = SafetyMonitorMediator.GetInfo();
                SafetyConnected = safetyInfo.Connected;
                if (SafetyConnected) {
                    AddSymbol(i, "IsSafe", safetyInfo.IsSafe);
                }

                FocuserInfo fInfo = FocuserMediator.GetInfo();
                FocuserConnected = fInfo.Connected;
                if (fInfo != null && FocuserConnected) {
                    AddSymbol(i, "FocuserPosition", fInfo.Position);
                    AddSymbol(i, "FocuserTemperature", fInfo.Temperature);
                }

                // Get SensorTemp
                CameraInfo cameraInfo = CameraMediator.GetInfo();
                CameraConnected = cameraInfo.Connected;
                if (CameraConnected) {
                    AddSymbol(i, "SensorTemp", cameraInfo.Temperature);

                    // Hidden
                    EquipmentKeys.Add("camera__PixelSize", cameraInfo.PixelSize);
                    EquipmentKeys.Add("camera__XSize", cameraInfo.XSize);
                    EquipmentKeys.Add("camera__YSize", cameraInfo.YSize);
                    EquipmentKeys.Add("camera__CoolerPower", cameraInfo.CoolerPower);
                    EquipmentKeys.Add("camera__CoolerOn", cameraInfo.CoolerOn);
                    EquipmentKeys.Add("telescope__FocalLength", ProfileService.ActiveProfile.TelescopeSettings.FocalLength);
                }

                DomeInfo domeInfo = DomeMediator.GetInfo();
                DomeConnected = domeInfo.Connected;
                if (DomeConnected) {
                    AddSymbol(i, "ShutterStatus", (int)domeInfo.ShutterStatus, ShutterConstants);
                    AddSymbol(i, "DomeAzimuth", domeInfo.Azimuth);
                }

                FlatDeviceInfo flatInfo = FlatMediator.GetInfo();
                FlatConnected = flatInfo.Connected;
                if (FlatConnected) {
                    AddSymbol(i, "CoverState", (int)flatInfo.CoverState, CoverConstants);
                }

                RotatorInfo rotatorInfo = RotatorMediator.GetInfo();
                RotatorConnected = rotatorInfo.Connected;
                if (RotatorConnected) {
                    AddSymbol(i, "RotatorPosition", rotatorInfo.MechanicalPosition);
                }

                FilterWheelInfo filterWheelInfo = FilterWheelMediator.GetInfo();
                FilterWheelConnected = filterWheelInfo.Connected;
                if (FilterWheelConnected) {
                    var f = ProfileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
                    foreach (FilterInfo filterInfo in f) {
                        try {
                            EquipmentKeys.Add("Filter_" + RemoveSpecialCharacters(filterInfo.Name), filterInfo.Position);
                        } catch (Exception) {
                            LogOnce("Exception trying to add filter '" + filterInfo.Name + "' in UpdateSwitchWeatherData");
                        }
                    }

                    if (filterWheelInfo.SelectedFilter != null) {
                        EquipmentKeys.Add("CurrentFilter", filterWheelInfo.SelectedFilter.Position);
                        i.Add("CurrentFilter: Filter_" + RemoveSpecialCharacters(filterWheelInfo.SelectedFilter.Name));
                    }
                }

                // Get switch values
                SwitchInfo switchInfo = SwitchMediator.GetInfo();
                SwitchConnected = switchInfo.Connected;
                if (SwitchConnected) {
                    foreach (ISwitch sw in switchInfo.ReadonlySwitches) {
                        string key = RemoveSpecialCharacters(sw.Name);
                        EquipmentKeys.TryAdd(key, sw.Value);
                        i.Add("G: " + key + ": " + sw.Value);
                    }
                    foreach (ISwitch sw in switchInfo.WritableSwitches) {
                        string key = RemoveSpecialCharacters(sw.Name);
                        EquipmentKeys.TryAdd(key, sw.Value);
                        i.Add("S: " + key + ": " + sw.Value);
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
                                EquipmentKeys.TryAdd(key, t);
                                i.Add("W: " + key + ": " + t);
                            }
                        }
                    }
                }

                EquipmentDataList = i;

            }
            return Task.CompletedTask;
        }

    }
}
