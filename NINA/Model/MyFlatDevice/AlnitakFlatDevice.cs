#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using NINA.Profile;
using NINA.Utility;
using NINA.Utility.FlatDeviceSDKs.AlnitakSDK;
using NINA.Utility.Notification;
using NINA.Utility.WindowService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyFlatDevice {

    public class AlnitakFlatDevice : BaseINPC, IFlatDevice {
        private readonly IProfileService _profileService;
        private const string AUTO = "AUTO";
        public IAlnitakDevice Sdk { get; set; } = new AlnitakDevice();

        public AlnitakFlatDevice(IProfileService profileService) {
            _profileService = profileService;
            PortName = profileService.ActiveProfile.FlatDeviceSettings.PortName;
        }

        public CoverState CoverState {
            get {
                if (!Connected) {
                    return CoverState.Unknown;
                }
                var command = new StateCommand();
                var response = Sdk.SendCommand<StateResponse>(command);
                if (response.IsValid) return response.CoverState;
                Logger.Error($"Invalid response from flat device. " +
                             $"Command was: {command} Response was: {response}.");
                Notification.ShowError(Locale.Loc.Instance["LblFlatDeviceInvalidResponse"]);
                return CoverState.Unknown;
            }
        }

        public int MaxBrightness => 255;

        public int MinBrightness => 0;

        private bool _supportsOpenClose;

        public bool SupportsOpenClose {
            get => _supportsOpenClose;
            set {
                if (_supportsOpenClose == value) return;
                _supportsOpenClose = value;
                RaisePropertyChanged();
            }
        }

        public bool LightOn {
            get {
                if (!Connected) {
                    return false;
                }
                var command = new StateCommand();
                var response = Sdk.SendCommand<StateResponse>(command);
                if (!response.IsValid) {
                    Logger.Error($"Invalid response from flat device. " +
                                 $"Command was: {command} Response was: {response}.");
                    Notification.ShowError(Locale.Loc.Instance["LblFlatDeviceInvalidResponse"]);
                    return false;
                }

                return response.LightOn;
            }
            set {
                if (Connected) {
                    if (value) {
                        var command = new LightOnCommand();
                        var response = Sdk.SendCommand<LightOnResponse>(command);
                        if (!response.IsValid) {
                            Logger.Error($"Invalid response from flat device. " +
                                         $"Command was: {command} Response was: {response}.");
                            Notification.ShowError(Locale.Loc.Instance["LblFlatDeviceInvalidResponse"]);
                        }
                    } else {
                        var command = new LightOffCommand();
                        var response = Sdk.SendCommand<LightOffResponse>(command);
                        if (!response.IsValid) {
                            Logger.Error($"Invalid response from flat device. " +
                                         $"Command was: {command} Response was: {response}.");
                            Notification.ShowError(Locale.Loc.Instance["LblFlatDeviceInvalidResponse"]);
                        }
                    }
                }
                RaisePropertyChanged();
            }
        }

        public double Brightness {
            get {
                var result = 0.0;
                if (!Connected) {
                    return result;
                }
                var command = new GetBrightnessCommand();
                var response = Sdk.SendCommand<GetBrightnessResponse>(command);
                if (!response.IsValid) {
                    Logger.Error($"Invalid response from flat device. " +
                                 $"Command was: {command} Response was: {response}.");
                    Notification.ShowError(Locale.Loc.Instance["LblFlatDeviceInvalidResponse"]);
                } else {
                    result = response.Brightness;
                }
                return Math.Round(result / (MaxBrightness - MinBrightness), 2);
            }
            set {
                if (Connected) {
                    if (value < 0) {
                        value = 0;
                    }

                    if (value > 1) {
                        value = 1;
                    }

                    var command = new SetBrightnessCommand {
                        Brightness = value * (MaxBrightness - MinBrightness) + MinBrightness
                    };
                    var response = Sdk.SendCommand<SetBrightnessResponse>(command);
                    if (!response.IsValid) {
                        Logger.Error($"Invalid response from flat device. " +
                                     $"Command was: {command} Response was: {response}.");
                        Notification.ShowError(Locale.Loc.Instance["LblFlatDeviceInvalidResponse"]);
                    }
                }
                RaisePropertyChanged();
            }
        }

        public ReadOnlyCollection<string> PortNames {
            get {
                var result = new List<string> { "AUTO" };
                result.AddRange(Sdk.PortNames);
                return new ReadOnlyCollection<string>(result);
            }
        }

        public string PortName {
            get => _profileService.ActiveProfile.FlatDeviceSettings.PortName;
            set {
                _profileService.ActiveProfile.FlatDeviceSettings.PortName = value;
                RaisePropertyChanged();
            }
        }

        public bool HasSetupDialog => !Connected;

        public string Id => "817b60ab-6775-41bd-97b5-3857cc676e51";

        public string Name => $"{Locale.Loc.Instance["LblAlnitakFlatPanel"]}";

        public string Category => "Alnitak Astrosystems";

        private bool _connected;

        public bool Connected {
            get => _connected;
            private set {
                _connected = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HasSetupDialog));
            }
        }

        private string _description;

        public string Description {
            get => _description;
            set {
                _description = value;
                RaisePropertyChanged();
            }
        }

        public string DriverInfo => "Serial Driver based on Alnitak Generic Commands Rev 44 6/12/2017.";

        public string DriverVersion => "1.1";

        public async Task<bool> Close(CancellationToken ct) {
            if (!Connected) return await Task.Run(() => false, ct);
            return await Task.Run(() => {
                var command = new CloseCommand();
                var response = Sdk.SendCommand<CloseResponse>(command);
                if (!response.IsValid) {
                    Logger.Error($"Invalid response from flat device. " +
                                 $"Command was: {command} Response was: {response}.");
                    Notification.ShowError(Locale.Loc.Instance["LblFlatDeviceInvalidResponse"]);
                    return false;
                }
                while (IsMotorRunning()) {
                    Thread.Sleep(300);
                }
                return CoverState == CoverState.Closed;
            }, ct);
        }

        public async Task<bool> Connect(CancellationToken ct) {
            if (!Sdk.InitializeSerialPort(PortName)) return false;
            return await Task.Run(() => {
                Connected = true;
                var pingResponse = Sdk.SendCommand<PingResponse>(new PingCommand());
                if (!pingResponse.IsValid) {
                    Logger.Debug($"First ping command on connect did not work. " +
                                 $"Response was: {pingResponse}.");
                }

                var stateResponse = Sdk.SendCommand<StateResponse>(new StateCommand());
                if (!stateResponse.IsValid) {
                    Logger.Error($"Invalid response from flat device. " +
                                 $"Response was: {stateResponse}.");
                    Notification.ShowError(Locale.Loc.Instance["LblFlatDeviceInvalidResponse"]);
                    Connected = false;
                    return Connected;
                }
                SupportsOpenClose = stateResponse.DeviceSupportsOpenClose;

                var fWResponse = Sdk.SendCommand<FirmwareVersionResponse>(new FirmwareVersionCommand());
                Description = $"{stateResponse.Name} on port {PortName}. Firmware version: " +
                              $"{(fWResponse.IsValid ? fWResponse.FirmwareVersion.ToString() : "No valid firmware version.")}";

                RaiseAllPropertiesChanged();
                return Connected;
            }, ct);
        }

        public void Disconnect() {
            Connected = false;
            Sdk.Dispose();
        }

        public IWindowService WindowService { get; set; } = new WindowService();

        public async Task<bool> Open(CancellationToken ct) {
            if (!Connected) return await Task.Run(() => false, ct);
            return await Task.Run(() => {
                var command = new OpenCommand();
                var response = Sdk.SendCommand<OpenResponse>(command);
                if (!response.IsValid) {
                    Logger.Error($"Invalid response from flat device. " +
                                 $"Command was: {command} Response was: {response}.");
                    Notification.ShowError(Locale.Loc.Instance["LblFlatDeviceInvalidResponse"]);
                    return false;
                }
                while (IsMotorRunning()) {
                    Thread.Sleep(300);
                }
                return CoverState == CoverState.Open;
            }, ct);
        }

        public void SetupDialog() {
            WindowService.ShowDialog(this, "Alnitak Flat Panel Setup", System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.SingleBorderWindow);
        }

        private bool IsMotorRunning() {
            var response = Sdk.SendCommand<StateResponse>(new StateCommand());
            return response.IsValid && response.MotorRunning;
        }
    }
}