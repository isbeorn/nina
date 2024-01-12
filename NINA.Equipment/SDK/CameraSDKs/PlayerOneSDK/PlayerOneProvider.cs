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
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Equipment.SDK.CameraSDKs.PlayerOneSDK {
    [Export(typeof(IEquipmentProvider))]
    public class PlayerOneProvider : IEquipmentProvider<ICamera> {
        public string Name => "PlayerOne";
        public string ContentId => this.GetType().FullName;
        private IProfileService profileService;
        private IPlayerOnePInvokeProxy playerOnePInvoke;
        private IExposureDataFactory exposureDataFactory;

        [ImportingConstructor]
        public PlayerOneProvider(IProfileService profileService, IExposureDataFactory exposureDataFactory) : this(profileService, exposureDataFactory, new PlayerOnePInvokeProxy()) {
        }

        public PlayerOneProvider(IProfileService profileService, IExposureDataFactory exposureDataFactory, IPlayerOnePInvokeProxy playerOnePInvoke) {
            this.profileService = profileService;
            this.exposureDataFactory = exposureDataFactory;
            this.playerOnePInvoke = playerOnePInvoke;
        }

        public IList<ICamera> GetEquipment() {
            Logger.Debug("Getting Player One cameras");
            var devices = new List<ICamera>();
            var cameras = playerOnePInvoke.POAGetCameraCount();
            if (cameras > 0) {
                for (var i = 0; i < cameras; i++) {
                    var err = playerOnePInvoke.POAGetCameraProperties(i, out var props);
                    if(err == POAErrors.POA_OK) {                        
                        var pOneCamera = new GenericCamera(props.cameraID, props.cameraModelName, "Player One", playerOnePInvoke.POAGetSDKVersion(), false, new PlayerOneSDK(props.cameraID, playerOnePInvoke), profileService, exposureDataFactory);
                        Logger.Debug($"Adding PlayerOne camera {i}: {props.cameraID} (as {props.cameraModelName})");
                        devices.Add(pOneCamera);
                    }
                }
            }
            return devices;
        }
    }
}
