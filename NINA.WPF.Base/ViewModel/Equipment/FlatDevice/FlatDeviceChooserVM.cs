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
using NINA.Core.Utility;
using NINA.Equipment.Equipment;
using NINA.Equipment.Equipment.MyFlatDevice;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Equipment.Utility;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NINA.WPF.Base.ViewModel.Equipment.FlatDevice {

    public class FlatDeviceChooserVM : DeviceChooserVM<IFlatDevice> {
        public FlatDeviceChooserVM(IProfileService profileService,
                                   IEquipmentProviders<IFlatDevice> equipmentProviders) : base(profileService, equipmentProviders) {
        }

        public override async Task GetEquipment() {
            await lockObj.WaitAsync();
            try {
                var ascomInteraction = new ASCOMInteraction(profileService);
                var devices = new List<IDevice>();
                devices.Add(new DummyDevice(Loc.Instance["LblFlatDeviceNoDevice"]));

                /* Plugin Providers */
                foreach (var provider in await equipmentProviders.GetProviders()) {
                    try {
                        var pluginDevices = provider.GetEquipment();
                        Logger.Info($"Found {pluginDevices?.Count} {provider.Name} Flat Devices");
                        devices.AddRange(pluginDevices);
                    } catch (Exception ex) {
                        Logger.Error(ex);
                    }
                }

                try {
                    foreach (IFlatDevice flatDevice in ascomInteraction.GetCoverCalibrators()) {
                        devices.Add(flatDevice);
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                devices.AddRange(new List<IDevice>{
                    new AllProSpikeAFlat(profileService),
                    new AlnitakFlipFlatSimulator(profileService),
                    new AlnitakFlatDevice(profileService),
                    new ArteskyFlatBox(profileService),
                    new PegasusAstroFlatMaster(profileService)
                });

                DetermineSelectedDevice(devices, profileService.ActiveProfile.FlatDeviceSettings.Id);

            } finally {
                lockObj.Release();
            }
        }
    }
}