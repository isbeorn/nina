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

    // Stop Bits
    /// <summary>
    /// Permitted stop bits for FTDI devices
    /// </summary>
    public class FT_STOP_BITS {

        /// <summary>
        /// 1 stop bit
        /// </summary>
        public const byte FT_STOP_BITS_1 = 0x00;

        /// <summary>
        /// 2 stop bits
        /// </summary>
        public const byte FT_STOP_BITS_2 = 0x02;
    }
}