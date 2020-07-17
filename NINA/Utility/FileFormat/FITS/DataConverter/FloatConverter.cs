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

namespace NINA.Utility.FileFormat.FITS.DataConverter {

    public class FloatConverter : IDataConverter {

        public ushort[] Convert(Array[] rawData, int width, int height) {
            ushort[] pixels = new ushort[width * height];
            var i = 0;
            foreach (var row in rawData) {
                foreach (object val in row) {
                    pixels[i++] = (ushort)((float)val * ushort.MaxValue);
                }
            }
            return pixels;
        }
    }
}