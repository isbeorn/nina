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
using System;

namespace NINA.Test.AstrometryTest {

    [TestFixture]
    public class RectangularCoordinatesTest {
        private const double AngleTolerance = 1e-10;

        /// <summary>
        /// Verifies rectangular-to-spherical coordinate conversion with canonical axes, guarding
        /// right-ascension quadrant handling and declination at the pole.
        /// </summary>
        [Test]
        public void RectangularCoordinatesToPolar_CanonicalAxes_ReturnsExpectedRaDec() {
            new RectangularCoordinates(1.0, 0.0, 0.0).ToPolar().RADegrees.Should().BeApproximately(0.0, AngleTolerance);
            new RectangularCoordinates(0.0, 1.0, 0.0).ToPolar().RADegrees.Should().BeApproximately(90.0, AngleTolerance);
            new RectangularCoordinates(0.0, -1.0, 0.0).ToPolar().RADegrees.Should().BeApproximately(270.0, AngleTolerance);
            new RectangularCoordinates(0.0, 0.0, 1.0).ToPolar().Dec.Should().BeApproximately(90.0, AngleTolerance);
        }

        /// <summary>
        /// Verifies ecliptic rotation around the x-axis, matching the standard obliquity rotation
        /// used when converting ecliptic vectors into equatorial vectors.
        /// </summary>
        [Test]
        public void RectangularCoordinatesRotateEcliptic_NinetyDegrees_RotatesYIntoZ() {
            RectangularCoordinates rotated = new RectangularCoordinates(0.0, 1.0, 0.0).RotateEcliptic(Angle.ByDegree(90.0));

            rotated.X.Should().BeApproximately(0.0, AngleTolerance);
            rotated.Y.Should().BeApproximately(0.0, AngleTolerance);
            rotated.Z.Should().BeApproximately(1.0, AngleTolerance);
        }

        /// <summary>
        /// Verifies rectangular vector arithmetic used by solar-system calculations, including
        /// distance preservation and string output useful for diagnostics.
        /// </summary>
        [Test]
        public void RectangularCoordinates_VectorArithmetic_ReturnsComponentWiseResults() {
            RectangularCoordinates left = new RectangularCoordinates(1.0, 2.0, 3.0);
            RectangularCoordinates right = new RectangularCoordinates(4.0, 6.0, 8.0);

            RectangularCoordinates sum = left + right;
            RectangularCoordinates difference = right - left;
            RectangularCoordinates product = left * 2.0;
            RectangularCoordinates quotient = right / 2.0;
            RectangularPV pv = new RectangularPV(left, right);

            sum.Distance.Should().BeApproximately(Math.Sqrt(5.0 * 5.0 + 8.0 * 8.0 + 11.0 * 11.0), AngleTolerance);
            difference.X.Should().Be(3.0);
            difference.Y.Should().Be(4.0);
            difference.Z.Should().Be(5.0);
            product.Z.Should().Be(6.0);
            quotient.Y.Should().Be(3.0);
            left.ToString().Should().Contain("X=1");
            pv.Position.Should().BeSameAs(left);
            pv.Velocity.Should().BeSameAs(right);
            pv.ToString().Should().Contain(nameof(RectangularPV.Position));
        }

        [Test]
        public void RectangularCoordinates_FromPolarRoundTripsRaDec() {
            Coordinates coordinates = new Coordinates(Angle.ByDegree(123.4), Angle.ByDegree(-45.6), Epoch.J2000);

            Coordinates roundTrip = RectangularCoordinates.FromPolar(coordinates).ToPolar(coordinates.Epoch);

            roundTrip.RADegrees.Should().BeApproximately(coordinates.RADegrees, AngleTolerance);
            roundTrip.Dec.Should().BeApproximately(coordinates.Dec, AngleTolerance);
        }

        [Test]
        public void RectangularCoordinates_DotCrossNormalizeAndAxisRotation_ReturnExpectedVectors() {
            RectangularCoordinates xAxis = new RectangularCoordinates(1.0, 0.0, 0.0);
            RectangularCoordinates yAxis = new RectangularCoordinates(0.0, 1.0, 0.0);
            RectangularCoordinates zAxis = new RectangularCoordinates(0.0, 0.0, 1.0);

            RectangularCoordinates cross = xAxis.Cross(yAxis);
            RectangularCoordinates normalized = new RectangularCoordinates(3.0, 0.0, 4.0).Normalize();
            RectangularCoordinates rotated = xAxis.RotateAroundAxis(zAxis, Angle.ByDegree(90.0));

            xAxis.Dot(yAxis).Should().BeApproximately(0.0, AngleTolerance);
            cross.X.Should().BeApproximately(0.0, AngleTolerance);
            cross.Y.Should().BeApproximately(0.0, AngleTolerance);
            cross.Z.Should().BeApproximately(1.0, AngleTolerance);
            normalized.Distance.Should().BeApproximately(1.0, AngleTolerance);
            rotated.X.Should().BeApproximately(0.0, AngleTolerance);
            rotated.Y.Should().BeApproximately(1.0, AngleTolerance);
            rotated.Z.Should().BeApproximately(0.0, AngleTolerance);
        }
    }
}
