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

using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace QHYCCD {

    public static class QHYFilterWheels {

        public static List<string> GetFilterWheels() {
            IntPtr FWheelP;
            StringBuilder cameraId = new StringBuilder(LibQHYCCD.QHYCCD_ID_LEN);
            StringBuilder cameraModel = new StringBuilder(0);
            List<string> FWheels = new List<string>();
            uint positions;
            uint num;

            /*
             * For each camera we find, open it and see if it has a filter wheel.
             * If it has a filter wheel, add the camera's ID to a list
             */
            if ((num = LibQHYCCD.ScanQHYCCD()) > 0) {
                for (uint i = 0; i < num; i++) {
                    LibQHYCCD.N_GetQHYCCDId(i, cameraId);
                    LibQHYCCD.N_GetQHYCCDModel(cameraId, cameraModel);

                    FWheelP = LibQHYCCD.N_OpenQHYCCD(cameraId);

                    if (LibQHYCCD.IsQHYCCDCFWPlugged(FWheelP) == LibQHYCCD.QHYCCD_SUCCESS) {
                        positions = (uint)LibQHYCCD.GetQHYCCDParam(FWheelP, LibQHYCCD.CONTROL_ID.CONTROL_CFWSLOTSNUM);

                        /*
                         * Ensure that the filter wheel we found is reporting that it has filter slots.
                         */
                        if (positions > 0) {
                            Logger.Debug($"QHYCFW: Camera {i} ({cameraId}) has a {positions}-position CFW");
                            FWheels.Add(cameraId.ToString());
                        } else {
                            Logger.Error($"QHYCFW: Camera {i} ({cameraId}) has a filter wheel but says it has {positions} slots! Skipping.");
                        }
                    }

                    LibQHYCCD.N_CloseQHYCCD(FWheelP);
                }
            }

            Logger.Debug($"QHYCFW: Found {FWheels.Count} filter wheel(s)");
            return FWheels;
        }
    }
}