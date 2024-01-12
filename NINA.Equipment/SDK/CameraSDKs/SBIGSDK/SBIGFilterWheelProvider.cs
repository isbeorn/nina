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
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Equipment.SDK.CameraSDKs.SBIGSDK {

    public class SBIGFilterWheelProvider : IEquipmentProvider<IFilterWheel> {
        public string Name => "NINA";
        public string ContentId => this.GetType().FullName;
        private readonly ISbigSdk sbigSdk;
        private readonly IProfileService profileService;

        public SBIGFilterWheelProvider(ISbigSdk sbigSdk, IProfileService profileService) {
            this.sbigSdk = sbigSdk;
            this.profileService = profileService;
        }

        public IList<IFilterWheel> GetEquipment() {
            Logger.Debug("Getting SBIG Filter Wheels");

            var devices = new List<IFilterWheel>();
            foreach (var instance in sbigSdk.QueryUsbDevices()) {
                if (instance.FilterWheelInfo.HasValue && instance.FilterWheelInfo.Value.Model != SDK.CameraSDKs.SBIGSDK.SbigSharp.SBIG.CfwModelSelect.CFWSEL_UNKNOWN) {
                    var fw = new SBIGFilterWheel(sbigSdk, instance, profileService);
                    devices.Add(fw);
                }
            }
            return devices;
        }
    }
}