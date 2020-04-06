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
using NINA.Utility.FlatDeviceSDKs.Artesky;
using NINA.Utility.Notification;
using NINA.Utility.WindowService;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyFlatDevice {

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

        public CoverState CoverState => CoverState.Unknown;
        public int MinBrightness => 0;
        public int MaxBrightness => 255;

        private bool _lightOn;

        public bool LightOn {
            get {
                if (Connected) {
                    return _lightOn;
                } else {
                    return false;
                }
            }
            set {
                if (Connected) {
                    if (value) {
                        var command = new LightOnCommand();
                        var response = Sdk.SendCommand<LightOnResponse>(command);
                        if (!response.IsValid) {
                            Logger.Error($"Invalid response from flat device. " +
                                         $"Command was: {command} Response was: {response}");
                            Notification.ShowError(Locale.Loc.Instance["LblFlatDeviceInvalidResponse"]);
                        }

                        _lightOn = true;
                    } else {
                        var command = new LightOffCommand();
                        var response = Sdk.SendCommand<LightOffResponse>(command);
                        if (!response.IsValid) {
                            Logger.Error($"Invalid response from flat device. " +
                                         $"Command was: {command} Response was: {response}");
                            Notification.ShowError(Locale.Loc.Instance["LblFlatDeviceInvalidResponse"]);
                        }

                        _lightOn = false;
                    }

                    RaisePropertyChanged();
                }
            }
        }

        private double _brightness;

        public double Brightness {
            get {
                if (Connected) {
                    return _brightness;
                } else {
                    return 0d;
                }
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
                        Brightness = Math.Round(value * (MaxBrightness - MinBrightness) + MinBrightness, 0)
                    };

                    var response = Sdk.SendCommand<SetBrightnessResponse>(command);
                    if (!response.IsValid) {
                        Logger.Error($"Invalid response from flat device. " +
                                     $"Command was: {command} Response was: {response}");
                        Notification.ShowError(Locale.Loc.Instance["LblFlatDeviceInvalidResponse"]);
                    }

                    _brightness = value;
                    RaisePropertyChanged();
                }
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
            if (!await Sdk.InitializeSerialPort(PortName, this)) return false;

            return await Task.Run(() => {
                Connected = true;

                var stateResponse = Sdk.SendCommand<StateResponse>(new StateCommand());
                if (!stateResponse.IsValid) {
                    Logger.Error($"Invalid response from flat device. " +
                                 $"Response was: {stateResponse}");
                    Notification.ShowError(Locale.Loc.Instance["LblFlatDeviceInvalidResponse"]);
                    Connected = false;
                    Sdk.Dispose(this);
                    return Connected;
                }

                _lightOn = stateResponse.LightOn;

                var brightResponse = Sdk.SendCommand<GetBrightnessResponse>(new GetBrightnessCommand());
                if (!brightResponse.IsValid) {
                    Logger.Error($"Invalid response from flat device. " +
                                 $"Response was: {brightResponse}");
                    Notification.ShowError(Locale.Loc.Instance["LblFlatDeviceInvalidResponse"]);
                    Connected = false;
                    Sdk.Dispose(this);
                    return Connected;
                }

                _brightness = Math.Round((double)brightResponse.Brightness / (MaxBrightness - MinBrightness), 2);

                var fWResponse = Sdk.SendCommand<FirmwareVersionResponse>(new FirmwareVersionCommand());
                Description = $"{Name} on {Sdk.SerialPort.PortName}. Firmware version: " +
                              $"{(fWResponse.IsValid ? fWResponse.FirmwareVersion.ToString() : "No valid firmware version.")}";

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

        public Task<bool> Open(CancellationToken ct) {
            throw new NotImplementedException();
        }

        public Task<bool> Close(CancellationToken ct) {
            throw new NotImplementedException();
        }

        public bool SupportsOpenClose => false;

        public void SetupDialog() {
            WindowService.ShowDialog(this, "Artesky Flat Box Setup", System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.SingleBorderWindow);
        }
    }
}