#region "copyright"

/*
    Copyright © 2016 - 2025 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Equipment.MyFilterWheel;
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
using ZWOptical.EFWSDK;
using NINA.Equipment.Equipment.MyCamera.ToupTekAlike;

namespace NINA.WPF.Base.ViewModel.Equipment.FilterWheel {

    public class FilterWheelChooserVM : DeviceChooserVM<IFilterWheel> {

        public FilterWheelChooserVM(IProfileService profileService,
                                    IEquipmentProviders<IFilterWheel> equipmentProviders) : base(profileService, equipmentProviders) {
        }

        public override async Task GetEquipment() {
            await lockObj.WaitAsync();
            try {
                var devices = new List<IDevice>();

                devices.Add(new DummyDevice(Loc.Instance["LblNoFilterwheel"]));

                /* ZWO filter wheels */
                try {
                    Logger.Trace("Adding ZWOptical filter wheels");

                    var wheels = EFWdll.GetNum();

                    for (int i = 0; i < wheels; i++) {
                        var fw = new ASIFilterWheel(i, profileService);
                        Logger.Debug($"Adding ZWOptical Filter Wheel: {fw.Name}");
                        devices.Add(fw);
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                /* ToupTek */
                try {
                    var toupTekWheels = ToupTek.ToupCam.EnumV2();
                    Logger.Info($"Found {toupTekWheels?.Length} ToupTek Filter Wheels");
                    foreach (var instance in toupTekWheels) {
                        var info = instance.ToDeviceInfo();
                        if (((ToupTekAlikeFlag)info.model.flag & ToupTekAlikeFlag.FLAG_FILTERWHEEL) > 0) {
                            var wheel = new ToupTekAlikeFilterWheel(info, new ToupTekSDKWrapper(), profileService);
                            devices.Add(wheel);
                        }
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                /* Plugin Providers */
                foreach (var provider in await equipmentProviders.GetProviders()) {
                    try {
                        var cameras = provider.GetEquipment();
                        Logger.Info($"Found {cameras?.Count} {provider.Name} Filter Wheels");
                        devices.AddRange(cameras);
                    } catch (Exception ex) {
                        Logger.Error(ex);
                    }
                }

                /* INDI telescopes */
                try {
                    var indiInteraction = new INDIInteraction(profileService);
                    var indiFilterWheel = await indiInteraction.GetFilterWheels();
                    devices.AddRange(indiFilterWheel);
                    Logger.Info($"Found {indiFilterWheel?.Count} INDI Filter Wheels");
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                /* INDIGO filter wheels */
                /*                try {
                                    var indigoInteraction = new INDIGOInteraction(profileService);
                                    var indigoFilterWheels = indigoInteraction.GetFilterWheels();
                                    devices.AddRange(indigoFilterWheels);
                                    Logger.Info($"Found {indigoFilterWheels?.Count} INDIGO FilterWheels");
                                } catch (Exception ex) {
                                    Logger.Error(ex);
                                }
                */
                devices.Add(new ManualFilterWheel(this.profileService));

                DetermineSelectedDevice(devices, profileService.ActiveProfile.FilterWheelSettings.Id, profileService.ActiveProfile.FilterWheelSettings.LastDeviceName);

            } finally {
                lockObj.Release();
            }
        }
    }
}
