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

    // Parity
    /// <summary>
    /// Permitted parity values for FTDI devices
    /// </summary>
    public class FT_PARITY {

        /// <summary>
        /// No parity
        /// </summary>
        public const byte FT_PARITY_NONE = 0x00;

        /// <summary>
        /// Odd parity
        /// </summary>
        public const byte FT_PARITY_ODD = 0x01;

        /// <summary>
        /// Even parity
        /// </summary>
        public const byte FT_PARITY_EVEN = 0x02;

        /// <summary>
        /// Mark parity
        /// </summary>
        public const byte FT_PARITY_MARK = 0x03;

        /// <summary>
        /// Space parity
        /// </summary>
        public const byte FT_PARITY_SPACE = 0x04;
    }
}