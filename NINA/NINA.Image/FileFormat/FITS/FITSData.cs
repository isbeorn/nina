#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.IO;

namespace NINA.Image.FileFormat.FITS {
    public class FITSData {
        public ushort[] Data { get; }

        public FITSData(ushort[] data) {
            this.Data = data;
        }

        public void Write(Stream s) {
            /* Write image data */
            for (int i = 0; i < this.Data.Length; i++) {
                var val = (short)(this.Data[i] - (short.MaxValue + 1));
                s.WriteByte((byte)(val >> 8));
                s.WriteByte((byte)val);
            }
        }
    }
}