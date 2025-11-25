#region "copyright"
/*
    Copyright © 2016 - 2025 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.Profile.Interfaces;

namespace NINA.Equipment.Equipment.MyFocuser {
    public partial class ToupTekAlikeFocuser : BaseINPC, IFocuser {
        private readonly IProfileService profileService;
        private string internalId;
        private IToupTekAlikeCameraSDK sdk;
        private static TimeSpan SameFocuserPositionTimeout = TimeSpan.FromMinutes(1);

        public ToupTekAlikeFocuser(ToupTekAlikeDeviceInfo deviceInfo, IToupTekAlikeCameraSDK toupSdk, IProfileService profileService) {
            this.profileService = profileService;
            sdk = toupSdk;
            Category = sdk.Category;
            internalId = deviceInfo.id;
            Id = $"{Category}_{deviceInfo.id}";

            Name = deviceInfo.displayname;

            var match = IdExtractorRegex().Match(deviceInfo.id);
            this.Description = $"{Category} Focuser";
            if (match.Success) {
                var vid = match.Groups[1].Value;
                var pid = match.Groups[2].Value;
                var tail = match.Groups[3].Value;
                this.Description += $" Vendor ID: {vid}, Product ID: {pid}, Filterwheel ID: {tail}";
            }
        }

        [GeneratedRegex(@"vid_([0-9a-fA-F]+)&pid_([0-9a-fA-F]+)#([^\\]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex IdExtractorRegex();

        public bool IsMoving {
            get {
                if (sdk.AAF(ToupTekAlikeAAF.AAF_ISMOVING, 0, out var moving)) {
                    return moving != 0;
                }
                // TODO disconnect on removed state
                Logger.Error("AAF error to get IsMoving state");
                return false;
            }
        }

        public int MaxIncrement => MaxStep;

        public int MaxStep {
            get {
                if (sdk.AAF(ToupTekAlikeAAF.AAF_GETMAXSTEP, 0, out var maxStep)) {
                    return maxStep;
                }
                Logger.Error($"AAF error to get MaxStep");
                return -1;
            }
            set {
                _ = sdk.AAF(ToupTekAlikeAAF.AAF_SETMAXSTEP, value, out var _);
                RaisePropertyChanged(nameof(MaxStep));
                RaisePropertyChanged(nameof(MaxIncrement));
            }
        }

        public int Position {
            get {
                if (sdk.AAF(ToupTekAlikeAAF.AAF_GETPOSITION, 0, out var position)) {
                    return position;
                }
                //                        DisconnectOnRemovedError();
                Logger.Error("AAF error to get Position");
                return -1;
            }
        }
        /*
                private void DisconnectOnRemovedError() {
                    try {
                        Logger.Error($"EAF device was removed");
                        Disconnect();
                    } catch (Exception ex) {
                        Logger.Error(ex);
                    }
                }
        */
        public double StepSize {
            get {
                if (sdk.AAF(ToupTekAlikeAAF.AAF_GETMAXINCREMENT, 0, out var stepSize)) {
                    return stepSize;
                }
                Logger.Error("AAF error to get StepSize");
                return double.NaN;
            }
        }

        public double Temperature {
            get {
                if (sdk.AAF(ToupTekAlikeAAF.AAF_GETTEMP, 0, out var temp)) {
                    return temp / 10.0;
                }
                //       DisconnectOnRemovedError();
                Logger.Error($"AAF error to get Temperature");
                return double.NaN;
            }
        }

        private string id;
        public string Id {
            get => id;
            set {
                id = value;
            }
        }

        private string name;
        public string Name {
            get => name;
            set {
                name = value;
            }
        }

        public string DisplayName => $"{Category} {Name} ({(Id.Length > 8 ? Id[^8..] : Id)})";

        public string Category { get; }

        private bool connected = false;
        public bool Connected {
            get => connected;
            private set {
                connected = value;
            }
        }

        private bool reversed = false;
        public bool Reversed {
            get => reversed;
            set {
                reversed = value;
            }
        }

        public void OnReversedChanged(bool value) {
            _ = sdk.AAF(ToupTekAlikeAAF.AAF_SETDIRECTION, value ? 1 : 0, out var _);
        }

        private int targetMaxStep;
        public int TargetMaxStep {
            get => targetMaxStep;
            set {
                targetMaxStep = value;
            }
        }

        public void OnTargetMaxStepChanged(int value) {
            _ = sdk.AAF(ToupTekAlikeAAF.AAF_SETMAXSTEP, value, out var _);
        }

        public void ResetPosition() {
            if (Position > 0) {
                _ = sdk.AAF(ToupTekAlikeAAF.AAF_SETZERO, 0, out var _);
            }
        }

        private string description;
        public string Description {
            get => description;
            set {
                description = value;
            }
        }

        public string DriverInfo => $"{Category} SDK";

        public string DriverVersion => sdk?.Version() ?? string.Empty;

        public Task<bool> Connect(CancellationToken token) {
            return Task<bool>.Run(() => {
                var success = false;
                try {
                    sdk = sdk.Open(this.internalId);
                    success = true;

                    _ = sdk.AAF(ToupTekAlikeAAF.AAF_GETDIRECTION, 0, out var rev);
                    Reversed = rev != 0;

                    TargetMaxStep = MaxStep;

                    // Connected flag
                    Connected = true;
                } catch (Exception ex) {
                    Logger.Error(ex);
                }
                return success;
            });
        }

        public void Disconnect() {
            Logger.Trace("ToupTekAlikeFocuser::Disconnect()");
            sdk.Close();
            Connected = false;
        }

        public void Halt() {
            _ = sdk.AAF(ToupTekAlikeAAF.AAF_HALT, 0, out var _);
        }

        public async Task Move(int position, CancellationToken ct, int waitInMs = 1000) {
            var lastPosition = int.MinValue;
            int samePositionCount = 0;
            var lastMovementTime = DateTime.Now;
            while (position != Position && !ct.IsCancellationRequested) {
                // Issue move command
                if (!sdk.AAF(ToupTekAlikeAAF.AAF_SETPOSITION, position, out var _)) {
                    Logger.Error("AAF failed to issue move command");
                    throw new Exception("Failed to move focuser");
                }

                await CoreUtil.Wait(TimeSpan.FromMilliseconds(100), ct);
                while (IsMoving && !ct.IsCancellationRequested) {
                    await CoreUtil.Wait(TimeSpan.FromMilliseconds(100), ct);
                }

                if (lastPosition == Position) {
                    ++samePositionCount;
                    var samePositionTime = DateTime.Now - lastMovementTime;
                    if (samePositionTime >= SameFocuserPositionTimeout) {
                        throw new Exception($"Focuser stuck at position {lastPosition} beyond {SameFocuserPositionTimeout} timeout");
                    }

                    // Make sure we wait in between Move requests when no progress is being made
                    // to avoid spamming the driver and spiking the CPU
                    await CoreUtil.Wait(TimeSpan.FromSeconds(1), ct);
                } else {
                    lastMovementTime = DateTime.Now;
                }
                _ = sdk.AAF(ToupTekAlikeAAF.AAF_GETPOSITION, 0, out lastPosition);
            }
        }

        public bool BeepOnMove {
            get {
                if (sdk.AAF(ToupTekAlikeAAF.AAF_GETBUZZER, 0, out var beep)) {
                    return beep != 0;
                }
                Logger.Error($"AAF error to get BeepOnMove");
                return false;
            }
            set {
                if (sdk.AAF(ToupTekAlikeAAF.AAF_SETBUZZER, value ? 1 : 0, out var _)) {
                    Logger.Info($"AAF: BeepOnMove set to {value}");
                } else {
                    Logger.Error($"AAF error to set BeepOnMove");
                }
            }
        }

        #region Unsupported
        public bool TempCompAvailable => false;
        public bool TempComp { get; set; }

        public bool HasSetupDialog => false;
        public IList<string> SupportedActions => new List<string>();
        public void SetupDialog() {
        }

        public string Action(string actionName, string actionParameters) {
            throw new NotImplementedException();
        }
        public void SendCommandBlind(string command, bool raw = true) {
            throw new NotImplementedException();
        }

        public bool SendCommandBool(string command, bool raw = true) {
            throw new NotImplementedException();
        }

        public string SendCommandString(string command, bool raw = true) {
            throw new NotImplementedException();
        }
        #endregion
    }
}
