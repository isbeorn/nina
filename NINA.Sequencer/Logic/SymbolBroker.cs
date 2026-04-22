#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

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
using NINA.Equipment.Equipment.MyGuider;
using NINA.Equipment.Equipment.MyRotator;
using NINA.Equipment.Equipment.MySafetyMonitor;
using NINA.Equipment.Equipment.MySwitch;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Equipment.MyWeatherData;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Logic.SymbolFunctions;
using NINA.WPF.Base.ViewModel;
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

    public class SymbolBroker : DockableVM, ISymbolBrokerProviderApi, ITelescopeConsumer, ISwitchConsumer, IWeatherDataConsumer, IFocuserConsumer, IFilterWheelConsumer,
        IDomeConsumer, ISafetyMonitorConsumer, ICameraConsumer, IFlatDeviceConsumer, IRotatorConsumer, IGuiderConsumer {

        private static List<string> _symbolProviders =
            new List<string> { "NINA", "Image", "Dome", "Camera", "Mount", "Rotator", "Weather", "Gauge", "Switch", "Focuser", "Safety", "Filter", "FilterWheel" };

        private static int _internalSymbolProviderCount = _symbolProviders.Count;

        private static string[] _weatherData = new string[] { "CloudCover", "DewPoint", "Humidity", "Pressure", "RainRate", "SkyBrightness", "SkyQuality", "SkyTemperature",
            "StarFWHM", "Temperature", "WindDirection", "WindGust", "WindSpeed"};

        private static Symbol[] CoverConstants = new Symbol[] {
            new Symbol("CoverUnknown", 0),
            new Symbol("CoverNeitherOpenNorClosed", 1),
            new Symbol("CoverClosed", 2),
            new Symbol("CoverOpen", 3),
            new Symbol("CoverError", 4),
            new Symbol("CoverNotPresent", 5)
        };

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
        private readonly ConcurrentDictionary<string, IList<SymbolFunction>> _functions = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _brokerStateLock = new object();

        private ICameraMediator _cameraMediator;
        private ConditionWatchdog _conditionWatchdog;
        private ConcurrentDictionary<string, IList<Symbol>> _dataSymbols = new ConcurrentDictionary<string, IList<Symbol>>();

        private IDomeMediator _domeMediator;
        private IFilterWheelMediator _filterWheelMediator;
        private IFlatDeviceMediator _flatMediator;
        private IFocuserMediator _focuserMediator;
        private IImagingMediator _imagingMediator;
        private IGuiderMediator _guiderMediator;
        private ConcurrentDictionary<string, IList<Symbol>> _hiddenSymbols = new ConcurrentDictionary<string, IList<Symbol>>();

        private readonly ConcurrentDictionary<string, string> _providerOwnership = new ConcurrentDictionary<string, string>();

        private IProfileService _profileService;
        private IList<string> _providers = new List<string>();

        private ISafetyMonitorMediator _safetyMonitorMediator;
        private ISwitchMediator _switchMediator;
        private ITelescopeMediator _telescopeMediator;
        private IWeatherDataMediator _weatherDataMediator;
        private IRotatorMediator _rotatorMediator;
        public static readonly char DELIMITER = '_';

        /// <summary>
        /// Internal fallback for legacy expression construction paths that cannot receive
        /// an <see cref="ISymbolBroker"/> through constructor injection. Plugin and app code
        /// should use the injected <see cref="ISymbolBroker"/> service instead.
        /// </summary>
        internal static SymbolBroker Instance { get; private set; }

        public SymbolBroker(IProfileService profileService, ISwitchMediator switchMediator, IWeatherDataMediator weatherDataMediator, ICameraMediator cameraMediator, IDomeMediator domeMediator,
                                                                                            IFlatDeviceMediator flatMediator, IFilterWheelMediator filterWheelMediator, IRotatorMediator rotatorMediator, ISafetyMonitorMediator safetyMonitorMediator,
            IFocuserMediator focuserMediator, ITelescopeMediator telescopeMediator, IGuiderMediator guiderMediator, IImagingMediator imagingMediator) : base(profileService) {
            _switchMediator = switchMediator;
            _weatherDataMediator = weatherDataMediator;
            _cameraMediator = cameraMediator;
            _domeMediator = domeMediator;
            _flatMediator = flatMediator;
            _filterWheelMediator = filterWheelMediator;
            _profileService = profileService;
            _rotatorMediator = rotatorMediator;
            _safetyMonitorMediator = safetyMonitorMediator;
            _focuserMediator = focuserMediator;
            _telescopeMediator = telescopeMediator;
            _imagingMediator = imagingMediator;
            _guiderMediator = guiderMediator;

            // Register the default Providers
            foreach (string provider in _symbolProviders) {
                RegisterSymbolProvider(provider);
            }
            // Register the core functions
            RegisterCoreFunctions();

            _imagingMediator.ImagePrepared += SetImageSymbols;

            _telescopeMediator.RegisterConsumer(this);
            _switchMediator.RegisterConsumer(this);
            _weatherDataMediator.RegisterConsumer(this);    
            _focuserMediator.RegisterConsumer(this);
            _domeMediator.RegisterConsumer(this);
            _safetyMonitorMediator.RegisterConsumer(this);
            _filterWheelMediator.RegisterConsumer(this);
            _cameraMediator.RegisterConsumer(this);
            _flatMediator.RegisterConsumer(this);
            _rotatorMediator.RegisterConsumer(this);
            _guiderMediator.RegisterConsumer(this);

            AddOrUpdateSymbol("NINA", "LastExternalScriptExitCode", 0);
            UpdateNINASymbols();
            _conditionWatchdog = new ConditionWatchdog(UpdateNINASymbols, TimeSpan.FromSeconds(3));
            _conditionWatchdog.Start();

            // This is a singleton, created once in CompositionRoot
            Instance = this;
        }

        private void AddHiddenSymbol(string source, Symbol sym) {
            IList<Symbol> symList;
            if (!_hiddenSymbols.TryGetValue(source, out symList)) {
                symList = new List<Symbol>();
                _hiddenSymbols.TryAdd(source, symList);
            }
            symList.Add(sym);
        }

        public ISymbolProvider GetInternalProvider(string name) {
            for (int i = 0; i < _internalSymbolProviderCount; i++) {
                if (string.Equals(_symbolProviders[i], name, StringComparison.OrdinalIgnoreCase)) {
                    return new SymbolProvider(name, this);
                }
            }
            return null;
        }

        private void AddOrUpdateSymbol(string source, string token, object value) {
            AddOrUpdateSymbol(source, token, value, null, SymbolType.SYMBOL_NORMAL);
        }

        private void AddOrUpdateSymbol(string source, string token, object value, SymbolType type) {
            AddOrUpdateSymbol(source, token, value, null, type);
        }

        private void AddOrUpdateSymbol(string source, string token, object value, Symbol[] values) {
            AddOrUpdateSymbol(source, token, value, values, SymbolType.SYMBOL_NORMAL);
        }
        private void AddOrUpdateHiddenSymbol(string source, string token, object value, Symbol[] values) {
            AddOrUpdateSymbol(source, token, value, values, SymbolType.SYMBOL_HIDDEN);
        }

        private void AddOrUpdateSymbol(string source, string token, object value, Symbol[] values, SymbolType type) {
            lock (_brokerStateLock) {
                if (!_providers.Contains(source)) {
                    _providers.Add(source);
                }

                if (!_dataSymbols.TryGetValue(token, out IList<Symbol> list)) {
                    list = new List<Symbol>();
                    _dataSymbols[token] = list;
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
                        if (type == SymbolType.SYMBOL_HIDDEN) {
                            AddHiddenSymbol(source, sym);
                        }
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
        }

        private SymbolFunction GetFunction(string key) {
            lock (_brokerStateLock) {
                // 1) Direct lookup - if exactly one function matches the key, return it.
                if (_functions.TryGetValue(key, out var list) && list.Count == 1) {
                    return list[0];
                }

                // 2) Parse prefix if key contains a delimiter (e.g., "prefix_key").
                string prefix = null;
                int delimiterIndex = key.IndexOf(DELIMITER);

                if (delimiterIndex > 0) {
                    // Split only once: "prefix_key" → ["prefix", "key"]
                    var parts = key.Split(DELIMITER, 2);
                    if (parts.Length == 2) {
                        prefix = parts[0];
                        key = parts[1]; // lookup is performed on the key part
                    }
                }

                // 3) Lookup base key (after removing prefix if present).
                if (!_functions.TryGetValue(key, out list)) {
                    throw new ArgumentException("Function not found: " + key); // not found
                }

                // 4) If a prefix is available, use it to disambiguate between multiple functions.
                if (prefix != null) {
                    foreach (var f in list) {
                        if (f.Category == prefix) {
                            return f;
                        }
                    }
                }

                // 5) If only one symbol exists at this point, return it.
                if (list.Count == 1) {
                    return list[0];
                }

                // 6) Multiple symbols remain → ambiguous.
                throw new ArgumentException("Ambiguous function symbol: " + key);
            }
        }

        private bool GetSymbol(string key, out Symbol symbol) {
            lock (_brokerStateLock) {
                IList<Symbol> list;
                string prefix = null;

                if (_dataSymbols.TryGetValue(key, out list) && list.Count == 1) {
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

                if (!_dataSymbols.TryGetValue(key, out list)) {
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
                    if (!string.IsNullOrWhiteSpace(prefix) && !symbol.Key.Contains(prefix)) {
                        symbol = null;
                        return false;
                    }

                    return true;
                }

                // Ambiguous
                symbol = new AmbiguousSymbol(key, list);
                return false;
            }
        }

        private void RegisterCoreFunctions() {
            foreach (var fn in new MathFunctions(this)) {
                RegisterFunction(fn.Category, fn);
            }

            foreach (var fn in new LogicFunctions(this)) {
                RegisterFunction(fn.Category, fn);
            }

            foreach (var fn in new TimeFunctions(this)) {
                RegisterFunction(fn.Category, fn);
            }

            foreach (var fn in new StringFunctions(this)) {
                RegisterFunction(fn.Category, fn);
            }

            foreach (var fn in new UtilityFunctions(this)) {
                RegisterFunction(fn.Category, fn);
            }
        }

        private void RegisterFunction(string source, SymbolFunction function) {
            lock (_brokerStateLock) {
                if (source != function.Category) {
                    throw new ArgumentException("Function category does not match source provider.");
                }

                if (!_providers.Contains(source)) {
                    _providers.Add(source);
                }

                if (!_functions.ContainsKey(function.Key)) {
                    _functions[function.Key] = new List<SymbolFunction>();
                }

                if (_functions[function.Key].Any(x => x.Category == source)) {
                    throw new ArgumentException("Function symbol already registered: " + function.Key + " in category " + source);
                }

                _functions[function.Key].Add(function);
            }
        }

        private void RemoveAllSymbols(string source) {
            lock (_brokerStateLock) {
                int count = 0;
                var keysToRemove = new List<string>();

                foreach (var kvp in _dataSymbols) {
                    var list = kvp.Value;

                    for (int i = list.Count - 1; i >= 0; i--) {
                        if (list[i].Category == source) {
                            list.RemoveAt(i);
                            count++;
                        }
                    }

                    if (list.Count == 0) {
                        keysToRemove.Add(kvp.Key);
                    }
                }

                foreach (var key in keysToRemove) {
                    _dataSymbols.Remove(key, out _);
                }

                _hiddenSymbols.Remove(source, out _);

                Logger.Info($"Removing all symbols from: {source} ({count})");
            }
        }

        private bool RemoveSymbol(string source, string key) {
            lock (_brokerStateLock) {
                IList<Symbol> list;

                if (!_dataSymbols.TryGetValue(key, out list)) {
                    return false;
                }

                if (list.Count == 1) {
                    if (list[0].Category == source) {
                        if (list[0].Type == SymbolType.SYMBOL_HIDDEN) {
                            RemoveHiddenSymbol(source, key);
                        }
                        _dataSymbols.Remove(key, out _);
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
                    if (toRemove.Type == SymbolType.SYMBOL_HIDDEN) {
                        RemoveHiddenSymbol(source, key);
                    }
                    list.Remove(toRemove);
                }

                return true;
            }
        }

        private void RemoveHiddenSymbol(string source, string key) {
            if (!_hiddenSymbols.TryGetValue(source, out IList<Symbol> list)) {
                return;
            }

            for (int i = list.Count - 1; i >= 0; i--) {
                if (list[i].Category == source && list[i].Key == key) {
                    list.RemoveAt(i);
                }
            }

            if (list.Count == 0) {
                _hiddenSymbols.Remove(source, out _);
            }
        }

        private Task UpdateNINASymbols() {
            var observer = new ObserverInfo() {
                Latitude = _profileService.ActiveProfile.AstrometrySettings.Latitude,
                Longitude = _profileService.ActiveProfile.AstrometrySettings.Longitude,
                Elevation = _profileService.ActiveProfile.AstrometrySettings.Elevation
            };

            NOVAS.SkyPosition sunPos = AstroUtil.GetSunPosition(DateTime.Now, observer);
            Coordinates sunCoords = new Coordinates(sunPos.RA, sunPos.Dec, Epoch.JNOW, Coordinates.RAType.Hours);
            TopocentricCoordinates tc = sunCoords.Transform(Angle.ByDegree(observer.Latitude), Angle.ByDegree(observer.Longitude), observer.Elevation);

            AddOrUpdateSymbol("NINA", "MoonAltitude", AstroUtil.GetMoonAltitude(DateTime.UtcNow, observer));
            AddOrUpdateSymbol("NINA", "MoonIllumination", AstroUtil.GetMoonIllumination(DateTime.Now, observer));
            AddOrUpdateSymbol("NINA", "SunAltitude", tc.Altitude.Degree);
            AddOrUpdateSymbol("NINA", "SunAzimuth", tc.Azimuth.Degree);

            double lst = AstroUtil.GetLocalSiderealTimeNow(_profileService.ActiveProfile.AstrometrySettings.Longitude);
            if (lst < 0) {
                lst = AstroUtil.EuclidianModulus(lst, 24);
            }
            AddOrUpdateSymbol("NINA", "LocalSiderealTime", lst);

            TimeSpan time = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
            double timeSeconds = Math.Floor(time.TotalSeconds);
            AddOrUpdateSymbol("NINA", "Uptime", timeSeconds);

            AddOrUpdateSymbol("NINA", "ProfileName", _profileService.ActiveProfile.Name);
            AddOrUpdateSymbol("NINA", "ProfileId", _profileService.ActiveProfile.Id);

            return Task.CompletedTask;
        }

        // DATA SYMBOLS
        /// <summary>
        /// Converts a string into a valid NCalc identifier by replacing illegal characters with underscores.
        /// Valid identifiers must match: ^[a-zA-Z_][a-zA-Z0-9_]*$
        /// </summary>
        public static string SanitizeIdentifier(string str) {
            if (str == null) {
                return "__Null__";
            }

            if (str.Length == 0) {
                return "__Empty__";
            }

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < str.Length; i++) {
                char c = str[i];

                if (i == 0) {
                    // First character must be a letter or underscore
                    if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '_') {
                        sb.Append(c);
                    } else if (c >= '0' && c <= '9') {
                        // If it starts with a digit, prefix with underscore
                        sb.Append('_');
                        sb.Append(c);
                    } else {
                        // Replace illegal first character with underscore
                        sb.Append('_');
                    }
                } else {
                    // Subsequent characters can be letters, digits, or underscores
                    if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '_') {
                        sb.Append(c);
                    } else {
                        // Replace illegal characters with underscore
                        sb.Append('_');
                    }
                }
            }

            return sb.ToString();
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

        public void AddOrUpdateHiddenSymbol(ISymbolProvider provider, string token, object value, Symbol[] values) {
            if (provider == null) {
                throw new ArgumentNullException(nameof(provider));
            }
            AddOrUpdateHiddenSymbol(provider.GetProviderName(), token, value, values);
        }

        public void Dispose() {
        }

        public IReadOnlyCollection<SymbolFunction> GetFunctions() {
            lock (_brokerStateLock) {
                return _functions.Values.SelectMany(l => l).ToList();
            }
        }

        public IList<Symbol> GetHiddenSymbols(string source) {
            lock (_brokerStateLock) {
                if (_hiddenSymbols.TryGetValue(source, out IList<Symbol> syms)) {
                    return syms.Select(CopySymbol).ToList();
                }

                return null;
            }
        }

        private static Symbol CopySymbol(Symbol symbol) {
            return new Symbol(symbol.Key, symbol.Value, symbol.Category, symbol.Constants, symbol.Type);
        }

        public List<Symbol> GetSymbols() {
            lock (_brokerStateLock) {
                IList<Symbol> ss = new List<Symbol>();

                foreach (var kvp in _dataSymbols) {
                    IList<Symbol> sources = kvp.Value;
                    foreach (Symbol ds in sources) {
                        ss.Add(CopySymbol(ds));
                    }
                }
                return ss.Where(x => x.Type == SymbolType.SYMBOL_NORMAL).OrderBy(x => x.Category).ThenBy(x => x.Key).ToList();
            }
        }

        public void InvokeFunction(string name, FunctionArgs args, out object result, out bool isVolatile) {
            result = null;
            isVolatile = false;

            var fn = GetFunction(name);

            fn.ValidateArgs(name, args);
            result = fn.Implementation(args);
            isVolatile = fn.IsVolatile;
        }

        public void RegisterFunction(ISymbolProvider symbolProvider, SymbolFunction symbolFunction) {
            RegisterFunction(symbolProvider.GetProviderName(), symbolFunction);
        }
        public ISymbolProvider RegisterSymbolProvider(string name) {
            lock (_brokerStateLock) {
                // Check if provider already exists
                if (_providers.Contains(name)) {
                    throw new ArgumentException($"Symbol provider '{name}' is already registered.");
                }

                var callingAssembly = Assembly.GetCallingAssembly();
                var provider = new SymbolProvider(name, this);

                // Track which assembly registered this provider
                _providerOwnership[name] = callingAssembly.FullName;
                _providers.Add(name);

                return provider;
            }
        }

        public IReadOnlyCollection<ISymbolProvider> GetMyProviders() {
            var callingAssembly = Assembly.GetCallingAssembly();
            var assemblyName = callingAssembly.FullName;

            lock (_brokerStateLock) {
                var ownedProviders = _providerOwnership
                    .Where(kvp => kvp.Value == assemblyName)
                    .Select(kvp => kvp.Key)
                    .ToList();

                // Return the actual provider instances
                var result = new List<ISymbolProvider>();
                foreach (var providerName in ownedProviders) {
                    result.Add(new SymbolProvider(providerName, this));
                }

                return result.AsReadOnly();
            }
        }

        public bool RemoveSymbol(ISymbolProvider provider, string token) {
            if (provider == null) {
                throw new ArgumentNullException(nameof(provider));
            }

            return RemoveSymbol(provider.GetProviderName(), token);
        }

        public void SetImageSymbols(object sender, ImagePreparedEventArgs e) {
            TimeSpan time = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();

            IStarDetectionAnalysis analysis = e.RenderedImage.RawImageData.StarDetectionAnalysis;
            if (double.IsNaN(analysis.HFR)) {
                analysis.HFR = 0;
            }

            var imageMetaData = e.RenderedImage.RawImageData.MetaData;

            double rms = 0;
            RMS recordedRMS = imageMetaData.Image.RecordedRMS;
            if (recordedRMS != null) {
                rms = recordedRMS.Total;
            }

            AddOrUpdateSymbol("Image", "HFR", Math.Round(analysis.HFR, 3));
            AddOrUpdateSymbol("Image", "FWHM", Math.Round(analysis.FWHM, 3));
            AddOrUpdateSymbol("Image", "Eccentricity", Math.Round(analysis.Eccentricity, 3));
            AddOrUpdateSymbol("Image", "StarCount", analysis.DetectedStars);
            AddOrUpdateSymbol("Image", "ImageId", imageMetaData.Image.Id);
            AddOrUpdateSymbol("Image", "ExposureTime", imageMetaData.Image.ExposureTime);
            AddOrUpdateSymbol("Image", "RMS", rms);
            AddOrUpdateSymbol("Image", "Gain", imageMetaData.Camera.Gain);
            AddOrUpdateSymbol("Image", "Offset", imageMetaData.Camera.Offset);
            AddOrUpdateSymbol("Image", "ImageType", imageMetaData.Image.ImageType);
        }

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
        public void UpdateDeviceInfo(TelescopeInfo deviceInfo) {
            AddOrUpdateSymbol("Mount", "Connected", deviceInfo.Connected);
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
        public void UpdateDeviceInfo(SwitchInfo deviceInfo) {
            AddOrUpdateSymbol("Switch", "Connected", deviceInfo.Connected);
            if (deviceInfo.Connected) {
                foreach (ISwitch sw in deviceInfo.ReadonlySwitches) {
                    string key = SanitizeIdentifier(sw.Name);
                    AddOrUpdateSymbol("Gauge", key, sw.Value);
                }
                foreach (ISwitch sw in deviceInfo.WritableSwitches) {
                    string key = SanitizeIdentifier(sw.Name);
                    AddOrUpdateSymbol("Switch", key, sw.Value);
                }
            } else {
                RemoveAllSymbols("Gauge");
                RemoveAllSymbols("Switch");
            }
        }

        public void UpdateDeviceInfo(WeatherDataInfo deviceInfo) {
            AddOrUpdateSymbol("Weather", "Connected", deviceInfo.Connected);
            if (deviceInfo.Connected) {
                foreach (string dataName in _weatherData) {
                    PropertyInfo info = deviceInfo.GetType().GetProperty(dataName);
                    if (info != null) {
                        object val = info.GetValue(deviceInfo);
                        if (val is double t && !Double.IsNaN(t)) {
                            t = Math.Round(t, 2);
                            string key = SanitizeIdentifier(dataName);
                            AddOrUpdateSymbol("Weather", SanitizeIdentifier(dataName), t);
                        }
                    }
                }
            } else {
                RemoveAllSymbols("Weather");
            }
        }

        public void UpdateDeviceInfo(FocuserInfo deviceInfo) {
            AddOrUpdateSymbol("Focuser", "Connected", deviceInfo.Connected);
            if (deviceInfo.Connected) {
                AddOrUpdateSymbol("Focuser", "Position", deviceInfo.Position);
                AddOrUpdateSymbol("Focuser", "Temperature", deviceInfo.Temperature);
            } else {
                RemoveSymbol("Focuser", "Position");
                RemoveSymbol("Focuser", "Temperature");
            }
        }

        public void UpdateDeviceInfo(Equipment.Equipment.MyFilterWheel.FilterWheelInfo deviceInfo) {
            AddOrUpdateSymbol("FilterWheel", "Connected", deviceInfo.Connected);
            if (deviceInfo.Connected) {
                var f = _profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
                foreach (FilterInfo filterInfo in f) {
                    AddOrUpdateSymbol("Filter", SanitizeIdentifier(filterInfo.Name), filterInfo.Position);
                }

                if (deviceInfo.SelectedFilter != null) {
                    AddOrUpdateSymbol("FilterWheel", "CurrentFilterIndex", deviceInfo.SelectedFilter.Position);
                }
            } else {
                var f = _profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
                foreach (FilterInfo filterInfo in f) {
                    RemoveSymbol("Filter", SanitizeIdentifier(filterInfo.Name));
                }
                RemoveSymbol("FilterWheel", "CurrentFilterIndex");
            }
        }

        public void UpdateDeviceInfo(DomeInfo deviceInfo) {
            AddOrUpdateSymbol("Dome", "Connected", deviceInfo.Connected);
            if (deviceInfo.Connected) {
                AddOrUpdateSymbol("Dome", "ShutterStatus", (int)deviceInfo.ShutterStatus, ShutterConstants);
                AddOrUpdateSymbol("Dome", "Azimuth", deviceInfo.Azimuth);
                AddOrUpdateSymbol("Dome", "Altitude", deviceInfo.Altitude);
                AddOrUpdateSymbol("Dome", "DomeAzimuth", deviceInfo.Azimuth, SymbolType.SYMBOL_HIDDEN);
                AddOrUpdateSymbol("Dome", "DomeAltitude", deviceInfo.Altitude, SymbolType.SYMBOL_HIDDEN);
            } else {
                RemoveSymbol("Dome", "ShutterStatus");
                RemoveSymbol("Dome", "Azimuth");
                RemoveSymbol("Dome", "Altitude");
                RemoveSymbol("Dome", "DomeAzimuth");
                RemoveSymbol("Dome", "DomeAltitude");
            }
        }

        public void UpdateDeviceInfo(SafetyMonitorInfo deviceInfo) {
            AddOrUpdateSymbol("Safety", "Connected", deviceInfo.Connected);
            if (profileService.ActiveProfile.SafetyMonitorSettings.Id != "No_Device") {
                AddOrUpdateSymbol("Safety", "IsSafe", deviceInfo.Connected && deviceInfo.IsSafe);
            } else {
                RemoveSymbol("Safety", "IsSafe");
            }
        }

        public void UpdateDeviceInfo(CameraInfo deviceInfo) {
            AddOrUpdateSymbol("Camera", "Connected", deviceInfo.Connected);
            if (deviceInfo.Connected) {
                AddOrUpdateSymbol("Camera", "Temperature", deviceInfo.Temperature);
                AddOrUpdateSymbol("Camera", "TemperatureSetPoint", deviceInfo.TemperatureSetPoint);
                AddOrUpdateSymbol("Camera", "CoolerOn", deviceInfo.CoolerOn);
                AddOrUpdateSymbol("Camera", "CoolerPower", deviceInfo.CoolerPower);

                // Hidden
                AddOrUpdateSymbol("Camera", "PixelSize", deviceInfo.PixelSize, SymbolType.SYMBOL_HIDDEN);
                AddOrUpdateSymbol("Camera", "XSize", deviceInfo.XSize, SymbolType.SYMBOL_HIDDEN);
                AddOrUpdateSymbol("Camera", "YSize", deviceInfo.YSize, SymbolType.SYMBOL_HIDDEN);
            } else {
                RemoveSymbol("Camera", "Temperature");
                RemoveSymbol("Camera", "TemperatureSetPoint");
                RemoveSymbol("Camera", "CoolerOn");
                RemoveSymbol("Camera", "CoolerPower");
                RemoveSymbol("Camera", "PixelSize");
                RemoveSymbol("Camera", "XSize");
                RemoveSymbol("Camera", "YSize");
            }
        }

        public void UpdateDeviceInfo(FlatDeviceInfo deviceInfo) {
            AddOrUpdateSymbol("FlatPanel", "Connected", deviceInfo.Connected);
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
            AddOrUpdateSymbol("Rotator", "Connected", deviceInfo.Connected);
            if (deviceInfo.Connected) {
                AddOrUpdateSymbol("Rotator", "Position", deviceInfo.Position);
                AddOrUpdateSymbol("Rotator", "MechanicalPosition", deviceInfo.MechanicalPosition);
            } else {
                RemoveSymbol("Rotator", "Position");
                RemoveSymbol("Rotator", "MechanicalPosition");
            }
        }

        public void UpdateEndAutoFocusRun(AutoFocusInfo info) {
        }

        public void UpdateUserFocused(FocuserInfo info) {
        }

        public void UpdateDeviceInfo(GuiderInfo deviceInfo) {
            AddOrUpdateSymbol("Guider", "Connected", deviceInfo.Connected);
            if (deviceInfo.Connected && deviceInfo.RMSError is not null) {
                AddOrUpdateSymbol("Guider", "RMSTotal", deviceInfo.RMSError.Total.Arcseconds);
                AddOrUpdateSymbol("Guider", "RMSRA", deviceInfo.RMSError.RA.Arcseconds);
                AddOrUpdateSymbol("Guider", "RMSDec", deviceInfo.RMSError.Dec.Arcseconds);
                AddOrUpdateSymbol("Guider", "PeakRA", deviceInfo.RMSError.PeakRA.Arcseconds);
                AddOrUpdateSymbol("Guider", "PeakDec", deviceInfo.RMSError.PeakDec.Arcseconds);
            } else {
                RemoveSymbol("Guider", "RMSTotal");
                RemoveSymbol("Guider", "RMSRA");
                RemoveSymbol("Guider", "RMSDec");
                RemoveSymbol("Guider", "PeakRA");
                RemoveSymbol("Guider", "PeakDec");
            }
        }
    }
}
