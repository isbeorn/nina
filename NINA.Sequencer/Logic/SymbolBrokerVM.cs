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

        private static ConcurrentDictionary<string, List<DataSource>> DataKeys = new ConcurrentDictionary<string, List<DataSource>>();

        public class DataSource {
            public string source;
            public object data;
            public bool display = true;

            public DataSource(string source, object data) {
                this.source = source;
                this.data = data;
            }
        }

        private const char DELIMITER = '_';

        public class Ambiguity {
            public string name;
            public List<string> sources;

            public Ambiguity(string name, List<DataSource> dataSources) {
                this.name = name;
                sources = new List<string>();
                foreach (DataSource ds in dataSources) {
                    sources.Add(ds.source);
                }
            }
        }

        public bool TryGetValue(string key, out object value) {
            List<DataSource> list;
            string prefix = null;

            if (key.IndexOf(DELIMITER) > 0) {
                string[] parts = key.Split(DELIMITER, 2);
                if (parts.Length == 2) {
                    key = parts[1];
                    prefix = parts[0];
                }
            }

            if (!DataKeys.TryGetValue(key, out list)) {
                value = null;
                return false;
            }

            if (prefix != null) {
                foreach (var kvp in list) {
                    if (kvp.source == prefix) {
                        value = kvp.data;
                        return true;
                    }
                }
            }

            // If the list has one item, we're done
            if (list.Count == 1) {
                value = list[0].data;
                return true;
            }

            // Ambiguous
            value = new Ambiguity(key, list);
            return false;
        }

        public DataSource GetDataSource(string key) {
            List<DataSource> list;
            if (!DataKeys.TryGetValue(key, out list)) {
                return null;
            }
            // For now, just one of each
            return new DataSource(list[0].source, list[0].data);
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

        private void AddSymbol(string token, object value) {
            AddSymbol("NINA", token, value, null, false);
        }
        private void AddSymbol(string source, string token, object value) {
            AddSymbol(source, token, value, null, false);
        }
        private void AddSymbol(string source, string token, object value, string[] values) {
            AddSymbol(source, token, value, values, false);
        }

        private void AddSymbol(string source, string token, object value, string[] values, bool silent) {
            List<DataSource> list;
            if (!DataKeys.ContainsKey(token)) {
                list = new List<DataSource>();
                DataKeys[token] = list;
                list.Add(new DataSource(source, value));
            } else {
                list = DataKeys[token];
                bool found = false;
                for (int idx = 0; idx < list.Count; idx++) {
                    DataSource s = list[idx];
                    if (s.source == source) {
                        s.data = value;
                        if (silent) {
                            s.display = false;
                        }
                        found = true;
                        break;
                    }
                }
                if (!found) {
                    list.Add(new DataSource(source, value));
                }
            }

            // Defined constants...
            // Not sure how to display these for now
            if (values != null) {
                for (int v = 0; v < values.Length; v++) {
                    if (values[v] != null) {
                        // Need a way to hide these in the list (silent flag not used)
                        AddSymbol(source, values[v], v - 1, null, true);
                    }
                }
            }
        }

        private static string[] PierConstants = new string[] { "PierUnknown", "PierEast", "PierWest" };

        private static string[] RoofConstants = new string[] { null, "RoofNotOpen", "RoofOpen", "RoofCannotOpenOrRead" };

        private static string[] ShutterConstants = new string[] { "ShutterNone", "ShutterOpen", "ShutterClosed", "ShutterOpening", "ShutterClosing", "ShutterError" };

        private static string[] CoverConstants = new string[] { null, "CoverUnknown", "CoverNeitherOpenNorClosed", "CoverClosed", "CoverOpen", "CoverError", "CoverNotPresent" };

        public IEnumerable<ConcurrentDictionary<string, object>> GetEquipmentKeys() {
            return (IEnumerable<ConcurrentDictionary<string, object>>)DataKeys;
        }

        private Task UpdateEquipmentKeys() {

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

            AddSymbol("MoonAltitude", AstroUtil.GetMoonAltitude(DateTime.UtcNow, Observer));
            AddSymbol("MoonIllumination", AstroUtil.GetMoonIllumination(DateTime.Now));
            AddSymbol("SunAltitude", tc.Altitude.Degree);
            AddSymbol("SunAzimuth", tc.Azimuth.Degree);

            double lst = AstroUtil.GetLocalSiderealTimeNow(ProfileService.ActiveProfile.AstrometrySettings.Longitude);
            if (lst < 0) {
                lst = AstroUtil.EuclidianModulus(lst, 24);
            }
            AddSymbol("LocalSiderealTime", lst);

            TimeSpan time = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
            double timeSeconds = Math.Floor(time.TotalSeconds);
            AddSymbol("TIME", timeSeconds);

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

        public class SourcedSymbols : List<Datum>;

        public class Datum {
            private string key;
            private object value;
            private string category;
            private bool display;

            public Datum(string key, object value, string category, bool display) {
                this.key = key;
                this.value = value;
                this.category = category;
                this.display = display;
            }

            public string Key { get { return key; } }
            public object Value { get { return value; } }
            public bool Display {get { return display; } }

            public object Category { get { return category;  } }

            public override string ToString() {
                return $"{key} : {value}";
            }
        }

        public List<Datum> GetDataSymbols() {
            SourcedSymbols ss = new SourcedSymbols();

            foreach (var kvp in DataKeys) {
                List<DataSource> sources = kvp.Value;
                foreach (DataSource ds in sources) {
                    Datum newDatum = new Datum(kvp.Key, ds.data, ds.source, ds.display);
                    ss.Add(newDatum);
                }
            }
            return ss.Where(x => x.Display == true).OrderBy(x => x.Category).ThenBy(x => x.Key).ToList();
        }
    }
}
