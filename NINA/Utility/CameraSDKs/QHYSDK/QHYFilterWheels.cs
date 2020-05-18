#region "copyright"

/*
    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
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
