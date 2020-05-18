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

namespace NINA.Model.ImageData {

    public class LRGBArrays {
        public ushort[] Lum { get; set; }
        public ushort[] Red { get; set; }
        public ushort[] Green { get; set; }
        public ushort[] Blue { get; set; }

        public LRGBArrays(ushort[] lum, ushort[] red, ushort[] green, ushort[] blue) {
            Lum = lum;
            Red = red;
            Green = green;
            Blue = blue;
        }
    }
}
