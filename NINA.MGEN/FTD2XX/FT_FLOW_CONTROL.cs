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

    // Flow Control
    /// <summary>
    /// Permitted flow control values for FTDI devices
    /// </summary>
    public class FT_FLOW_CONTROL {

        /// <summary>
        /// No flow control
        /// </summary>
        public const ushort FT_FLOW_NONE = 0x0000;

        /// <summary>
        /// RTS/CTS flow control
        /// </summary>
        public const ushort FT_FLOW_RTS_CTS = 0x0100;

        /// <summary>
        /// DTR/DSR flow control
        /// </summary>
        public const ushort FT_FLOW_DTR_DSR = 0x0200;

        /// <summary>
        /// Xon/Xoff flow control
        /// </summary>
        public const ushort FT_FLOW_XON_XOFF = 0x0400;
    }
}