#region "copyright"

/*
    Copyright © 2016 - 2025 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using CommunityToolkit.Mvvm.Input;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Utility;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyFilterWheel {

    public partial class ToupTekAlikeFilterWheel : BaseINPC, IFilterWheel {
        private readonly string internalId;
        private IToupTekAlikeCameraSDK sdk;

        public ToupTekAlikeFilterWheel(ToupTekAlikeDeviceInfo deviceInfo, IToupTekAlikeCameraSDK sdk, IProfileService profileService) {
            Category = sdk.Category;

            this.profileService = profileService;
            this.sdk = sdk;
            this.internalId = deviceInfo.id;
            this.Id = Category + "_" + deviceInfo.id;

            this.Name = deviceInfo.displayname;

            var match = IdExtractorRegex().Match(deviceInfo.id);

            this.Description = $"{Category} filterwheel.";
            if (match.Success) {
                var vid = match.Groups[1].Value;
                var pid = match.Groups[2].Value;
                var tail = match.Groups[3].Value;
                this.Description += $" Vendor ID: {vid}, Product ID: {pid}, Filterwheel ID: {tail}";
            }
        }

        [GeneratedRegex(@"vid_([0-9a-fA-F]+)&pid_([0-9a-fA-F]+)#([^\\]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex IdExtractorRegex();

        private IProfileService profileService;

        public string Category { get; }

        public int[] FocusOffsets => Filters.Select((x) => x.FocusOffset).ToArray();

        public string[] Names => Filters.Select((x) => x.Name).ToArray();

        private bool unidirectional;
        public bool Unidirectional {
            get => unidirectional;
            set {
                unidirectional = value;
                profileService.ActiveProfile.FilterWheelSettings.Unidirectional = value;
                RaisePropertyChanged();
            }
        }

        public IList<string> SupportedActions => new List<string>();

        private object lockObj = new object();
        public AsyncObservableCollection<FilterInfo> Filters {
            get {
                lock (lockObj) {
                    var filtersList = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
                    sdk.get_Option(ToupTekAlikeOption.OPTION_FILTERWHEEL_SLOT, out var positions);
                    return new FilterManager().SyncFiltersWithPositions(filtersList, positions);
                }
            }
        }

        public bool HasSetupDialog => false;

        private string id;
        public string Id {
            get => id;
            set {
                id = value;
                RaisePropertyChanged();
            }
        }

        private string name;
        public string Name {
            get => name;
            set {
                name = value;
                RaisePropertyChanged();
            }
        }

        public string DisplayName => $"{Name} ({(Id.Length > 8 ? Id[^8..] : Id)})";

        private bool _connected;
        public bool Connected {
            get => _connected;
            private set {
                _connected = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HasSetupDialog));
            }
        }

        private string description;
        public string Description {
            get => description;
            set {
                description = value;
                RaisePropertyChanged();
            }
        }

        public string DriverInfo => $"{Category} SDK";

        public string DriverVersion => sdk?.Version() ?? string.Empty;

        public short Position {
            get {
                sdk.get_Option(ToupTekAlikeOption.OPTION_FILTERWHEEL_POSITION, out var position);
                return (short)position;
            }
            set {
                var position = (Unidirectional ? 0 : 0x100) | (ushort)value;
                sdk.put_Option(ToupTekAlikeOption.OPTION_FILTERWHEEL_POSITION, position);
            }
        }

        public Task<bool> Connect(CancellationToken ct) {
            return Task<bool>.Run(async () => {
                var success = false;
                try {
                    SupportedActions.Clear();

                    sdk = sdk.Open(this.internalId);

                    // Read number of positions
                    sdk.get_Option(ToupTekAlikeOption.OPTION_FILTERWHEEL_SLOT, out var slotNum);

                    // Initialize filter wheel with number of positions
                    sdk.put_Option(ToupTekAlikeOption.OPTION_FILTERWHEEL_SLOT, slotNum);

                    // Initially reset filter wheel
                    sdk.put_Option(ToupTekAlikeOption.OPTION_FILTERWHEEL_POSITION, -1);

                    // Wait for calibration to finish
                    await WaitForReadyState(ct);

                    // Connected flag
                    Connected = true;

                    success = true;
                    var profile = profileService.ActiveProfile.FilterWheelSettings;
                    Unidirectional = profile.Unidirectional;

                    RaiseAllPropertiesChanged();
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
                }
                return success;
            });
        }

        private async Task WaitForReadyState(CancellationToken ct) {
            int readyState = -1;
            while (readyState == -1) {
                ct.ThrowIfCancellationRequested();
                sdk.get_Option(ToupTekAlikeOption.OPTION_FILTERWHEEL_POSITION, out readyState);
                await Task.Delay(TimeSpan.FromSeconds(2), ct);
            }
        }

        [RelayCommand]
        private async Task<bool> CalibrateAfw(object arg, CancellationToken ct) {
            return await Task.Run(async () => {
                if (Connected) {
                    var currentPostion = Position;

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                    var ct = cts.Token;

                    try {
                        sdk.put_Option(ToupTekAlikeOption.OPTION_FILTERWHEEL_POSITION, -1);

                        // Wait for calibration to finish
                        await WaitForReadyState(ct);

                        // Set initial position
                        sdk.put_Option(ToupTekAlikeOption.OPTION_FILTERWHEEL_POSITION, currentPostion);

                        // Wait for position to be set
                        await WaitForReadyState(ct);

                        return true;
                    } catch (OperationCanceledException) {
                        Logger.Error("Calibration of filter wheel timed out.");
                        Notification.ShowError("Calibration of filter wheel timed out.");
                    }
                }
                return false;
            });
        }

        public void Disconnect() {
            this.Connected = false;
            sdk.Close();
        }

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
