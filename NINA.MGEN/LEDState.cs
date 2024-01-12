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
using System.Drawing;

namespace NINA.MGEN {

    public class LEDState {
        public Color TopLeft { get; internal set; } = Color.Transparent;
        public Color TopRight { get; internal set; } = Color.Transparent;
        public Color Up { get; internal set; } = Color.Transparent;
        public Color Down { get; internal set; } = Color.Transparent;
        public Color Left { get; internal set; } = Color.Transparent;
        public Color Right { get; internal set; } = Color.Transparent;
    }
}