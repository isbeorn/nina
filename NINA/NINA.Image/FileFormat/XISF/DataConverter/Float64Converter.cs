#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace NINA.Image.FileFormat.XISF.DataConverter {

    internal class Float64Converter : IDataConverter {

        public unsafe ushort[] Convert(byte[] rawData) {
            ushort[] data = new ushort[rawData.Length / 8];
            for (var i = 0; i < data.Length; i++) {
                var integer = (((long)rawData[(i * 8) + 7] << 56) | ((long)rawData[(i * 8) + 6] << 48) | ((long)rawData[(i * 8) + 5] << 40) | ((long)rawData[(i * 8) + 4] << 32) | ((long)rawData[(i * 8) + 3] << 24) | ((long)rawData[(i * 8) + 2] << 16) | ((long)rawData[(i * 8) + 1] << 8) | ((long)rawData[i * 8]));
                data[i] = (ushort)((*(double*)&integer) * ushort.MaxValue);
            }
            return data;
        }
    }
}