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
using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Core.Utility.SerialCommunication;
using NINA.Core.Utility.WindowService;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using NINA.Equipment.SDK.FlatDeviceSDKs.Artesky;
using NINA.Equipment.Interfaces;
using System.Collections.Generic;

namespace NINA.Equipment.Equipment.MyFlatDevice {

    public class ArteskyFlatBox : BaseINPC, IFlatDevice {
        private readonly IProfileService _profileService;
        private IArteskyFlatBox _sdk;

        public IArteskyFlatBox Sdk {
            get => _sdk ?? (_sdk = new ArteskyUSBFlatBox());
            set => _sdk = value;
        }

        public ArteskyFlatBox(IProfileService profileService) {
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

        public CoverState CoverState => CoverState.Unknown;
        public int MinBrightness => 0;
        public int MaxBrightness => 255;

        private bool _lightOn;

        public bool LightOn {
            get => Connected && _lightOn;
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

                    _lightOn = true;
                } else {
                    var command = new LightOffCommand();
                    try {
                        _ = Sdk.SendCommand<LightOffResponse>(command).Result;
                    } catch (InvalidDeviceResponseException ex) {
                        LogAndNotify(command, ex);
                    } catch (SerialPortClosedException ex) {
                        HandlePortClosed(command, ex);
                    }

                    _lightOn = false;
                }

                RaisePropertyChanged();
            }
        }

        private int _brightness;

        public int Brightness {
            get => Connected ? _brightness : 0;
            set {
                if (!Connected) return;
                if (value < MinBrightness) {
                    value = MinBrightness;
                }

                if (value > MaxBrightness) {
                    value = MaxBrightness;
                }

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

                _brightness = value;
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

        public string Id => "bc0c02ef-83f7-67f7-f05e-fd97a80f1b3e";

        public string Name => "Artesky Flat Box";

        public string Category => "Artesky";

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

        public string DriverInfo => "Artesky USB Flat Box serial protocol driver";

        public string DriverVersion => "1.0";

        public async Task<bool> Connect(CancellationToken ct) {
            if (!Sdk.InitializeSerialPort(PortName, this)) return false;

            return await Task.Run(async () => {
                var stateCommand = new StateCommand();
                try {
                    var stateResponse = await Sdk.SendCommand<StateResponse>(stateCommand);
                    Connected = true;
                    _lightOn = stateResponse.LightOn;
                    Description = $"{Name} on {Sdk.SerialPort?.PortName ?? "null"}. Firmware version: ";
                } catch (InvalidDeviceResponseException ex) {
                    LogAndNotify(stateCommand, ex);
                    Sdk.Dispose(this);
                    Connected = false;
                } catch (SerialPortClosedException ex) {
                    HandlePortClosed(stateCommand, ex);
                    Connected = false;
                }
                if (!Connected) return false;

                var brightCommand = new GetBrightnessCommand();
                try {
                    var brightResponse = await Sdk.SendCommand<GetBrightnessResponse>(brightCommand);
                    _brightness = brightResponse.Brightness;
                } catch (InvalidDeviceResponseException ex) {
                    LogAndNotify(stateCommand, ex);
                    Sdk.Dispose(this);
                    Connected = false;
                } catch (SerialPortClosedException ex) {
                    HandlePortClosed(stateCommand, ex);
                    Connected = false;
                }
                if (!Connected) return false;

                var fwCommand = new FirmwareVersionCommand();
                try {
                    var fWResponse = await Sdk.SendCommand<FirmwareVersionResponse>(fwCommand);
                    Description += fWResponse.FirmwareVersion.ToString();
                } catch (InvalidDeviceResponseException ex) {
                    LogAndNotify(stateCommand, ex);
                    Description += Loc.Instance["LblNoValidFirmwareVersion"];
                } catch (SerialPortClosedException ex) {
                    HandlePortClosed(stateCommand, ex);
                    Connected = false;
                }
                RaiseAllPropertiesChanged();
                return Connected;
            }, ct);
        }

        public void Disconnect() {
            if (LightOn) {
                LightOn = false;
            }

            Connected = false;
            Sdk.Dispose(this);
        }

        public IWindowService WindowService { get; set; } = new WindowService();

        public Task<bool> Open(CancellationToken ct, int delay = 300) {
            throw new NotImplementedException();
        }

        public Task<bool> Close(CancellationToken ct, int delay = 300) {
            throw new NotImplementedException();
        }

        public bool SupportsOpenClose => false;

        public bool SupportsOnOff => true;

        public void SetupDialog() {
            WindowService.ShowDialog(this, "Artesky Flat Box Setup", System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.SingleBorderWindow);
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
    }
}