#region "copyright"
/*
    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

/*
 * Copyright (c) 2019 Dale Ghent <daleg@elemental.org> All rights reserved.
 */
#endregion "copyright"

using NINA.Model.MyCamera;
using NINA.Utility.Profile;
using NINA.Utility;
using System;

namespace QHYCCD {

    public static class QHYCameras {
        private static readonly QHYCamera[] _cameras = new QHYCamera[16];

        public static uint Count {
            get {
                uint num;

                num = LibQHYCCD.ScanQHYCCD();
                Logger.Trace(String.Format("QHYCamera - found {0} camera(s)", num));
                return num;
            }
        }

        public static QHYCamera GetCamera(uint cameraId, IProfileService profileService) {
            if (cameraId > Count)
                throw new IndexOutOfRangeException();

            return _cameras[cameraId] ?? (_cameras[cameraId] = new QHYCamera(cameraId, profileService));
        }
    }
}