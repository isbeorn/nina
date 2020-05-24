#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Profile;
using NINA.Utility;
using NINA.Utility.SwitchSDKs.PegasusAstro;
using System;
using System.Threading;
using System.Threading.Tasks;
using NINA.Locale;
using NINA.Utility.Notification;
using NINA.Utility.WindowService;
using NINA.Utility.SerialCommunication;
using ICommand = System.Windows.Input.ICommand;

namespace NINA.Model.MyFocuser {

    public class UltimatePowerboxV2 : BaseINPC, IFocuser, IDisposable {
        private readonly IProfileService _profileService;
        private const string AUTO = "AUTO";
        private IPegasusDevice _sdk;

        public IPegasusDevice Sdk {
            get => _sdk ?? (Sdk = PegasusDevice.Instance);
            set => _sdk = value;
        }

        public UltimatePowerboxV2(IProfileService profileService) {
            _profileService = profileService;
            PortName = profileService?.ActiveProfile?.SwitchSettings?.Upbv2PortName ?? AUTO;
            SetMotorDirectionCommand = new RelayCommand(SetMotorDirection);
            SetCurrentPositionCommand = new RelayCommand(SetCurrentPosition);
            SetMaximumSpeedCommand = new RelayCommand(SetMaximumSpeed);
            SetBacklashStepsCommand = new RelayCommand(SetBacklashSteps);
        }

        private void LogAndNotify(Utility.SerialCommunication.ICommand command, InvalidDeviceResponseException ex) {
            Logger.Error($"Invalid response from Ultimate Powerbox V2 on port {PortName}. " +
                         $"Command was: {command} Response was: {ex.Message}.");
            Notification.ShowError(Loc.Instance["LblUPBV2InvalidResponse"]);
        }

        private void HandlePortClosed(Utility.SerialCommunication.ICommand command, SerialPortClosedException ex) {
            Logger.Error($"Serial port was closed. Command was: {command} Exception: {ex.InnerException}.");
            Notification.ShowError(Loc.Instance["LblUPBV2InvalidResponse"]);
            Disconnect();
            RaiseAllPropertiesChanged();
        }

        public string PortName {
            get => _profileService.ActiveProfile.SwitchSettings.Upbv2PortName;
            set {
                _profileService.ActiveProfile.SwitchSettings.Upbv2PortName = value;
                RaisePropertyChanged();
            }
        }

        public bool HasSetupDialog => Connected;

        public string Id => "e793459f-07da-40b0-924b-f48c45eca9c3";

        public string Name => "Ultimate Powerbox V2";

        public string Category => "Pegasus Astro";
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

        public string DriverInfo => "Serial driver for devices with firmware >= v1.3 (July 2019)";
        public string DriverVersion => "1.0";

        public async Task<bool> Connect(CancellationToken token) {
            if (!Sdk.InitializeSerialPort(PortName, this)) return false;
            if (Connected) return true;
            return await Task.Run(async () => {
                var statusCommand = new StatusCommand();
                try {
                    _ = await Sdk.SendCommand<StatusResponse>(statusCommand);
                    Connected = true;
                    Description = $"Ultimate Powerbox V2 on port {PortName}. Firmware version: ";
                } catch (InvalidDeviceResponseException ex) {
                    LogAndNotify(statusCommand, ex);
                    Sdk.Dispose(this);
                    Connected = false;
                } catch (SerialPortClosedException ex) {
                    HandlePortClosed(statusCommand, ex);
                    Connected = false;
                }
                if (!Connected) return false;

                var fwCommand = new FirmwareVersionCommand();
                try {
                    var response = await Sdk.SendCommand<FirmwareVersionResponse>(fwCommand);
                    Description += $"{response.FirmwareVersion}";
                } catch (InvalidDeviceResponseException ex) {
                    LogAndNotify(fwCommand, ex);
                    Description += Loc.Instance["LblNoValidFirmwareVersion"];
                } catch (SerialPortClosedException ex) {
                    HandlePortClosed(fwCommand, ex);
                }
                RaiseAllPropertiesChanged();
                return Connected;
            }, token);
        }

        public void Disconnect() {
            if (!Connected) return;
            Connected = false;
            Sdk.Dispose(this);
        }

        public IWindowService WindowService { get; set; } = new WindowService();

        public void SetupDialog() {
            WindowService.ShowDialog(this, "Pegasus Astro Ultimate Powerbox V2 Stepper Motor Setup", System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.SingleBorderWindow);
        }

        public bool IsMoving {
            get {
                if (!Connected) return false;
                var command = new StepperMotorIsMovingCommand();
                try {
                    var response = Sdk.SendCommand<StepperMotorIsMovingResponse>(command).Result;
                    return response.IsMoving;
                } catch (InvalidDeviceResponseException ex) {
                    LogAndNotify(command, ex);
                } catch (SerialPortClosedException ex) {
                    HandlePortClosed(command, ex);
                }
                return false;
            }
        }

