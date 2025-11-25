#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.INDI.Enums;
using System;

namespace NINA.INDI.Model {
    public struct TrackingRate {
        public static TrackingRate STOPPED = new TrackingRate() { TrackingMode = TrackingMode.Stopped };
        public TrackingMode TrackingMode { get; set; }
        public double? CustomRightAscensionRate { get; set; }
        public double? CustomDeclinationRate { get; set; }

        public override bool Equals(object obj) {
            return obj is TrackingRate rate &&
                   TrackingMode == rate.TrackingMode &&
                   CustomRightAscensionRate == rate.CustomRightAscensionRate &&
                   CustomDeclinationRate == rate.CustomDeclinationRate;
        }

        public override int GetHashCode() {
            return HashCode.Combine(TrackingMode, CustomRightAscensionRate, CustomDeclinationRate);
        }

        public static bool operator ==(TrackingRate left, TrackingRate right) {
            return left.Equals(right);
        }

        public static bool operator !=(TrackingRate left, TrackingRate right) {
            return !(left == right);
        }
    }
}
