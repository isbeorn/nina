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
using NINA.Core.Model;
using NINA.ViewModel;
using NUnit.Framework;
using System;
using System.Reflection;
using System.Threading;

namespace NINA.Test.ViewModel {

    [TestFixture]
    public class SkyAtlasVMTest {

        [Test]
        public void IsAboveHorizonForDuration_DoesNotPopulateAltitudeChart() {
            var referenceDate = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Local);
            var dso = new DeepSkyObject("Equator", new Coordinates(0, 0, Epoch.J2000, Coordinates.RAType.Hours), null);

            var result = SkyAtlasVM.IsAboveHorizonForDuration(
                dso,
                referenceDate,
                referenceDate,
                referenceDate.AddHours(3),
                latitude: 0,
                siderealTimeAtReferenceDate: 0,
                skyWindow: null,
                minimumDuration: 2,
                token: CancellationToken.None);

            result.Should().BeTrue();
            GetAltitudeCache(dso).Should().BeNull();
        }

        [Test]
        public void IsAboveHorizonForDuration_RejectsBelowHorizonWindow() {
            var referenceDate = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Local);
            var dso = new DeepSkyObject("Equator", new Coordinates(0, 0, Epoch.J2000, Coordinates.RAType.Hours), null);

            var result = SkyAtlasVM.IsAboveHorizonForDuration(
                dso,
                referenceDate,
                referenceDate.AddHours(8),
                referenceDate.AddHours(10),
                latitude: 0,
                siderealTimeAtReferenceDate: 0,
                skyWindow: null,
                minimumDuration: 1,
                token: CancellationToken.None);

