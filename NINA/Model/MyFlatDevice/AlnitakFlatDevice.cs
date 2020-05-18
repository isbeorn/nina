#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Locale;
using NINA.Profile;
using NINA.Utility;
using NINA.Utility.FlatDeviceSDKs.AlnitakSDK;
using NINA.Utility.Notification;
using NINA.Utility.SerialCommunication;
using NINA.Utility.WindowService;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyFlatDevice {

    public class AlnitakFlatDevice : BaseINPC, IFlatDevice {
        private readonly IProfileService _profileService;
        private IAlnitakDevice _sdk;

        public IAlnitakDevice Sdk {
            get => _sdk ?? (_sdk = new AlnitakDevice());
            set => _sdk = value;
        }

        public AlnitakFlatDevice(IProfileService profileService) {
            _profileService = profileService;
            PortName = profileService.ActiveProfile.FlatDeviceSettings.PortName;
        }

        private static void LogAndNotify(ICommand command, InvalidDeviceResponseException ex) {
            Logger.Error($"Invalid response from flat device. " +
                         $"Command was: {command} Response was: {ex.Message}.");
            Notification.ShowError(Loc.Instance["LblFlatDeviceInvalidResponse"]);
        }

        private void HandlePortClosed(ICommand command, SerialPortClosedException ex) {
            Logger.Error($"Serial port was closed. Command was: {command} Exception: {ex.InnerException}.");
            Notification.ShowError(Loc.Instance["LblFlatDeviceInvalidResponse"]);
            Disconnect();
            RaiseAllPropertiesChanged();
        }

        public CoverState CoverState {
            get {
                if (!Connected) return CoverState.Unknown;

                var command = new StateCommand();
                try {
                    var response = Sdk.SendCommand<StateResponse>(command).Result;
                    return response.CoverState;
                } catch (InvalidDeviceResponseException ex) {
                    LogAndNotify(command, ex);
                } catch (SerialPortClosedException ex) {
                    HandlePortClosed(command, ex);
                }
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
                if (!Connected) return false;

                var command = new StateCommand();
                try {
                    var response = Sdk.SendCommand<StateResponse>(command).Result;
                    return response.LightOn;
                } catch (InvalidDeviceResponseException ex) {
                    LogAndNotify(command, ex);
                } catch (SerialPortClosedException ex) {
                    HandlePortClosed(command, ex);
                }

                return false;
            }
            set {
                if (!Connected) return;
                if (value) {
                    var command = new LightOnCommand();
                    try {
                        _ = Sdk.SendCommand<LightOnResponse>(command).Result;
                    } catch (InvalidDeviceResponseException ex) {
                        LogAndNotify(command, ex);
                    } catch (SerialPortClosedException ex) {
                        HandlePortClosed(command, ex);
                    }
                } else {
                    var command = new LightOffCommand();
                    try {
                        _ = Sdk.SendCommand<LightOffResponse>(command).Result;
                    } catch (InvalidDeviceResponseException ex) {
                        LogAndNotify(command, ex);
                    } catch (SerialPortClosedException ex) {
                        HandlePortClosed(command, ex);
                    }
                }
                RaisePropertyChanged();
            }
        }

        public double Brightness {
            get {
                var result = 0d;
                if (!Connected) return result;

                var command = new GetBrightnessCommand();
                try {
                    var response = Sdk.SendCommand<GetBrightnessResponse>(command).Result;
                    result = response.Brightness;
                } catch (InvalidDeviceResponseException ex) {
                    LogAndNotify(command, ex);
                } catch (SerialPortClosedException ex) {
                    HandlePortClosed(command, ex);
                }
                return Math.Round(result / (MaxBrightness - MinBrightness), 2);
            }
            set {
                if (!Connected) return;
                if (value < 0) value = 0;
                if (value > 1) value = 1;

                var command = new SetBrightnessCommand {
                    Brightness = value * (MaxBrightness - MinBrightness) + MinBrightness
                };
                try {
                    _ = Sdk.SendCommand<SetBrightnessResponse>(command).Result;
                } catch (InvalidDeviceResponseException ex) {
                    LogAndNotify(command, ex);
                } catch (SerialPortClosedException ex) {
                    HandlePortClosed(command, ex);
                }
                RaisePropertyChanged();
            }
        }

        public ReadOnlyCollection<string> PortNames => Sdk?.PortNames;

        public string PortName {
            get => _profileService.ActiveProfile.FlatDeviceSettings.PortName;
            set {
                _profileService.ActiveProfile.FlatDeviceSettings.PortName = value;
                RaisePropertyChanged();
            }
        }

        public bool HasSetupDialog => !Connected;

        public string Id => "817b60ab-6775-41bd-97b5-3857cc676e51";

        public string Name => $"{Loc.Instance["LblAlnitakFlatPanel"]}";

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

        public async Task<bool> Close(CancellationToken ct, int delay = 300) {
            if (!Connected) return false;
            return await Task.Run(async () => {
                var command = new CloseCommand();
                try {
                    await Sdk.SendCommand<CloseResponse>(command);
                } catch (InvalidDeviceResponseException ex) {
                    LogAndNotify(command, ex);
                    return false;
                } catch (SerialPortClosedException ex) {
                    HandlePortClosed(command, ex);
                    return false;
                }
                while (await IsMotorRunning()) {
                    await Utility.Utility.Delay(delay, ct);
                }
                return CoverState == CoverState.Closed;
            }, ct);
        }

        public async Task<bool> Connect(CancellationToken ct) {
            if (!await Sdk.InitializeSerialPort(PortName, this)) return false;
            return await Task.Run(async () => {
                var stateCommand = new StateCommand();
                try {
                    var stateResponse = await Sdk.SendCommand<StateResponse>(stateCommand);
                    Connected = true;
                    Description = $"{stateResponse.Name} on port {PortName}. Firmware version: ";
                    SupportsOpenClose = stateResponse.DeviceSupportsOpenClose;
                } catch (InvalidDeviceResponseException ex) {
                    LogAndNotify(stateCommand, ex);
                    Connected = false;
                    Sdk.Dispose(this);
                } catch (SerialPortClosedException ex) {
                    HandlePortClosed(stateCommand, ex);
                    Connected = false;
                }

                var fwCommand = new FirmwareVersionCommand();
                try {
                    var fWResponse = await Sdk.SendCommand<FirmwareVersionResponse>(fwCommand);
                    Description += fWResponse.FirmwareVersion.ToString();
                } catch (InvalidDeviceResponseException ex) {
                    LogAndNotify(stateCommand, ex);
                    Description += Loc.Instance["LblNoValidFirmwareVersion"];
                } catch (SerialPortClosedException ex) {
                    HandlePortClosed(stateCommand, ex);
                }
                RaiseAllPropertiesChanged();
                return Connected;
            }, ct);
        }

        public void Disconnect() {
            Connected = false;
            Sdk.Dispose(this);
        }

        public IWindowService WindowService { get; set; } = new WindowService();

        public async Task<bool> Open(CancellationToken ct, int delay = 300) {
            if (!Connected) return await Task.Run(() => false, ct);
            return await Task.Run(async () => {
                var command = new OpenCommand();
                try {
                    await Sdk.SendCommand<OpenResponse>(command);
                } catch (InvalidDeviceResponseException ex) {
                    LogAndNotify(command, ex);
                    return false;
                } catch (SerialPortClosedException ex) {
                    HandlePortClosed(command, ex);
                    return false;
                }

                while (await IsMotorRunning()) {
                    await Utility.Utility.Delay(delay, ct);
                }
                return CoverState == CoverState.Open;
            }, ct);
        }

        public void SetupDialog() {
            WindowService.ShowDialog(this, "Alnitak Flat Panel Setup", System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.SingleBorderWindow);
        }

        private async Task<bool> IsMotorRunning() {
            var command = new StateCommand();
            try {
                var response = await Sdk.SendCommand<StateResponse>(command);
                return response.MotorRunning;
            } catch (InvalidDeviceResponseException ex) {
                LogAndNotify(command, ex);
                return false;
            } catch (SerialPortClosedException ex) {
                HandlePortClosed(command, ex);
                return false;
            }
        }
    }
}
