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

    // Word Lengths
    /// <summary>
    /// Permitted data bits for FTDI devices
    /// </summary>
    public class FT_DATA_BITS {

        /// <summary>
        /// 8 data bits
        /// </summary>
        public const byte FT_BITS_8 = 0x08;

        /// <summary>
        /// 7 data bits
        /// </summary>
        public const byte FT_BITS_7 = 0x07;
    }
}
