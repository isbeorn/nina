#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Equipment.MyCamera;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NINA.Image.Interfaces;
using System;

namespace QHYCCD {

    public class QHYCameras(IExposureDataFactory exposureDataFactory) {
        private static readonly QHYCamera[] _cameras = new QHYCamera[16];
        private readonly IExposureDataFactory exposureDataFactory = exposureDataFactory;

        public IQhySdk Sdk { get; set; } = QhySdk.Instance;

        public uint Count {
            get {
                uint num;

                Sdk.InitSdk();
                num = Sdk.Scan();
                Logger.Trace(string.Format("QHYCamera - found {0} camera(s)", num));
                return num;
            }
        }

        public QHYCamera GetCamera(uint cameraId, IProfileService profileService) {
            if (cameraId > Count) {
                throw new IndexOutOfRangeException();
            }

            _cameras[cameraId] = new QHYCamera(cameraId, profileService, exposureDataFactory);
            return _cameras[cameraId];
        }
    }
}