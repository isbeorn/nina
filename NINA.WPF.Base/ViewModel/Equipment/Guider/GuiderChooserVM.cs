#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Equipment.MyGuider;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Equipment.MyGuider.PHD2;
using NINA.Core.Utility;
using System;
using System.IO;
using System.Collections.Generic;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Equipment.MyGuider.SkyGuard;
using System.Threading.Tasks;
using NINA.Equipment.Interfaces.ViewModel;

namespace NINA.WPF.Base.ViewModel.Equipment.Guider {

    public class GuiderChooserVM : DeviceChooserVM<IGuider> {
        private readonly ICameraMediator cameraMediator;
        private readonly ITelescopeMediator telescopeMediator;
        private readonly IWindowServiceFactory windowServiceFactory;

        public GuiderChooserVM(IProfileService profileService,
                               ICameraMediator cameraMediator,
                               ITelescopeMediator telescopeMediator,
                               IWindowServiceFactory windowServiceFactory,
                               IEquipmentProviders<IGuider> equipmentProviders) : base(profileService, equipmentProviders) {
            this.cameraMediator = cameraMediator;
            this.profileService = profileService;
            this.telescopeMediator = telescopeMediator;
            this.windowServiceFactory = windowServiceFactory;
        }

        public override async Task GetEquipment() {
            await lockObj.WaitAsync();
            try {
                var devices = new List<IDevice>();
                devices.Add(new DummyGuider(profileService));
                devices.Add(new PHD2Guider(profileService, windowServiceFactory));
                devices.Add(new DirectGuider(profileService, telescopeMediator));
                devices.Add(new MetaGuideGuider(profileService, windowServiceFactory));
                devices.Add(new SkyGuardGuider(profileService, windowServiceFactory));

                /* Plugin Providers */
                foreach (var provider in await equipmentProviders.GetProviders()) {
                    try {
                        var pluginDevices = provider.GetEquipment();
                        Logger.Info($"Found {pluginDevices?.Count} {provider.Name} Guiders");
                        devices.AddRange(pluginDevices);
                    } catch (Exception ex) {
                        Logger.Error(ex);
                    }
                }

                DetermineSelectedDevice(devices, profileService.ActiveProfile.GuiderSettings.GuiderName, profileService.ActiveProfile.GuiderSettings.LastDeviceName);

            } finally {
                lockObj.Release();
            }
        }
    }
}
