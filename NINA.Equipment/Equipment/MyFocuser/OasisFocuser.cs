using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NINA.Core.Locale;
using NINA.Core.MyMessageBox;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static Astroasis.AstroasisSDK.AOFocus;

namespace NINA.Equipment.Equipment.MyFocuser {
    public partial class OasisFocuser : BaseINPC, IFocuser {
        private readonly IProfileService profileService;
        private int id;
        private static TimeSpan SameFocuserPositionTimeout = TimeSpan.FromMinutes(1);

        public OasisFocuser(int id, IProfileService profileService) {
            this.profileService = profileService;

            this.id = id;

            FocuserGetProductModel(id, out var model);

            focuserAlias = GetAlias();

            Logger.Debug($"Oasis: Focuser ID/Alias: {focuserAlias}");

            Name = model;

            SetId();
        }
        public bool IsMoving { 
            get {
                var err = FocuserGetStatus(id, out var status);
                if (err == AOReturn.AO_SUCCESS) {
                    return status.moving == 1;
                } else {
                    if (err == AOReturn.AO_ERROR_COMMUNICATION) {
                        Logger.Error($"Oasis communication error to get moving state {err}");
                        DisconnectOnRemovedError();
                    } else {
                        Logger.Error($"Oasis communication error to get moving state {err}");
                    }
                    return false;
                }
            }
        }

        public int MaxIncrement => MaxStep;

        public int MaxStep {
            get {
                var err = FocuserGetConfig(id, out var config);
                if (err == AOReturn.AO_SUCCESS) {
                    return config.maxStep;
                } else {
                    Logger.Error($"Oasis communication error to get MaxStep {err}");
                    return -1;
                }
            }
        }

        public int Position {
            get {
                var err = FocuserGetStatus(id, out var status);
                if (err == AOReturn.AO_SUCCESS) {
                    return status.position;
                } else {
                    if (err == AOReturn.AO_ERROR_COMMUNICATION) {
                        Logger.Error($"Oasis communication error to get Position state {err}");
                        DisconnectOnRemovedError();
                    } else {
                        Logger.Error($"Oasis Communication error to get Position {err}");
                    }
                    return -1;
                }
            }
        }

        private void DisconnectOnRemovedError() {
            try {
                Notification.ShowWarning(Loc.Instance["LblFocuserConnectionLost"]);
                Logger.Error($"Oasis device was removed");
                Disconnect();
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        public double StepSize => -1;

        public double Temperature {
            get {
                var err = FocuserGetStatus(id, out var status);
                if (err == AOReturn.AO_SUCCESS) {
                    if (status.temperatureDetection == 0) {
                        // Ambient probe not connected, display internal temperature
                        return status.temperatureInt / 100.0;
                    } else {
                        return status.temperatureExt / 100.0;
                    }
                } else {
                    if (err == AOReturn.AO_ERROR_COMMUNICATION) {
                        Logger.Error($"Oasis communication error to get Temperature {err}");
                        DisconnectOnRemovedError();
                    } else {
                        Logger.Error($"Oasis communication error to get Temperature {err}");
                    }
                    return double.NaN;
                }
            }
        }

        public string Id { get; private set; }
        private void SetId() {
            Id = $"{Category}_{Name}_{FocuserAlias}";
        }

        private string focuserAlias;

        public string FocuserAlias {
            get {
                return focuserAlias;
            }

            set {
                Logger.Debug($"Oasis: Setting Focuser ID/Alias to: {value}");

                FocuserSetFriendlyName(id, value);
                focuserAlias = GetAlias();

                FocuserGetFriendlyName(id, out var name);
                Name = new string(name);
                SetId();

                Logger.Info($"Oasis: Focuser ID/Alias set to: {focuserAlias}");

                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Name));
                RaisePropertyChanged(nameof(DisplayName));
                RaisePropertyChanged(nameof(Id));
                profileService.ActiveProfile.FocuserSettings.Id = Id;
            }
        }

        private string GetAlias() {
            FocuserGetFriendlyName(id, out var name);
            var strName = new string(name);

            if (strName.Contains('(') && strName.Contains(')') && strName.EndsWith(")")) {
                var openparen = strName.IndexOf('(');
                var closeparen = strName.LastIndexOf(')');
                var alias = closeparen - openparen;
                return strName.Substring(openparen + 1, alias - 1);
            } else {
                return string.Empty;
            }
        }

        public string Name { get; private set; }
        public string DisplayName => Name;

        public string Category => "Oasis";

        [ObservableProperty]
        private bool connected = false;

        [ObservableProperty]
        private bool reversed = false;

        partial void OnReversedChanged(bool value)
        {
            AOFocuserConfig config = new AOFocuserConfig();
            config.mask = (uint)AOConfig.MASK_REVERSE_DIRECTION;
            config.reverseDirection = value ? 1 : 0;
            _ = FocuserSetConfig(id, ref config);
        }

        [ObservableProperty]
        private int targetMaxStep;

        partial void OnTargetMaxStepChanged(int value) {
            AOFocuserConfig config = new AOFocuserConfig();
            config.mask = (uint)AOConfig.MASK_MAX_STEP;
            config.maxStep = value;
            _ = FocuserSetConfig(id, ref config);

            RaisePropertyChanged(nameof(MaxStep));
            RaisePropertyChanged(nameof(MaxIncrement));
        }

        [RelayCommand]
        public void ResetPosition() {
            if (MyMessageBox.Show(Loc.Instance["LblZwoResetZeroPositionPrompt"], "", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxResult.No) == System.Windows.MessageBoxResult.Yes) {
                if(Position > 0) {
                    FocuserSetZeroPosition(id);
                    RaisePropertyChanged(nameof(Position));
                }
            }
        }

        public string Description => "Native driver for Oasis focusers";

        public string DriverInfo { get; private set; } = string.Empty;

        public string DriverVersion {
            get {
                _ = FocuserGetSDKVersion(out var version);
                return new string(version);
            }
        }

        public Task<bool> Connect(CancellationToken token) {
            return Task.Run(() => {
                if (FocuserOpen(id) == AOReturn.AO_SUCCESS) {
                    DriverInfo = $"SDK: {DriverVersion}; FW: {GetFwVersionString()}";

                    FocuserGetConfig(id, out var config);
                    Reversed = config.reverseDirection == 1;

                    TargetMaxStep = config.maxStep;

                    Connected = true;
                    return true;
                } else {
                    Logger.Error("Failed to connect to Oasis focuser");
                    return false;
                }
            });
        }

        private string GetFwVersionString() {
            AOFocuserVersion version = new AOFocuserVersion();
            _ = FocuserGetVersion(id, out version);
            return version.firmware.ToString();
        }

        public void Disconnect() {
            _ = FocuserClose(id);
            this.Connected = false;
        }

        public void Halt() {
            FocuserStopMove(id);
        }

        public async Task Move(int position, CancellationToken ct, int waitInMs = 1000) {

            var lastPosition = int.MinValue;
            int samePositionCount = 0;
            var lastMovementTime = DateTime.Now;
            while (position != Position && !ct.IsCancellationRequested) {
                FocuserMoveTo(id, position);

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

                FocuserGetStatus(id, out var status);
                lastPosition = status.position;
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
