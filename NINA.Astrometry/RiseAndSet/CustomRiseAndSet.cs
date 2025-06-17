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
using System.Threading.Tasks;

namespace NINA.Astrometry.RiseAndSet {

    public class CustomRiseAndSet : RiseAndSetEvent {
        private DateTime? rise;
        private DateTime? set;

        public CustomRiseAndSet(DateTime? rise, DateTime? set) : base(DateTime.Now, 0, 0, 0) {
            this.rise = rise;
            this.set = set;
        }

        [Obsolete("Use method with elevation parameter instead")]
        public CustomRiseAndSet(DateTime date, double latitude, double longitude) : this(date, latitude, longitude, elevation: 0) { }
        public CustomRiseAndSet(DateTime date, double latitude, double longitude, double elevation) : base(date, latitude, longitude, elevation) {
        }

        public override Task<bool> Calculate() {
            return Task.FromResult(true);
        }

        public override DateTime? Rise => rise;
        public override DateTime? Set => set;

        protected override double AdjustAltitude(BasicBody body) {
            return 0;
        }

        protected override BasicBody GetBody(DateTime date) {
            return null;
        }
    }
}