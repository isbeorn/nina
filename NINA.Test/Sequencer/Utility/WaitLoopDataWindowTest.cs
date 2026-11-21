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
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.Sequencer.SequenceItem.Utility;
using NUnit.Framework;
using static NINA.Test.HorizonTestData;
using System;

namespace NINA.Test.Sequencer.Utility {

    [TestFixture]
    public class WaitLoopDataWindowTest {
        private const double Latitude = 47;
        private const double Longitude = 8;

        // Target transits at the zenith (dec == latitude) so it covers the whole altitude range over a day
        private static readonly Coordinates Target = new Coordinates(Angle.ByHours(3), Angle.ByDegree(47), Epoch.J2000);
        private static readonly DateTime Start = DateTime.SpecifyKind(new DateTime(2020, 1, 1, 0, 0, 0), DateTimeKind.Utc);

        private AstrometrySettings settings;
        private Mock<IProfileService> profileServiceMock;

        [SetUp]
        public void Setup() {
            settings = new AstrometrySettings() { Latitude = Latitude, Longitude = Longitude };
            profileServiceMock = new Mock<IProfileService>();
            profileServiceMock.Setup(m => m.ActiveProfile.AstrometrySettings).Returns(settings);
        }

        private WaitLoopData CreateData(bool useCustomHorizon = true, double offset = 0, double upperOffset = 0) {
            var data = new WaitLoopData(profileServiceMock.Object, useCustomHorizon, "Test");
            data.Coordinates = new InputCoordinates(Target);
            data.Offset = offset;
            data.UpperOffset = upperOffset;
            return data;
        }

        private static double AltitudeAt(DateTime time) {
            return Target.Transform(Angle.ByDegree(Latitude), Angle.ByDegree(Longitude), 0, time).Altitude.Degree;
        }

        private static double AzimuthAt(DateTime time) {
            return Target.Transform(Angle.ByDegree(Latitude), Angle.ByDegree(Longitude), 0, time).Azimuth.Degree;
        }

        /// <summary>Independent brute force search with one minute resolution</summary>
        private static DateTime? BruteForceFirst(DateTime from, Func<DateTime, bool> predicate) {
            for (var t = from; t <= from.AddHours(24); t = t.AddMinutes(1)) {
                if (predicate(t)) return t;
            }
            return null;
        }

        /// <summary>Like <see cref="BruteForceFirst"/>, but the test expects an event within 24 hours</summary>
        private static DateTime BruteForceRequired(DateTime from, Func<DateTime, bool> predicate) {
            return BruteForceFirst(from, predicate) ?? throw new InvalidOperationException("The brute force search found no event within 24 hours");
        }

        /// <summary>Independent rule for "inside the available sky" for flat horizons: lower + offset &lt; altitude &lt;= upper - upperOffset</summary>
        private static bool Inside(DateTime time, double? lower, double? upper, double offset, double upperOffset = 0) {
            var altitude = AltitudeAt(time);
            return altitude > (lower ?? 0) + offset && (!upper.HasValue || altitude <= upper.Value - upperOffset);
        }

        [Test]
        public void NoUpperHorizon_HasUpperHorizonIsFalse_AndUpperTargetAltitudeIsNinety() {
            settings.Horizon = Flat(10);
            var data = CreateData();

            data.HasUpperHorizon.Should().BeFalse();
            data.CalculateTargetExpectedTime(Start, TargetCrossingComparison.AboveStrict);
            data.UpperTargetAltitude.Should().Be(90);
            data.TargetAltitude.Should().Be(10);
        }

        [Test]
        public void UpperHorizon_IsOnlyUsedWhenCustomHorizonIsRequested() {
            settings.Horizon = Dual(null, 60);

            CreateData(useCustomHorizon: true).HasUpperHorizon.Should().BeTrue();
            CreateData(useCustomHorizon: false).HasUpperHorizon.Should().BeFalse();
        }

        [Test]
        public void HorizonSetter_ReplacesTheWholeWindow() {
            settings.Horizon = Dual(null, 60);
            var data = CreateData();
            data.HasUpperHorizon.Should().BeTrue();

            // A horizon without upper values does not keep the upper horizon of the previous window
            data.Horizon = Flat(15);

            data.Horizon.Should().NotBeNull();
            data.HasUpperHorizon.Should().BeFalse();
            data.SkyWindow.GetLower(0).Should().Be(15);
            data.SkyWindow.GetUpper(0).Should().Be(90);

            // A horizon with an embedded upper horizon brings it along
            data.Horizon = Dual(10, 70);

            data.HasUpperHorizon.Should().BeTrue();
            data.SkyWindow.GetLower(0).Should().Be(10);
            data.SkyWindow.GetUpper(0).Should().Be(70);
        }

