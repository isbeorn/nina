#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace NINA.Equipment.Equipment.MyGuider.PHD2 {

    public sealed class PhdAppState {
        public static readonly string STOPPED = "Stopped";
        public static readonly string SELECTED = "Selected";
        public static readonly string CALIBRATING = "Calibrating";
        public static readonly string GUIDING = "Guiding";
        public static readonly string LOSTLOCK = "LostLock";
        public static readonly string PAUSED = "Paused";
        public static readonly string LOOPING = "Looping";
    }
}