#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Locale;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Utility;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Astroasis.AstroasisSDK.AOFilterWheel;
using static NINA.Image.FileFormat.XISF.XISFImageProperty.Instrument;

namespace NINA.Equipment.Equipment.MyFilterWheel {
    public class OasisFilterWheel : BaseINPC, IFilterWheel {
        private readonly int id;
        private readonly IProfileService profileService;
        private CancellationTokenSource propertyMonitor;

        public OasisFilterWheel(int id, IProfileService profileService) {
            this.profileService = profileService;
            this.id = id;

            // Grab model and alias
            FilterWheelGetProductModel(id, out var model);
            filterWheelAlias = GetAlias();

            Logger.Debug($"OFW: Filter wheel ID/Alias: {filterWheelAlias}");

            Name = $"{model} ({filterWheelAlias})";
            CalibrateOfwCommand = new AsyncCommand<bool>(CalibrateOfw);
            SyncFocusOffsetsCommand = new AsyncCommand<bool>(SyncFocusOffsets);
            StoreFocusOffsetsCommand = new AsyncCommand<bool>(StoreFocusOffsets);
            SyncNamesCommand = new AsyncCommand<bool>(SyncNames);
            StoreNamesCommand = new AsyncCommand<bool>(StoreNames);
            SetId();
        }

        public int[] FocusOffsets => Filters.Select((x) => x.FocusOffset).ToArray();

        public string[] Names => Filters.Select((x) => x.Name).ToArray();

        public IList<string> SupportedActions => new List<string>();

        private object lockObj = new object();
        public AsyncObservableCollection<FilterInfo> Filters {
            get {
                lock (lockObj) {
                    var filtersList = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
                    _ = FilterWheelGetSlotNum(id, out var positions);

                    var filters = new FilterManager().SyncFiltersWithPositions(filtersList, positions);
                    profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters = filters;
                    return filters;
                }
            }
        }

        public bool HasSetupDialog => false;

        public string Id { get; private set; }
        private void SetId() {
            Id = $"{Category}_{Name}_{FilterWheelAlias}";
        }

        public string Name { get; private set; }
        public string DisplayName => Name;

        public string Category => "Oasis";

        private bool _connected = false;

        public bool Connected {
            get => _connected;
            private set {
                _connected = value;
                RaisePropertyChanged();
            }
        }

        public string Description => "Native driver for Oasis filter wheels";

        public string DriverInfo { get; private set; } = string.Empty;

        public string DriverVersion {
            get {
                _ = FilterWheelGetSDKVersion(out var version);
                return new string(version);
            }
        }