        [Test]
        public void MockedSettingsWithoutSkyWindow_FallBackToPlainLowerHorizon() {
            var lower = Flat(20);
            var mock = new Mock<IProfileService>();
            mock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Horizon).Returns(lower);
            var data = new WaitLoopData(mock.Object, true, "Test");

            data.Horizon.Should().BeSameAs(lower);
            data.HasUpperHorizon.Should().BeFalse();
        }

        [Test]
        public void UpperTargetAltitude_IsUpperHorizonMinusUpperOffset() {
            settings.Horizon = Dual(null, 60);
            var data = CreateData(upperOffset: 4);

            data.CalculateTargetExpectedTime(Start, TargetCrossingComparison.AboveStrict);

            data.UpperTargetAltitude.Should().Be(56);
        }

        [Test]
        public void UpperOffsetDefaultsToZero_AndOffsetDoesNotAffectTheUpperHorizon() {
            settings.Horizon = Dual(null, 60);
            var data = CreateData(offset: 4);

            data.UpperOffset.Should().Be(0);
            data.CalculateTargetExpectedTime(Start, TargetCrossingComparison.AboveStrict);

            data.UpperTargetAltitude.Should().Be(60);
            data.TargetAltitude.Should().Be(4);
        }

        [Test]
        public void Clone_KeepsTheUpperOffset() {
            settings.Horizon = Dual(null, 60);
            var data = CreateData(offset: 4, upperOffset: 7);

            var clone = data.Clone();

            clone.Offset.Should().Be(4);
            clone.UpperOffset.Should().Be(7);
        }

        [Test]
        public void UpdateCurrentTargetPosition_UpdatesTheUpperTargetAltitude() {
            settings.Horizon = Dual(10, 60);
            var data = CreateData(offset: 2, upperOffset: 3);

            data.UpdateCurrentTargetPosition(Start).Should().BeTrue();

            data.TargetAltitude.Should().Be(12);
            data.UpperTargetAltitude.Should().Be(57);
            data.CurrentAltitude.Should().BeApproximately(AltitudeAt(Start), 0.01);
        }

        [Test]
        public void ExpectedTime_WhenEnteringTheWindow_MatchesBruteForce() {
            settings.Horizon = Dual(10, 60);
            var data = CreateData();

            // Every start time of the day: outside of the window the next entry has to match the brute force search
            for (var start = Start; start < Start.AddHours(24); start = start.AddMinutes(37)) {
                var result = data.CalculateTargetExpectedTime(start, TargetCrossingComparison.AboveStrict);

                if (Inside(start, 10, 60, 0)) {
                    result.Status.Should().Be(TargetCrossingStatus.AlreadySatisfied, $"at {start:u}");
                    continue;
                }

                var expected = BruteForceRequired(start, t => Inside(t, 10, 60, 0));
                result.Status.Should().Be(TargetCrossingStatus.Found, $"at {start:u}");
                result.Time.Should().BeOnOrBefore(expected.AddSeconds(1), $"at {start:u}");
                result.Time.Should().BeAfter(expected.AddSeconds(-61), $"at {start:u}");
            }
        }

        [Test]
        public void ExpectedTime_WhenLeavingTheWindow_MatchesBruteForce() {
            settings.Horizon = Dual(10, 60);
            var data = CreateData();

            for (var start = Start; start < Start.AddHours(24); start = start.AddMinutes(37)) {
                var result = data.CalculateTargetExpectedTime(start, TargetCrossingComparison.BelowStrict);

                if (!Inside(start, 10, 60, 0)) {
                    result.Status.Should().Be(TargetCrossingStatus.AlreadySatisfied, $"at {start:u}");
                    continue;
                }

                var expected = BruteForceRequired(start, t => !Inside(t, 10, 60, 0));
                result.Status.Should().Be(TargetCrossingStatus.Found, $"at {start:u}");
                result.Time.Should().BeOnOrBefore(expected.AddSeconds(1), $"at {start:u}");
                result.Time.Should().BeAfter(expected.AddSeconds(-61), $"at {start:u}");
            }
        }

        // The offset only moves the lower bound and the upper offset only the upper bound
        [TestCase(5, 0)]
        [TestCase(3, 8)]
        public void ExpectedTime_WithOffsets_MatchesBruteForce(double offset, double upperOffset) {
            settings.Horizon = Dual(10, 60);
            var data = CreateData(offset: offset, upperOffset: upperOffset);

            for (var start = Start; start < Start.AddHours(24); start = start.AddMinutes(53)) {
                var result = data.CalculateTargetExpectedTime(start, TargetCrossingComparison.AboveStrict);
                if (Inside(start, 10, 60, offset, upperOffset)) {
                    result.Status.Should().Be(TargetCrossingStatus.AlreadySatisfied, $"at {start:u}");
                    continue;
                }

                var expected = BruteForceRequired(start, t => Inside(t, 10, 60, offset, upperOffset));
                result.Status.Should().Be(TargetCrossingStatus.Found, $"at {start:u}");
                result.Time.Should().BeOnOrBefore(expected.AddSeconds(1), $"at {start:u}");
                result.Time.Should().BeAfter(expected.AddSeconds(-61), $"at {start:u}");
            }
        }

