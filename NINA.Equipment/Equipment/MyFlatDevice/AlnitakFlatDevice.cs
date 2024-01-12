#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Core.Utility.SerialCommunication;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Exceptions;
using NINA.Equipment.Interfaces;
using NINA.Equipment.SDK.FlatDeviceSDKs.AlnitakSDK;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyFlatDevice {

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

        private static void LogAndNotify(ISerialCommand command, InvalidDeviceResponseException ex) {
            Logger.Error($"Invalid response from flat device. " +
                         $"Command was: {command} Response was: {ex.Message}.");
            Notification.ShowError(Loc.Instance["LblFlatDeviceInvalidResponse"]);
        }

        private void HandlePortClosed(ISerialCommand command, SerialPortClosedException ex) {
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

        public bool SupportsOnOff => true;

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

        public int Brightness {
            get {
                var result = 0;
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
                return result;
            }
            set {
                if (!Connected) return;
                if (value < MinBrightness) value = MinBrightness;
                if (value > MaxBrightness) value = MaxBrightness;

                var command = new SetBrightnessCommand {
                    Brightness = value
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
                    throw new FlatDeviceCoverErrorException();
                } catch (SerialPortClosedException ex) {
                    HandlePortClosed(command, ex);
                    throw new FlatDeviceCoverErrorException();
                }
                while (await IsMotorRunning()) {
                    await CoreUtil.Delay(delay, ct);
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
                    throw new FlatDeviceCoverErrorException();
                } catch (SerialPortClosedException ex) {
                    HandlePortClosed(command, ex);
                    throw new FlatDeviceCoverErrorException();
                }

                while (await IsMotorRunning()) {
                    await CoreUtil.Delay(delay, ct);
                }
                return CoverState == CoverState.Open;
            }, ct);
        }

        public void SetupDialog() {
            WindowService.ShowDialog(this, "Alnitak Flat Panel Setup", System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.SingleBorderWindow);
        }

        public IList<string> SupportedActions => new List<string>();

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