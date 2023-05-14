#region "copyright"

/*
    Copyright © 2016 - 2023 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using System.IO;

namespace NINA.Image.FileFormat.FITS {

    public class FITSData {
        private int width;
        private int height;
        private FITSRowOrder rowOrder;

        public ushort[] Data { get; }


        public FITSData(ushort[] data, int width, int height, FITSRowOrder rowOrder) {
            this.Data = data;
            this.width = width;
            this.height = height;
            this.rowOrder = rowOrder;
        }

        public void Write(Stream s) {
            if (rowOrder == FITSRowOrder.TOP_DOWN) {
                /* Write image data */
                for (int i = 0; i < this.Data.Length; i++) {
                    var val = (short)(this.Data[i] - (short.MaxValue + 1));
                    s.WriteByte((byte)(val >> 8));
                    s.WriteByte((byte)val);
                }
            } else {
                int row = height - 1;
                var column = 0;
                for (int i = 0; i < this.Data.Length; i++) {
                    var arrayIndex = width * row + (column % width);

                    var val = (short)(this.Data[arrayIndex] - (short.MaxValue + 1));
                    s.WriteByte((byte)(val >> 8));
                    s.WriteByte((byte)val);


                    column++;
                    if (column % width == 0) { row--; }
                }
            }
        }
    }
}