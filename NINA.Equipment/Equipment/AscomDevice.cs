#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using ASCOM;
using ASCOM.Common;
using ASCOM.Common.DeviceInterfaces;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment {

    /// <summary>
    /// The unified class that handles the shared properties of all ASCOM devices like Connection, Generic Info and Setup
    /// </summary>
    public abstract class AscomDevice<DeviceT> : BaseINPC, IDevice
        where DeviceT : IAscomDeviceV2 {

        private string ascomRegistrationName;


        public AscomDevice(string id, string name) {
            Id = id;
            ascomRegistrationName = name;
            DisplayName = name;
            this.Category = "ASCOM";
        }

        public AscomDevice(ASCOM.Alpaca.Discovery.AscomDevice deviceMeta) : this(deviceMeta.UniqueId, deviceMeta.AscomDeviceName) {
            this.deviceMeta = deviceMeta;
            name = deviceMeta.AscomDeviceName;
            DisplayName = $"{Name} @ {deviceMeta.HostName} #{deviceMeta.AlpacaDeviceNumber}";
            this.Category = "ASCOM Alpaca";
        }

        public bool IsAlpacaDevice() {
            return deviceMeta != null;
        }

        protected readonly ASCOM.Alpaca.Discovery.AscomDevice deviceMeta;

        protected DeviceT device;
        public string Category { get; private set; }
        protected abstract string ConnectionLostMessage { get; }

        protected object lockObj = new object();

        public bool HasSetupDialog => !Connected;

        public string Id { get; }

        private string name;
        public string Name => name ?? ascomRegistrationName;

        public string DisplayName { get; private set; }

        public string Description {
            get {
                try {
                    return device?.Description ?? string.Empty;
                } catch (Exception) { }
                return string.Empty;
            }
        }

        public string DriverInfo {
            get {
                try {
                    return device?.DriverInfo ?? string.Empty;
                } catch (Exception) { }
                return string.Empty;
            }
        }

        public string DriverVersion {
            get {
                try {
                    return device?.DriverVersion ?? string.Empty;
                } catch (Exception) { }
                return string.Empty;
            }
        }

        private bool connectedExpectation;

        private void DisconnectOnConnectionError() {
            try {
                Notification.ShowWarning(ConnectionLostMessage);
                Disconnect();
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        private bool TryReconnect() {
            Connected = true;
            if (propertyGETMemory.TryGetValue(nameof(Connected), out var getmemory)) {
                getmemory.InvalidateCache();
            }
            if (!device.Connected) {
                throw new NotConnectedException();
            }            
            Logger.Info($"{Name} reconnection successful");
            return true;
        }

        public bool Connected {
            get {
                lock (lockObj) {
                    if (connectedExpectation && device != null) {
                        bool val = false;
                        bool expected = connectedExpectation;
                        try {
                            val = GetProperty(nameof(Connected), defaultValue: false, cacheInterval: TimeSpan.FromSeconds(1), rethrow: true);
                            if (expected != val) {
                                Logger.Error($"{Name} should be connected but reports to be disconnected. Trying to reconnect...");
                                try {
                                    val = TryReconnect();
                                } catch (Exception ex) {
                                    Logger.Error("Reconnection failed. The device might be disconnected! - ", ex.InnerException ?? ex);
                                    DisconnectOnConnectionError();
                                }
                            }
                        } catch (Exception ex) {
                            if (IsAlpacaDevice() && expected != val) {
                                Logger.Error($"{Name} should be connected but reports to be disconnected. Trying to reconnect...");
                                try {
                                    val = TryReconnect();
                                } catch (Exception ex2) {
                                    Logger.Error("Reconnection failed. The device might be disconnected! - ", ex2.InnerException ?? ex2);
                                    ex = ex2;
                                }
                            }
                            Logger.Error(ex.InnerException ?? ex);
                            DisconnectOnConnectionError();
                        }
                        return val;
                    } else {
                        return false;
                    }
                }
            }
            private set {
                lock (lockObj) {
                    if (device != null) {
                        Logger.Debug($"SET {Name} Connected to {value}");
                        device.Connected = value;
                        connectedExpectation = value;
                        if (propertyGETMemory.TryGetValue(nameof(Connected), out var getmemory)) {
                            getmemory.InvalidateCache();
                        }
                        RaisePropertyChanged(nameof(HasSetupDialog));
                    }
                }
            }
        }

        public IList<string> SupportedActions {
            get {
                try {
                    var list = device?.SupportedActions ?? new List<string>();
                    return list.Cast<object>().Select(x => x.ToString()).ToList();
                } catch (Exception) { }
                return new List<string>();
            }
        }

        public string Action(string actionName, string actionParameters) {
            if (Connected) {
                return device.Action(actionName, actionParameters);
            } else {
                return null;
            }
        }

        public string SendCommandString(string command, bool raw = true) {
            if (Connected) {
                lock (lockObj) {
                    return device.CommandString(command, raw);
                }
            } else {
                return null;
            }
        }

        public bool SendCommandBool(string command, bool raw = true) {
            if (Connected) {
                lock (lockObj) {
                    return device.CommandBool(command, raw);
                }
            } else {
                return false;
            }
        }

        public void SendCommandBlind(string command, bool raw = true) {
            if (Connected) {
                lock (lockObj) {
                    device.CommandBlind(command, raw);
                }
            }
        }

        /// <summary>
        /// Customizing hook called before connection
        /// </summary>
        protected virtual Task PreConnect() {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Customizing hook called after successful connection
        /// </summary>
        protected virtual Task PostConnect() {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Customizing hook called before disconnection
        /// </summary>
        protected virtual void PreDisconnect() {
        }

        /// <summary>
        /// Customizing hook called after disconnection
        /// </summary>
        protected virtual void PostDisconnect() {
        }

        public Task<bool> Connect(CancellationToken token) {
            return Task.Run(async () => {
                try {
                    propertyGETMemory = new Dictionary<string, PropertyMemory>();
                    propertySETMemory = new Dictionary<string, PropertyMemory>();
                    Logger.Trace($"{Name} - Calling PreConnect");
                    await PreConnect();

                    Logger.Trace($"{Name} - Creating instance for {Id}");
                    var concreteDevice = GetInstance();
                    device = concreteDevice;

                    Logger.Trace($"{Name} - Calling Connect for {Id}");

                    Connected = true;

                    if (Connected) {
                        Logger.Trace($"{Name} - Calling PostConnect");

                        if (name == null && !IsAlpacaDevice()) {
                            try {
                                // Update name of ASCOM after connection
                                name = string.IsNullOrEmpty(device?.Name) ? ascomRegistrationName : device?.Name;
                                DisplayName = $"{name} (ASCOM)";
                            } catch {
                                name = ascomRegistrationName;
                            }
                        }

                        await PostConnect();
                        RaiseAllPropertiesChanged();
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowExternalError(string.Format(Loc.Instance["LblUnableToConnect"], Name, ex.Message), Loc.Instance["LblASCOMDriverError"]);
                    try {
                        Disconnect();
                    } catch { }
                }
                return Connected;
            });
        }

        private DeviceTypes ToDeviceType() => device switch {
            ICameraV3 => DeviceTypes.Camera,
            IDomeV2 => DeviceTypes.Dome,
            IFilterWheelV2 => DeviceTypes.FilterWheel,
            ICoverCalibratorV1 => DeviceTypes.CoverCalibrator,
            IFocuserV3 => DeviceTypes.Focuser,
            IRotatorV3 => DeviceTypes.Rotator,
            ASCOM.Common.DeviceInterfaces.ISafetyMonitor => DeviceTypes.SafetyMonitor,
            ISwitchV2 => DeviceTypes.Switch,
            ITelescopeV3 => DeviceTypes.Telescope,
            IObservingConditions => DeviceTypes.ObservingConditions,
            _ => throw new ArgumentException("Unknown Device Type")
        };

        protected abstract DeviceT GetInstance();

        public void SetupDialog() {
            if (HasSetupDialog) {
                if (!IsAlpacaDevice()) {
                    // ASCOM
                    try {
                        bool dispose = false;
                        if (device == null) {
                            Logger.Trace($"{Name} - Creating instance for {Id}");
                            var concreteDevice = GetInstance();
                            device = concreteDevice;
                            dispose = true;
                        }
                        Logger.Trace($"{Name} - Creating Setup Dialog for {Id}");
                        var t = device.GetType();
                        var method = t.GetMethod("SetupDialog");
                        method.Invoke(device, null);
                        if (dispose) {
                            device.Dispose();
                            device = default;
                        }
                    } catch (Exception ex) {
                        Logger.Error(ex);
                        Notification.ShowExternalError(ex.Message, Loc.Instance["LblASCOMDriverError"]);
                    }
                } else {
                    // Alpaca
                    var protocol = deviceMeta.ServiceType == ASCOM.Common.Alpaca.ServiceType.Http ? "http" : "https";
                    var ipAddress = deviceMeta.IpAddress;
                    var port = deviceMeta.IpPort;
                    var deviceType = deviceMeta.AscomDeviceType.ToString().ToLower();
                    var deviceNumber = deviceMeta.AlpacaDeviceNumber;
                    var url = $"{protocol}://{ipAddress}:{port}/setup/v1/{deviceType}/{deviceNumber}/setup";
                    try {
                        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    } catch (Exception ex) {
                        Logger.Error(ex);
                        Notification.ShowError(ex.Message);
                    }
                }
            }
        }

        public void Disconnect() {
            try {
                Logger.Info($"Disconnecting from {Id} {Name}");
                Logger.Trace($"{Name} - Calling PreDisconnect");
                PreDisconnect();
                try {
                    Connected = false;
                } catch (Exception ex) {
                    Logger.Error(ex);
                }
                lock (lockObj) {
                    connectedExpectation = false;
                    InvalidatePropertyCache();
                }

                Logger.Trace($"{Name} - Calling PostDisconnect");
                PostDisconnect();
                if (!IsAlpacaDevice()) {
                    name = null;
                    DisplayName = ascomRegistrationName;
                }
                RaiseAllPropertiesChanged();
            } finally {
                try {
                    Dispose();
                } catch { }
                
            }
        }

        private void WaitForConnectingFlag() {
            var start = DateTimeOffset.UtcNow;
            while (device.Connecting && (DateTimeOffset.UtcNow - start) < TimeSpan.FromMinutes(1)) {
                Thread.Sleep(100);
            }
        }

        public void Dispose() {
            Logger.Trace($"{Name} - Disposing device");
            device?.Dispose();
            device = default;
        }

        protected Dictionary<string, PropertyMemory> propertyGETMemory = new Dictionary<string, PropertyMemory>();
        protected Dictionary<string, PropertyMemory> propertySETMemory = new Dictionary<string, PropertyMemory>();

        /// <summary>
        /// Tries to get a property by its name. If an exception occurs the last known value will be used instead.
        /// If an ASCOM.NotImplementedException occurs, the "isImplemetned" value will be set to false
        /// </summary>
        /// <typeparam name="PropT"></typeparam>
        /// <param name="propertyName">Property Name of the AscomDevice property</param>
        /// <param name="defaultValue">The default value to be returned when not connected or not implemented</param>
        /// <param name="cacheInterval">The minimum interval between actual polls to the device</param>
        /// <param name="rethrow">When set - any error will be rethrown and not handled internally</param>
        /// <param name="useLastKnownValueOnError">When rethrow is false and this is set, the last known value will be used as a fallback. Otherwise the errorValue parameter will be used</param>
        /// <param name="errorValue">The value to be returned when rethrow and useLastKnownValueOnError are both set to false and an error occurs during reading of the property</param>
        /// <returns></returns>
        protected PropT GetProperty<PropT>(string propertyName, PropT defaultValue, TimeSpan? cacheInterval = null, bool rethrow = false, bool useLastKnownValueOnError = true, PropT errorValue = default) {
            if (device != null) {
                var type = device.GetType();

                if (!propertyGETMemory.TryGetValue(propertyName, out var memory)) {
                    if (cacheInterval == null) { cacheInterval = TimeSpan.FromMilliseconds(100); }
                    memory = new PropertyMemory(type.GetProperty(propertyName), cacheInterval.Value);
                    lock (propertyGETMemory) {
                        propertyGETMemory[propertyName] = memory;
                    }
                }

                // Retry three times in normal conditions - disable retry when consecutive errors exceed threshold as it will not likely succeed on a retry anyways
                var retries = memory.ConsecutiveErrors >= memory.ConsecutiveErrorThreshold ? 1 : 3;
                var interval = TimeSpan.FromMilliseconds(200);

                for (int i = 0; i < retries; i++) {
                    try {
                        if (i > 0) {
                            Thread.Sleep(interval);
                            Logger.Info($"Retrying to GET {type.Name}.{propertyName} - Attempt {i + 1} / {retries}");
                        }

                        if (memory.IsImplemented) {
                            PropT value = (PropT)memory.GetValue(device);

                            Logger.Trace($"GET {type.Name}.{propertyName}: {value}");
                            memory.ConsecutiveErrors = 0;
                            return (PropT)memory.LastValue;
                        } else {
                            return defaultValue;
                        }
                    } catch (Exception ex) {
                        if (rethrow) { throw; }

                        memory.ConsecutiveErrors++;
                        if (memory.ConsecutiveErrors == memory.ConsecutiveErrorThreshold) {
                            Logger.Warning($"GET of {type.Name}.{propertyName} encountered {memory.ConsecutiveErrorThreshold} consecutive errors. Further logs for this property access are logged on TRACE level until the property access is successful again");
                        }

                        if (ex is PropertyNotImplementedException || ex.InnerException is PropertyNotImplementedException
                            || ex is ASCOM.NotImplementedException || ex.InnerException is ASCOM.NotImplementedException
                            || ex is System.NotImplementedException || ex.InnerException is System.NotImplementedException) {
                            Logger.Info($"Property {type.Name}.{propertyName} GET is not implemented in this driver ({Name})");

                            memory.IsImplemented = false;
                            return defaultValue;
                        }

                        var logEx = ex.InnerException ?? ex;

                        if (ex is NotConnectedException || ex.InnerException is NotConnectedException) {
                            if (memory.ConsecutiveErrors > memory.ConsecutiveErrorThreshold) {
                                Logger.Trace($"{Name} is not connected {logEx}");
                            } else {
                                Logger.Error($"{Name} is not connected ", logEx);
                            }
                        }

                        if (memory.ConsecutiveErrors > memory.ConsecutiveErrorThreshold) {
                            Logger.Trace($"An unexpected exception occurred during GET of {type.Name}.{propertyName} - Consecutive Errors: {memory.ConsecutiveErrors} - Error: {logEx.Message} {logEx.StackTrace}");
                        } else {
                            Logger.Error($"An unexpected exception occurred during GET of {type.Name}.{propertyName}: ", logEx);
                        }
                    }
                }

                // Polling the property failed for all retries
                var val = useLastKnownValueOnError ? (PropT)memory.LastValue : errorValue;
                if (memory.ConsecutiveErrors > memory.ConsecutiveErrorThreshold) {
                    Logger.Trace($"GET {type.Name}.{propertyName} failed - Returning {(useLastKnownValueOnError ? "last known" : "error")} value {val}");
                } else {
                    Logger.Info($"GET {type.Name}.{propertyName} failed - Returning {(useLastKnownValueOnError ? "last known" : "error")} value {val}");
                }
                return val;
            }
            return defaultValue;
        }

        protected void InvalidatePropertyCache() {
            if (propertyGETMemory?.Values?.Count > 0) {
                foreach (var property in propertyGETMemory.Values) {
                    property.InvalidateCache();
                }
            }
        }

        /// <summary>
        /// Tries to set a property by its name. If an exception occurs it will be logged.
        /// If a ASCOM.NotImplementedException occurs, the "isImplemetned" value will be set to false
        /// </summary>
        /// <typeparam name="PropT"></typeparam>
        /// <param name="propertyName">Property Name of the AscomDevice property</param>
        /// <param name="value">The value to be set for the given property</param>
        /// <returns></returns>
        protected bool SetProperty<PropT>(string propertyName, PropT value, TimeSpan? cacheInterval = null, [CallerMemberName] string originalPropertyName = null) {
            if (device != null) {
                var type = device.GetType();

                if (!propertySETMemory.TryGetValue(propertyName, out var memory)) {
                    if (cacheInterval == null) { cacheInterval = TimeSpan.FromMilliseconds(100); }
                    memory = new PropertyMemory(type.GetProperty(propertyName), cacheInterval.Value);
                    lock (propertySETMemory) {
                        propertySETMemory[propertyName] = memory;
                    }
                }

                try {
                    if (memory.IsImplemented && Connected) {
                        memory.SetValue(device, value);
                        if (propertyGETMemory.TryGetValue(propertyName, out var getmemory)) {
                            getmemory.InvalidateCache();
                        }

                        Logger.Trace($"SET {type.Name}.{propertyName}: {value}");
                        RaisePropertyChanged(originalPropertyName);
                        return true;
                    } else {
                        return false;
                    }
                } catch (Exception ex) {
                    if (ex is ASCOM.NotImplementedException || ex.InnerException is ASCOM.NotImplementedException
                            || ex is ASCOM.NotImplementedException || ex.InnerException is ASCOM.NotImplementedException
                            || ex is System.NotImplementedException || ex.InnerException is System.NotImplementedException) {
                        Logger.Info($"Property {type.Name}.{propertyName} SET is not implemented in this driver ({Name})");
                        memory.IsImplemented = false;
                        return false;
                    }

                    if (ex is InvalidValueException) {
                        Logger.Warning(ex.Message);
                        return false;
                    }

                    if (ex is ASCOM.InvalidOperationException) {
                        Logger.Warning(ex.Message);
                        return false;
                    }

                    if (ex is NotConnectedException || ex.InnerException is NotConnectedException) {
                        Logger.Error($"{Name} is not connected ", ex.InnerException ?? ex);
                        return false;
                    }

                    var message = ex.InnerException?.Message ?? ex.Message;
                    Logger.Error($"An unexpected exception occurred during SET of {type.Name}.{propertyName}: {message}");
                }
            }
            return false;
        }

        protected class PropertyMemory {

            public PropertyMemory(PropertyInfo p, TimeSpan cacheInterval) {
                info = p;
                this.cacheInterval = cacheInterval;
                IsImplemented = true;
                ConsecutiveErrors = 0;
                ConsecutiveErrorThreshold = 9;

                LastValue = null;
                if (p.PropertyType.IsValueType) {
                    LastValue = Activator.CreateInstance(p.PropertyType);
                }
                LastValueUpdate = DateTimeOffset.MinValue;
            }

            private object lockObj = new object();
            private PropertyInfo info;
            private readonly TimeSpan cacheInterval;

            public bool IsImplemented { get; set; }
            public object LastValue { get; set; }
            public DateTimeOffset LastValueUpdate { get; set; }

            public int ConsecutiveErrors { get; set; }

            public int ConsecutiveErrorThreshold;

            public void InvalidateCache() {
                LastValueUpdate = DateTimeOffset.MinValue;
            }

            public object GetValue(DeviceT device) {
                lock (lockObj) {
                    if ((DateTimeOffset.UtcNow - LastValueUpdate) < cacheInterval) {
                        return LastValue;
                    }

                    var value = info.GetValue(device);

                    LastValue = value;
                    LastValueUpdate = DateTimeOffset.UtcNow;

                    return value;
                }
            }

            public void SetValue(DeviceT device, object value) {
                lock (lockObj) {
                    info.SetValue(device, value);
                }
            }
        }
    }
}