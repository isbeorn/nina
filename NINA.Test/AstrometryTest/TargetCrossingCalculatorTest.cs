#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.IO;
using System.Globalization;
using System.Linq;
using NINA.Astrometry;
using NINA.Core.Model;
using NINA.Sequencer.Utility;

namespace NINA.Test.AstrometryTest {
    [TestFixture]
    public class TargetCrossingCalculatorTest {
        private static readonly DateTime Start = new(2026, 1, 1, 22, 0, 0, DateTimeKind.Utc);

        private static TargetCrossingRequest Request(double ra = 5, double dec = 20, double latitude = 47,
            double offset = 30, CustomHorizon? horizon = null, TargetCrossingComparison comparison = TargetCrossingComparison.AboveInclusive) =>
            new(new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Hours), latitude, 8, 100, horizon, offset, comparison);

        private static Coordinates InputCoordinates(double ra = 5, double dec = 20) =>
            new(ra, dec, Epoch.J2000, Coordinates.RAType.Hours);

        private static bool OraclePredicate(TargetCrossingRequest request, DateTime time, Coordinates? coordinates = null) {
            var position = (coordinates ?? InputCoordinates()).Transform(Angle.ByDegree(request.Latitude), Angle.ByDegree(request.Longitude), request.Elevation, time);
            var difference = position.Altitude.Degree - (request.Offset + (request.Horizon?.GetAltitude(position.Azimuth.Degree) ?? 0));
            return request.Comparison switch {
                TargetCrossingComparison.AboveInclusive => difference >= 0,
                TargetCrossingComparison.AboveStrict => difference > 0,
                TargetCrossingComparison.BelowInclusive => difference <= 0,
                TargetCrossingComparison.BelowStrict => difference < 0,
                _ => position.AltitudeSite != NINA.Core.Enum.AltitudeSite.EAST && difference < 0
            };
        }

        // Independent reference: scan full Coordinates.Transform results, then refine the first
        // observed transition. The narrow-window cases use a separate fine scan around their notch.
        private static DateTime Oracle(TargetCrossingRequest request, DateTime start, double step = 30, double hours = 24, Coordinates? coordinates = null) {
            if (OraclePredicate(request, start, coordinates)) {
                return start;
            }

            for (var right = start.AddSeconds(step); right <= start.AddHours(hours); right = right.AddSeconds(step)) {
                if (!OraclePredicate(request, right, coordinates)) {
                    continue;
                }

                var left = right.AddSeconds(-step);
                while ((right - left).TotalSeconds > 0.005) {
                    var middle = left.AddTicks((right.Ticks - left.Ticks) / 2);
                    if (OraclePredicate(request, middle, coordinates)) {
                        right = middle;
                    } else {
                        left = middle;
                    }
                }

                return right;
            }

            return DateTime.MinValue;
        }

        [TestCase(0, -5.0)]
        [TestCase(0, 30.0)]
        [TestCase(1, 30.0)]
        [TestCase(2, -5.0)]
        [TestCase(2, 30.0)]
        [TestCase(3, 30.0)]
        [TestCase(4, 30.0)]
        [TestCase(4, 80.0)]
        public void FlatCrossingsAgreeWithFullTransform(int policy, double offset) {
            var request = Request(offset: offset, comparison: (TargetCrossingComparison)policy);
            var expected = Oracle(request, Start);
            var actual = new TargetCrossingCalculator(request).Find(Start);

            Assert.That(actual.Status, Is.EqualTo(expected == Start ? TargetCrossingStatus.AlreadySatisfied : TargetCrossingStatus.Found));
            Assert.That(Math.Abs((actual.Time - expected).TotalSeconds), Is.LessThanOrEqualTo(0.25));

            Assert.That(OraclePredicate(request, actual.Time), Is.True);
            TestContext.Out.WriteLine($"Flat policy {policy}, offset {offset}: {actual.Evaluations} coordinate evaluations");
        }

