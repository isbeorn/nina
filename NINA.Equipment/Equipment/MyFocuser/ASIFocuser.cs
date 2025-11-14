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
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZWOptical.ASISDK;

namespace NINA.Equipment.Equipment.MyFocuser {
    public partial class ASIFocuser : BaseINPC, IFocuser {
        private readonly IProfileService profileService;
        private int id;
        private ASIEAF.EAF_INFO eafInfo;
        private static TimeSpan SameFocuserPositionTimeout = TimeSpan.FromMinutes(1);

        public ASIFocuser(int idx, IProfileService profileService) {
            ASIEAF.EAF_ERROR_CODE rv;
            this.profileService = profileService;

            _ = ASIEAF.GetID(idx, out var id);
            this.id = id;

            rv = ASIEAF.GetProperty(id, out eafInfo);

            if (string.IsNullOrEmpty(eafInfo.Name)) {
                Logger.Error($"EAF: Unable to get focuser properties for EAF at index {idx}: {rv}");
                throw new Exception($"EAF: Unable to get focuser properties for EAF at index {idx}: {rv}");
            }

            focuserAlias = GetAlias();

            Logger.Debug($"EAF: Focuser ID/Alias: {focuserAlias}");

            Name = eafInfo.Name;

            this.profileService = profileService;
            SetId();
        }
        public bool IsMoving { 
            get {
                var err = ASIEAF.IsMoving(id, out var isMoving);
                if (err == ASIEAF.EAF_ERROR_CODE.EAF_SUCCESS) {
                    return isMoving;
                } else {
                    if (err == ASIEAF.EAF_ERROR_CODE.EAF_ERROR_REMOVED) {
                        DisconnectOnRemovedError();
                    } else {
                        Logger.Error($"EAF Communication error to get moving state {err}");
                    }
                    return false;
                }
            } 
        }

        public int MaxIncrement => MaxStep;

        public int MaxStep {
            get {
                var err = ASIEAF.GetMaxStep(id, out var maxStep);
                if (err == ASIEAF.EAF_ERROR_CODE.EAF_SUCCESS) {
                    return maxStep;
                } else {
                    Logger.Error($"EAF Communication error to get MaxStep {err}");
                    return -1;
                }
            }
        }

        public int Position {
            get {
                var err = ASIEAF.GetPosition(id, out var position);
                if (err == ASIEAF.EAF_ERROR_CODE.EAF_SUCCESS) {
                    return position;
                } else {
                    if (err == ASIEAF.EAF_ERROR_CODE.EAF_ERROR_REMOVED) {
                        DisconnectOnRemovedError();
                    } else {
                        Logger.Error($"EAF Communication error to get Position {err}");
                    }
                    return -1;
                }
            }
        }

        private void DisconnectOnRemovedError() {
            try {
                Notification.ShowWarning(Loc.Instance["LblFocuserConnectionLost"]);
                Logger.Error($"EAF device was removed");
                Disconnect();
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        public double StepSize {
            get {
                var err = ASIEAF.GetStepRange(id, out var stepRange);
                if (err == ASIEAF.EAF_ERROR_CODE.EAF_SUCCESS) {
                    return stepRange;
                } else {
                    Logger.Error($"EAF Communication error to get StepRange {err}");
                    return double.NaN;
                }
            }
        }

        public double Temperature {
            get {
                var err = ASIEAF.GetTemperature(id, out var temp);
                if (err == ASIEAF.EAF_ERROR_CODE.EAF_SUCCESS) {
                    return temp;
                } else {
                    if (err == ASIEAF.EAF_ERROR_CODE.EAF_ERROR_REMOVED) {
                        DisconnectOnRemovedError();
                    } else {
                        Logger.Error($"EAF Communication error to get Temperature {err}");
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
                Logger.Debug($"EAF: Setting Focuser ID/Alias to: {value}");

                ASIEAF.SetID(eafInfo.ID, value);
                focuserAlias = GetAlias();

                _ = ASIEAF.GetProperty(eafInfo.ID, out eafInfo);
                Name = eafInfo.Name;
                SetId();

                Logger.Info($"EAF: Focuser ID/Alias set to: {focuserAlias}");

                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Name));
                RaisePropertyChanged(nameof(DisplayName));
                RaisePropertyChanged(nameof(Id));
                profileService.ActiveProfile.FocuserSettings.Id = Id;
            }
        }

        private string GetAlias() {

            _ = ASIEAF.GetProperty(eafInfo.ID, out ASIEAF.EAF_INFO info);

            if (info.Name.Contains('(') && info.Name.Contains(')') && info.Name.EndsWith(")")) {
                var openparen = info.Name.IndexOf('(');
                var closeparen = info.Name.LastIndexOf(')');
                var alias = closeparen - openparen;
                return info.Name.Substring(openparen + 1, alias - 1);
            } else {
                return string.Empty;
            }
        }

        public string Name { get; private set; }
        public string DisplayName => Name;

        public string Category => "ZWOptical";

        [ObservableProperty]
        private bool connected = false;

        [ObservableProperty]
        private bool reversed = false;

        partial void OnReversedChanged(bool value) {
            ASIEAF.SetReverse(id, value);
        }

        [ObservableProperty]
        private int targetMaxStep;

        partial void OnTargetMaxStepChanged(int value) {
            ASIEAF.SetMaxStep(id, value);
            RaisePropertyChanged(nameof(MaxStep));
            RaisePropertyChanged(nameof(MaxIncrement));
        }

        [RelayCommand]
        public void ResetPosition() {
            if (MyMessageBox.Show(Loc.Instance["LblZwoResetZeroPositionPrompt"], "", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxResult.No) == System.Windows.MessageBoxResult.Yes) {
                if(Position > 0) {
                    ASIEAF.SetZeroPosition(id, 0);
                    RaisePropertyChanged(nameof(Position));
                }
            }
        }

        public string Description => "Native driver for ZWOptical focusers";

        public string DriverInfo { get; private set; } = string.Empty;

        public string DriverVersion => "1.0";



        public Task<bool> Connect(CancellationToken token) {
            return Task.Run(() => {
                if (ASIEAF.Open(id) == ASIEAF.EAF_ERROR_CODE.EAF_SUCCESS) {
                    DriverInfo = $"SDK: {ASIEAF.GetSDKVersion()}; FW: {GetFwVersionString()}";
                    
                    ASIEAF.GetReverse(id, out var rev);
                    Reversed = rev;

                    TargetMaxStep = MaxStep;

                    Connected = true;
                    return true;
                } else {
                    Logger.Error("Failed to connect to EAF");
                    return false;
                };
            });
        }
        private string GetFwVersionString() {
            _ = ASIEAF.GetFirmwareVersion(eafInfo.ID, out var major, out var minor, out var patch);
            return $"{major}.{minor}.{patch}";
        }

        public void Disconnect() {
            _ = ASIEAF.Close(id);
            this.Connected = false;
        }

        public void Halt() {
            ASIEAF.Stop(id);
        }

        public async Task Move(int position, CancellationToken ct, int waitInMs = 1000) {

            var lastPosition = int.MinValue;
            int samePositionCount = 0;
            var lastMovementTime = DateTime.Now;
            while (position != Position && !ct.IsCancellationRequested) {
                // Issue move command
                if (ASIEAF.Move(id, position) != ASIEAF.EAF_ERROR_CODE.EAF_SUCCESS) {
                    Logger.Error("EAF failed to issue move command");
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
                ASIEAF.GetPosition(id, out lastPosition);
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
