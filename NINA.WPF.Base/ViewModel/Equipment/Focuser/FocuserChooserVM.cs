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
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Equipment.Utility;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZWOptical.ASISDK;

namespace NINA.WPF.Base.ViewModel.Equipment.Focuser {

    public class FocuserChooserVM : DeviceChooserVM<IFocuser> {

        public FocuserChooserVM(
                IProfileService profileService, 
                IEquipmentProviders<IFocuser> equipmentProviders) : base(profileService, equipmentProviders) {
        }

        public override async Task GetEquipment() {
            await lockObj.WaitAsync();
            try {
                var devices = new List<IDevice>();
                devices.Add(new DummyDevice(Loc.Instance["LblNoFocuser"]));

                /* Plugin Providers */
                foreach (var provider in await equipmentProviders.GetProviders()) {
                    try {
                        var pluginDevices = provider.GetEquipment();
                        Logger.Info($"Found {pluginDevices?.Count} {provider.Name} Focusers");
                        devices.AddRange(pluginDevices);
                    } catch (Exception ex) {
                        Logger.Error(ex);
                    }
                }

                /* ZWO focusers */
                try {
                    Logger.Trace("Adding ZWOptical Focusers");

                    var focusers = ASIEAF.GetNum();

                    for (int i = 0; i < focusers; i++) {
                        var focuser = new ASIFocuser(i, profileService);
                        Logger.Debug($"Adding ZWOptical Focuser: {focuser.Name}");
                        devices.Add(focuser);
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                try {
                    var ascomInteraction = new ASCOMInteraction(profileService);
                    devices.AddRange(ascomInteraction.GetFocusers());
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                /* Alpaca */
                try {
                    var alpacaInteraction = new AlpacaInteraction(profileService);
                    var alpacaFocusers = await alpacaInteraction.GetFocusers(default);
                    foreach (IFocuser focuser in alpacaFocusers) {
                        devices.Add(focuser);
                    }
                    Logger.Info($"Found {alpacaFocusers?.Count} Alpaca Focusers");
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                DetermineSelectedDevice(devices, profileService.ActiveProfile.FocuserSettings.Id, profileService.ActiveProfile.FocuserSettings.LastDeviceName);

            } finally {
                lockObj.Release();
            }
        }
    }
}