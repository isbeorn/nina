#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.INDI.Enums;
using NINA.INDI.Protocol;
using NINA.INDI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Utility;

namespace NINA.INDI.Devices {

    [Flags]
    public enum IndiDeviceInterface {
        GENERAL_INTERFACE = 0,
        TELESCOPE_INTERFACE = (1 << 0),
        CCD_INTERFACE = (1 << 1),
        GUIDER_INTERFACE = (1 << 2),
        FOCUSER_INTERFACE = (1 << 3),
        FILTER_INTERFACE = (1 << 4),
        DOME_INTERFACE = (1 << 5),
        GPS_INTERFACE = (1 << 6),
        WEATHER_INTERFACE = (1 << 7),
        AO_INTERFACE = (1 << 8),
        DUSTCAP_INTERFACE = (1 << 9),
        LIGHTBOX_INTERFACE = (1 << 10),
        DETECTOR_INTERFACE = (1 << 11),
        ROTATOR_INTERFACE = (1 << 12),
        SPECTROGRAPH_INTERFACE = (1 << 13),
        AUX_INTERFACE = (1 << 14),
    }

    public class PropertyEventArgs : EventArgs {
        public INDIProperty Property { get; }

        public PropertyEventArgs(INDIProperty property) {
            Property = property;
        }
    }

    public class INDIDevice : IINDIDevice {

        private readonly INDIDeviceInfo _device;

        public INDIDevice(INDIDeviceInfo device) {
            _device = device;

            // Register device to receive property updates
            INDIClient.Instance.RegisterDevice(this);
        }

        private bool _connected;
        public bool Connected {
            get => _connected;
            set {
                if (_connected && !value) {
                    // Transitioning from connected to disconnected
                    Disconnect();
                }
                _connected = value;
            }
        }

        public string Category => "INDI Device";
        public string Id => _device.Id;
        public string DeviceName => _device.Name;
        public string Name => DeviceName;
        public string DisplayName => DeviceName;
        public string Description => $"INDI Device: {DeviceName}";
        public string DriverInfo => _device?.Driver ?? "INDI Driver";
        public string DriverVersion => _device?.Version ?? "1.0";

        private readonly Dictionary<string, INDIProperty> _properties = new();

        public void AddProperty(INDIProperty property) {
            lock (_properties) {
                var isNew = !_properties.ContainsKey(property.Name);
                _properties[property.Name] = property;
            }
        }

        public void RemoveProperty(string propertyName) {
            lock (_properties) {
                if (_properties.TryGetValue(propertyName, out var prop)) {
                    _properties.Remove(propertyName);
                }
            }
        }

        public INDIProperty GetProperty(string propertyName) {
            lock (_properties) {
                _properties.TryGetValue(propertyName, out var property);
                return property;
            }
        }

        public INDINumberProperty GetNumberProperty(string propertyName) {
            return GetProperty(propertyName) as INDINumberProperty;
        }

        public INDISwitchProperty GetSwitchProperty(string propertyName) {
            return GetProperty(propertyName) as INDISwitchProperty;
        }

        public INDITextProperty GetTextProperty(string propertyName) {
            return GetProperty(propertyName) as INDITextProperty;
        }

        public double? GetNumberPropertyValue(string propertyName, string elementName) {
            var prop = GetNumberProperty(propertyName);
            return prop?.Numbers.FirstOrDefault(n => n.Name == elementName)?.Value;
        }

        public bool? GetSwitchPropertyValue(string propertyName, string elementName) {
            var prop = GetSwitchProperty(propertyName);
            return prop?.Switches.FirstOrDefault(s => s.Name == elementName)?.Value;
        }

        public string GetTextPropertyValue(string propertyName, string elementName) {
            var prop = GetTextProperty(propertyName);
            return prop?.Texts.FirstOrDefault(t => t.Name == elementName)?.Value;
        }

        public void SetNumberValue(string propertyName, string elementName, double value) {
            var prop = GetNumberProperty(propertyName) ?? throw new ArgumentException($"Number property '{propertyName}' not found");
            if (prop == null) return;

            var number = prop.Numbers.FirstOrDefault(n => n.Name == elementName);
            if (number == null) return;

            number.Value = value;
            INDIClient.Instance.SendProperty(prop);
        }

