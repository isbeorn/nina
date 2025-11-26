#region "copyright"

/*
    Copyright © 2016 - 2025 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.INDI;
using NINA.INDI.Interfaces;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment {
    public abstract class IndiDevice<DeviceT> : BaseINPC, IDevice where DeviceT : IINDIDevice {

        private string indiRegistrationName;

        protected readonly INDIDeviceInfo _device;

        public IndiDevice(INDIDeviceInfo info) {
            this.Category = "INDI";
            _device = info;
            Id = info.Id;
            indiRegistrationName = info.Name;
            DisplayName = $"{info.Name} ({Category})";
        }

        protected DeviceT device;

        public string Category { get; private set; }
        protected abstract string ConnectionLostMessage { get; }

        protected object lockObj = new object();

        public string Id { get; }

        private string name;
        public string Name => name ?? indiRegistrationName;

        public string DisplayName { get; private set; }

        public string Description {
            get {
                try {
                    return device?.Description ?? "INDI Device";
                } catch (Exception) { }
                return string.Empty;
            }
        }

        public string DriverInfo {
            get {
                try {
                    return device?.DriverInfo ?? "INDI Driver";
                } catch (Exception) { }
                return string.Empty;
            }
        }

        public string DriverVersion {
            get {
                try {
                    return device?.DriverVersion ?? "1.0";
                } catch (Exception) { }
                return string.Empty;
            }
        }

        private bool connectedExpectation;

        private void DisconnectOnConnectionError() {
            try {
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
                Logger.Info($"{Name} reconnection failed");
                return false;
            }
            Logger.Info($"{Name} reconnection successful");
            return true;
        }

        protected bool ShouldBeConnected => connectedExpectation;

        public bool Connected {
            get {
                lock (lockObj) {
                    if (connectedExpectation && device != null) {
                        bool val = false;
                        bool expected = connectedExpectation;
                        try {
                            val = GetProperty(nameof(Connected), defaultValue: false, cacheInterval: TimeSpan.FromSeconds(1));
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
                    }
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

                    // Actually connect the INDI device
                    Connected = await device.Connect(token);

                    if (Connected) {
                        Logger.Trace($"{Name} - Calling PostConnect");

                        if (name == null) {
                            try {
                                // Update name of INDI after connection
                                name = string.IsNullOrEmpty(device?.Name) ? indiRegistrationName : device?.Name;
                                DisplayName = $"{name} (INDI)";
                            } catch {
                                name = indiRegistrationName;
                            }
                        }

                        await PostConnect();
                    } else {
                        Logger.Error($"Could not connect to {Name}");
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                    try {
                        Disconnect();
                    } catch { }
                }
                return Connected;
            });
        }

        protected abstract DeviceT GetInstance();

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
                name = null;
                DisplayName = $"{indiRegistrationName} ({Category})";
            } finally {
                try {
                    Dispose();
                } catch { }
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
        /// If a NotImplementedException occurs, the "isImplemetned" value will be set to false
        /// </summary>
        /// <typeparam name="PropT"></typeparam>
        /// <param name="propertyName">Property Name of the IndiDevice property</param>
        /// <param name="defaultValue">The default value to be returned when not connected or not implemented</param>
        /// <param name="cacheInterval">The minimum interval between actual polls to the device</param>
        /// <param name="rethrow">When set - any error will be rethrown and not handled internally</param>
        /// <param name="useLastKnownValueOnError">When rethrow is false and this is set, the last known value will be used as a fallback. Otherwise the errorValue parameter will be used</param>
        /// <param name="errorValue">The value to be returned when rethrow and useLastKnownValueOnError are both set to false and an error occurs during reading of the property</param>
        /// <returns></returns>
        protected PropT GetProperty<PropT>(string propertyName, PropT defaultValue, TimeSpan? cacheInterval = null, bool useLastKnownValueOnError = true, PropT errorValue = default) {
            if (device == null) { return defaultValue; }
            if (!ShouldBeConnected) { return defaultValue; }

            if (cacheInterval == null) { cacheInterval = TimeSpan.FromMilliseconds(100); }
            var interval = TimeSpan.FromMilliseconds(200);
            var type = device.GetType();

            // Get property without device state
            if (!propertyGETMemory.TryGetValue(propertyName, out var memory)) {
                memory = new PropertyMemory(type.GetProperty(propertyName), cacheInterval.Value);
                lock (propertyGETMemory) {
                    propertyGETMemory[propertyName] = memory;
                }
            }

            // Retry three times in normal conditions - disable retry when consecutive errors exceed threshold as it will not likely succeed on a retry anyways
            var retries = memory.ConsecutiveErrors >= memory.ConsecutiveErrorThreshold ? 1 : 3;

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
                    Logger.Trace(ex.Message);
                    return defaultValue;
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
        /// <param name="propertyName">Property Name of the IndiDevice property</param>
        /// <param name="value">The value to be set for the given property</param>
        /// <returns></returns>
        protected bool SetProperty<PropT>(string propertyName, PropT value, TimeSpan? cacheInterval = null, [CallerMemberName] string originalPropertyName = null) {
            if (device != null) {
                if (!ShouldBeConnected) { return false; }
                var type = device.GetType();

                if (!propertySETMemory.TryGetValue(propertyName, out var memory)) {
                    if (cacheInterval == null) { cacheInterval = TimeSpan.FromMilliseconds(100); }
                    memory = new PropertyMemory(type.GetProperty(propertyName), cacheInterval.Value);
                    lock (propertySETMemory) {
                        propertySETMemory[propertyName] = memory;
                    }
                }

                try {
                    if (memory.IsImplemented && ShouldBeConnected) {
                        memory.SetValue(device, value);
                        if (propertyGETMemory.TryGetValue(propertyName, out var getmemory)) {
                            getmemory.InvalidateCache();
                        }

                        Logger.Trace($"SET {type.Name}.{propertyName}: {value}");
                        return true;
                    } else {
                        return false;
                    }
                } catch (Exception ex) {
                    Logger.Error(ex.Message);
                    return false;
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

            public object GetValue(object device) {
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

            public void SetValue(object device, object value) {
                lock (lockObj) {
                    info.SetValue(device, value);
                }
            }
        }

        public bool HasSetupDialog => false;
        public IList<string> SupportedActions => new List<string>();
        public void SetupDialog() {
        }

        public string Action(string actionName, string actionParameters) {
            throw new System.NotImplementedException();
        }
        public void SendCommandBlind(string command, bool raw = true) {
            throw new System.NotImplementedException();
        }

        public bool SendCommandBool(string command, bool raw = true) {
            throw new System.NotImplementedException();
        }

        public string SendCommandString(string command, bool raw = true) {
            throw new System.NotImplementedException();
        }
    }
}
