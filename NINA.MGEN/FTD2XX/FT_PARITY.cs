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