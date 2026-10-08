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
using Moq;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.Sequencer.SequenceItem.Utility;
using System.IO;

namespace NINA.Test.Sequencer.SequenceItem.Utility {
    [TestFixture]
    public class WaitLoopDataTest {
        private static readonly DateTime ObservationTime = new(2026, 1, 15, 22, 0, 0, DateTimeKind.Utc);

        [TestCase(-90, -90)]
        [TestCase(-12.346, -12.35)]
        [TestCase(0, 0)]
        [TestCase(12.346, 12.35)]
        [TestCase(90, 90)]
        public void NoCustomHorizon_UsesRoundedOffset(double offset, double expected) {
            var data = CreateData();
            data.Offset = offset;

            data.GetTargetAltitudeWithHorizon(ObservationTime).Should().Be(expected);
        }

        [Test]
        public void NoCustomHorizon_DoesNotAllocateCoordinateTransformObjects() {
            var data = CreateData();
            data.Offset = 12.346;
            data.GetTargetAltitudeWithHorizon(ObservationTime);

            long before = GC.GetAllocatedBytesForCurrentThread();
            double altitude = 0;
            for (int i = 0; i < 100; i++) altitude = data.GetTargetAltitudeWithHorizon(ObservationTime);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            altitude.Should().Be(12.35);
            allocated.Should().BeLessThan(1024, "a flat horizon needs only the offset, with no coordinate conversion");
        }

        [Test]
        public void CustomHorizon_CanBeAddedReplacedAndRemoved() {
            var data = CreateData();
            data.Offset = 2.346;
            foreach (var (horizon, expected) in new[] { (FlatHorizon(10), 12.35), (FlatHorizon(20), 22.35), (null, 2.35) }) {
                data.Horizon = horizon;
                data.SetTargetAltitudeWithHorizon(ObservationTime);
                data.TargetAltitude.Should().Be(expected);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MissingCoordinates_StillReturnsZero(bool customHorizon) {
            var data = CreateData();
            data.Offset = 12;
            data.Horizon = customHorizon ? FlatHorizon(10) : null;
            data.Coordinates = null;

            data.GetTargetAltitudeWithHorizon(ObservationTime).Should().Be(0);
        }

        private static CustomHorizon FlatHorizon(int altitude) =>
            CustomHorizon.FromReader_Standard(new StringReader($"0 {altitude}\n360 {altitude}"));

        private static WaitLoopData CreateData() {
            var profile = new Mock<IProfileService>();
            profile.SetupGet(x => x.ActiveProfile.AstrometrySettings).Returns(new AstrometrySettings { Latitude = 47, Longitude = 8 });
            return new WaitLoopData(profile.Object, true, "Horizon") {
                Coordinates = new InputCoordinates(new Coordinates(20, 20, Epoch.J2000, Coordinates.RAType.Degrees))
            };
        }
    }
}