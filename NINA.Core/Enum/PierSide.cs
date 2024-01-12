#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace NINA.Core.Enum {

    public enum PierSide {

        // Telescope is East of Pier
        // This is CW Down when going from highest altitude to lowest altitude of an object (Hour Angle 0 .. 12)
        // It is CW Up when the meridian has been traversed at the lowest point
        pierEast = 0,

        //Telescope Side of Pier is Unknown
        pierUnknown = -1,

        //Telescope is West of Pier
        // This is CW Down when going from lowest altitude to highest altitude of an object (Hour Angle 12 .. 24)
        // It is CW Up when the meridian has been traversed at the highest point
        pierWest = 1
    }
}