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

namespace NINA.MGEN {

    [Flags]
    public enum LEDS : byte {
        BLUE = 1, // Exposure Focus Line Active
        GREEN = 2, // Exposure Shutter Line Active
        UP_RED = 4, // DEC- correction active
        DOWN_RED = 8, //DEC+ correction active
        LEFT_RED = 16, // RA- correction active
        RIGHT_RED = 32 // RA+ correction active
    }
}
