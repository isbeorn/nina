#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

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