        private void DisconnectOnRemovedError() {
            try {
                Notification.ShowWarning(Loc.Instance["LblFilterwheelConnectionLost"]);
                Logger.Error($"Oasis filter wheel device was removed");
                Disconnect();
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        private int counter;
        public int Counter {
            get => counter;
            private set {
                if (counter != value) {
                    counter = value;
                    RaisePropertyChanged();
                }
            }
        }

        public short Position {
            get {
                if (!Connected) {
                    return -1;
                }
                var err = FilterWheelGetStatus(id, out var status);
                if (err == AOReturn.AO_SUCCESS) {
                    return (short)(status.filterPosition - 1);
                } else {
                    if (err == AOReturn.AO_ERROR_COMMUNICATION) {
                        Logger.Error($"Oasis communication error to get Position state {err}");
                        DisconnectOnRemovedError();
                    } else {
                        Logger.Error($"Oasis error to get position {err}");
                    }
                    return -1;
                }
            }
            set {
                if (FilterWheelSetPosition(id, value + 1) == AOReturn.AO_SUCCESS) {
                    WaitForReadyState();
                    Logger.Info($"OFW: Filter position set to {value}");
                    RaisePropertyChanged();
                } else {
                    Logger.Error($"OFW: Failed to set filter position to {value}");
                }
            }
        }

        public bool Autorun {
            get {
                if (!Connected) {
                    return false;
                }
                var err = FilterWheelGetConfig(id, out var config);
                if (err == AOReturn.AO_SUCCESS) {
                    return config.autorun != 0;
                } else {
                    if (err == AOReturn.AO_ERROR_COMMUNICATION) {
                        Logger.Error($"Oasis communication error to get config {err}");
                        DisconnectOnRemovedError();
                    } else {
                        Logger.Error($"Oasis error to get config {err}");
                    }
                    return false;
                }
            }
            set {
                OFWConfig config = new();
                config.mask = (uint)AOConfig.MASK_AUTORUN;
                config.autorun = value ? 1 : 0;
                if (FilterWheelSetConfig(id, ref config) == AOReturn.AO_SUCCESS) {
                    Logger.Info($"Oasis: Autorun set to: {value}");
                    RaisePropertyChanged();
                } else {
                    Logger.Error($"Oasis error to set autorun");
                }
            }
        }

        private bool hasTurboMode = false;
        public bool HasTurboMode => hasTurboMode;

        public bool TurboMode {
            get {
                if (!Connected || !HasTurboMode) {
                    return false;
                }
                var err = FilterWheelGetConfig(id, out var config);
                if (err == AOReturn.AO_SUCCESS) {
                    return config.turbo != 0;
                } else {
                    if (err == AOReturn.AO_ERROR_COMMUNICATION) {
                        Logger.Error($"Oasis communication error to get config {err}");
                        DisconnectOnRemovedError();
                    } else {
                        Logger.Error($"Oasis error to get config {err}");
                    }
                    return false;
                }
            }
            set {
                OFWConfig config = new();
                config.mask = (uint)AOConfig.MASK_TURBO;
                config.turbo = value ? 1 : 0;
                if (FilterWheelSetConfig(id, ref config) == AOReturn.AO_SUCCESS) {
                    Logger.Info($"Oasis: Turbo mode set to: {value}");
                    RaisePropertyChanged();
                } else {
                    Logger.Error($"Oasis error to set turbo mode");
                }
            }
        }

        public List<string> Speeds { get; } = new() { "Fast", "Normal", "Slow" };
        public int Speed {
            get {
                if (!Connected) {
                    return 0;
                }
                var err = FilterWheelGetConfig(id, out var config);
                if (err == AOReturn.AO_SUCCESS) {
                    return config.speed;
                } else {
                    if (err == AOReturn.AO_ERROR_COMMUNICATION) {
                        Logger.Error($"Oasis communication error to get config {err}");
                        DisconnectOnRemovedError();
                    } else {
                        Logger.Error($"Oasis error to get config {err}");
                    }
                    return 0;
                }
            }
            set {
                OFWConfig config = new();
                config.mask = (uint)AOConfig.MASK_SPEED;
                config.speed = value;
                if (FilterWheelSetConfig(id, ref config) == AOReturn.AO_SUCCESS) {
                    Logger.Info($"Oasis: Speed set to: {Speeds[value]}");
                    RaisePropertyChanged();
                } else {
                    Logger.Error($"Oasis error to set speed");
                }
            }
        }

        private double boardTemperature;
        public double BoardTemperature {
            get => boardTemperature;
            private set {
                if (boardTemperature != value) {
                    boardTemperature = value;
                    RaisePropertyChanged();
                }
            }
        }

        public Task<bool> Connect(CancellationToken token) {
            return Task.Run(() => {
                // Verify the filter wheel id actually exists
                int[] ids = new int[OFW_MAX_NUM];
                FilterWheelScan(out var count, ids);
                if (!ids.Take(count).Contains(id)) {
                    Notification.ShowError(Loc.Instance["LblOasisFilterWheelNotAvailableError"]);
                    Logger.Error("Selected Oasis filter wheel not available (disconnected?)");
                    return false;
                }
                if (FilterWheelOpen(id) == AOReturn.AO_SUCCESS) {
                    DriverInfo = $"SDK: {DriverVersion}; FW: {GetFwVersionString()}";

                    // Wait for initialization to complete
                    if (!WaitForReadyState(token).Result) {
                        return false;
                    }

                    // If autorun is disabled, load filter0 on connect
                    if (!Autorun) {
                        Position = 0;
                    }

                    // Fetch protocol version
                    OFWVersion version = new OFWVersion();
                    _ = FilterWheelGetVersion(id, out version);

                    // Turbo mode is supported since v1.1.0
                    if (version.protocal > ((1 << 24) | (1 << 16))) {
                        hasTurboMode = true;
                    }

                    // Start background task to monitor board temperature
                    propertyMonitor?.Cancel();
                    propertyMonitor?.Dispose();

                    propertyMonitor = CancellationTokenSource.CreateLinkedTokenSource(token);
                    var monitorCts = propertyMonitor;
                    var monitorToken = propertyMonitor.Token;

                    _ = Task.Run(async () => {
                        try {
                            while (!monitorToken.IsCancellationRequested) {
                                var err = FilterWheelGetStatus(id, out var status);
                                if (err == AOReturn.AO_SUCCESS) {
                                    // Update properties
                                    BoardTemperature = status.temperature / 100.0;
                                }
                                await Task.Delay(2000, monitorToken);
                            }
                        } catch (OperationCanceledException) {
                            // Expected when cancellation is requested
                            Logger.Debug("Oasis: property monitoring task canceled");
                        }
                    }, monitorToken);

                    Connected = true;
                    RaiseAllPropertiesChanged();

                    return true;
                } else {
                    Logger.Error("Failed to connect to OFW");
                    return false;
                }
            });
        }

        public List<string> States { get; } = new() { "Idle", "Moving", "Calibrating", "Benchmarking" };

        private int state = (int)AOStatus.STATUS_IDLE;
        public int State {
            get => state;
            private set {
                if (state != value) {
                    state = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(StateText));
                }
            }
        }

        public string StateText => State >= 0 && State < States.Count ? States[state] : "Unknown";

        private Task<bool> WaitForReadyState(CancellationToken ct = default) {
            OFWStatus status;
            AOReturn err;
            do {
                err = FilterWheelGetStatus(id, out status);
                State = (int)status.filterStatus;

                if (err == AOReturn.AO_ERROR_COMMUNICATION) {
                    Logger.Error($"Oasis communication error to get status {err}");
                    DisconnectOnRemovedError();
                    return Task.FromResult(false);
                } else if (err != AOReturn.AO_SUCCESS) {
                    Logger.Error("Failed to query OFW status");
                    return Task.FromResult(false);
                }

                CoreUtil.Wait(TimeSpan.FromMilliseconds(100), ct);
            } while (status.filterStatus != AOStatus.STATUS_IDLE && !ct.IsCancellationRequested);
            State = (int)status.filterStatus;

            // Update counter
            Counter = status.seq;

            return Task.FromResult(true);
        }

        private string filterWheelAlias;

        public string FilterWheelAlias {
            get => filterWheelAlias;
            set {
                Logger.Debug($"OFW: Setting FilterWheel ID/Alias to: {value}");

                FilterWheelSetFriendlyName(id, value);
                filterWheelAlias = GetAlias();

                FilterWheelGetProductModel(id, out var model);
                Name = $"{model} ({filterWheelAlias})";
                SetId();

                Logger.Info($"OFW: Filter wheel ID/Alias set to: {filterWheelAlias}");

                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Name));
                RaisePropertyChanged(nameof(DisplayName));
                RaisePropertyChanged(nameof(Id));
                profileService.ActiveProfile.FilterWheelSettings.Id = Id;
            }
        }

