#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using System;
using ZWOptical.ASISDK;

namespace NINA.Equipment.Equipment.MyCamera {

    public static class ASICameras {

        public static int Count => ASICameraDll.GetNumOfConnectedCameras();

        public static ASICamera GetCamera(int cameraId, IProfileService profileService, IExposureDataFactory exposureDataFactory) {
            if (cameraId >= Count || cameraId < 0)
                throw new IndexOutOfRangeException();

            return new ASICamera(cameraId, profileService, exposureDataFactory);
        }
    }
}