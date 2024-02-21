#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Interfaces;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;

namespace NINA.Equipment.SDK.CameraSDKs.SVBonySDK {

    [Export(typeof(IEquipmentProvider))]
    public class SVBonyProvider : IEquipmentProvider<ICamera> {
        public string Name => "SVBony";
        public string ContentId => this.GetType().FullName;

        private IProfileService profileService;
        private ISVBonyPInvokeProxy sVBonyPInvoke;
        private IExposureDataFactory exposureDataFactory;

        [ExcludeFromCodeCoverage]
        [ImportingConstructor]
        public SVBonyProvider(IProfileService profileService, IExposureDataFactory exposureDataFactory) : this(profileService, exposureDataFactory, new SVBonyPInvokeProxy()) {
        }

        public SVBonyProvider(IProfileService profileService, IExposureDataFactory exposureDataFactory, ISVBonyPInvokeProxy sVBonyPInvoke) {
            this.profileService = profileService;
            this.exposureDataFactory = exposureDataFactory;
            this.sVBonyPInvoke = sVBonyPInvoke;
        }

        public IList<ICamera> GetEquipment() {
            Logger.Debug("Getting SVBony Cameras");
            var devices = new List<ICamera>();
            var cameras = sVBonyPInvoke.SVBGetNumOfConnectedCameras();
            if (cameras > 0) {
                for (var i = 0; i < cameras; i++) {
                    var info = sVBonyPInvoke.GetCameraInfo(i);

                    Logger.Debug($"Found SVBony camera - id: {info.CameraID}; name: {info.FriendlyName}");
                    devices.Add(new SVBonyCamera(info.CameraSN, info.FriendlyName, sVBonyPInvoke.GetSDKVersion(), new SVBonySDK((int)info.CameraID), profileService, exposureDataFactory));
                }
            }
            return devices;
        }
    }
}