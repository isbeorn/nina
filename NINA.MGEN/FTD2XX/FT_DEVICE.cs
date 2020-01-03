#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FTD2XX_NET {

    // Device type identifiers for FT_GetDeviceInfoDetail and FT_GetDeviceInfo
    /// <summary>
    /// List of FTDI device types
    /// </summary>
    public enum FT_DEVICE {

        /// <summary>
        /// FT232B or FT245B device
        /// </summary>
        FT_DEVICE_BM = 0,

        /// <summary>
        /// FT8U232AM or FT8U245AM device
        /// </summary>
        FT_DEVICE_AM,

        /// <summary>
        /// FT8U100AX device
        /// </summary>
        FT_DEVICE_100AX,

        /// <summary>
        /// Unknown device
        /// </summary>
        FT_DEVICE_UNKNOWN,

        /// <summary>
        /// FT2232 device
        /// </summary>
        FT_DEVICE_2232,

        /// <summary>
        /// FT232R or FT245R device
        /// </summary>
        FT_DEVICE_232R,

        /// <summary>
        /// FT2232H device
        /// </summary>
        FT_DEVICE_2232H,

        /// <summary>
        /// FT4232H device
        /// </summary>
        FT_DEVICE_4232H,

        /// <summary>
        /// FT232H device
        /// </summary>
        FT_DEVICE_232H,

        /// <summary>
        /// FT X-Series device
        /// </summary>
        FT_DEVICE_X_SERIES,

        /// <summary>
        /// FT4222 hi-speed device Mode 0 - 2 interfaces
        /// </summary>
        FT_DEVICE_4222H_0,

        /// <summary>
        /// FT4222 hi-speed device Mode 1 or 2 - 4 interfaces
        /// </summary>
        FT_DEVICE_4222H_1_2,

        /// <summary>
        /// FT4222 hi-speed device Mode 3 - 1 interface
        /// </summary>
        FT_DEVICE_4222H_3,

        /// <summary>
        /// OTP programmer board for the FT4222.
        /// </summary>
        FT_DEVICE_4222_PROG,
    };
}