        private string GetAlias() {
            FilterWheelGetFriendlyName(id, out var name);
            return name;
        }

        private async Task<bool> CalibrateOfw(object arg) {
            return await Task.Run(async () => {
                if (Connected) {
                    var currentPostion = Position;
                    var err = FilterWheelCalibrate(id, 0);
                    if(err == AOReturn.AO_ERROR_COMMUNICATION) {
                        Logger.Error($"Oasis communication error to start calibration {err}");
                        DisconnectOnRemovedError();
                        return false;
                    } else if (err != AOReturn.AO_SUCCESS) {
                        Logger.Error("OFW: Failed to start calibration");
                        return false;
                    }

                    await WaitForReadyState();

                    Position = currentPostion;

                    return true;
                } else {
                    return false;
                }
            });
        }

        private async Task<bool> StoreFocusOffsets(object arg) {
            return await Task.Run(() => {
                if (Connected) {
                    var err = FilterWheelSetFocusOffset(id, FocusOffsets.Length, FocusOffsets);
                    if (err == AOReturn.AO_ERROR_COMMUNICATION) {
                        Logger.Error($"Oasis communication error to store focus offsets {err}");
                        DisconnectOnRemovedError();
                        return false;
                    } else if (err != AOReturn.AO_SUCCESS) {
                        Logger.Error("OFW: Failed to store focus offsets");
                        return false;
                    }
                    return true;
                } else {
                    return false;
                }
            });
        }

