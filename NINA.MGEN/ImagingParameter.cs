#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

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

namespace NINA.MGEN {

    public class ImagingParameter {

        public ImagingParameter(int gain, int exposureTime, int threshold) {
            this.Gain = gain;
            this.ExposureTime = exposureTime;
            this.Threshold = threshold;
        }

        public int Gain { get; private set; }
        public int ExposureTime { get; private set; }
        public int Threshold { get; private set; }
    }
}