        [TestCase(5, 20, 47)]
        [TestCase(0, 0, 0)]
        [TestCase(5, 89.9, 80)]
        [TestCase(5, -89.9, -80)]
        [TestCase(5, 47, 47)]
        public void SnapshotMatchesExistingSofaTransform(double ra, double dec, double latitude) {
            var request = Request(ra, dec, latitude);
            for (int hour = 0; hour < 24; hour++) {
                var time = Start.AddHours(hour);
                var expected = InputCoordinates(ra, dec).Transform(Angle.ByDegree(latitude), Angle.ByDegree(8), 100, time);
                var actual = request.Position(time);

                Assert.That(actual.Altitude, Is.EqualTo(expected.Altitude.Degree).Within(1e-10));
                Assert.That(actual.Azimuth, Is.EqualTo(expected.Azimuth.Degree).Within(1e-10));

                Assert.That(actual.IsRising, Is.EqualTo(expected.AltitudeSite == NINA.Core.Enum.AltitudeSite.EAST));
            }
        }

        [TestCase(0, 0, 0, 30)]
        [TestCase(5, 47, 47, 60)]
        [TestCase(5, 89.9, 80, 80)]
        [TestCase(5, -89.9, -80, 80)]
        [TestCase(5, 20, 90, 20)]
        public void EquatorialZenithAndPolarCrossingsMatchFullTransform(double ra, double dec, double latitude, double offset) {
            var request = Request(ra, dec, latitude, offset);
            var expected = Oracle(request, Start, coordinates: InputCoordinates(ra, dec));
            var actual = new TargetCrossingCalculator(request).Find(Start);

            if (expected == DateTime.MinValue) {
                Assert.That(actual.Status, Is.EqualTo(TargetCrossingStatus.NoEvent));
            } else {
                Assert.That(actual.Status, Is.EqualTo(expected == Start ? TargetCrossingStatus.AlreadySatisfied : TargetCrossingStatus.Found));
                Assert.That(Math.Abs((actual.Time - expected).TotalSeconds), Is.LessThanOrEqualTo(10));
            }
        }

        [Test]
        public void UsnoEquatorialHourAngleFormulaSeedsActualCrossing() {
            // USNO: sin(altitude) = sin(dec)sin(lat) + cos(dec)cos(lat)cos(hour angle).
            // At dec=lat=0, altitude30 implies a setting hour angle of60 degrees.
            var request = Request(ra: 5, dec: 0, latitude: 0, offset: 30, comparison: TargetCrossingComparison.BelowStrict);
            var current = request.Position(Start);
            var actual = new TargetCrossingCalculator(request).Find(Start);
            var hours = AstroUtil.EuclidianModulus(60 - current.HourAngle, 360) / (15 * 1.00273781191135448);
            var approximate = Start.AddHours(hours);

            Assert.That(actual.Status, Is.EqualTo(TargetCrossingStatus.Found));
            Assert.That(Math.Abs((actual.Time - approximate).TotalSeconds), Is.LessThan(1));
        }

        [Test]
        public void RisingBelowCutoffContinuesUntilTheSettingTransit() {
            var request = Request(offset: 80, comparison: TargetCrossingComparison.SettingBelow);
            var time = Start.AddHours(-6);

            Assert.That(request.Position(time).IsRising, Is.True);
            Assert.That(request.Position(time).Altitude, Is.LessThan(80));

            var actual = new TargetCrossingCalculator(request).Find(time);
            var expected = Oracle(request, time);

            Assert.That(actual.Status, Is.EqualTo(TargetCrossingStatus.Found));
            Assert.That(Math.Abs((actual.Time - expected).TotalSeconds), Is.LessThanOrEqualTo(10));

            Assert.That(request.Position(actual.Time).IsRising, Is.False);
        }

        [Test]
        public void DetectsTenSecondGrazingWindow() {
            var request = Request();
            var coordinates = InputCoordinates();
            double Altitude(DateTime time) => coordinates.Transform(Angle.ByDegree(47), Angle.ByDegree(8), 100, time).Altitude.Degree;
            var left = Start.AddHours(-2);
            var right = Start.AddHours(2);
            for (int iteration = 0; iteration < 45; iteration++) {
                var first = left.AddTicks((right.Ticks - left.Ticks) / 3);
                var second = right.AddTicks(-(right.Ticks - left.Ticks) / 3);
                if (Altitude(first) < Altitude(second)) {
                    left = first;
                } else {
                    right = second;
                }
            }

            var peak = left.AddTicks((right.Ticks - left.Ticks) / 2);
            var grazing = Request(offset: Math.Min(Altitude(peak.AddSeconds(-5)), Altitude(peak.AddSeconds(5))));
            var actual = new TargetCrossingCalculator(grazing).Find(Start.AddHours(-2));
            var expected = Oracle(grazing, peak.AddSeconds(-10), 0.05, 0.006);

            Assert.That(expected, Is.Not.EqualTo(DateTime.MinValue));

            Assert.That(actual.Status, Is.EqualTo(TargetCrossingStatus.Found));
            Assert.That((actual.Time - expected).TotalSeconds, Is.InRange(-0.005, 10));
            Assert.That(OraclePredicate(grazing, actual.Time), Is.True);

            TestContext.Out.WriteLine($"Grazing result: {actual.Status}, {actual.Evaluations} coordinate evaluations");
        }

