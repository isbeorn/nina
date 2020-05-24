#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FTD2XX_NET {

    /// <summary>
    /// Type that holds device information for GetDeviceInformation method.
    /// Used with FT_GetDeviceInfo and FT_GetDeviceInfoDetail in FTD2XX.DLL
    /// </summary>
    public class FT_DEVICE_INFO_NODE {

        /// <summary>
        /// Indicates device state.  Can be any combination of the following: FT_FLAGS_OPENED, FT_FLAGS_HISPEED
        /// </summary>
        public UInt32 Flags;

        /// <summary>
        /// Indicates the device type.  Can be one of the following: FT_DEVICE_232R, FT_DEVICE_2232C, FT_DEVICE_BM, FT_DEVICE_AM, FT_DEVICE_100AX or FT_DEVICE_UNKNOWN
        /// </summary>
        public FT_DEVICE Type;

        /// <summary>
        /// The Vendor ID and Product ID of the device
        /// </summary>
        public UInt32 ID;

        /// <summary>
        /// The physical location identifier of the device
        /// </summary>
        public UInt32 LocId;

        /// <summary>
        /// The device serial number
        /// </summary>
        public string SerialNumber;

        /// <summary>
        /// The device description
        /// </summary>
        public string Description;

        /// <summary>
        /// The device handle.  This value is not used externally and is provided for information only.
        /// If the device is not open, this value is 0.
        /// </summary>
        public IntPtr ftHandle;
    }
}