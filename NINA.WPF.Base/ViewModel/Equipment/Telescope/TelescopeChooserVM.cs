#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Equipment.MyTelescope;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using NINA.Equipment.Utility;
using NINA.Core.Locale;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Equipment;
using System.Threading.Tasks;
using NINA.Equipment.Interfaces.ViewModel;

namespace NINA.WPF.Base.ViewModel.Equipment.Telescope {

    public class TelescopeChooserVM : DeviceChooserVM<ITelescope> {

        public TelescopeChooserVM(IProfileService profileService,
                                  IEquipmentProviders<ITelescope> equipmentProviders) : base(profileService, equipmentProviders) {
        }

        public override async Task GetEquipment() {
            await lockObj.WaitAsync();
            try {
                var devices = new List<IDevice>();

                devices.Add(new DummyDevice(Loc.Instance["LblNoMount"]));

                /* Plugin Providers */
                foreach (var provider in await equipmentProviders.GetProviders()) {
                    try {
                        var pluginDevices = provider.GetEquipment();
                        Logger.Info($"Found {pluginDevices?.Count} {provider.Name} Telescopes");
                        devices.AddRange(pluginDevices);
                    } catch (Exception ex) {
                        Logger.Error(ex);
                    }
                }

                try {
                    var ascomInteraction = new ASCOMInteraction(profileService);
                    foreach (ITelescope telescope in ascomInteraction.GetTelescopes()) {
                        devices.Add(telescope);
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                /* Alpaca */
                try {
                    var alpacaInteraction = new AlpacaInteraction(profileService);
                    var alpacaTelescopes = await alpacaInteraction.GetTelescopes(default);
                    foreach (ITelescope t in alpacaTelescopes) {
                        devices.Add(t);
                    }
                    Logger.Info($"Found {alpacaTelescopes?.Count} Alpaca Telescopes");
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                DetermineSelectedDevice(devices, profileService.ActiveProfile.TelescopeSettings.Id, profileService.ActiveProfile.TelescopeSettings.LastDeviceName);

            } finally {
                lockObj.Release();
            }
        }
    }
}