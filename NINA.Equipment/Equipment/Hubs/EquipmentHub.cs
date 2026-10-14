#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using CommunityToolkit.Mvvm.ComponentModel;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.Hubs {

    public abstract partial class EquipmentHub : BaseINPC, IEquipmentHub {
        private HubDevice[] devices = Array.Empty<HubDevice>();
        private HubDeviceStatus[] statuses = Array.Empty<HubDeviceStatus>();
        private readonly List<HubDevice> attempted = new List<HubDevice>();
        private readonly List<HubDevice> pendingCleanup = new List<HubDevice>();
        private HubDevice[] sessionDevices = Array.Empty<HubDevice>();
        private int sessionVersion;
        private bool connecting;

        protected EquipmentHub(string id, string name, string category) {
            Id = id;
            Name = name;
            Category = category;
        }

        public string Id { get; }

        public string Name { get; }

        public string DisplayName => Name;

        public string Category { get; }

        public string Description => Name;

        public string DriverInfo => Name;

        public string DriverVersion => typeof(EquipmentHub).Assembly.GetName().Version.ToString();

        public bool HasSetupDialog => false;

        [ObservableProperty]
        public partial bool IsSessionActive { get; private set; }

        public IReadOnlyList<HubDeviceStatus> DeviceStatuses => Array.AsReadOnly(statuses);

        protected IReadOnlyList<HubDevice> Devices => devices;

        protected IReadOnlyList<HubDevice> SessionDevices => sessionDevices;

        protected int SessionVersion => sessionVersion;

        protected bool Connecting => connecting;

        public abstract bool Connected { get; }

        protected void ConfigureDevices(IReadOnlyList<HubDevice> configuredDevices) {
            if (IsSessionActive || connecting) {
                throw new InvalidOperationException();
            }

            devices = configuredDevices?.ToArray() ?? Array.Empty<HubDevice>();
            statuses = devices.Select(x => new HubDeviceStatus(x.SourceNumber, x.Device?.Id ?? string.Empty)).ToArray();
            RaisePropertyChanged(nameof(DeviceStatuses));
        }

        protected abstract bool IsSupportedDevice(IDevice device);

        protected virtual IEnumerable<HubDevice> DevicesToConnect => devices;

        protected virtual bool AllowPartialConnection => false;

        protected virtual void SessionConnected() {
        }

        protected virtual void SessionEnded() {
        }

        protected virtual void ChildChanged(HubDevice child, PropertyChangedEventArgs e) {
            RaisePropertyChanged(nameof(Connected));
        }

        protected HubDeviceStatus GetStatus(HubDevice child) {
            var index = Array.IndexOf(devices, child);
            return index >= 0 ? statuses[index] : null;
        }

        protected void Report(HubDevice child, string error = "") {
            RefreshConnectionStatus(child);
            var status = GetStatus(child);
            if (status != null) {
                status.Error = error;
            }
        }

        public void RefreshConnectionStatuses() {
            foreach (var child in devices) {
                RefreshConnectionStatus(child);
            }
        }

        protected bool RefreshConnectionStatus(HubDevice child) {
            var status = GetStatus(child);
            if (status == null) {
                return false;
            }

            bool connected = false;
            string error = string.Empty;
            if (IsSessionActive && sessionDevices.Contains(child)) {
                try {
                    connected = child.Device.Connected;
                    if (!connected) {
                        error = Loc.Instance["LblEquipmentHubDeviceDisconnected"];
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                    error = ex.Message;
                }
            }

            status.Connected = connected;
            status.ConnectionError = error;
            return connected;
        }

        protected bool ReadConnected(IDevice device) {
            try {
                return device?.Connected == true;
            } catch (Exception ex) {
                Logger.Error(ex);
                return false;
            }
        }

        public async Task<bool> Connect(CancellationToken token) {
            if (IsSessionActive || connecting) {
                return Connected;
            }

            foreach (var status in statuses) {
                status.Connected = false;
                status.Error = string.Empty;
                status.ConnectionError = string.Empty;
            }

            IsSessionActive = true;
            connecting = true;
            int version = ++sessionVersion;
            var selected = DevicesToConnect.ToArray();
            bool valid = selected.Length > 0
                && devices.All(x => x.Device != null && !string.IsNullOrEmpty(x.Device.Id))
                && devices.Select(x => x.Device?.Id).Distinct(StringComparer.Ordinal).Count() == devices.Length
                && devices.Select(x => x.SourceNumber).Distinct().Count() == devices.Length
                && selected.All(x => (AllowPartialConnection || IsSupportedDevice(x.Device))
                    && (AllowPartialConnection || x.Device is not OfflineDevice)
                    && x.Device is not IEquipmentHub
                    && !x.Device.Id.StartsWith("NINA.SafetyMonitor.Hub", StringComparison.Ordinal)
                    && !x.Device.Id.StartsWith("NINA.Switch.Hub", StringComparison.Ordinal)
                    && !x.Device.Id.StartsWith("NINA.Weather.Hub", StringComparison.Ordinal)
                    && (AllowPartialConnection || x.Device.Id != "No_Device"));
            if (!valid) {
                var error = Loc.Instance["LblEquipmentHubInvalidConfiguration"];
                if (devices.All(device => string.IsNullOrEmpty(device.Device?.Id) || device.Device.Id == "No_Device")) {
                    error = Loc.Instance["LblEquipmentHubNoDevices"];
                } else if (selected.Length == 0) {
                    error = Loc.Instance["LblEquipmentHubNoSources"];
                }

                foreach (var status in statuses) {
                    status.Connected = false;
                    status.Error = error;
                }

                connecting = false;
                IsSessionActive = false;
                Notification.ShowError($"{Name}: {error}");
                return false;
            }

            bool sessionEstablished = false;
            try {
                foreach (var child in selected) {
                    token.ThrowIfCancellationRequested();
                    EnsureCurrentSession(version);
                    attempted.Add(child);
                    bool success;
                    try {
                        success = IsSupportedDevice(child.Device) && child.Device.Id != "No_Device" && child.Device is not OfflineDevice && await child.Device.Connect(token);
                        success = success && ReadConnected(child.Device);
                        if (!success) {
                            Report(child, Loc.Instance["LblEquipmentHubConnectionFailed"]);
                        }
                    } catch (OperationCanceledException) {
                        if (!attempted.Contains(child)) {
                            DisconnectChild(child);
                        }

                        throw;
                    } catch (Exception ex) {
                        Logger.Error(ex);
                        Report(child, ex.Message);
                        success = false;
                    }

                    if (version != sessionVersion || !IsSessionActive) {
                        DisconnectChild(child);
                    }

                    token.ThrowIfCancellationRequested();
                    EnsureCurrentSession(version);
                    if (success) {
                        sessionDevices = sessionDevices.Append(child).ToArray();
                        Report(child);
                    }

                    if (!success) {
                        if (!AllowPartialConnection) {
                            return FailConnection();
                        }

                        DisconnectChild(child);
                        attempted.Remove(child);
                    }
                }

                if (attempted.Count == 0) {
                    return FailConnection();
                }

                sessionDevices = attempted.ToArray();
                foreach (var child in sessionDevices) {
                    child.Device.PropertyChanged += OnChildChanged;
                }

                SessionConnected();
                RaisePropertyChanged(nameof(Connected));
                if (!Connected) {
                    return FailConnection();
                }

                sessionEstablished = true;
                return true;
            } catch (OperationCanceledException) {
                CleanupAttempted();
                throw;
            } catch (Exception ex) {
                Logger.Error(ex);
                foreach (var child in attempted) {
                    Report(child, ex.Message);
                }

                return FailConnection();
            } finally {
                connecting = false;
                if (!sessionEstablished) {
                    IsSessionActive = false;
                }

                RaisePropertyChanged(nameof(Connected));
            }
        }

        private bool FailConnection() {
            var errors = devices.Select(child => {
                var error = GetStatus(child).Error;
                return string.IsNullOrEmpty(error) ? null : $"{child.Device.DisplayName}: {error}";
            }).Where(error => !string.IsNullOrEmpty(error)).ToArray();
            var message = errors.Length == 0
                ? Loc.Instance["LblEquipmentHubSessionFault"]
                : string.Join(Environment.NewLine, errors);

            CleanupAttempted();
            Notification.ShowError($"{Name}: {message}");
            return false;
        }

        private void EnsureCurrentSession(int version) {
            if (!IsSessionActive || version != sessionVersion) {
                throw new OperationCanceledException();
            }
        }

        private void OnChildChanged(object sender, PropertyChangedEventArgs e) {
            foreach (var child in devices.Where(x => ReferenceEquals(x.Device, sender))) {
                ChildChanged(child, e);
            }
        }

        private void DisconnectChild(HubDevice child) {
            sessionDevices = sessionDevices.Where(device => device != child).ToArray();
            child.Device.PropertyChanged -= OnChildChanged;
            try {
                child.Device.Disconnect();
                pendingCleanup.Remove(child);
            } catch (Exception ex) {
                Logger.Error(ex);
                Report(child, ex.Message);
                if (!pendingCleanup.Contains(child)) {
                    pendingCleanup.Add(child);
                }
            }

            var index = Array.IndexOf(devices, child);
            if (index >= 0) {
                statuses[index].Connected = false;
            }
        }

        private void CleanupAttempted() {
            foreach (var child in attempted.Concat(pendingCleanup).Distinct().ToArray()) {
                DisconnectChild(child);
            }

            attempted.Clear();
            sessionDevices = Array.Empty<HubDevice>();
            foreach (var status in statuses) {
                status.Connected = false;
                status.ConnectionError = string.Empty;
                status.Error = string.Empty;
            }

            SessionEnded();
            RaisePropertyChanged(nameof(Connected));
        }

        public void Disconnect() {
            ++sessionVersion;
            CleanupAttempted();
            IsSessionActive = false;
        }

        public IList<string> SupportedActions => Array.Empty<string>();

        public void SetupDialog() => throw new NotSupportedException();

        public string Action(string actionName, string actionParameters) => throw new NotSupportedException();

        public string SendCommandString(string command, bool raw = true) => throw new NotSupportedException();

        public bool SendCommandBool(string command, bool raw = true) => throw new NotSupportedException();

        public void SendCommandBlind(string command, bool raw = true) => throw new NotSupportedException();
    }
}
