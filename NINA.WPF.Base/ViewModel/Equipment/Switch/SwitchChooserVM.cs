#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Equipment.MySwitch;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NINA.Core.Locale;
using NINA.Equipment.Utility;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Equipment;
using NINA.Equipment.Interfaces.ViewModel;

namespace NINA.WPF.Base.ViewModel.Equipment.Switch {

    public class SwitchChooserVM : DeviceChooserVM<ISwitchHub> {
        public SwitchChooserVM(IProfileService profileService,
                               IEquipmentProviders<ISwitchHub> equipmentProviders) : base(profileService, equipmentProviders) {
        }

        public override async Task GetEquipment() {
            await lockObj.WaitAsync();
            try {
                var ascomInteraction = new ASCOMInteraction(profileService);
                var devices = new List<IDevice>();

                devices.Add(new DummyDevice(Loc.Instance["LblNoSwitch"]));

                /* Plugin Providers */
                foreach (var provider in await equipmentProviders.GetProviders()) {
                    try {
                        var pluginDevices = provider.GetEquipment();
                        Logger.Info($"Found {pluginDevices?.Count} {provider.Name} Switch Hubs");
                        devices.AddRange(pluginDevices);
                    } catch (Exception ex) {
                        Logger.Error(ex);
                    }
                }

                /* ASCOM */
                try {
                    foreach (ISwitchHub ascomSwitch in ascomInteraction.GetSwitches()) {
                        devices.Add(ascomSwitch);
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                DetermineSelectedDevice(devices, profileService.ActiveProfile.SwitchSettings.Id);

            } finally {
                lockObj.Release();
            }
        }
    }
}