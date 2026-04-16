#region "copyright"
/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using FluentAssertions;
using NINA.Astrometry.RiseAndSet;
using NUnit.Framework;
using System;

namespace NINA.Test.AstrometryTest {

    [TestFixture]
    public class CustomRiseAndSetTest {
        /// <summary>
        /// Verifies custom rise/set events preserve caller-supplied times and do not attempt body
        /// calculations, which is used when external horizon/event data is injected.
        /// </summary>
        [Test]
        public void CustomRiseAndSet_ExplicitEvents_PreservesRiseAndSetTimes() {
            DateTime rise = new DateTime(2024, 3, 20, 18, 0, 0, DateTimeKind.Utc);
            DateTime set = new DateTime(2024, 3, 21, 6, 0, 0, DateTimeKind.Utc);
            CustomRiseAndSet custom = new CustomRiseAndSet(rise, set);

            custom.Compute().Should().BeTrue();
#pragma warning disable CS0618
            custom.Calculate().Result.Should().BeTrue();
#pragma warning restore CS0618
            custom.Rise.Should().Be(rise);
            custom.Set.Should().Be(set);
        }
    }
}