        [TestCase(0)]
        [TestCase(3)]
        public void HorizonSeamCrossingPreservesDistinctZeroAnd360Values(int policy) {
            var coordinates = Request(ra: 5, dec: 70, latitude: 47);
            var time = Start;
            for (int minute = 0; minute < 1440; minute++) {
                var left = coordinates.Position(Start.AddMinutes(minute));
                var right = coordinates.Position(Start.AddMinutes(minute + 1));
                if (Math.Abs(left.Azimuth - right.Azimuth) > 180) {
                    time = Start.AddMinutes(minute - 10);
                    break;
                }
            }

            var startsOnLowAzimuthSide = coordinates.Position(time).Azimuth < 180;
            var sourceHeight = policy == 0 ? 90 : -90;
            var lowHeight = startsOnLowAzimuthSide ? sourceHeight : -sourceHeight;
            var highHeight = -lowHeight;
            var definition = FormattableString.Invariant($"0 {lowHeight}\n90 {lowHeight}\n180 {lowHeight}\n270 {highHeight}\n359 {highHeight}\n360 {highHeight}");
            var horizon = CustomHorizon.FromReader_Standard(new StringReader(definition));
            var request = Request(ra: 5, dec: 70, latitude: 47, offset: 0, horizon: horizon, comparison: (TargetCrossingComparison)policy);
            Assert.That(request.Qualifies(request.Position(time)), Is.False);

            var expected = Oracle(request, time, 1, coordinates: InputCoordinates(5, 70));
            var actual = new TargetCrossingCalculator(request).Find(time);

            Assert.That(actual.Status, Is.EqualTo(TargetCrossingStatus.Found));
            Assert.That(Math.Abs((actual.Time - expected).TotalSeconds), Is.LessThanOrEqualTo(10));

            Assert.That(OraclePredicate(request, actual.Time, InputCoordinates(5, 70)), Is.True);
        }

        [TestCase(0, -4)]
        [TestCase(0, 4)]
        [TestCase(3, -4)]
        [TestCase(3, 4)]
        public void CustomCrossingsAgreeWithFullTransform(int policy, double offset) {
            var horizon = CustomHorizon.FromReader_Standard(new StringReader("0 20\n50 40\n100 15\n180 55\n210 10\n300 40\n360 5"));
            var request = Request(offset: offset, horizon: horizon, comparison: (TargetCrossingComparison)policy);
            var expected = Oracle(request, Start);
            var actual = new TargetCrossingCalculator(request).Find(Start);

            Assert.That(actual.Status, Is.EqualTo(expected == Start ? TargetCrossingStatus.AlreadySatisfied : TargetCrossingStatus.Found));
            Assert.That(Math.Abs((actual.Time - expected).TotalSeconds), Is.LessThanOrEqualTo(10));

            Assert.That(OraclePredicate(request, actual.Time), Is.True);
            TestContext.Out.WriteLine($"Custom policy {policy}, offset {offset}: {actual.Evaluations} coordinate evaluations");
        }

