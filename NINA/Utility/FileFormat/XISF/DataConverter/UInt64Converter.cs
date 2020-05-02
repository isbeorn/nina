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

namespace NINA.Utility.FileFormat.XISF.DataConverter {

    internal class UInt64Converter : IDataConverter {

        public ushort[] Convert(byte[] rawData) {
            ushort[] data = new ushort[rawData.Length / 8];
            for (var i = 0; i < data.Length; i++) {
                data[i] = (ushort)((((long)rawData[(i * 8) + 7] << 56) | ((long)rawData[(i * 8) + 6] << 48) | ((long)rawData[(i * 8) + 5] << 40) | ((long)rawData[(i * 8) + 4] << 32) | ((long)rawData[(i * 8) + 3] << 24) | ((long)rawData[(i * 8) + 2] << 16) | ((long)rawData[(i * 8) + 1] << 8) | ((long)rawData[i * 8])) / (double)long.MaxValue * ushort.MaxValue);
            }
            return data;
        }
    }
}