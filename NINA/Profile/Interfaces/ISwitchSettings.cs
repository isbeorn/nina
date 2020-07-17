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
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Profile {

    public interface ISwitchSettings : ISettings {
        string Id { get; set; }
        string EagleUrl { get; set; }

        #region UltimatePowerboxV2

        string Upbv2PortName { get; set; }
        string Upbv2PowerName1 { get; set; }
        string Upbv2PowerName2 { get; set; }
        string Upbv2PowerName3 { get; set; }
        string Upbv2PowerName4 { get; set; }
        string Upbv2PowerName5 { get; set; }
        string Upbv2UsbName1 { get; set; }
        string Upbv2UsbName2 { get; set; }
        string Upbv2UsbName3 { get; set; }
        string Upbv2UsbName4 { get; set; }
        string Upbv2UsbName5 { get; set; }
        string Upbv2UsbName6 { get; set; }

        #endregion UltimatePowerboxV2
    }
}