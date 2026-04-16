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
using NINA.Core.Enum;
using NUnit.Framework;

namespace NINA.Test.AstrometryTest {

    [TestFixture]
    public class TopocentricCoordinatesTest {
        /// <summary>
        /// Verifies topocentric coordinate value semantics: altitude-side classification, clone
        /// independence, default constructors, and diagnostic text.
        /// </summary>
        [Test]
        public void TopocentricCoordinates_ValueSemantics_PreserveAnglesAndSiteSide() {
            TopocentricCoordinates east = new TopocentricCoordinates(
                Angle.ByDegree(90.0),
                Angle.ByDegree(45.0),
                Angle.ByDegree(51.5),
                Angle.ByDegree(13.0),
                100.0);
            TopocentricCoordinates west = new TopocentricCoordinates(
                Angle.ByDegree(270.0),
                Angle.ByDegree(45.0),
                Angle.ByDegree(51.5),
                Angle.ByDegree(13.0));

            TopocentricCoordinates copy = east.Copy();
            TopocentricCoordinates clone = east.Clone();

            east.AltitudeSite.Should().Be(AltitudeSite.EAST);
            west.AltitudeSite.Should().Be(AltitudeSite.WEST);
            copy.Should().NotBeSameAs(east);
            clone.Azimuth.Degree.Should().Be(east.Azimuth.Degree);
            clone.Elevation.Should().Be(100.0);
            west.Elevation.Should().Be(0.0);
            east.ToString().Should().Contain("Alt:");
        }

        /// <summary>
        /// Verifies topocentric compatibility transform overloads return finite celestial
        /// coordinates when no explicit observation time is supplied.
        /// </summary>
        [Test]
        public void TopocentricTransform_CompatibilityOverloads_ReturnFiniteCoordinates() {
            TopocentricCoordinates topocentric = new TopocentricCoordinates(
                Angle.ByDegree(180.0),
                Angle.ByDegree(60.0),
                Angle.ByDegree(35.0),
                Angle.ByDegree(-105.0),
                1600.0);

            Coordinates noRefraction = topocentric.Transform(Epoch.J2000);
            Coordinates withRefraction = topocentric.Transform(Epoch.J2000, 800.0, 5.0, 20.0, 0.574);

            noRefraction.RADegrees.Should().BeInRange(0.0, 360.0);
            noRefraction.Dec.Should().BeInRange(-90.0, 90.0);
            withRefraction.RADegrees.Should().BeInRange(0.0, 360.0);
            withRefraction.Dec.Should().BeInRange(-90.0, 90.0);
        }
    }
}
