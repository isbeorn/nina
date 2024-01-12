#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry.Body;
using System;

namespace NINA.Astrometry.RiseAndSet {

    public class NauticalTwilightRiseAndSet : RiseAndSetEvent {

        public NauticalTwilightRiseAndSet(DateTime date, double latitude, double longitude) : base(date, latitude, longitude) {
        }

        private double NauticalTwilightDegree => -12;

        protected override double AdjustAltitude(BasicBody body) {
            return body.Altitude - NauticalTwilightDegree;
        }

        protected override BasicBody GetBody(DateTime date) {
            return new Sun(date, Latitude, Longitude);
        }
    }
}