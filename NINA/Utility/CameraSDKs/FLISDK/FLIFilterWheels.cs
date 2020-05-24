#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility;
using System.Collections.Generic;
using System.Linq;

namespace FLI {

    public static class FLIFilterWheels {

        public static List<string> GetFilterWheels() {
            List<string> fwheels;
            uint domain = (uint)(LibFLI.FLIDomains.DEV_FILTERWHEEL | LibFLI.FLIDomains.IF_USB);

            fwheels = LibFLI.N_FLIList(domain);
            Logger.Debug(string.Format("FLI: Found {0} filter wheel(s)", fwheels.Count()));

            return fwheels;
        }
    }
}