        private async Task<bool> SyncFocusOffsets(object arg) {
            return await Task.Run(() => {
                if (Connected) {
                    var err = FilterWheelGetFocusOffset(id, FocusOffsets.Length, out var offsets);
                    if (err == AOReturn.AO_ERROR_COMMUNICATION) {
                        Logger.Error($"Oasis communication error to sync focus offsets {err}");
                        DisconnectOnRemovedError();
                        return false;
                    } else if (err != AOReturn.AO_SUCCESS) {
                        Logger.Error("OFW: Failed to sync focus offsets");
                        return false;
                    }
                    foreach (var filter in Filters) {
                        filter.FocusOffset = offsets[filter.Position];
                    }
                    return true;
                } else {
                    return false;
                }
            });
        }

        private async Task<bool> StoreNames(object arg) {
            return await Task.Run(() => {
                if (Connected) {
                    foreach (var filter in Filters) {
                        var err = FilterWheelSetSlotName(id, filter.Position + 1, filter.Name);
                        if (err == AOReturn.AO_ERROR_COMMUNICATION) {
                            Logger.Error($"Oasis communication error to store name {err}");
                            DisconnectOnRemovedError();
                            return false;
                        } else if (err != AOReturn.AO_SUCCESS) {
                            Logger.Error("OFW: Failed to store name");
                            return false;
                        }
                    }
                    return true;
                } else {
                    return false;
                }
            });
        }

        private async Task<bool> SyncNames(object arg) {
            return await Task.Run(() => {
                if (Connected) {
                    foreach (var filter in Filters) {
                        var err = FilterWheelGetSlotName(id, filter.Position + 1, out var name);
                        if (err == AOReturn.AO_ERROR_COMMUNICATION) {
                            Logger.Error($"Oasis communication error to sync name {err}");
                            DisconnectOnRemovedError();
                            return false;
                        } else if (err != AOReturn.AO_SUCCESS) {
                            Logger.Error("OFW: Failed to sync name");
                            return false;
                        }
                        filter.Name = name;
                    }
                    return true;
                } else {
                    return false;
                }
            });
        }

        private string GetFwVersionString() {
            OFWVersion version = new OFWVersion();
            _ = FilterWheelGetVersion(id, out version);

            uint major = (version.firmware >> 24) & 0xFF;
            uint minor = (version.firmware >> 16) & 0xFF;
            uint patch = (version.firmware >> 8) & 0xFF;

            return $"{major}.{minor}.{patch}";
        }

        public void Disconnect() {
            try {
                // Cancel property monitoring
                propertyMonitor?.Cancel();
                propertyMonitor?.Dispose();
                propertyMonitor = null;

                _ = FilterWheelClose(id);
                Connected = false;
            } catch (Exception ex) {
                Logger.Error($"Error on disconnect: {ex.Message}");
            }
        }

        public IAsyncCommand CalibrateOfwCommand { get; }
        public IAsyncCommand SyncFocusOffsetsCommand { get; }
        public IAsyncCommand StoreFocusOffsetsCommand { get; }
        public IAsyncCommand SyncNamesCommand { get; }
        public IAsyncCommand StoreNamesCommand { get; }

        public void SetupDialog() {
        }

        public string Action(string actionName, string actionParameters) {
            throw new NotImplementedException();
        }

        public string SendCommandString(string command, bool raw) {
            throw new NotImplementedException();
        }

        public bool SendCommandBool(string command, bool raw) {
            throw new NotImplementedException();
        }

        public void SendCommandBlind(string command, bool raw) {
            throw new NotImplementedException();
        }
    }
}
