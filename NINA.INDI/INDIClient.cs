#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.INDI.Devices;
using NINA.INDI.Protocol;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace NINA.INDI {
    public class INDIDeviceInfo {
        public string Id { get; set; }
        public string Name { get; set; }
        public IndiDeviceInterface Interface { get; set; }
        public string Version;
        public string Driver;
    }

    public class INDIClient : IDisposable {
        private static INDIClient _instance;
        private static readonly object _lock = new();

        public static INDIClient Instance {
            get {
                if (_instance == null) {
                    lock (_lock) {
                        if (_instance == null) {
                            _instance = new INDIClient(7654);
                        }
                    }
                }
                return _instance;
            }
        }

        private readonly int _port;
        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private Task _receiveTask;
        private CancellationTokenSource _cts;
        private TaskCompletionSource<bool> _operationTcs;
        private readonly object _operationLock = new();

        // Signals when the server is ready for driver operations
        private readonly TaskCompletionSource<bool> _serverReadyTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly IList<string> _loadedDrivers = [];
        private readonly Dictionary<string, INDIDeviceInfo> _discoveredDevices = [];
        private readonly Dictionary<string, INDIDevice> _registeredDevices = [];

        private readonly IReadOnlyDictionary<string, string> AvailableDriverNames = new Dictionary<string, string>() {
            {"indi_simulator_focus", "Focuser Simulator (INDI)" },
            {"indi_simulator_rotator", "Rotator Simulator (INDI)" },
            {"indi_wanderer_lite_rotator", "WandererRotator Lite (INDI)" },
            {"indi_wanderer_rotator_lite_v2", "WandererRotator Lite v2 (INDI)" },
            {"indi_simulator_telescope", "Mount Simulator" },
            {"indi_lx200_OnStep", "OnStep Mount (INDI)" },
            {"indi_simulator_weather", "Weather Simulator (INDI)" },
            {"indi_simulator_wheel", "FilterWheel Simulator (INDI)" }
        };

        public INDIClient(int port) {
            if (port < 1 || port > 65535) {
                throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1 and 65535.");
            }

            _port = port;
            Task.Run(async () => await StartServerInFifoMode());
        }

        public bool IsConnected => _tcpClient?.Connected ?? false;

        public async Task<bool> Connect(CancellationToken ct = default) {
            if (IsConnected) {
                Logger.Warning("INDI client already connected to server");
                return true;
            }

            try {
                // Check cancellation before starting
                ct.ThrowIfCancellationRequested();

                // Attach client
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync("localhost", _port, ct);
                _stream = _tcpClient.GetStream();

                _cts = new CancellationTokenSource();
                _receiveTask = Task.Run(() => ReceiveLoop(_cts.Token));

                // Request all properties from all devices
                GetProperties();

                if (IsConnected) {
                    Logger.Info("Connected to INDI server");
                } else {
                    Logger.Error("Failed to connect to INDI server");
                }
                return IsConnected;
            } catch (OperationCanceledException) {
                Logger.Warning("INDI bus client attachment was cancelled");
                return false;
            } catch (Exception ex) {
                Logger.Error($"Exception attaching INDI bus client: {ex.Message}");
                return false;
            }
        }

        public void Disconnect() {
            if (!IsConnected) {
                Logger.Warning("INDI not connected to server");
                return;
            }

            try {
                // Cancel receive loop and close connection FIRST
                // This makes IsConnected return false immediately
                _cts?.Cancel();
                _stream?.Close();
                _tcpClient?.Close();
                _stream = null;
                _tcpClient = null;
                Logger.Debug("INDI client disconnected");

                // Now unload drivers - devices will see IsConnected=false and skip waiting
                // Only try to gracefully unload drivers if server process is still alive
                if (_process != null && !_process.HasExited) {
                    foreach (var driver in _loadedDrivers.ToList()) {
                        UnloadDriver(driver);
                    }
                } else {
                    Logger.Debug("INDI server process not running, skipping graceful driver unload");
                    _loadedDrivers.Clear();
                }
            } catch (Exception ex) {
                Logger.Error($"Exception disconnecting from INDI server: {ex.Message}");
            }
        }

        public async Task<bool> LoadDriver(string driverName, TimeSpan? loadTimeout = null, CancellationToken ct = default) {
            if (!AvailableDriverNames.ContainsKey(driverName)) {
                Logger.Warning($"Driver '{driverName}' is not in the available INDI drivers list");
                return false;
            }

            if (_loadedDrivers.Contains(driverName)) {
                UnloadDriver(driverName);
            }

            lock (_operationLock) {
                _operationTcs = new TaskCompletionSource<bool>();
            }

            try {
                ct.ThrowIfCancellationRequested();

                using var fs = new FileStream(_fifoPath, FileMode.Open, FileAccess.Write);
                using var writer = new StreamWriter(fs);
                writer.WriteLine($"start {driverName}");
                writer.Flush();

                // Wait for the driver to be loaded with timeout and cancellation support
                var timeout = loadTimeout ?? TimeSpan.FromSeconds(15);
                var timeoutTask = Task.Delay(timeout, ct);
                var completedTask = await Task.WhenAny(_operationTcs.Task, timeoutTask);

                if (completedTask == timeoutTask) {
                    // Check if it was a timeout or cancellation
                    if (ct.IsCancellationRequested) {
                        Logger.Warning("INDI loading driver was cancelled");
                        return false;
                    }
                    Logger.Error("INDI loading driver timed out");
                    return false;
                }

                bool success = await _operationTcs.Task;
                if (success) {
                    _loadedDrivers.Add(driverName);
                    Logger.Info($"Loaded driver '{driverName}'");
                }

                return success;
            } catch (OperationCanceledException) {
                Logger.Warning("INDI loading driver was cancelled");
                return false;
            } catch (Exception ex) {
                Logger.Error($"Exception loading INDI driver: {ex.Message}");
                return false;
            }
        }

        public void UnloadDriver(string driverName) {
            lock (_operationLock) {
                if (_loadedDrivers.Contains(driverName)) {
                    try {
                        Logger.Debug($"Removing driver '{driverName}'");

                        using var fs = new FileStream(_fifoPath, FileMode.Open, FileAccess.Write);
                        using var writer = new StreamWriter(fs);
                        writer.WriteLine($"stop {driverName}");
                        writer.Flush();

                        _loadedDrivers.Remove(driverName);

                        // Remove devices associated with this driver
                        var devicesToRemove = _discoveredDevices.Where(d => d.Value.Driver == driverName).Select(d => d.Key).ToList();
                        foreach (var deviceKey in devicesToRemove) {
                            _discoveredDevices.Remove(deviceKey);
                            Logger.Debug($"Removed device '{deviceKey}' (driver: {driverName})");
                        }

                        Logger.Info($"Unloaded driver '{driverName}'");
                    } catch (Exception ex) {
                        Logger.Error(ex.Message);
                    }
                } else {
                    Logger.Warning($"Driver '{driverName}' is not loaded");
                }
            }
        }

        internal void RegisterDevice(INDIDevice device) {
            lock (_lock) {
                _registeredDevices[device.Id] = device;
                Logger.Debug($"Registered device: '{device.Id}' (Name: '{device.DeviceName}')");
            }
        }

        internal void UnregisterDevice(INDIDevice device) {
            lock (_lock) {
                _registeredDevices.Remove(device.Id);
                Logger.Debug($"Unregistered device: '{device.Id}'");
            }
        }

        public void GetProperties(string device = null, string name = null) {
            if (!IsConnected) {
                Logger.Warning("Cannot enumerate properties: not attached to INDI server");
                return;
            }

            var element = new XElement("getProperties");
            element.Add(new XAttribute("version", "1.7"));
            if (device != null) {
                element.Add(new XAttribute("device", device));
            }
            if (name != null) {
                element.Add(new XAttribute("name", name));
            }
            SendMessage(element);
        }

        private void SendMessage(XElement element) {
            if (!IsConnected) {
                return;
            }

            lock (_operationLock) {
                try {
                    var xml = element.ToString(SaveOptions.DisableFormatting);
                    var bytes = Encoding.UTF8.GetBytes(xml);
                    _stream.Write(bytes, 0, bytes.Length);
                    _stream.Flush();
                } catch (Exception ex) {
                    Logger.Error($"Send error: {ex.Message}");
                }
            }
        }

        public void SendProperty(INDIProperty prop) {
            SendMessage(prop.ToXml());
        }

        private async Task ReceiveLoop(CancellationToken ct) {
            var buffer = new byte[65536]; // 64KB buffer - good balance for INDI messages and BLOBs
            var xmlBuffer = new StringBuilder();

            while (!ct.IsCancellationRequested && _stream != null) {
                try {
                    var bytesRead = await _stream.ReadAsync(buffer, ct);
                    if (bytesRead == 0)
                    {
                        Logger.Error("Server disconnected");
                        break;
                    }

                    var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    xmlBuffer.Append(TrimXml(message));

                    // Process complete XML elements
                    ProcessXmlMessage(xmlBuffer);
                } catch (Exception ex) when (ex is not OperationCanceledException) {
                    Logger.Error($"Receive error: {ex.Message}");
                    break;
                }
            }
        }

        public async Task<IReadOnlyList<INDIDeviceInfo>> GetDrivers(IndiDeviceInterface deviceInterface, CancellationToken ct = default) {
            // Wait for the server to be ready before proceeding
            await _serverReadyTcs.Task;
            var devices = new Dictionary<string, INDIDeviceInfo>();

            var types = new List<string>();
            switch (deviceInterface) {
                case IndiDeviceInterface.CCD_INTERFACE:
                    types.Add("ccd");
                    break;
                case IndiDeviceInterface.FILTER_INTERFACE:
                    types.Add("wheel");
                    break;
                case IndiDeviceInterface.FOCUSER_INTERFACE:
                    types.Add("focus");
                    break;
                case IndiDeviceInterface.ROTATOR_INTERFACE:
                    types.Add("rotator");
                    break;
                case IndiDeviceInterface.TELESCOPE_INTERFACE:
                    types.Add("telescope");
                    types.Add("lx200");
                    break;
                case IndiDeviceInterface.WEATHER_INTERFACE:
                    types.Add("weather");
                    break;
                case IndiDeviceInterface.LIGHTBOX_INTERFACE:
                    types.Add("cover");
                    break;
            }

            // Check available devices
            foreach (var driver in AvailableDriverNames) {
                ct.ThrowIfCancellationRequested();
                if (types.Any(type => driver.Key.Contains(type))) {
                    // Give slow drivers an extra short window to appear after load.
                    if (await LoadDriver(driver.Key, TimeSpan.FromSeconds(10), ct)) {
                        // Wait up to a few seconds for the driver to announce itself
                        await WaitForDevicesForDriverAsync(driver.Key, TimeSpan.FromSeconds(5), ct);

                        foreach (var dev in _discoveredDevices) {
                            if ((dev.Value.Interface & deviceInterface) != 0) {
                                if (!devices.ContainsKey(dev.Key)) {
                                    Logger.Info($"Found driver {dev.Key}");
                                    devices.Add(dev.Key, dev.Value);
                                }
                            }
                        }
                        UnloadDriver(driver.Key);
                    }
                }
            }

            return devices.Values.ToList();
        }

        /// <summary>
        /// Wait for the INDI server to be ready. Returns true if the server
        /// reports ready (SetResult(true)), false if it reports not-ready or
        /// if waiting timed out or was cancelled.
        /// </summary>
        public async Task<bool> WaitForServerReadyAsync(TimeSpan? timeout = null, CancellationToken ct = default) {
            try {
                var serverReadyTask = _serverReadyTcs.Task;
                if (timeout.HasValue) {
                    var completed = await Task.WhenAny(serverReadyTask, Task.Delay(timeout.Value, ct));
                    if (completed != serverReadyTask) {
                        Logger.Debug("WaitForServerReadyAsync timed out");
                        return false;
                    }
                } else {
                    await serverReadyTask;
                }

                return serverReadyTask.IsCompleted && serverReadyTask.Result;
            } catch (OperationCanceledException) {
                Logger.Warning("WaitForServerReadyAsync cancelled");
                return false;
            } catch (Exception ex) {
                Logger.Error($"WaitForServerReadyAsync failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Return a human-readable version string for the indiserver process.
        /// May be empty if unknown. This is populated from the server's stdout
        /// banner or from the running process metadata where available.
        /// </summary>
        public string GetServerVersionString() {
            if (!string.IsNullOrWhiteSpace(_serverVersionString)) return _serverVersionString;
            // Fallback: return a tiny default if we have no info
            return IsConnected ? "indiserver (connected)" : "indiserver (unknown)";
        }

        /// <summary>
        /// Returns an optional platform version derived from the indiserver binary
        /// metadata (if readable). If not available returns Version(0,0,0,0).
        /// </summary>
        public Version GetServerPlatformVersion() {
            return _serverPlatformVersion ?? new Version(0, 0, 0, 0);
        }

        /// <summary>
        /// Wait for any discovered device matching a driver name to appear.
        /// This helps when drivers take a short time to announce themselves
        /// after being started via FIFO.
        /// </summary>
        public async Task<bool> WaitForDevicesForDriverAsync(string driverName, TimeSpan timeout, CancellationToken ct = default) {
            try {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                while (sw.Elapsed < timeout) {
                    ct.ThrowIfCancellationRequested();
                    lock (_lock) {
                        if (_discoveredDevices.Values.Any(d => !string.IsNullOrEmpty(d.Driver) && (d.Driver.Equals(driverName, StringComparison.OrdinalIgnoreCase) || d.Driver.Contains(driverName, StringComparison.OrdinalIgnoreCase)))) {
                            return true;
                        }
                    }
                    await Task.Delay(200, ct);
                }
                return false;
            } catch (OperationCanceledException) {
                return false;
            } catch (Exception ex) {
                Logger.Error($"WaitForDevicesForDriverAsync failed: {ex.Message}");
                return false;
            }
        }

        public void Dispose() {
            Logger.Info("INDIClient.Dispose() starting cleanup");
            CleanupServer();
            Logger.Info("INDIClient.Dispose() complete");
        }

        private void CheckForNewDevice(INDIProperty p) {
            if (p is INDITextProperty t) {
                // Device enumeration
                if (t.Name == "DRIVER_INFO") {
                    // Track this device
                    if (!_discoveredDevices.ContainsKey(t.DeviceName)) {
                        var id = p.DeviceName;

                        var name = string.Empty;
                        var exec = string.Empty;
                        var version = string.Empty;
                        var deviceInterface = 0;

                        foreach (var text in t.Texts) {
                            switch (text.Name) {
                                case "DRIVER_NAME":
                                    name = text.Value;
                                    break;
                                case "DRIVER_EXEC":
                                    exec = text.Value;
                                    break;
                                case "DRIVER_VERSION":
                                    version = text.Value;
                                    break;
                                case "DRIVER_INTERFACE":
                                    if (int.TryParse(text.Value, out var interfaceNumber)) {
                                        deviceInterface = interfaceNumber;
                                    }
                                    break;
                            }
                        }

                        Logger.Info($"Found {name} (INDI) device");

                        if (!_discoveredDevices.ContainsKey(t.DeviceName)) {
                            _discoveredDevices.Add(t.DeviceName, new INDIDeviceInfo {
                                Id = id,
                                Name = name,
                                Interface = (IndiDeviceInterface)deviceInterface,
                                Version = version,
                                Driver = exec
                            });
                        }
                    }

                    // Release tcs (moved outside to handle duplicate DRIVER_INFO messages)
                    lock (_operationLock) {
                        if (_operationTcs != null && !_operationTcs.Task.IsCompleted) {
                            _operationTcs.SetResult(true);
                            Logger.Debug($"LoadDriver operation completed for {t.DeviceName}");
                        }
                    }
                }
            }
        }

        #region XML

        private static string TrimXml(string xmlString) {
            if (string.IsNullOrEmpty(xmlString)) {
                return string.Empty;
            }
            return new string(xmlString.Where(XmlConvert.IsXmlChar).ToArray());
        }

        private void ProcessXmlMessage(StringBuilder message) {
            var xmlString = message.ToString();

            // Check for valid string
            if (string.IsNullOrEmpty(xmlString))
            {
                return;
            }
            
            int lastProcessed = 0;
            var elementsToProcess = new List<string>();

            while (lastProcessed < xmlString.Length) {
                // Find the next complete xml element
                var startTag = xmlString.IndexOf('<', lastProcessed);
                if (startTag == -1) {
                    break;
                }

                // Find the tag name
                var tagNameEnd = -1;
                for (int i = startTag + 1; i < xmlString.Length; i++) {
                    var c = xmlString[i];
                    if (c == ' ' || c == '>' || c == '/') {
                        tagNameEnd = i;
                        break;
                    }
                }

                if (tagNameEnd == -1) {
                    Logger.Debug("Tag name end not found, waiting for more data");
                    break;
                }

                var tagNameLength = tagNameEnd - startTag - 1;
                if (tagNameLength <= 0) {
                    Logger.Warning("Invalid tag name length, skipping");
                    lastProcessed = startTag + 1;
                    continue;
                }

                var tagName = xmlString.Substring(startTag + 1, tagNameLength);

                // Check for self-closing tags first (most common for INDI property updates)
                var selfClosingEnd = xmlString.IndexOf("/>", startTag, StringComparison.Ordinal);
                if (selfClosingEnd != -1) {
                    // Make sure this /> belongs to our tag (not nested inside)
                    var nextOpenTag = xmlString.IndexOf('<', startTag + 1);
                    if (nextOpenTag == -1 || selfClosingEnd < nextOpenTag) {
                        var elementEnd = selfClosingEnd + 2;
                        var xmlText = xmlString.Substring(startTag, elementEnd - startTag);
                        Logger.Debug($"Found self-closing element: {xmlText.Substring(0, Math.Min(100, xmlText.Length))}");
                        elementsToProcess.Add(xmlText);
                        lastProcessed = elementEnd;
                        continue;
                    }
                }

                // Try to find matching end tag
                var endTagStr = "</" + tagName + ">";
                var endIndex = xmlString.IndexOf(endTagStr, startTag + tagNameLength, StringComparison.Ordinal);

                if (endIndex == -1) {
                    // Incomplete element, wait for more data
                    Logger.Debug($"End tag {endTagStr} not found, waiting for more data");
                    break;
                }

                var elementEnd2 = endIndex + endTagStr.Length;
                var xmlText2 = xmlString.Substring(startTag, elementEnd2 - startTag);
                Logger.Debug($"Found complete element: {xmlText2.Substring(0, Math.Min(100, xmlText2.Length))}");
                elementsToProcess.Add(xmlText2);
                lastProcessed = elementEnd2;
            }

            // Remove processed data from buffer BEFORE parallel processing
            if (lastProcessed > 0) {
                message.Remove(0, lastProcessed);
            }

            // Now process all collected elements in parallel
            if (elementsToProcess.Count > 0) {
                Parallel.ForEach(elementsToProcess, xmlText => {
                    try {
                        Logger.Debug($"Parsing XML element: {xmlText.Substring(0, Math.Min(100, xmlText.Length))}...");
                        var element = XElement.Parse(xmlText);
                        ProcessElement(element);
                    } catch (System.Xml.XmlException ex) {
                        // Silently ignore incomplete buffer errors
                        if (!ex.Message.Contains("Unexpected end of file") &&
                            !ex.Message.Contains("Unexpected end tag") &&
                            !ex.Message.Contains("not closed")) {
                            Logger.Error($"XML parse error: {ex.Message}");
                        }
                    } catch (Exception ex) {
                        Logger.Error($"Error processing element: {ex.Message}");
                    }
                });
            }
        }

        private void ProcessElement(XElement element) {
            var deviceName = element.Attribute("device")?.Value ?? string.Empty;
            var propertyName = element.Attribute("name")?.Value ?? string.Empty;

            lock (_lock) {
                INDIProperty property;
                switch (element.Name.LocalName) {
                    case "defNumberVector": {
                            property = INDIProtocolParser.ParseDefNumberVector(element);
                            // Add property to registered device
                            if (_registeredDevices.TryGetValue(deviceName, out var deviceInstance)) {
                                deviceInstance.AddProperty(property);
                                // Immediately process initial value as an update
                                if (property is INDINumberProperty np)
                                    deviceInstance.OnNumberPropertyUpdated(np);
                            }
                            break;
                        }
                    case "defSwitchVector": {

                            property = INDIProtocolParser.ParseDefSwitchVector(element);
                            // Add property to registered device
                            if (_registeredDevices.TryGetValue(deviceName, out var deviceInstance)) {
                                deviceInstance.AddProperty(property);
                                // Immediately process initial value as an update
                                if (property is INDISwitchProperty sp)
                                    deviceInstance.OnSwitchPropertyUpdated(sp);
                            }
                            break;
                        }
                    case "defTextVector": {
                            property = INDIProtocolParser.ParseDefTextVector(element);
                            CheckForNewDevice(property);
                            // Add property to registered device
                            if (_registeredDevices.TryGetValue(deviceName, out var deviceInstance)) {
                                deviceInstance.AddProperty(property);
                                // Immediately process initial value as an update
                                if (property is INDITextProperty tp)
                                    deviceInstance.OnTextPropertyUpdated(tp);
                            }
                            break;
                        }
                    case "defBLOBVector": {
                            property = INDIProtocolParser.ParseDefBlobVector(element);
                            // Add property to registered device
                            if (_registeredDevices.TryGetValue(deviceName, out var deviceInstance)) {
                                deviceInstance.AddProperty(property);
                                // Immediately process initial value as an update
                                if (property is INDIBlobProperty bp)
                                    deviceInstance.OnBlobPropertyUpdated(bp);
                            }
                            break;
                        }
                    case "setBLOBVector": {
                            // Update registered device property if it exists
                            if (_registeredDevices.TryGetValue(deviceName, out var deviceInstance)) {
                                if (deviceInstance.GetProperty(propertyName) is INDIBlobProperty bp) {
                                    INDIProtocolParser.UpdateBlobProperty(bp, element);
                                    deviceInstance.OnBlobPropertyUpdated(bp);
                                }
                            }
                            break;
                        }
                    case "setTextVector": {
                            // Update registered device property if it exists
                            if (_registeredDevices.TryGetValue(deviceName, out var deviceInstance)) {
                                if (deviceInstance.GetProperty(propertyName) is INDITextProperty tp) {
                                    INDIProtocolParser.UpdateTextProperty(tp, element);
                                    deviceInstance.OnTextPropertyUpdated(tp);
                                }
                            }
                            break;
                        }
                    case "setNumberVector": {
                            // Update registered device property if it exists
                            if (_registeredDevices.TryGetValue(deviceName, out var deviceInstance)) {
                                if (deviceInstance.GetProperty(propertyName) is INDINumberProperty np) {
                                    INDIProtocolParser.UpdateNumberProperty(np, element);
                                    deviceInstance.OnNumberPropertyUpdated(np);
                                }
                            }
                            break;
                        }
                    case "setSwitchVector": {
                            // Update registered device property if it exists
                            if (_registeredDevices.TryGetValue(deviceName, out var deviceInstance)) {
                                if (deviceInstance.GetProperty(propertyName) is INDISwitchProperty sp) {
                                    INDIProtocolParser.UpdateSwitchProperty(sp, element);
                                    deviceInstance.OnSwitchPropertyUpdated(sp);
                                }
                            }
                            break;
                        }
                    case "delProperty": {
                            if (_registeredDevices.TryGetValue(deviceName, out var deviceInstance)) {
                                deviceInstance.RemoveProperty(propertyName);
                            }
                            break;
                        }
                    case "message": {
                            var message = element.Attribute("message")?.Value ?? element.Value;
                            Logger.Trace($"[INDI Message][{deviceName}] {message}");
                            break;
                        }
                }
            }
        }

        #endregion

        #region Server

        private Process _process;
        // Cached server info (read from the indiserver stdout or process metadata)
        private string _serverVersionString = string.Empty;
        private Version _serverPlatformVersion = null;
        private const string _fifoPath = "/tmp/indiFIFO";

        private async Task StartServerInFifoMode() {
            try {
                // Kill any existing indiserver processes
                KillExistingServer();

                // First create the FIFO (named pipe) if it doesnt exist
                CreateFifo(_fifoPath);

                var startInfo = new ProcessStartInfo {
                    FileName = "indiserver",
                    Arguments = $"-v -p {_port} -m 1000 -f {_fifoPath}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                var process = Process.Start(startInfo);
                if (process != null) {
                    Logger.Info($"INDI server started in FIFO mode with PID {process.Id}, port: {_port}, using FIFO: {_fifoPath}");

                    // Store the process reference for cleanup on app exit
                    _process = process;
                } else {
                    Logger.Error("Failed to start INDI server");
                    _serverReadyTcs.SetResult(false);
                    return;
                }

                // Wait for server to be ready with retries
                bool connected = false;
                for (int attempt = 1; attempt <= 10; attempt++) {
                    await Task.Delay(100 * attempt); // Exponential backoff: 100ms, 200ms, 300ms...

                    if (await INDIClient.Instance.Connect()) {
                        connected = true;
                        Logger.Info($"Connected to INDI server on attempt {attempt}");
                        break;
                    }

                    Logger.Debug($"Connection attempt {attempt} failed, retrying...");
                }

                if (!connected) {
                    Logger.Error("Could not connect to INDI server after 10 attempts");
                    _serverReadyTcs.SetResult(false);
                    return;
                }

                // Server is ready!
                _serverReadyTcs.SetResult(true);
            } catch (Exception ex) {
                Logger.Error($"Error to start INDI server: {ex.Message}");
                _serverReadyTcs.SetResult(false);
            }
        }

        private void CreateFifo(string fifoPath) {
            try {
                // Check if FIFO already exists
                if (File.Exists(fifoPath)) {
                    Logger.Info($"FIFO already exists: {fifoPath}");
                    return;
                }

                // Create FIFO using mkfifo command
                var mkfifoStartInfo = new ProcessStartInfo {
                    FileName = "mkfifo",
                    Arguments = fifoPath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var mkfifoProcess = Process.Start(mkfifoStartInfo);
                if (mkfifoProcess != null) {
                    mkfifoProcess.WaitForExit();
                    if (mkfifoProcess.ExitCode == 0) {
                        Logger.Info($"Successfully created FIFO: {fifoPath}");
                    } else {
                        Logger.Error($"mkfifo failed with exit code: {mkfifoProcess.ExitCode}");
                    }
                } else {
                    Logger.Error("Failed to start mkfifo process");

                }
            } catch (Exception ex) {
                Logger.Error($"Error creating FIFO {fifoPath}: {ex.Message}");
            }
        }

        private void CleanupServer() {
            // Disconnect will check if process is alive and skip graceful cleanup if not
            if (IsConnected) {
                Disconnect();
            }

            try {
                if (_process != null && !_process.HasExited) {
                    Logger.Info("Shutting down INDI server...");
                    _process.Kill();
                    _process.WaitForExit(5000);
                    _process.Dispose();
                    _process = null;
                    Logger.Info("INDI server shutdown complete");
                }

                // Clean up the FIFO
                if (File.Exists(_fifoPath)) {
                    try {
                        File.Delete(_fifoPath);
                        Logger.Info($"Cleaned up FIFO: {_fifoPath}");
                    } catch (Exception fifoEx) {
                        Logger.Warning($"Failed to delete FIFO {_fifoPath}: {fifoEx.Message}");
                    }
                }
            } catch (Exception ex) {
                Logger.Error($"Error shutting down INDI server: {ex.Message}");
            }
        }

        private void KillExistingServer() {
            try {
                // Use pkill to kill all indiserver processes
                var pkillStartInfo = new ProcessStartInfo {
                    FileName = "pkill",
                    Arguments = "-9 indiserver",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var pkillProcess = Process.Start(pkillStartInfo)) {
                    if (pkillProcess != null) {
                        pkillProcess.WaitForExit();
                        if (pkillProcess.ExitCode == 0) {
                            Logger.Info("Killed existing indiserver processes");
                        } else if (pkillProcess.ExitCode == 1) {
                            // Exit code 1 means no processes found, which is fine
                            Logger.Debug("No existing indiserver processes found");
                        } else {
                            Logger.Warning($"pkill returned exit code: {pkillProcess.ExitCode}");
                        }
                    }
                }
            } catch (Exception ex) {
                Logger.Error($"Error killing existing indiserver processes: {ex.Message}");
            }
        }

        #endregion
    }
}
