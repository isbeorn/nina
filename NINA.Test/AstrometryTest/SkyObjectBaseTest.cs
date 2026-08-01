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
using NINA.Astrometry;
using NUnit.Framework;
using OxyPlot;
using System;
using System.Collections.Generic;

namespace NINA.Test.AstrometryTest {

    [TestFixture]
    public class SkyObjectBaseTest {

        [Test]
        public void SetDateAndPosition_RefreshesBoundAltitudeData() {
            DeepSkyObject sut = new DeepSkyObject(
                "M31",
                new Coordinates(0.712, 41.269, Epoch.J2000, Coordinates.RAType.Hours),
                null);
            DateTime firstDate = new DateTime(2026, 7, 28, 12, 0, 0, DateTimeKind.Local);
            sut.SetDateAndPosition(firstDate, 50, 16.5);
            List<DataPoint> firstAltitudes = sut.Altitudes;
            List<DataPoint> boundAltitudes = firstAltitudes;
            sut.PropertyChanged += (_, e) => {
                if (e.PropertyName == nameof(SkyObjectBase.Altitudes)) {
                    boundAltitudes = sut.Altitudes;
                }
            };

            sut.SetDateAndPosition(firstDate.AddDays(1), 50, 16.5);

            boundAltitudes.Should().NotBeSameAs(firstAltitudes);
            boundAltitudes[0].X.Should().NotBe(firstAltitudes[0].X);
        }
    }
}