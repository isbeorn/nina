#region "copyright"

/*
    Copyright � 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Equipment.MyWeatherData;
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

namespace NINA.WPF.Base.ViewModel.Equipment.WeatherData {

    public class WeatherDataChooserVM : DeviceChooserVM<IWeatherData> {

        public WeatherDataChooserVM(IProfileService profileService,
                                    IEquipmentProviders<IWeatherData> equipmentProviders) : base(profileService, equipmentProviders) {
        }

        public override async Task GetEquipment() {
            await lockObj.WaitAsync();
            try {
                var devices = new List<IDevice>();

                devices.Add(new DummyDevice(Loc.Instance["LblWeatherNoSource"]));

                /* Plugin Providers */
                foreach (var provider in await equipmentProviders.GetProviders()) {
                    try {
                        var pluginDevices = provider.GetEquipment();
                        Logger.Info($"Found {pluginDevices?.Count} {provider.Name} Weather Data");
                        devices.AddRange(pluginDevices);
                    } catch (Exception ex) {
                        Logger.Error(ex);
                    }
                }

                /* INDI weather data */
                try {
                    var indiWeatherData = await INDIInteraction.GetWeatherData();
                    devices.AddRange(indiWeatherData);
                    Logger.Info($"Found {indiWeatherData?.Count} INDI Weather Data");
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                devices.Add(new OpenWeatherMap(this.profileService));
                devices.Add(new TheWeatherCompany(this.profileService));
                devices.Add(new WeatherUnderground(this.profileService));
                devices.Add(new OpenMeteo(this.profileService));

                DetermineSelectedDevice(devices, profileService.ActiveProfile.WeatherDataSettings.Id, profileService.ActiveProfile.WeatherDataSettings.LastDeviceName);

            } finally {
                lockObj.Release();
            }
        }
    }
}
