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
using NINA.Utility.FlatDeviceSDKs.PegasusAstroSDK;
using NINA.Utility.Notification;
using NINA.Utility.SerialCommunication;
using NINA.Utility.WindowService;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

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

        private static void LogAndNotify(ICommand command, InvalidDeviceResponseException ex) {
            Logger.Error("Invalid response from flat device. " +
                         $"Command was: {command} Response was: {ex.Message}.");
            Notification.ShowError(Locale.Loc.Instance["LblFlatDeviceInvalidResponse"]);
        }

        private void HandlePortClosed(ICommand command, SerialPortClosedException ex) {
            Logger.Error($"Serial port was closed. Command was: {command} Exception: {ex.InnerException}.");
            Notification.ShowError(Locale.Loc.Instance["LblFlatDeviceInvalidResponse"]);
            Disconnect();
            RaiseAllPropertiesChanged();
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
            return await Task.Run(async () => {
                var statusCommand = new StatusCommand();
                try {
                    _ = await Sdk.SendCommand<StatusResponse>(statusCommand);
                    Description = $"FlatMaster on port {PortName}. Firmware version: ";
                    Connected = true;
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
                    var fWResponse = await Sdk.SendCommand<FirmwareVersionResponse>(fwCommand);
                    Description += fWResponse.FirmwareVersion.ToString(CultureInfo.InvariantCulture);
                } catch (InvalidDeviceResponseException ex) {
                    LogAndNotify(statusCommand, ex);
                    Description += Loc.Instance["LblNoValidFirmwareVersion"];
                } catch (SerialPortClosedException ex) {
                    HandlePortClosed(statusCommand, ex);
                }

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

        public Task<bool> Open(CancellationToken ct, int delay = 300) {
            throw new NotImplementedException();
        }

        public Task<bool> Close(CancellationToken ct, int delay = 300) {
            throw new NotImplementedException();
        }

        private bool _lightOn;

        public bool LightOn {
            get => Connected && _lightOn;
            set {
                if (!Connected) return;
                var command = new OnOffCommand { On = value };
                try {
                    _ = Sdk.SendCommand<OnOffResponse>(command).Result;
                    _lightOn = value;
                    RaisePropertyChanged();
                } catch (InvalidDeviceResponseException ex) {
                    LogAndNotify(command, ex);
                } catch (SerialPortClosedException ex) {
                    HandlePortClosed(command, ex);
                }
            }
        }

        private double _brightness;

        public double Brightness {
            get => _brightness;
            set {
                if (!Connected) return;
                _brightness = value;
                if (value < 0d) _brightness = 0d;
                if (value > 1d) _brightness = 1d;

                var command = new SetBrightnessCommand { Brightness = (1d - _brightness) * (MinBrightness - MaxBrightness) + MaxBrightness };
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

        public bool SupportsOpenClose => false;
    }
}