        public void SetSwitchValue(string propertyName, string elementName, bool value) {
            var prop = GetSwitchProperty(propertyName) ?? throw new ArgumentException($"Switch property '{propertyName}' not found");
            if (prop == null) return;

            // Handle switch rules
            if (prop.Rule == SwitchRule.OneOfMany) {
                if (value) {
                    // Setting a switch to ON - turn off all others
                    foreach (var sw in prop.Switches) {
                        sw.Value = sw.Name == elementName;
                    }
                } else {
                    // Setting a switch to OFF in OneOfMany - need to turn on another one
                    // For CONNECTION property, if turning off CONNECT, turn on DISCONNECT and vice versa
                    string oppositeSwitch = null;
                    if (elementName == "CONNECT")
                        oppositeSwitch = "DISCONNECT";
                    else if (elementName == "DISCONNECT")
                        oppositeSwitch = "CONNECT";

                    if (oppositeSwitch != null) {
                        foreach (var sw in prop.Switches) {
                            sw.Value = sw.Name == oppositeSwitch;
                        }
                    } else {
                        // Generic case: turn on the first switch that isn't this one
                        foreach (var sw in prop.Switches) {
                            if (sw.Name != elementName) {
                                sw.Value = true;
                                break;
                            }
                        }
                        // Turn off the specified switch
                        var targetSw = prop.Switches.FirstOrDefault(s => s.Name == elementName);
                        if (targetSw != null) {
                            targetSw.Value = false;
                        }
                    }
                }
            } else if (prop.Rule == SwitchRule.AtMostOne) {
                if (value) {
                    // Turn off all other switches
                    foreach (var sw in prop.Switches) {
                        sw.Value = sw.Name == elementName;
                    }
                } else {
                    // Just turn off this switch, leave others as is
                    var sw = prop.Switches.FirstOrDefault(s => s.Name == elementName);
                    if (sw != null) {
                        sw.Value = false;
                    }
                }
            } else // AnyOfMany
              {
                var sw = prop.Switches.FirstOrDefault(s => s.Name == elementName);
                if (sw != null) {
                    sw.Value = value;
                }
            }

            INDIClient.Instance.SendProperty(prop);
        }

        public void SetSwitchProperty(string propertyName, Dictionary<string, bool> values) {
            var prop = GetSwitchProperty(propertyName) ?? throw new ArgumentException($"Switch property '{propertyName}' not found");
            if (prop == null) return;

            // Validate based on switch rule
            if (prop.Rule == SwitchRule.OneOfMany) {
                // Must have exactly one switch set to true
                var trueCount = values.Values.Count(v => v);
                if (trueCount != 1) {
                    throw new ArgumentException($"OneOfMany rule requires exactly one switch to be true, got {trueCount}");
                }
            } else if (prop.Rule == SwitchRule.AtMostOne) {
                // Can have at most one switch set to true
                var trueCount = values.Values.Count(v => v);
                if (trueCount > 1) {
                    throw new ArgumentException($"AtMostOne rule allows at most one switch to be true, got {trueCount}");
                }
            }
            // AnyOfMany has no restrictions

            // Apply the values
            foreach (var sw in prop.Switches) {
                if (values.TryGetValue(sw.Name, out bool value)) {
                    sw.Value = value;
                }
            }

            INDIClient.Instance.SendProperty(prop);
        }

        public void SetTextValue(string propertyName, string elementName, string value) {
            var prop = GetTextProperty(propertyName) ?? throw new ArgumentException($"Text property '{propertyName}' not found");
            if (prop == null) return;

            var text = prop.Texts.FirstOrDefault(t => t.Name == elementName);
            if (text == null) return;

            text.Value = value;
            INDIClient.Instance.SendProperty(prop);
        }

        private TaskCompletionSource<bool> _operationTcs;
        private readonly object _operationLock = new();

