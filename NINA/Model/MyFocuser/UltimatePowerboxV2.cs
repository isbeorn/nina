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
using NINA.Utility.SwitchSDKs.PegasusAstro;
using System;
using System.Threading;
using System.Threading.Tasks;
using NINA.Locale;
using NINA.Utility.Notification;
using NINA.Utility.WindowService;
using System.Windows.Input;

namespace NINA.Model.MyFocuser {

    public class UltimatePowerboxV2 : BaseINPC, IFocuser, IDisposable {
        private readonly IProfileService _profileService;
        private const string AUTO = "AUTO";
        public IPegasusDevice Sdk { get; set; } = PegasusDevice.Instance;

        public UltimatePowerboxV2(IProfileService profileService) {
            _profileService = profileService;
            PortName = profileService?.ActiveProfile?.SwitchSettings?.Upbv2PortName ?? AUTO;
            SetMotorDirectionCommand = new RelayCommand(SetMotorDirection);
            SetCurrentPositionCommand = new RelayCommand(SetCurrentPosition);
            SetMaximumSpeedCommand = new RelayCommand(SetMaximumSpeed);
            SetBacklashStepsCommand = new RelayCommand(SetBacklashSteps);
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
            return await Task.Run(() => {
                try {
                    var command = new FirmwareVersionCommand();
                    var response = Sdk.SendCommand<FirmwareVersionResponse>(command);
                    if (!response.IsValid) {
                        Logger.Error($"Invalid response from Ultimate Powerbox V2 on port {PortName}. " +
                                     $"Command was: {command} Response was: {response}.");
                        Notification.ShowError(Loc.Instance["LblUPBV2InvalidResponse"]);
                        Connected = false;
                        return Connected;
                    }

                    Description =
                        $"Ultimate Powerbox V2 on port {PortName}. Firmware version: {response.FirmwareVersion}";
                    Connected = true;
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Connected = false;
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
                var response = Sdk.SendCommand<StepperMotorIsMovingResponse>(command);
                if (response.IsValid) return response.IsMoving;
                Logger.Error($"Invalid response from Ultimate Powerbox V2 on port {PortName}. " +
                             $"Command was: {command} Response was: {response}.");
                Notification.ShowError(Loc.Instance["LblUPBV2InvalidResponse"]);
                return false;
            }
        }

        public int MaxIncrement => 9999;
        public int MaxStep => int.MaxValue;

        public int Position {
            get {
                if (!Connected) return 0;
                var command = new StepperMotorGetCurrentPositionCommand();
                var response = Sdk.SendCommand<StepperMotorGetCurrentPositionResponse>(command);
                if (response.IsValid) return response.Position;
                Logger.Error($"Invalid response from Ultimate Powerbox V2 on port {PortName}. " +
                             $"Command was: {command} Response was: {response}.");
                Notification.ShowError(Loc.Instance["LblUPBV2InvalidResponse"]);
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
                var response = Sdk.SendCommand<StepperMotorTemperatureResponse>(command);
                if (response.IsValid) return response.Temperature;
                Logger.Error($"Invalid response from Ultimate Powerbox V2 on port {PortName}. " +
                             $"Command was: {command} Response was: {response}.");
                Notification.ShowError(Loc.Instance["LblUPBV2InvalidResponse"]);
                return 0d;
            }
        }

        public Task Move(int position, CancellationToken ct) {
            if (!Connected) return Task.FromResult(false);
            return Task.Run(() => {
                try {
                    var command = new StepperMotorMoveToPositionCommand { Position = position };
                    var response = Sdk.SendCommand<StepperMotorMoveToPositionResponse>(command);
                    if (!response.IsValid) {
                        Logger.Error($"Invalid response from Ultimate Powerbox V2 on port {PortName}. " +
                                     $"Command was: {command} Response was: {response}.");
                        Notification.ShowError(Loc.Instance["LblUPBV2InvalidResponse"]);
                        return Task.FromResult(false);
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }
                return Task.FromResult(true);
            }, ct);
        }

        public void Halt() {
            if (!Connected) return;
            try {
                var command = new StepperMotorHaltCommand();
                var response = Sdk.SendCommand<StepperMotorHaltResponse>(command);
                if (response.IsValid) return;
                Logger.Error($"Invalid response from Ultimate Powerbox V2 on port {PortName}. " +
                             $"Command was: {command} Response was: {response}.");
                Notification.ShowError(Loc.Instance["LblUPBV2InvalidResponse"]);
            } catch (Exception ex) {
                Logger.Error(ex);
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
            var response = Sdk?.SendCommand<StepperMotorDirectionResponse>(command);
            if (response != null && response.IsValid) return;
            Logger.Error($"Invalid response from Ultimate Powerbox V2 on port {PortName}. " +
                         $"Command was: {command} Response was: {response}.");
            Notification.ShowError(Loc.Instance["LblUPBV2InvalidResponse"]);
        }

        public void SetCurrentPosition(object o) {
            if (!Connected) return;
            var command = new StepperMotorSetCurrentPositionCommand { Position = int.Parse((string)o) };
            var response = Sdk?.SendCommand<StepperMotorSetCurrentPositionResponse>(command);
            if (response != null && response.IsValid) return;
            Logger.Error($"Invalid response from Ultimate Powerbox V2 on port {PortName}. " +
                         $"Command was: {command} Response was: {response}.");
            Notification.ShowError(Loc.Instance["LblUPBV2InvalidResponse"]);
        }

        public void SetMaximumSpeed(object o) {
            if (!Connected) return;
            var command = new StepperMotorSetMaximumSpeedCommand { Speed = int.Parse((string)o) };
            var response = Sdk?.SendCommand<StepperMotorSetMaximumSpeedResponse>(command);
            if (response != null && response.IsValid) return;
            Logger.Error($"Invalid response from Ultimate Powerbox V2 on port {PortName}. " +
                         $"Command was: {command} Response was: {response}.");
            Notification.ShowError(Loc.Instance["LblUPBV2InvalidResponse"]);
        }

        public void SetBacklashSteps(object o) {
            if (!Connected) return;
            var command = new StepperMotorSetBacklashStepsCommand { Steps = int.Parse((string)o) };
            var response = Sdk?.SendCommand<StepperMotorSetBacklashStepsResponse>(command);
            if (response != null && response.IsValid) return;
            Logger.Error($"Invalid response from Ultimate Powerbox V2 on port {PortName}. " +
                         $"Command was: {command} Response was: {response}.");
            Notification.ShowError(Loc.Instance["LblUPBV2InvalidResponse"]);
        }
    }
}