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
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Equipment.SDK.CameraSDKs.SBIGSDK.SbigSharp;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Equipment.SDK.CameraSDKs.SBIGSDK {

    public class SBIGCameraProvider : IEquipmentProvider<ICamera> {
        public string Name => "SBIG";
        public string ContentId => this.GetType().FullName;
        private readonly ISbigSdk sbigSdk;
        private readonly IProfileService profileService;
        private readonly IExposureDataFactory exposureDataFactory;

        public SBIGCameraProvider(ISbigSdk sbigSdk, IProfileService profileService, IExposureDataFactory exposureDataFactory) {
            this.sbigSdk = sbigSdk;
            this.profileService = profileService;
            this.exposureDataFactory = exposureDataFactory;
        }

        public IList<ICamera> GetEquipment() {
            Logger.Debug("Getting SBIG Cameras");

            var devices = new List<ICamera>();
            foreach (var instance in sbigSdk.QueryUsbDevices()) {
                if (instance.CameraType != SbigSharp.SBIG.CameraType.NoCamera) {
                    var cam = new SBIGCamera(sbigSdk, SBIG.CCD.Imaging, instance, profileService, exposureDataFactory);
                    devices.Add(cam);
                }
            }
            return devices;
        }
    }
}