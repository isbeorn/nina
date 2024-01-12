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
using ASCOM.Com.DriverAccess;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
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
        where DeviceT : ASCOMDevice {

        public AscomDevice(string id, string name) {
            Id = id;
            Name = name;
        }

        protected DeviceT device;
        public string Category { get; } = "ASCOM";
        protected abstract string ConnectionLostMessage { get; }

        protected object lockObj = new object();

        public bool HasSetupDialog => true;

        public string Id { get; }

        public string Name { get; }

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

        private bool connected;

        private void DisconnectOnConnectionError() {
            try {
                Notification.ShowWarning(ConnectionLostMessage);
                Disconnect();
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        public bool Connected {
            get {
                lock (lockObj) {
                    if (connected && device != null) {
                        bool val = false;

                        try {
                            bool expected;
                            val = device?.Connected ?? false;
                            expected = connected;
                            if (expected != val) {
                                Logger.Error($"{Name} should be connected but reports to be disconnected. Trying to reconnect...");
                                try {
                                    Connected = true;
                                    if (!device.Connected) {
                                        throw new NotConnectedException();
                                    }
                                    val = true;
                                    Logger.Info($"{Name} reconnection successful");
                                } catch (Exception ex) {
                                    Logger.Error("Reconnection failed. The device might be disconnected! - ", ex);
                                    DisconnectOnConnectionError();
                                }
                            }
                        } catch (Exception ex) {
                            Logger.Error(ex);
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
                        connected = value;
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
                    var concreteDevice = GetInstance(Id);
                    device = concreteDevice;

                    Connected = true;
                    if (Connected) {
                        Logger.Trace($"{Name} - Calling PostConnect");
                        await PostConnect();
                        RaiseAllPropertiesChanged();
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowExternalError(string.Format(Loc.Instance["LblUnableToConnect"], Name, ex.Message), Loc.Instance["LblASCOMDriverError"]);
                }
                return Connected;
            });
        }

        protected abstract DeviceT GetInstance(string id);

        public void SetupDialog() {
            if (HasSetupDialog) {
                try {
                    bool dispose = false;
                    if (device == null) {
                        Logger.Trace($"{Name} - Creating instance for {Id}");
                        var concreteDevice = GetInstance(Id);
                        device = concreteDevice;
                        dispose = true;
                    }
                    Logger.Trace($"{Name} - Creating Setup Dialog for {Id}");
                    device.SetupDialog();
                    if (dispose) {
                        device.Dispose();
                        device = null;
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowExternalError(ex.Message, Loc.Instance["LblASCOMDriverError"]);
                }
            }
        }

        public void Disconnect() {
            lock (lockObj) {
                Logger.Info($"Disconnecting from {Id} {Name}");
                Logger.Trace($"{Name} - Calling PreDisconnect");
                PreDisconnect();
                try {
                    Connected = false;
                } catch (Exception ex) {
                    Logger.Error(ex);
                }
                connected = false;
                Logger.Trace($"{Name} - Calling PostDisconnect");
                PostDisconnect();
                Dispose();
            }
            RaiseAllPropertiesChanged();
        }

        public void Dispose() {
            Logger.Trace($"{Name} - Disposing device");
            device?.Dispose();
            device = null;
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
        /// <returns></returns>
        protected PropT GetProperty<PropT>(string propertyName, PropT defaultValue) {
            if (device != null) {
                var retries = 3;

                var interval = TimeSpan.FromMilliseconds(100);
                var type = device.GetType();

                if (!propertyGETMemory.TryGetValue(propertyName, out var memory)) {
                    memory = new PropertyMemory(type.GetProperty(propertyName));
                    lock (propertyGETMemory) {
                        propertyGETMemory[propertyName] = memory;
                    }
                }

                for (int i = 0; i < retries; i++) {
                    try {
                        if (i > 0) {
                            Thread.Sleep(interval);
                            Logger.Info($"Retrying to GET {type.Name}.{propertyName} - Attempt {i + 1} / {retries}");
                        }

                        if (memory.IsImplemented && Connected) {
                            PropT value = (PropT)memory.GetValue(device);

                            Logger.Trace($"GET {type.Name}.{propertyName}: {value}");

                            return (PropT)memory.LastValue;
                        } else {
                            return defaultValue;
                        }
                    } catch (Exception ex) {
                        if (ex is PropertyNotImplementedException || ex.InnerException is PropertyNotImplementedException
                            || ex is ASCOM.NotImplementedException || ex.InnerException is ASCOM.NotImplementedException
                            || ex is System.NotImplementedException || ex.InnerException is System.NotImplementedException) {
                            Logger.Info($"Property {type.Name}.{propertyName} GET is not implemented in this driver ({Name})");

                            memory.IsImplemented = false;
                            return defaultValue;
                        }

                        if (ex is NotConnectedException || ex.InnerException is NotConnectedException) {
                            Logger.Error($"{Name} is not connected ", ex.InnerException ?? ex);
                        }

                        var logEx = ex.InnerException ?? ex;
                        Logger.Error($"An unexpected exception occurred during GET of {type.Name}.{propertyName}: ", logEx);
                    }
                }

                var val = (PropT)memory.LastValue;
                Logger.Info($"GET {type.Name}.{propertyName} failed - Returning last known value {val}");
                return val;
            }
            return defaultValue;
        }

        /// <summary>
        /// Tries to set a property by its name. If an exception occurs it will be logged.
        /// If a ASCOM.NotImplementedException occurs, the "isImplemetned" value will be set to false
        /// </summary>
        /// <typeparam name="PropT"></typeparam>
        /// <param name="propertyName">Property Name of the AscomDevice property</param>
        /// <param name="value">The value to be set for the given property</param>
        /// <returns></returns>
        protected bool SetProperty<PropT>(string propertyName, PropT value, [CallerMemberName] string originalPropertyName = null) {
            if (device != null) {
                var type = device.GetType();

                if (!propertySETMemory.TryGetValue(propertyName, out var memory)) {
                    memory = new PropertyMemory(type.GetProperty(propertyName));
                    lock (propertySETMemory) {
                        propertySETMemory[propertyName] = memory;
                    }
                }

                try {
                    if (memory.IsImplemented && Connected) {
                        memory.SetValue(device, value);

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

            public PropertyMemory(PropertyInfo p) {
                info = p;
                IsImplemented = true;

                LastValue = null;
                if (p.PropertyType.IsValueType) {
                    LastValue = Activator.CreateInstance(p.PropertyType);
                }
            }

            private PropertyInfo info;
            public bool IsImplemented { get; set; }
            public object LastValue { get; set; }

            public object GetValue(DeviceT device) {
                var value = info.GetValue(device);

                LastValue = value;

                return value;
            }

            public void SetValue(DeviceT device, object value) {
                info.SetValue(device, value);
            }
        }
    }
}