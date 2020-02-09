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

using NINA.Locale;
using NINA.Model.MySwitch.PegasusAstro;
using NINA.Profile;
using NINA.Utility;
using NINA.Utility.Notification;
using NINA.Utility.SwitchSDKs.PegasusAstro;
using NINA.Utility.WindowService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MySwitch {

    public class UltimatePowerBoxV2 : BaseINPC, ISwitchHub, IDisposable {
        private readonly IProfileService _profileService;
        private const string AUTO = "AUTO";
        public IPegasusDevice Sdk { get; set; } = PegasusDevice.Instance;

        public UltimatePowerBoxV2(IProfileService profileService) {
            _profileService = profileService;
            PortName = profileService?.ActiveProfile?.SwitchSettings?.Upbv2PortName ?? AUTO;
        }

        public ReadOnlyCollection<string> PortNames => Sdk?.PortNames;

        public string PortName {
            get => _profileService.ActiveProfile.SwitchSettings.Upbv2PortName;
            set {
                _profileService.ActiveProfile.SwitchSettings.Upbv2PortName = value;
                RaisePropertyChanged();
            }
        }

        public bool HasSetupDialog => !Connected;
        public string Id => "21f52eee-842b-42f3-92ab-901952d07718";
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

                    var powerSwitch = new PegasusAstroPowerSwitch(0) {
                        Name = _profileService.ActiveProfile.SwitchSettings.Upbv2PowerName1
                    };
                    PowerSwitches.Add(powerSwitch);
                    Switches.Add(powerSwitch);
                    powerSwitch = new PegasusAstroPowerSwitch(1) {
                        Name = _profileService.ActiveProfile.SwitchSettings.Upbv2PowerName2
                    };
                    PowerSwitches.Add(powerSwitch);
                    Switches.Add(powerSwitch);
                    powerSwitch = new PegasusAstroPowerSwitch(2) {
                        Name = _profileService.ActiveProfile.SwitchSettings.Upbv2PowerName3
                    };
                    PowerSwitches.Add(powerSwitch);
                    Switches.Add(powerSwitch);
                    powerSwitch = new PegasusAstroPowerSwitch(3) {
                        Name = _profileService.ActiveProfile.SwitchSettings.Upbv2PowerName4
                    };
                    PowerSwitches.Add(powerSwitch);
                    Switches.Add(powerSwitch);

                    var variablePowerSwitch = new VariablePowerSwitch {
                        Name = _profileService.ActiveProfile.SwitchSettings.Upbv2PowerName5
                    };
                    PowerSwitches.Add(variablePowerSwitch);
                    Switches.Add(variablePowerSwitch);

                    var usbSwitch = new PegasusAstroUsbSwitch(0) {
                        Name = _profileService.ActiveProfile.SwitchSettings.Upbv2UsbName1
                    };
                    UsbSwitches.Add(usbSwitch);
                    Switches.Add(usbSwitch);
                    usbSwitch = new PegasusAstroUsbSwitch(1) {
                        Name = _profileService.ActiveProfile.SwitchSettings.Upbv2UsbName2
                    };
                    UsbSwitches.Add(usbSwitch);
                    Switches.Add(usbSwitch);
                    usbSwitch = new PegasusAstroUsbSwitch(2) {
                        Name = _profileService.ActiveProfile.SwitchSettings.Upbv2UsbName3
                    };
                    UsbSwitches.Add(usbSwitch);
                    Switches.Add(usbSwitch);
                    usbSwitch = new PegasusAstroUsbSwitch(3) {
                        Name = _profileService.ActiveProfile.SwitchSettings.Upbv2UsbName4
                    };
                    UsbSwitches.Add(usbSwitch);
                    Switches.Add(usbSwitch);
                    usbSwitch = new PegasusAstroUsbSwitch(4) {
                        Name = _profileService.ActiveProfile.SwitchSettings.Upbv2UsbName5
                    };
                    UsbSwitches.Add(usbSwitch);
                    Switches.Add(usbSwitch);
                    usbSwitch = new PegasusAstroUsbSwitch(5) {
                        Name = _profileService.ActiveProfile.SwitchSettings.Upbv2UsbName6
                    };
                    UsbSwitches.Add(usbSwitch);
                    Switches.Add(usbSwitch);

                    for (short i = 0; i < 3; i++) {
                        var dewHeater = new DewHeater(i);
                        DewHeaters.Add(dewHeater);
                        Switches.Add(dewHeater);
                    }

                    DataProvider = new DataProviderSwitch();
                    Switches.Add(DataProvider);

                    Connected = true;
                    RaiseAllPropertiesChanged();
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                return Connected;
            }, token);
        }

        public void Disconnect() {
            if (!Connected) return;
            Connected = false;
            PowerSwitches.Clear();
            UsbSwitches.Clear();
            DewHeaters.Clear();
            DataProvider = null;
            Switches.Clear();
            Sdk.Dispose(this);
        }

        public IWindowService WindowService { get; set; } = new WindowService();

        public void SetupDialog() {
            WindowService.ShowDialog(this, "Ultimate Powerbox V2 Setup", System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.SingleBorderWindow);
        }

        public ICollection<ISwitch> PowerSwitches { get; } = new AsyncObservableCollection<ISwitch>();
        public ICollection<ISwitch> UsbSwitches { get; } = new AsyncObservableCollection<ISwitch>();
        public ICollection<ISwitch> DewHeaters { get; } = new AsyncObservableCollection<ISwitch>();
        public ICollection<ISwitch> Switches { get; } = new AsyncObservableCollection<ISwitch>();
        public ISwitch DataProvider { get; protected set; }

        public void Dispose() {
            if (Connected) Disconnect();
        }
    }
}