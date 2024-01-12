#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZWOptical.EFWSDK;
using NINA.Core.Model.Equipment;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Utility;

namespace NINA.Equipment.Equipment.MyFilterWheel {

    public class ASIFilterWheel : BaseINPC, IFilterWheel {
        private readonly int id;
        private readonly IProfileService profileService;
        private EFWdll.EFW_INFO efwInfo;

        public ASIFilterWheel(int idx, IProfileService profileService) {
            EFWdll.EFW_ERROR_CODE rv;
            this.profileService = profileService;

            _ = EFWdll.GetID(idx, out var id);
            this.id = id;

            // Keep trying to get properties if we are trying to connect to the filter wheel while it is initializing
            do {
                rv = EFWdll.GetProperty(id, out efwInfo);
            } while (rv == EFWdll.EFW_ERROR_CODE.EFW_ERROR_MOVING);

            if (string.IsNullOrEmpty(efwInfo.Name)) {
                Logger.Error($"EFW: Unable to get filter wheel properties for EFW at index {idx}: {rv}");
                return;
            }

            Name = efwInfo.Name;

            CalibrateEfwCommand = new AsyncCommand<bool>(CalibrateEfw);
        }

        public int[] FocusOffsets => Filters.Select((x) => x.FocusOffset).ToArray();

        public string[] Names => Filters.Select((x) => x.Name).ToArray();

        public bool Unidirectional {
            get {
                if (Connected) {
                    _ = EFWdll.GetDirection(efwInfo.ID, out var unidirectional);
                    return unidirectional;
                }

                return false;
            }

            set {
                if (Connected) {
                    Logger.Trace($"EFW: Setting Unidirectional to {value}");

                    _ = EFWdll.SetDirection(efwInfo.ID, value);
                    profileService.ActiveProfile.FilterWheelSettings.Unidirectional = value;
                    RaisePropertyChanged();
                }
            }
        }

        public IList<string> SupportedActions => new List<string>();

        private object lockObj = new object();
        public AsyncObservableCollection<FilterInfo> Filters {
            get {
                lock (lockObj) {
                    var filtersList = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
                    var positions = efwInfo.slotNum;

                    return new FilterManager().SyncFiltersWithPositions(filtersList, positions);
                }
            }
        }

        public bool HasSetupDialog => false;

        public string Id => string.IsNullOrEmpty(FilterWheelAlias) ? $"{Name} #{id}" : Name;

        public string Name { get; private set; }

        public string Category => "ZWOptical";

        private bool _connected = false;

        public bool Connected {
            get => _connected;
            private set {
                _connected = value;
                RaisePropertyChanged();
            }
        }

        public string Description => "Native driver for ZWOptical filter wheels";

        public string DriverInfo { get; private set; } = string.Empty;

        public string DriverVersion => "1.0";

        public short Position {
            get {
                var err = EFWdll.GetPosition(efwInfo.ID, out var position);
                if (err == EFWdll.EFW_ERROR_CODE.EFW_SUCCESS) {
                    return (short)position;
                } else {
                    Logger.Error($"EFW Communication error to get position {err}");
                    return -1;
                }
            }

            set {
                var err = EFWdll.SetPosition(efwInfo.ID, value);
                if (err != EFWdll.EFW_ERROR_CODE.EFW_SUCCESS) {
                    Logger.Error($"EFW Communication error during position change {err}");
                }
            }
        }