            result.Should().BeFalse();
            GetAltitudeCache(dso).Should().BeNull();
        }

        // The equator object at latitude 0 transits at the zenith at the reference time, so its altitude is 90 - |hour angle|
        // and it is above an upper horizon of 60 degrees during the first two hours only.
        private static bool IsAvailableForDuration(
            SkyWindow? skyWindow,
            double windowEndHours,
            double minimumDuration,
            double siderealTimeAtReferenceDate = 0,
            double windowStartHours = 0) {
            var referenceDate = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Local);
            var dso = new DeepSkyObject("Equator", new Coordinates(0, 0, Epoch.J2000, Coordinates.RAType.Hours), null);

            return SkyAtlasVM.IsAboveHorizonForDuration(
                dso,
                referenceDate,
                referenceDate.AddHours(windowStartHours),
                referenceDate.AddHours(windowEndHours),
                latitude: 0,
                siderealTimeAtReferenceDate: siderealTimeAtReferenceDate,
                skyWindow: skyWindow!,
                minimumDuration: minimumDuration,
                token: CancellationToken.None);
        }

        [Test]
        public void IsAboveHorizonForDuration_FullInterval_RejectsObjectAboveTheUpperHorizon() {
            IsAvailableForDuration(null, windowEndHours: 3, minimumDuration: double.MaxValue).Should().BeTrue();

            IsAvailableForDuration(HorizonTestData.Dual(null, 60), windowEndHours: 3, minimumDuration: double.MaxValue).Should().BeFalse();
        }

        [Test]
        public void IsAboveHorizonForDuration_RequiresTheDurationBelowTheUpperHorizon() {
            var upper = HorizonTestData.Dual(null, 60);

            // Without the upper horizon the object is up for the whole window
            IsAvailableForDuration(null, windowEndHours: 4, minimumDuration: 3).Should().BeTrue();
            // With it only the last two of the four hours count
            IsAvailableForDuration(upper, windowEndHours: 4, minimumDuration: 3).Should().BeFalse();
            IsAvailableForDuration(upper, windowEndHours: 4, minimumDuration: 1).Should().BeTrue();
        }

        // At this sidereal time the equator object at latitude 0 transits at the zenith 3 minutes after the reference time, between the
        // samples at 0 and 6 minutes. With a flat upper horizon of 89.5 degrees it is above the limit for about 4 minutes,
        // which none of the 6 minute samples sees.
        private const double TransitBetweenSamples = -0.05;

        [Test]
        public void IsAboveHorizonForDuration_FullInterval_RejectsExcursionAboveTheUpperHorizonBetweenSamples() {
            var upper = HorizonTestData.Dual(null, 89.5);

            IsAvailableForDuration(null, windowEndHours: 1, minimumDuration: double.MaxValue, TransitBetweenSamples).Should().BeTrue();

            IsAvailableForDuration(upper, windowEndHours: 1, minimumDuration: double.MaxValue, TransitBetweenSamples).Should().BeFalse();
        }

        [Test]
        public void IsAboveHorizonForDuration_Duration_DoesNotCountAcrossAnExcursionAboveTheUpperHorizonBetweenSamples() {
            var upper = HorizonTestData.Dual(null, 89.5);

            // Without the upper horizon the whole 0.3 h window is one run
            IsAvailableForDuration(null, windowEndHours: 0.3, minimumDuration: 0.25, TransitBetweenSamples).Should().BeTrue();

            // The excursion ends about 4 minutes into the window, so the remaining run is shorter than 0.25 h
            IsAvailableForDuration(upper, windowEndHours: 0.3, minimumDuration: 0.25, TransitBetweenSamples).Should().BeFalse();
            IsAvailableForDuration(upper, windowEndHours: 0.3, minimumDuration: 0.2, TransitBetweenSamples).Should().BeTrue();
        }

        [Test]
        public void IsAboveHorizonForDuration_Duration_CountsTheRunThatEndsAtTheFirstExcursion() {
            // Transit (zenith) 61.5 minutes into the window. Above a flat 89.9 degrees for about 48 seconds, so the first
            // available run lasts about 61 minutes and 6 seconds, the second about 58 minutes.
            var upper = HorizonTestData.Dual(null, 89.9);
            var transitAt61Point5Minutes = -61.5 / 60;

            IsAvailableForDuration(upper, windowEndHours: 2, minimumDuration: 1, transitAt61Point5Minutes).Should().BeTrue();

            IsAvailableForDuration(upper, windowEndHours: 2, minimumDuration: 1.05, transitAt61Point5Minutes).Should().BeFalse();
        }

        [Test]
        public void IsAboveHorizonForDuration_FullInterval_ChecksTheStartOfTheWindowBeforeTheFirstSample() {
            // The excursion (minutes 1 to 5) lies between the start of the window at minute 1 and its first sample at minute 6
            var upper = HorizonTestData.Dual(null, 89.5);
            var windowStart = 1d / 60;

            IsAvailableForDuration(null, windowEndHours: 1, minimumDuration: double.MaxValue, TransitBetweenSamples, windowStart).Should().BeTrue();

            IsAvailableForDuration(upper, windowEndHours: 1, minimumDuration: double.MaxValue, TransitBetweenSamples, windowStart).Should().BeFalse();

            // A window that starts after the excursion is not affected
            IsAvailableForDuration(upper, windowEndHours: 1, minimumDuration: double.MaxValue, TransitBetweenSamples, windowStartHours: 6d / 60).Should().BeTrue();
        }

        [Test]
        public void IsAboveHorizonForDuration_FullInterval_ChecksTheEndOfTheWindowAfterTheLastSample() {
            // Transit 58.5 minutes into the window: the excursion (minutes 56.5 to 60.5) starts after the last sample at minute 54
            var upper = HorizonTestData.Dual(null, 89.5);
            var transitAt58Point5Minutes = -58.5 / 60;

            IsAvailableForDuration(upper, windowEndHours: 59d / 60, minimumDuration: double.MaxValue, transitAt58Point5Minutes).Should().BeFalse();

            // A window that ends before the excursion is not affected
            IsAvailableForDuration(upper, windowEndHours: 56d / 60, minimumDuration: double.MaxValue, transitAt58Point5Minutes).Should().BeTrue();
        }

        [Test]
        public void IsAboveHorizonForDuration_FullInterval_ChecksAWindowWithoutAnySample() {
            // Minutes 1 to 5 contain no sample (the grid has 0 and 6 minutes), but the zenith transit at minute 3
            var upper = HorizonTestData.Dual(null, 89.5);
            var windowStart = 1d / 60;

            IsAvailableForDuration(null, windowEndHours: 5d / 60, minimumDuration: double.MaxValue, TransitBetweenSamples, windowStart).Should().BeTrue();

            IsAvailableForDuration(upper, windowEndHours: 5d / 60, minimumDuration: double.MaxValue, TransitBetweenSamples, windowStart).Should().BeFalse();
        }

        [Test]
        public void IsAboveHorizonForDuration_FullInterval_ChecksTheEndOfTheDayAfterTheLastSample() {
            // The last sample of the day is at 23 h 54 min. The transit at 23 h 56 min lies in the window 23 h 50 min to 23 h 58 min.
            var upper = HorizonTestData.Dual(null, 89.9);
            var transitAt23h56 = -(23 + (56d / 60));
            var windowStart = 23 + (50d / 60);
            var windowEnd = 23 + (58d / 60);

            IsAvailableForDuration(null, windowEnd, minimumDuration: double.MaxValue, transitAt23h56, windowStart).Should().BeTrue();

            IsAvailableForDuration(upper, windowEnd, minimumDuration: double.MaxValue, transitAt23h56, windowStart).Should().BeFalse();
        }

        [Test]
        public void IsAboveHorizonForDuration_Duration_DoesNotExtendTheRunToTheFirstUnavailableSample() {
            // The equator object sets 6 hours after its transit (altitude 90 - hour angle). Within the window from 5 to 7 hours the last
            // available sample is at 6 hours, so the run is 1 hour long, not the 1.1 hours up to the first sample below the horizon.
            // The upper horizon is never exceeded in this window.
            var upper = HorizonTestData.Dual(null, 89.9);

            IsAvailableForDuration(null, windowEndHours: 7, minimumDuration: 1, windowStartHours: 5).Should().BeFalse();
            IsAvailableForDuration(upper, windowEndHours: 7, minimumDuration: 1, windowStartHours: 5).Should().BeFalse();

            // Shorter durations still fit in the run
            IsAvailableForDuration(null, windowEndHours: 7, minimumDuration: 0.8, windowStartHours: 5).Should().BeTrue();
            IsAvailableForDuration(upper, windowEndHours: 7, minimumDuration: 0.8, windowStartHours: 5).Should().BeTrue();
        }

        [Test]
        public void IsAboveHorizonForDuration_Duration_CreditsAtMostUpToTheLastSample() {
            // The last sample of the window is at minute 60, the excursion above 89.9 degrees starts at minute 61 and 6 seconds.
            // The upper horizon must not lengthen the run beyond what the same window gives without it.
            var upper = HorizonTestData.Dual(null, 89.9);
            var transitAt61Point5Minutes = -61.5 / 60;
            var windowEnd = 61.9 / 60;

            IsAvailableForDuration(null, windowEnd, minimumDuration: 1, transitAt61Point5Minutes).Should().BeFalse();
            IsAvailableForDuration(upper, windowEnd, minimumDuration: 1, transitAt61Point5Minutes).Should().BeFalse();

            // Shorter durations still fit in the run
            IsAvailableForDuration(null, windowEnd, minimumDuration: 0.9, transitAt61Point5Minutes).Should().BeTrue();
            IsAvailableForDuration(upper, windowEnd, minimumDuration: 0.9, transitAt61Point5Minutes).Should().BeTrue();
        }

        private static object? GetAltitudeCache(DeepSkyObject dso) {
            return typeof(SkyObjectBase)
                .GetField("_altitudes", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(dso);
        }
    }
}