        [TestCase(30, 25, 45)]
        [TestCase(30, 10, 60)]
        [TestCase(0, 15, 40)]
        [TestCase(60, 30, 50)]
        public void ExpectedTime_ForTargetsThatDoNotCrossTheZenith_MatchesBruteForce(double declination, double lower, double upper) {
            settings.Horizon = Dual(lower, upper);
            var data = CreateData();
            data.Coordinates = new InputCoordinates(new Coordinates(Angle.ByHours(3), Angle.ByDegree(declination), Epoch.J2000));

            for (var start = Start; start < Start.AddHours(24); start = start.AddMinutes(41)) {
                var result = data.CalculateTargetExpectedTime(start, TargetCrossingComparison.AboveStrict);
                var expected = BruteForceFirst(start, t => InsideFor(data.Coordinates.Coordinates, t, lower, upper));

                if (InsideFor(data.Coordinates.Coordinates, start, lower, upper)) {
                    result.Status.Should().Be(TargetCrossingStatus.AlreadySatisfied, $"at {start:u}");
                } else if (expected == null) {
                    result.Status.Should().NotBe(TargetCrossingStatus.Found, $"at {start:u}");
                } else {
                    result.Status.Should().Be(TargetCrossingStatus.Found, $"at {start:u}");
                    result.Time.Should().BeOnOrBefore(expected.Value.AddSeconds(1), $"at {start:u}");
                    result.Time.Should().BeAfter(expected.Value.AddSeconds(-61), $"at {start:u}");
                }
            }
        }

        private static bool InsideFor(Coordinates coordinates, DateTime time, double lower, double upper) {
            var altitude = coordinates.Transform(Angle.ByDegree(Latitude), Angle.ByDegree(Longitude), 0, time).Altitude.Degree;
            return altitude > lower && altitude <= upper;
        }

        [Test]
        public void TargetReappearsInSecondWindow_AfterBeingAboveTheUpperHorizon() {
            settings.Horizon = Dual(10, 60);
            var data = CreateData();

            // Altitude rises through the window, passes above the upper horizon (zenith transit) and comes back down
            var blocked = BruteForceRequired(Start, t => AltitudeAt(t) > 60);
            Inside(blocked, 10, 60, 0).Should().BeFalse();

            var result = data.CalculateTargetExpectedTime(blocked, TargetCrossingComparison.AboveStrict);

            result.Status.Should().Be(TargetCrossingStatus.Found);
            result.Time.Should().BeAfter(blocked);
            AltitudeAt(result.Time).Should().BeInRange(59.9, 60.01);
        }

        [Test]
        public void UpperBelowLower_NothingIsInside() {
            settings.Horizon = Dual(50, 40);
            var data = CreateData();

            // Never inside the window: either provably (no event) or unresolved within the evaluation budget, but never found
            data.CalculateTargetExpectedTime(Start, TargetCrossingComparison.AboveStrict).Status
                .Should().BeOneOf(TargetCrossingStatus.NoEvent, TargetCrossingStatus.Exhausted);
            data.ExpectedTime.Should().Be("--");
        }

        [Test]
        public void TargetAlwaysAboveTheUpperHorizon_HasNoEvent() {
            // Circumpolar target that never drops below 37 degrees at this latitude
            settings.Horizon = Dual(null, 30);
            var data = CreateData();
            data.Coordinates = new InputCoordinates(new Coordinates(Angle.ByHours(3), Angle.ByDegree(80), Epoch.J2000));

            data.CalculateTargetExpectedTime(Start, TargetCrossingComparison.AboveStrict).Status.Should().Be(TargetCrossingStatus.NoEvent);
            data.ExpectedTime.Should().Be("--");
        }

        [Test]
        public void TargetNeverAboveTheLowerHorizon_HasNoEvent() {
            settings.Horizon = Dual(30, 40);
            var data = CreateData();
            data.Coordinates = new InputCoordinates(new Coordinates(Angle.ByHours(3), Angle.ByDegree(-80), Epoch.J2000));

            data.CalculateTargetExpectedTime(Start, TargetCrossingComparison.AboveStrict).Status.Should().Be(TargetCrossingStatus.NoEvent);
        }

    }
}
