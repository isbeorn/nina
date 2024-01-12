#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Equipment.MyRotator;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using NINA.Core.Locale;
using NINA.Equipment.Utility;
using NINA.Equipment.Equipment;
using NINA.Equipment.Interfaces;
using System.Threading.Tasks;
using NINA.Equipment.Interfaces.ViewModel;

namespace NINA.WPF.Base.ViewModel.Equipment.Rotator {

    public class RotatorChooserVM : DeviceChooserVM<IRotator> {
        public RotatorChooserVM(IProfileService profileService,
                                IEquipmentProviders<IRotator> equipmentProviders) : base(profileService, equipmentProviders) {
        }

        public override async Task GetEquipment() {
            await lockObj.WaitAsync();
            try {
                var ascomInteraction = new ASCOMInteraction(profileService);
                var devices = new List<IDevice>();

                devices.Add(new DummyDevice(Loc.Instance["LblNoRotator"]));
                
                /* Plugin Providers */
                foreach (var provider in await equipmentProviders.GetProviders()) {
                    try {
                        var pluginDevices = provider.GetEquipment();
                        Logger.Info($"Found {pluginDevices?.Count} {provider.Name} Rotators");
                        devices.AddRange(pluginDevices);
                    } catch (Exception ex) {
                        Logger.Error(ex);
                    }
                }

                try {
                    foreach (IRotator rotator in ascomInteraction.GetRotators()) {
                        devices.Add(rotator);
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                devices.Add(new ManualRotator(profileService));

                DetermineSelectedDevice(devices, profileService.ActiveProfile.RotatorSettings.Id);

            } finally {
                lockObj.Release();
            }
        }
    }
}