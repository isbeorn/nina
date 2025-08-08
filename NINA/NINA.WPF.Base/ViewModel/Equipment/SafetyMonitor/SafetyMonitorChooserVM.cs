#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Equipment.MySafetyMonitor;
using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NINA.Core.Locale;
using NINA.Equipment.Utility;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Equipment;
using NINA.WPF.Base.Model.Equipment.MySafetyMonitor.Simulator;
using NINA.Equipment.Interfaces.ViewModel;

namespace NINA.WPF.Base.ViewModel.Equipment.SafetyMonitor {

    public class SafetyMonitorChooserVM : DeviceChooserVM<ISafetyMonitor> {
        public SafetyMonitorChooserVM(IProfileService profileService,
                                      IEquipmentProviders<ISafetyMonitor> equipmentProviders) : base(profileService, equipmentProviders) {
        }

        public override async Task GetEquipment() {
            await lockObj.WaitAsync();
            try {
                var devices = new List<IDevice>();

                devices.Add(new DummyDevice(Loc.Instance["LblNoSafetyMonitor"]));

                /* Plugin Providers */
                foreach (var provider in await equipmentProviders.GetProviders()) {
                    try {
                        var pluginDevices = provider.GetEquipment();
                        Logger.Info($"Found {pluginDevices?.Count} {provider.Name} Safety Monitors");
                        devices.AddRange(pluginDevices);
                    } catch (Exception ex) {
                        Logger.Error(ex);
                    }
                }

                try {
                    var ascomInteraction = new ASCOMInteraction(profileService);
                    foreach (ISafetyMonitor safetyMonitor in ascomInteraction.GetSafetyMonitors()) {
                        devices.Add(safetyMonitor);
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                /* Alpaca */
                try {
                    var alpacaInteraction = new AlpacaInteraction(profileService);
                    var alpacaSafetyMonitors = await alpacaInteraction.GetSafetyMonitors(default);
                    foreach (ISafetyMonitor safetyMonitor in alpacaSafetyMonitors) {
                        devices.Add(safetyMonitor);
                    }
                    Logger.Info($"Found {alpacaSafetyMonitors?.Count} Alpaca Safety Monitors");
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                devices.Add(new SafetyMonitorSimulator());

                DetermineSelectedDevice(devices, profileService.ActiveProfile.SafetyMonitorSettings.Id, profileService.ActiveProfile.SafetyMonitorSettings.LastDeviceName);

            } finally {
                lockObj.Release();
            }
        }
    }
}