        public Task<bool> Connect(CancellationToken token) {
            return Task.Run(() => {
                EFWdll.EFW_ERROR_CODE rv;

                if (EFWdll.Open(id) == EFWdll.EFW_ERROR_CODE.EFW_SUCCESS) {
                    // Keep trying to get properties if we are trying to connect to the filter wheel while it is initializing
                    do {
                        rv = EFWdll.GetProperty(id, out efwInfo);
                    } while (rv == EFWdll.EFW_ERROR_CODE.EFW_ERROR_MOVING);

                    if (rv != EFWdll.EFW_ERROR_CODE.EFW_SUCCESS) {
                        Logger.Error($"EFW: Unable to get filter wheel properties for EFW at index {id}: {rv}");
                        return false;
                    }

                    DriverInfo = $"SDK: {EFWdll.GetSDKVersion()}; FW: {GetFwVersionString()}";
                    Connected = true;

                    Unidirectional = profileService.ActiveProfile.FilterWheelSettings.Unidirectional;

                    return true;
                } else {
                    Logger.Error("Failed to connect to EFW");
                    return false;
                };
            });
        }

        // ZWO device alias is limited to 8 ASCII characters. Initialize with something longer to know we haven't yet asked the device for it
        private string filterWheelAlias = "%%UNINITIALIZED%%";

        public string FilterWheelAlias {
            get {
                if (filterWheelAlias.Equals("%%UNINITIALIZED%%")) {
                    // We must connect to the filter wheel to get its ID. Quickly do this if we are not (such as during building the Chooser list)
                    if (!Connected) {
                        EFWdll.Open(efwInfo.ID);
                    }

                    filterWheelAlias = GetAlias();

                    if (!Connected) {
                        EFWdll.Close(efwInfo.ID);
                    }

                    Logger.Debug($"EFW: Filter wheel ID/Alias: {filterWheelAlias}");
                }

                return filterWheelAlias;
            }

            set {
                if (!CanGetSetAlias) { return; }

                Logger.Debug($"EFW: Setting Camera ID/Alias to: {value}");

                EFWdll.SetID(efwInfo.ID, value);
                filterWheelAlias = GetAlias();

                _ = EFWdll.GetProperty(efwInfo.ID, out efwInfo);
                Name = efwInfo.Name;

                Logger.Info($"EFW: Filter wheel ID/Alias set to: {filterWheelAlias}");

                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Name));
                RaisePropertyChanged(nameof(Id));
                profileService.ActiveProfile.FilterWheelSettings.Id = Id;
            }
        }

        public bool CanGetSetAlias {
            get {
                _ = EFWdll.GetFirmwareVersion(efwInfo.ID, out var major, out var minor, out var patch);

                if ((major * 100) + (minor * 10) + patch >= 309) {
                    return true;
                } else {
                    return false;
                }
            }
        }

        // Come on, ZWO. Make some proper management interfaces
        private string GetAlias() {

            if (!CanGetSetAlias) { return string.Empty; }

            _ = EFWdll.GetProperty(efwInfo.ID, out EFWdll.EFW_INFO info);

            if (info.Name.Contains('(') && info.Name.Contains(')') && info.Name.EndsWith(")")) {
                var openparen = info.Name.IndexOf('(');
                var closeparen = info.Name.LastIndexOf(')');
                var alias = closeparen - openparen;
                return info.Name.Substring(openparen + 1, alias - 1);
            } else {
                return string.Empty;
            }
        }

        private async Task<bool> CalibrateEfw(object arg) {
            return await Task.Run(async () => {
                EFWdll.EFW_ERROR_CODE rv;

                if (Connected) {
                    var currentPostion = Position;

                    rv = EFWdll.Calibrate(efwInfo.ID);

                    while (EFWdll.SetPosition(efwInfo.ID, currentPostion) == EFWdll.EFW_ERROR_CODE.EFW_ERROR_MOVING) {
                        await Task.Delay(TimeSpan.FromSeconds(2));
                    }

                    return true;
                } else {
                    return false;
                }
            });
        }

        private string GetFwVersionString() {
            _ = EFWdll.GetFirmwareVersion(efwInfo.ID, out var major, out var minor, out var patch);
            return $"{major}.{minor}.{patch}";
        }

        public void Disconnect() {
            _ = EFWdll.Close(id);
            this.Connected = false;
        }

        public IAsyncCommand CalibrateEfwCommand { get; }

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