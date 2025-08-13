#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Astrometry;
using System;

namespace NINA.Equipment.Interfaces {

    public interface IDomeSynchronization {

        [Obsolete("Use interface that provides site elevation")]
        TopocentricCoordinates TargetDomeCoordinates(
            Coordinates scopeCoordinates,
            double localSiderealTime,
            Angle siteLatitude,
            Angle siteLongitude,
            PierSide sideOfPier);
        TopocentricCoordinates TargetDomeCoordinates(
            Coordinates scopeCoordinates,
            double localSiderealTime,
            Angle siteLatitude,
            Angle siteLongitude,
            double siteElevation,
            PierSide sideOfPier);
    }
}