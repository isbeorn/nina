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
using System.Collections.Generic;
using System.Text;

namespace QHYCCD {

    public class QHYFilterWheels {
        public IQhySdk Sdk { get; set; } = QhySdk.Instance;

        public List<string> GetFilterWheels() {
            StringBuilder cameraId = new StringBuilder(QhySdk.QHYCCD_ID_LEN);
            StringBuilder cameraModel = new StringBuilder(0);
            List<string> FWheels = new List<string>();
            uint positions;
            uint num;

            /*
             * For each camera we find, open it and see if it has a filter wheel.
             * If it has a filter wheel, add the camera's ID to a list
             */
            Sdk.InitSdk();

            if ((num = Sdk.Scan()) > 0) {
                for (uint i = 0; i < num; i++) {
                    Sdk.GetId(i, cameraId);
                    Sdk.GetModel(cameraId, cameraModel);

                    Sdk.Open(cameraId);

                    if (Sdk.IsCfwPlugged()) {
                        positions = (uint)Sdk.GetControlValue(QhySdk.CONTROL_ID.CONTROL_CFWSLOTSNUM);

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

                    Sdk.Close();
                }
            }

            Logger.Debug($"QHYCFW: Found {FWheels.Count} filter wheel(s)");
            return FWheels;
        }
    }
}