        [TestCase(0, false)]
        [TestCase(3, false)]
        [TestCase(12, false)]
        [TestCase(0, true)]
        [TestCase(3, true)]
        [TestCase(12, true)]
        public void DetectsTenSecondCustomWindowBeforeAnyLaterCrossing(int futureHour, bool obstruction) {
            var baseRequest = Request();
            var start = Start.AddHours(futureHour);
            var center = start.AddSeconds(600);
            var outside = obstruction ? -91.0 : 91.0;
            var edges = new[] { -5.25, -5.0, 5.0, 5.25 }
                .Select(seconds => (
                    Azimuth: baseRequest.Position(center.AddSeconds(seconds)).Azimuth,
                    Altitude: Math.Abs(seconds) > 5 ? outside : -outside))
                .OrderBy(vertex => vertex.Azimuth)
                .ToArray();
            var text = FormattableString.Invariant($"0 {outside:R}\n")
                + string.Join("\n", edges.Select(vertex => FormattableString.Invariant($"{vertex.Azimuth:R} {vertex.Altitude:R}")))
                + FormattableString.Invariant($"\n360 {outside:R}");
            var request = Request(
                offset: 0,
                horizon: CustomHorizon.FromReader_Standard(new StringReader(text)),
                comparison: obstruction ? TargetCrossingComparison.BelowStrict : TargetCrossingComparison.AboveStrict);
            var expected = Oracle(request, center.AddSeconds(-6), 0.05, 0.004);
            var actual = new TargetCrossingCalculator(request).Find(start);

            Assert.That(actual.Status, Is.EqualTo(TargetCrossingStatus.Found));
            Assert.That((actual.Time - expected).TotalSeconds, Is.InRange(-0.005, 10));

            Assert.That(OraclePredicate(request, actual.Time), Is.True);
            TestContext.Out.WriteLine($"Ten-second window hour {futureHour}: {actual.Evaluations} coordinate evaluations");
        }

        [TestCase(89.9, 80, 85)]
        [TestCase(-89.9, -80, 85)]
        [TestCase(20, 47, 90)]
        public void UnreachableAltitudeHasNoInventedTimestamp(double dec, double latitude, double offset) {
            var actual = new TargetCrossingCalculator(Request(dec: dec, latitude: latitude, offset: offset)).Find(Start);

            Assert.That(actual.Status, Is.EqualTo(TargetCrossingStatus.NoEvent));
            Assert.That(actual.Time, Is.EqualTo(default(DateTime)));

            Assert.That(actual.Evaluations, Is.LessThanOrEqualTo(TargetCrossingCalculator.MaximumEvaluations));
        }

        [Test]
        public void BudgetExhaustionCannotPublishALaterRoot() {
            var request = Request(offset: 80);
            var actual = new TargetCrossingCalculator(request, 2).Find(Start);

            Assert.That(actual.Status, Is.EqualTo(TargetCrossingStatus.Exhausted));
            Assert.That(actual.Time, Is.EqualTo(default(DateTime)));

            Assert.That(actual.Evaluations, Is.EqualTo(2));
        }

        [Test]
        public void JnowNormalizationRetainsOriginalReferenceDate() {
            var reference = new DateTime(2004, 12, 31, 23, 0, 0, DateTimeKind.Utc);
            var coordinates = new Coordinates(Angle.ByHours(5), Angle.ByDegree(20), Epoch.JNOW, reference);
            var request = new TargetCrossingRequest(coordinates, 47, 8, 100, null, 30, TargetCrossingComparison.BelowStrict);
            var full = coordinates.Transform(Angle.ByDegree(47), Angle.ByDegree(8), 100, Start);

            Assert.That(request.Position(Start).Altitude, Is.EqualTo(full.Altitude.Degree).Within(1e-10));
            Assert.That(new TargetCrossingCalculator(request).Find(Start).Time, Is.EqualTo(Oracle(request, Start, coordinates: coordinates)).Within(TimeSpan.FromSeconds(0.25)));
        }

        [Test]
        public void UtcAndLocalTimesReferToSameInstantAcrossYearBoundary() {
            var request = Request(comparison: TargetCrossingComparison.BelowStrict);
            var utc = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc);