        public int MaxIncrement => 9999;
        public int MaxStep => int.MaxValue;

        public int Position {
            get {
                if (!Connected) return 0;
                var command = new StepperMotorGetCurrentPositionCommand();
                try {
                    var response = Sdk.SendCommand<StepperMotorGetCurrentPositionResponse>(command).Result;
                    return response.Position;
                } catch (InvalidDeviceResponseException ex) {
                    LogAndNotify(command, ex);
                } catch (SerialPortClosedException ex) {
                    HandlePortClosed(command, ex);
                }
                return 0;
            }
        }

        public double StepSize => 1.0d;
        public bool TempCompAvailable => false;
        public bool TempComp { get; set; }

        public double Temperature {
            get {
                if (!Connected) return 0d;
                var command = new StepperMotorTemperatureCommand();
                try {
                    var response = Sdk.SendCommand<StepperMotorTemperatureResponse>(command).Result;
                    return response.Temperature;
                } catch (InvalidDeviceResponseException ex) {
                    LogAndNotify(command, ex);
                } catch (SerialPortClosedException ex) {
                    HandlePortClosed(command, ex);
                }
                return 0d;
            }
        }

        public Task Move(int position, CancellationToken ct) {
            if (!Connected) return Task.FromResult(false);
            return Task.Run(async () => {
                var command = new StepperMotorMoveToPositionCommand { Position = position };
                try {
                    _ = await Sdk.SendCommand<StepperMotorMoveToPositionResponse>(command);
                    return Task.FromResult(true);
                } catch (InvalidDeviceResponseException ex) {
                    LogAndNotify(command, ex);
                } catch (SerialPortClosedException ex) {
                    HandlePortClosed(command, ex);
                }
                return Task.FromResult(false);
            }, ct);
        }

        public void Halt() {
            if (!Connected) return;
            var command = new StepperMotorHaltCommand();
            try {
                _ = Sdk.SendCommand<StepperMotorHaltResponse>(command).Result;
            } catch (InvalidDeviceResponseException ex) {
                LogAndNotify(command, ex);
            } catch (SerialPortClosedException ex) {
                HandlePortClosed(command, ex);
            }
        }

        public void Dispose() {
            Connected = false;
            Sdk?.Dispose(this);
        }

        public ICommand SetMotorDirectionCommand { get; }
        public ICommand SetCurrentPositionCommand { get; }
        public ICommand SetMaximumSpeedCommand { get; }
        public ICommand SetBacklashStepsCommand { get; }

        public void SetMotorDirection(object o) {
            if (!Connected) return;
            var command = new StepperMotorDirectionCommand { DirectionClockwise = ((string)o).Equals("clockWise") };
            try {
                if (Sdk == null) return;
                _ = Sdk.SendCommand<StepperMotorDirectionResponse>(command).Result;
            } catch (InvalidDeviceResponseException ex) {
                LogAndNotify(command, ex);
            } catch (SerialPortClosedException ex) {
                HandlePortClosed(command, ex);
            }
        }

        public void SetCurrentPosition(object o) {
            if (!Connected) return;
            var command = new StepperMotorSetCurrentPositionCommand { Position = int.Parse((string)o) };
            try {
                if (Sdk == null) return;
                _ = Sdk.SendCommand<StepperMotorSetCurrentPositionResponse>(command).Result;
            } catch (InvalidDeviceResponseException ex) {
                LogAndNotify(command, ex);
            } catch (SerialPortClosedException ex) {
                HandlePortClosed(command, ex);
            }
        }

        public void SetMaximumSpeed(object o) {
            if (!Connected) return;
            var command = new StepperMotorSetMaximumSpeedCommand { Speed = int.Parse((string)o) };
            try {
                if (Sdk == null) return;
                _ = Sdk.SendCommand<StepperMotorSetMaximumSpeedResponse>(command).Result;
            } catch (InvalidDeviceResponseException ex) {
                LogAndNotify(command, ex);
            } catch (SerialPortClosedException ex) {
                HandlePortClosed(command, ex);
            }
        }

        public void SetBacklashSteps(object o) {
            if (!Connected) return;
            var command = new StepperMotorSetBacklashStepsCommand { Steps = int.Parse((string)o) };
            try {
                _ = Sdk?.SendCommand<StepperMotorSetBacklashStepsResponse>(command).Result;
            } catch (InvalidDeviceResponseException ex) {
                LogAndNotify(command, ex);
            } catch (SerialPortClosedException ex) {
                HandlePortClosed(command, ex);
            }
        }
    }
}