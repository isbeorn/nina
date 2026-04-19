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
using NINA.PlateSolving;

namespace NINA.Test.PlateSolving {

    [TestFixture]
    public class PlateSolveModelBehaviorTest {

        /// <summary>
        /// Verifies plate solve parameters apply binning to pixel size, clamp binning below one, and normalize coordinates to J2000.
        /// </summary>
        [Test]
        public void PlateSolveParameter_PixelSizeAndCoordinatesUseSolverConventions() {
            var parameter = new PlateSolveParameter {
                PixelSize = 3.76,
                Binning = 2,
                FocalLength = 600,
                Coordinates = new Coordinates(Angle.ByDegree(123.4), Angle.ByDegree(-22.5), Epoch.JNOW)
            };

            parameter.PixelSize.Should().BeApproximately(7.52, 1e-10);
            parameter.Coordinates.Epoch.Should().Be(Epoch.J2000);

            parameter.Binning = 0;
            parameter.PixelSize.Should().BeApproximately(3.76, 1e-10);
        }

        /// <summary>
        /// Verifies cloning preserves all solving options while producing a separate parameter instance with normalized coordinates.
        /// </summary>
        [Test]
        public void PlateSolveParameter_ClonePreservesScientificOptions() {
            var original = new PlateSolveParameter {
                FocalLength = 910,
                PixelSize = 2.4,
                Binning = 3,
                SearchRadius = 2.5,
                Regions = 128,
                DownSampleFactor = 2,
                MaxObjects = 500,
                BlindFailoverEnabled = false,
                DisableNotifications = true,
                Coordinates = new Coordinates(Angle.ByDegree(45), Angle.ByDegree(12), Epoch.J2000)
            };

            PlateSolveParameter clone = original.Clone();

            clone.Should().NotBeSameAs(original);
            clone.FocalLength.Should().Be(original.FocalLength);
            clone.PixelSize.Should().Be(original.PixelSize);
            clone.SearchRadius.Should().Be(original.SearchRadius);
            clone.Regions.Should().Be(original.Regions);
            clone.DownSampleFactor.Should().Be(original.DownSampleFactor);
            clone.MaxObjects.Should().Be(original.MaxObjects);
            clone.BlindFailoverEnabled.Should().BeFalse();
            clone.DisableNotifications.Should().BeTrue();
            clone.Coordinates.RADegrees.Should().BeApproximately(45, 1e-10);
            clone.Coordinates.Dec.Should().BeApproximately(12, 1e-10);
            clone.Coordinates.Epoch.Should().Be(Epoch.J2000);
        }

        /// <summary>
        /// Verifies result orientation normalization, legacy orientation mapping, and pixel-error helpers from a solved separation.
        /// </summary>
        [Test]
        public void PlateSolveResult_NormalizesAnglesAndFormatsSeparationErrors() {
            var result = new PlateSolveResult(new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc)) {
                Pixscale = 2.0,
                PositionAngle = -10,
                Separation = new Separation {
                    RA = Angle.ByDegree(4d / 3600d),
                    Dec = Angle.ByDegree(-6d / 3600d)
                }
            };

            result.Success.Should().BeTrue();
            result.SolveTime.Should().Be(new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc));
            result.PositionAngle.Should().BeApproximately(350, 1e-10);
            result.Orientation.Should().BeApproximately(10, 1e-10);
            result.Orientation = 45;

            result.PositionAngle.Should().BeApproximately(315, 1e-10);
            result.RaPixError.Should().Be(2);
            result.DecPixError.Should().Be(-3);
            result.RaErrorString.Should().NotBe("--");
            result.DecErrorString.Should().NotBe("--");
        }

        /// <summary>
        /// Verifies null separations expose placeholder strings and NaN pixel errors for UI-safe unsolved states.
        /// </summary>
        [Test]
        public void PlateSolveResult_NullSeparationUsesPlaceholders() {
            var result = new PlateSolveResult();

            result.RaErrorString.Should().Be("--");
            result.DecErrorString.Should().Be("--");
            result.RaPixError.Should().Be(double.NaN);
            result.DecPixError.Should().Be(double.NaN);
        }
    }
}