            Assert.That(new TargetCrossingCalculator(request).Find(utc.ToLocalTime()).Time, Is.EqualTo(new TargetCrossingCalculator(request).Find(utc).Time));
        }

        [Test]
        public void NonfiniteInitialSampleIsUnresolvedWithoutSearch() {
            var sample = new TargetPosition(double.NaN, double.NaN, double.NaN, double.NaN);
            var actual = new TargetCrossingCalculator(Request()).Find(Start, sample);

            Assert.That(actual.Status, Is.EqualTo(TargetCrossingStatus.Exhausted));
            Assert.That(actual.Time, Is.EqualTo(default(DateTime)));
            Assert.That(actual.Evaluations, Is.Zero);
        }

        [TestCase(1)]
        [TestCase(3)]
        [TestCase(4)]
        public void StrictEqualityFindsTheImmediateCrossingInsteadOfTomorrow(int policy) {
            var comparison = (TargetCrossingComparison)policy;
            var initial = Request();
            var coordinates = InputCoordinates();
            var first = comparison == TargetCrossingComparison.AboveStrict ? Start.AddHours(-4) : Start;

            // Different hour angles exercise both signs of the roundoff from inverting altitude.
            for (int minute = 0; minute < 120; minute += 5) {
                var time = first.AddMinutes(minute);
                var position = initial.Position(time);
                var request = new TargetCrossingRequest(coordinates, 47, 8, 100, null, position.Altitude, comparison);
                Assert.That(position.IsRising, Is.EqualTo(comparison == TargetCrossingComparison.AboveStrict));
                Assert.That(request.Qualifies(position), Is.False);

                var actual = new TargetCrossingCalculator(request).Find(time);

                Assert.That(actual.Status, Is.EqualTo(TargetCrossingStatus.Found));
                Assert.That((actual.Time - time).TotalSeconds, Is.InRange(0, 0.25), $"Exact equality at {time:O}");
                Assert.That(request.Qualifies(request.Position(actual.Time)), Is.True);
            }
        }

        [Test]
        public void LegacyNonfiniteObserverReturnsMissingEventsInsteadOfThrowing() {
            var coordinates = new Coordinates(5, 20, Epoch.J2000, Coordinates.RAType.Hours);
            var actual = ItemUtility.CalculateTimeAtAltitude(coordinates, double.NaN, 8, 100, 30, Start);

            Assert.That(actual.Rise, Is.EqualTo(DateTime.MinValue));
            Assert.That(actual.Set, Is.EqualTo(DateTime.MinValue));
            Assert.That(actual.Meridian, Is.EqualTo(DateTime.MinValue));
            Assert.That(actual.IsRising, Is.False);
        }

        [TestCase(2026, 3, 29)]
        [TestCase(2026, 10, 25)]
        public void UtcSearchAndLocalPresentationAgreeOnDstTransitionDays(int year, int month, int day) {
            var utc = new DateTime(year, month, day, 0, 59, 59, DateTimeKind.Utc);
            var request = Request(comparison: TargetCrossingComparison.BelowStrict);
            var result = new TargetCrossingCalculator(request).Find(utc);
            var local = new TargetCrossingCalculator(request).Find(utc.ToLocalTime());

            Assert.That(local.Status, Is.EqualTo(result.Status));
            Assert.That(local.Time, Is.EqualTo(result.Time));
            if (result.Status == TargetCrossingStatus.Found) {
                Assert.That(result.Time.ToLocalTime().ToUniversalTime(), Is.EqualTo(result.Time));
            }
        }

        [Test]
        public void MotionCapContainsFullTransformSamplesIncludingZenithAndPoles() {
            foreach (var latitude in new[] { -90.0, -47, 0, 47, 90 }) {
                foreach (var dec in new[] { -89.9, -47, 0, 47, 89.9 }) {
                    var request = Request(dec: dec, latitude: latitude);
                    for (int hour = 0; hour < 24; hour++) {
                        var first = request.Position(Start.AddHours(hour));
                        var next = request.Position(Start.AddHours(hour).AddSeconds(30));
                        var radians = Math.PI / 180;
                        var cosine = Math.Sin(first.Altitude * radians) * Math.Sin(next.Altitude * radians)
                            + Math.Cos(first.Altitude * radians) * Math.Cos(next.Altitude * radians) * Math.Cos((first.Azimuth - next.Azimuth) * radians);
                        var distance = Math.Acos(Math.Clamp(cosine, -1, 1)) / radians;

                        Assert.That(distance, Is.LessThanOrEqualTo(16 * 30.0 / 3600 + 1e-7));
                    }
                }
            }
        }

        [TestCase(2004, 12, 31)]
        [TestCase(2034, 1, 1)]
        public void LegacyExplicitDateUsesSuppliedDay(int year, int month, int day) {
            var time = new DateTime(year, month, day, 12, 0, 0, DateTimeKind.Utc);
            var coordinates = new Coordinates(5, 20, Epoch.J2000, Coordinates.RAType.Hours);
            var result = ItemUtility.CalculateTimeAtAltitude(coordinates, 47, 8, 100, 30, time);

            Assert.That(result.Rise.Year, Is.EqualTo(year));
            Assert.That((result.Rise.ToUniversalTime() - time.Date).TotalDays, Is.InRange(0, 2));

            Assert.That(result.CurrentAltitude, Is.EqualTo(coordinates.Transform(Angle.ByDegree(47), Angle.ByDegree(8), 100, time).Altitude.Degree).Within(1e-10));
        }
    }
}
