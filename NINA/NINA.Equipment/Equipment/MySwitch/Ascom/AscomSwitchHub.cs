#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using ASCOM.Common.DeviceInterfaces;
using ASCOM.Com.DriverAccess;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ASCOM.Alpaca.Discovery;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NINA.Equipment.Equipment.MySwitch.Ascom {

    public partial class AscomSwitchHub : AscomDevice<ISwitchV3>, ISwitchHub, IDisposable {
        public AscomSwitchHub(string id, string name) : base(id, name) {
            switches = new AsyncObservableCollection<ISwitch>();
        }
        public AscomSwitchHub(AscomDevice deviceMeta) : base(deviceMeta) {
            switches = new AsyncObservableCollection<ISwitch>();
        }

        [ObservableProperty]
        private ICollection<ISwitch> switches;

        protected override string ConnectionLostMessage => Loc.Instance["LblSwitchConnectionLost"];

        private async Task ScanForSwitches() {
            Logger.Trace("Scanning for Ascom Switches");
            var numberOfSwitches = device.MaxSwitch;
            for (short i = 0; i < numberOfSwitches; i++) {
                try {
                    var canWrite = device.CanWrite(i);

                    if (canWrite) {
                        Logger.Trace($"Writable Switch found for index {i}");
                        var s = new AscomWritableSwitch(device, i);
                        Switches.Add(s);
                    } else {
                        Logger.Trace($"Readable Switch found for index {i}");
                        var s = new AscomSwitch(device, i);
                        Switches.Add(s);
                    }
                } catch (ASCOM.MethodNotImplementedException e) {
                    Logger.Trace($"MethodNotImplementedException for Switch index {i}: {e.Message}");
                    //ISwitchV1 Fallbacks
                    try {                        
                        var s = new AscomWritableV1Switch(device, i);
                        s.TargetValue = s.Value;
                        s.SetValue();
                        Logger.Trace($"Writable v1 Switch found for index {i}");
                        Switches.Add(s);
                    } catch (Exception e2) {
                        Logger.Trace($"Error occurred for Switch index {i} and it is thus most likely a readable v1 Switch: {e2.Message}");
                        var s = new AscomV1Switch(device, i);
                        Switches.Add(s);
                    }
                }
            }
        }

        protected override async Task PreConnect() {
            Switches = new AsyncObservableCollection<ISwitch>();
        }

        protected override async Task PostConnect() {
            await ScanForSwitches();
        }

        protected override void PostDisconnect() {
            Switches = new AsyncObservableCollection<ISwitch>();
        }

        protected override ISwitchV3 GetInstance() {
            if (!IsAlpacaDevice()) {
                return new Switch(Id);
            } else {
                return new ASCOM.Alpaca.Clients.AlpacaSwitch(deviceMeta.ServiceType, deviceMeta.IpAddress, deviceMeta.IpPort, deviceMeta.AlpacaDeviceNumber, false, null);
            }
        }
    }
}