        public Task<bool> Connect(CancellationToken ct) {
            return Task.Run(async () => {
                if (Connected) {
                    Logger.Warning($"Device '{DeviceName}' is already connected");
                    return true;
                }

                Logger.Info($"Connecting to INDI device: {DeviceName}");

                // Initialize operation TCS
                lock (_operationLock) {
                    _operationTcs = new TaskCompletionSource<bool>();
                }

                // Send connect command
                if (!await INDIClient.Instance.ConnectDevice(_device, ct)) {
                    Logger.Info($"Failed to connect to device '{DeviceName}'");
                    return false;
                }

                try {
                    // Check token before we start
                    ct.ThrowIfCancellationRequested();

                    // Wait for the connection callback with timeout and cancellation support
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5), ct);
                    var completedTask = await Task.WhenAny(_operationTcs.Task, timeoutTask);

                    if (completedTask == timeoutTask) {
                        // Check if it was a timeout or cancellation
                        if (ct.IsCancellationRequested) {
                            Logger.Warning($"Connecting to {DeviceName} was cancelled");
                        } else {
                            Logger.Error($"Connecting to {DeviceName} timed out");
                        }
                        return false;
                    }

                    bool success = await _operationTcs.Task;
                    if (success) {
                        Logger.Info($"Connected to INDI device: {DeviceName}");

                        // Wait for initial property definitions to arrive from the driver
                        var requiredProps = GetRequiredConnectionProperties();
                        if (requiredProps != null && requiredProps.Length > 0) {
                            Logger.Debug($"Waiting for required properties: {string.Join(", ", requiredProps)}");

                            // Poll for properties with timeout
                            var propTimeout = DateTime.Now.AddSeconds(20);
                            while (!HasRequiredProperties(requiredProps) && DateTime.Now < propTimeout && !ct.IsCancellationRequested) {
                                await CoreUtil.Wait(TimeSpan.FromMilliseconds(200), ct);
                            }
                        }
                    } else {
                        Logger.Error($"Connecting to {DeviceName} failed");
                    }

                    Connected = success;
                    return success;
                } catch (OperationCanceledException) {
                    Logger.Warning($"Connecting to {DeviceName} was cancelled");
                    return false;
                } catch (Exception ex) {
                    Logger.Error(ex.Message);
                    return false;
                }
            });
        }

        public void Disconnect() {
            if (!_connected) {
                Logger.Warning($"Device '{DeviceName}' is not connected");
                return;
            }

            Logger.Info($"Disconnecting from INDI device: {DeviceName}");

            // Check if INDI client is still connected to server
            if (!INDIClient.Instance.IsConnected) {
                Logger.Info($"INDI server already disconnected, skipping graceful disconnect for {DeviceName}");
                _connected = false;
                return;
            }

            // Initialize operation TCS for disconnect
            lock (_operationLock) {
                _operationTcs = new TaskCompletionSource<bool>();
            }

            INDIClient.Instance.DisconnectDevice(_device);

            // Wait for disconnection synchronously to avoid race with Dispose()
            try {
                // Wait for the disconnection callback with shorter timeout (server may be dead)
                var completedTask = Task.WhenAny(_operationTcs.Task, Task.Delay(TimeSpan.FromSeconds(2))).Result;

                if (completedTask == _operationTcs.Task) {
                    bool isConnected = _operationTcs.Task.Result;
                    if (!isConnected) {
                        Logger.Info($"Disconnected from INDI device: {DeviceName}");
                    } else {
                        Logger.Warning($"Disconnect command completed but device reports still connected");
                    }
                } else {
                    Logger.Warning($"Disconnecting from {DeviceName} timed out (server may be disconnected)");
                }

                // Always try to unload driver after disconnect (or timeout)
                // Only try to unload driver if server is still connected
                if (INDIClient.Instance.IsConnected) {
                    INDIClient.Instance.UnloadDriver(_device.Driver);
                }
            } catch (Exception ex) {
                Logger.Error($"Error during disconnect: {ex.Message}");
            }

            // Update the backing field directly to avoid recursion
            _connected = false;
        }

        public void Dispose() {
            if (Connected) {
                Disconnect();
            }

            // Unregister device from client
            INDIClient.Instance.UnregisterDevice(this);
        }

        /// <summary>
        /// Override this to specify which properties must be received before Connect() completes.
        /// Return null/empty to skip waiting (uses fixed delay fallback).
        /// </summary>
        protected virtual string[] GetRequiredConnectionProperties() {
            return null;
        }

        /// <summary>
        /// Check if all required properties have been received
        /// </summary>
        private bool HasRequiredProperties(string[] requiredProps) {
            if (requiredProps == null || requiredProps.Length == 0) {
                return true;
            }

            lock (_properties) {
                foreach (var propName in requiredProps) {
                    if (!_properties.ContainsKey(propName)) {
                        return false;
                    }
                }
            }
            return true;
        }

        public virtual void OnSwitchPropertyUpdated(INDISwitchProperty p) {
            Logger.Info($"INDIDevice::OnSwitchPropertyUpdated({p.Name})");

            // Check for CONNECTION property updates (for device connection flow)
            if (p.Name == "CONNECTION") {
                var connectSwitch = p.Switches.FirstOrDefault(s => s.Name == "CONNECT");

                if (connectSwitch != null) {
                    bool isConnected = connectSwitch.Value;

                    // Complete the operation when:
                    // - State is Ok for successful connection
                    // - State is Idle for successful disconnection
                    // - Don't complete for Busy (operation in progress) or Alert (error)
                    if (p.State == PropertyState.Ok || (p.State == PropertyState.Idle && !isConnected)) {
                        lock (_operationLock) {
                            if (_operationTcs != null && !_operationTcs.Task.IsCompleted) {
                                Logger.Info($"Completing connection TCS with result: {isConnected} (state: {p.State})");
                                _operationTcs.SetResult(isConnected);
                            }
                        }
                    } else {
                        Logger.Debug($"CONNECTION property state is {p.State}, isConnected={isConnected}, waiting for completion state");
                    }
                }
            }
        }

        public virtual void OnNumberPropertyUpdated(INDINumberProperty p) {
        }

        public virtual void OnTextPropertyUpdated(INDITextProperty p) {
        }

        public virtual void OnBlobPropertyUpdated(INDIBlobProperty p) {
        }

        #region Unsupported
        public virtual IList<string> SupportedActions => new List<string>();

        public virtual string Action(string actionName, string actionParameters) {
            throw new NotImplementedException();
        }

        public virtual void CommandBlind(string command, bool raw = false) {
            throw new NotImplementedException();
        }

        public virtual bool CommandBool(string command, bool raw = false) {
            throw new NotImplementedException();
        }

        public virtual string CommandString(string command, bool raw = false) {
            throw new NotImplementedException();
        }
        #endregion
    }
}
