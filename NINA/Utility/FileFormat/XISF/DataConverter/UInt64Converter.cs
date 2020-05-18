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
