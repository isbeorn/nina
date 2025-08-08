#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using NINA.Core.Locale;
using NINA.Equipment.Utility;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Equipment;
using System.Threading.Tasks;
using NINA.Equipment.Interfaces.ViewModel;

namespace NINA.WPF.Base.ViewModel.Equipment.Dome {

    public class DomeChooserVM : DeviceChooserVM<IDome> {
        public DomeChooserVM(IProfileService profileService,
                             IEquipmentProviders<IDome> equipmentProvicers) : base(profileService, equipmentProvicers) {
        }

        public override async Task GetEquipment() {
            await lockObj.WaitAsync();
            try {
                var devices = new List<IDevice>();

                devices.Add(new DummyDevice(Loc.Instance["LblDomeNoSource"]));

                /* Plugin Providers */
                foreach (var provider in await equipmentProviders.GetProviders()) {
                    try {
                        var cameras = provider.GetEquipment();
                        Logger.Info($"Found {cameras?.Count} {provider.Name} Domes");
                        devices.AddRange(cameras);
                    } catch (Exception ex) {
                        Logger.Error(ex);
                    }
                }

                try {
                    var ascomInteraction = new ASCOMInteraction(profileService);
                    foreach (IDome dome in ascomInteraction.GetDomes()) {
                        devices.Add(dome);
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                /* Alpaca */
                try {
                    var alpacaInteraction = new AlpacaInteraction(profileService);
                    var alpacaDomes = await alpacaInteraction.GetDomes(default);
                    foreach (IDome dome in alpacaDomes) {
                        devices.Add(dome);
                    }
                    Logger.Info($"Found {alpacaDomes?.Count} Alpaca Domes");
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                DetermineSelectedDevice(devices, profileService.ActiveProfile.DomeSettings.Id, profileService.ActiveProfile.DomeSettings.LastDeviceName);

            } finally {
                lockObj.Release();
            }
        }
    }
}