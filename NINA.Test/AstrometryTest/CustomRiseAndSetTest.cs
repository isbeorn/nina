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
using NINA.Astrometry.Body;
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

        [TestCase(1.0)]
        [TestCase(-1.0)]
        [TestCase(0.0)]
        public void Compute_ConstantAltitude_TerminatesWithoutCrossings(double altitude) {
            var events = new SampledRiseAndSet(_ => altitude);

            events.Compute().Should().BeFalse();

            events.Rise.Should().BeNull();
            events.Set.Should().BeNull();
            events.LatestSample.Should().Be(events.Date.AddHours(26));
            events.SampleCount.Should().BeLessThanOrEqualTo(39, "the 13 two-hour windows must terminate after at most three samples each");
        }

        [TestCase(1.0)]
        [TestCase(-1.0)]
        public void Compute_ExtremumBeyondThreshold_DoesNotRefineAnUnnecessaryExtremum(double direction) {
            var events = new SampledRiseAndSet(hour => direction * ((hour - 1.0) * (hour - 1.0) - 5.0));

            events.Compute().Should().BeTrue();

            (direction > 0.0 ? events.Rise : events.Set).Should().BeCloseTo(
                events.Date.AddHours(1.0 + Math.Sqrt(5.0)), TimeSpan.FromSeconds(0.1));
            (direction > 0.0 ? events.Set : events.Rise).Should().BeNull();
            events.SampleCount.Should().BeLessThanOrEqualTo(53,
                "an extremum already beyond the threshold needs only coarse samples, bounded proposals and bisection fallback");
        }

        [TestCase(1.0)]
        [TestCase(-1.0)]
        public void Compute_RepeatedCalculation_RemovesEarlierEvents(double direction) {
            var events = new SampledRiseAndSet(hour => direction * (hour - 2.0));
            events.Compute().Should().BeTrue();
            (direction > 0 ? events.Rise : events.Set).Should().Be(events.Date.AddHours(2));

            events.Altitude = _ => 1.0;

            events.Compute().Should().BeFalse();
            events.Rise.Should().BeNull();
            events.Set.Should().BeNull();
        }

        [TestCase(1.0, 2.0)]
        [TestCase(-1.0, 2.0)]
        [TestCase(1.0, 0.0)]
        [TestCase(-1.0, 0.0)]
        [TestCase(1.0, 25.0)]
        [TestCase(-1.0, 25.0)]
        [TestCase(1.0, 26.0)]
        [TestCase(-1.0, 26.0)]
        public void Compute_LinearCrossing_PreservesBoundaryAndTwentySixHourSearch(double direction, double hour) {
            var events = new SampledRiseAndSet(time => direction * (time - hour));

            events.Compute().Should().BeTrue();

            (direction > 0 ? events.Rise : events.Set).Should().Be(events.Date.AddHours(hour));
            (direction > 0 ? events.Set : events.Rise).Should().BeNull();
        }

        [TestCase(1.0)]
        [TestCase(-1.0)]
        public void Compute_NarrowCrossingsBetweenCoarseSamples_RefinesBothEvents(double direction) {
            // A narrow excursion whose two true roots differ markedly from the fitted quadratic.
            var events = new SampledRiseAndSet(hour => direction * (Math.Pow(hour - 0.31, 4) - Math.Pow(0.002, 4)));

            events.Compute().Should().BeTrue();

            (direction > 0 ? events.Set : events.Rise).Should().BeCloseTo(events.Date.AddHours(0.308), TimeSpan.FromSeconds(0.1));
            (direction > 0 ? events.Rise : events.Set).Should().BeCloseTo(events.Date.AddHours(0.312), TimeSpan.FromSeconds(0.1));
        }

        [TestCase(1.0)]
        [TestCase(-1.0)]
        public void Compute_CoarsePolynomialPredictsCrossingButBodyNeverCrosses_ReturnsNoEvents(double direction) {
            var events = new SampledRiseAndSet(hour => direction * (Math.Pow(hour - 0.31, 4) + 1e-9));

            events.Compute().Should().BeFalse();

            events.Rise.Should().BeNull();
            events.Set.Should().BeNull();
        }

        [TestCase(1.0)]
        [TestCase(-1.0)]
        public void Compute_TangentAtSampleTime_DoesNotReportACrossing(double direction) {
            var events = new SampledRiseAndSet(hour => direction * (hour - 1.0) * (hour - 1.0));

            events.Compute().Should().BeFalse();

            events.Rise.Should().BeNull();
            events.Set.Should().BeNull();
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void Compute_NonfiniteAltitude_ReportsCalculationFailure(double altitude) {
            var events = new SampledRiseAndSet(_ => altitude);

            Action calculate = () => events.Compute();

            calculate.Should().Throw<InvalidOperationException>().WithMessage("*altitude*2026*");
        }

        private sealed class SampledRiseAndSet(Func<double, double> altitude) : SunCustomRiseAndSet(
            new DateTime(2026, 10, 8, 0, 0, 0, DateTimeKind.Utc), 52.0, 13.0, 0.0, 0.0) {
            private int samples;
            public int SampleCount => samples;
            public Func<double, double> Altitude { get; set; } = altitude;
            public DateTime LatestSample { get; private set; }

            protected override double AdjustAltitude(BasicBody body) {
                // Guard the old constant-altitude infinite loop without hanging the test process.
                if (++samples > 2000) {
                    throw new InvalidOperationException("The rise/set search did not terminate.");
                }
                if (body.Date > LatestSample) {
                    LatestSample = body.Date;
                }
                return Altitude((body.Date - Date).TotalHours);
            }
        }
    }
}
