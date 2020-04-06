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
using NINA.Utility.FlatDeviceSDKs.PegasusAstroSDK;
using NINA.Utility.Notification;
using NINA.Utility.WindowService;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using NINA.Locale;

namespace NINA.Model.MyFlatDevice {

    public class PegasusAstroFlatMaster : BaseINPC, IFlatDevice {
        private readonly IProfileService _profileService;
        private IPegasusFlatMaster _sdk;

        public IPegasusFlatMaster Sdk {
            get => _sdk ?? (_sdk = new PegasusFlatMaster());
            set => _sdk = value;
        }

        public PegasusAstroFlatMaster(IProfileService profileService) {
            _profileService = profileService;
            PortName = profileService.ActiveProfile?.FlatDeviceSettings?.PortName ?? "AUTO";
        }

        public bool HasSetupDialog => !Connected;
        public string Id => "b4aee7ad-effe-4cf8-b02b-fd94f2780974";
        public string Name => $"{Loc.Instance["LblPegasusFlatMaster"]}";
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

        public string DriverInfo => "Last modified 2020/01/31.";
        public string DriverVersion => "1.0";

        public async Task<bool> Connect(CancellationToken token) {
            if (!Sdk.InitializeSerialPort(PortName, this)) return false;
            return await Task.Run(() => {
                Connected = true;
                var statusResponse = Sdk.SendCommand<StatusResponse>(new StatusCommand());
                if (!statusResponse.IsValid) {
                    Logger.Error($"Invalid response from flat device. " +
                                 $"Response was: {statusResponse}.");
                    Notification.ShowError(Loc.Instance["LblFlatDeviceInvalidResponse"]);
                    Connected = false;
                    return Connected;
                }

                var fWResponse = Sdk.SendCommand<FirmwareVersionResponse>(new FirmwareVersionCommand());
                Description = $"FlatMaster on port {PortName}. Firmware version: " +
                              $"{(fWResponse.IsValid ? fWResponse.FirmwareVersion.ToString(CultureInfo.InvariantCulture) : "No valid firmware version.")}";

                RaiseAllPropertiesChanged();
                return Connected;
            }, token);
        }

        public void Disconnect() {
            Connected = false;
            Sdk.Dispose(this);
        }

        public IWindowService WindowService { get; set; } = new WindowService();

        public void SetupDialog() {
            WindowService.ShowDialog(this, "Pegasus Astro FlatMaster Setup", System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.SingleBorderWindow);
        }

        public CoverState CoverState => CoverState.Unknown;
        public int MaxBrightness => 20;
        public int MinBrightness => 220;

        public Task<bool> Open(CancellationToken ct) {
            throw new NotImplementedException();
        }

        public Task<bool> Close(CancellationToken ct) {
            throw new NotImplementedException();
        }

        private bool _lightOn;

        public bool LightOn {
            get => Connected && _lightOn;
            set {
                if (Connected) {
                    var command = new OnOffCommand { On = value };
                    var response = Sdk.SendCommand<OnOffResponse>(command);
                    if (response.IsValid) {
                        _lightOn = value;
                    } else {
                        Logger.Error($"Invalid response from flat device. " +
                                     $"Command was: {command} Response was: {response}.");
                        Notification.ShowError(Loc.Instance["LblFlatDeviceInvalidResponse"]);
                    }
                }
                RaisePropertyChanged();
            }
        }

        private double _brightness;

        public double Brightness {
            get => _brightness;
            set {
                if (Connected) {
                    _brightness = value;
                    if (value < 0d) {
                        _brightness = 0d;
                    }

                    if (value > 1d) {
                        _brightness = 1d;
                    }

                    var command = new SetBrightnessCommand { Brightness = (1d - _brightness) * (MinBrightness - MaxBrightness) + MaxBrightness };
                    var response = Sdk.SendCommand<SetBrightnessResponse>(command);
                    if (!response.IsValid) {
                        Logger.Error($"Invalid response from flat device. " +
                                     $"Command was: {command} Response was: {response}.");
                        Notification.ShowError(Loc.Instance["LblFlatDeviceInvalidResponse"]);
                    }
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

        public bool SupportsOpenClose => false;
    }
}