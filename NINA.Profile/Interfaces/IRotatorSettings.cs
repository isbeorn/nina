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
using System;

namespace NINA.Profile.Interfaces {

    public interface IRotatorSettings : ISettings {
        string Id { get; set; }
        string LastDeviceName { get; set; }

        /// <summary>
        /// Historically N.I.N.A. was expressing rotation in clockwise orientation
        /// As this was changed to follow the standard of counter clockwise orientation, the reverse setting is flipped for migration purposes
        /// </summary>
        bool Reverse2 { get; set; }
        RotatorRangeTypeEnum RangeType { get; set; }
        float RangeStartMechanicalPosition { get; set; }
    }
}