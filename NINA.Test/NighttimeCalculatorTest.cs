#region "copyright"
/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using NINA.Astrometry;
using FluentAssertions;
using Moq;
using NINA.Profile.Interfaces;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Test {

    [TestFixture]
    public class NighttimeCalculatorTest {

        [Test]
        public void AfterNoonTest() {
            var date = new DateTime(2020, 5, 4, 14, 0, 0);
            var referenceDate = NighttimeCalculator.GetReferenceDate(date);
            var expectedDate = new DateTime(date.Year, date.Month, date.Day, 12, 0, 0);
            Assert.That(referenceDate, Is.EqualTo(expectedDate));
        }

        [Test]
        public void BeforeNoonTest() {
            var date = new DateTime(2020, 5, 4, 10, 0, 0);
            var referenceDate = NighttimeCalculator.GetReferenceDate(date);
            var dayBefore = date.AddDays(-1);
            var expectedDate = new DateTime(dayBefore.Year, dayBefore.Month, dayBefore.Day, 12, 0, 0);
            Assert.That(referenceDate, Is.EqualTo(expectedDate));
        }

        [Test]
        public void AtNoonSlightlyBeforeTest() {
            var date = new DateTime(2020, 5, 4, 11, 59, 0);
            var referenceDate = NighttimeCalculator.GetReferenceDate(date);
            var dayBefore = date.AddDays(-1);
            var expectedDate = new DateTime(dayBefore.Year, dayBefore.Month, dayBefore.Day, 12, 0, 0);
            Assert.That(referenceDate, Is.EqualTo(expectedDate));
        }

        [Test]
        public void AtNoonTest() {
            var date = new DateTime(2020, 5, 4, 12, 0, 0);
            var referenceDate = NighttimeCalculator.GetReferenceDate(date);
            var dayBefore = date.AddDays(-1);
            var expectedDate = new DateTime(date.Year, date.Month, date.Day, 12, 0, 0);
            Assert.That(referenceDate, Is.EqualTo(expectedDate));
        }

        [Test]
        public void AtNoonSlightlyAfterTest() {
            var date = new DateTime(2020, 5, 4, 12, 1, 0);
            var referenceDate = NighttimeCalculator.GetReferenceDate(date);
            var expectedDate = new DateTime(date.Year, date.Month, date.Day, 12, 0, 0);
            Assert.That(referenceDate, Is.EqualTo(expectedDate));
        }

        /// <summary>
        /// Verifies a complete Greenwich equinox night calculation, including reference-day selection,
        /// physical ordering of twilight and sunrise/sunset events, lunar illumination bounds, and cache
        /// reuse for multiple requested times within the same astronomical reference night.
        /// </summary>
        [Test]
        public void Calculate_GreenwichEquinox_ReturnsOrderedEventsAndCachesReferenceNight() {
            Mock<IProfileService> profileService = CreateProfileService(51.4769, 0.0, 46.0);
            NighttimeCalculator calculator = new NighttimeCalculator(profileService.Object);
            DateTime evening = new DateTime(2024, 3, 20, 22, 0, 0, DateTimeKind.Utc);
            DateTime beforeDawn = new DateTime(2024, 3, 21, 4, 0, 0, DateTimeKind.Utc);

            NighttimeData data = calculator.Calculate(evening);
            NighttimeData cached = calculator.Calculate(beforeDawn);

            cached.Should().BeSameAs(data);
            data.Date.Should().Be(evening);
            data.ReferenceDate.Should().Be(new DateTime(2024, 3, 20, 12, 0, 0, DateTimeKind.Utc));
            data.SunRiseAndSet.Set.Should().NotBeNull();
            data.SunRiseAndSet.Rise.Should().NotBeNull();
            data.CivilTwilightRiseAndSet.Set.Should().BeBefore(data.NauticalTwilightRiseAndSet.Set ?? throw new AssertionException("Expected nautical sunset."));
            data.NauticalTwilightRiseAndSet.Set.Should().BeBefore(data.TwilightRiseAndSet.Set ?? throw new AssertionException("Expected astronomical sunset."));
            data.TwilightRiseAndSet.Rise.Should().BeBefore(data.NauticalTwilightRiseAndSet.Rise ?? throw new AssertionException("Expected nautical sunrise."));
            data.NauticalTwilightRiseAndSet.Rise.Should().BeBefore(data.CivilTwilightRiseAndSet.Rise ?? throw new AssertionException("Expected civil sunrise."));
            data.Illumination.Should().BeInRange(0.0, 1.0);
            data.ReferenceDateSpan.Should().HaveCount(2);
            data.NightDuration.Should().HaveCount(2);
            data.TwilightDuration.Should().HaveCount(6);
            data.NauticalTwilightDuration.Should().HaveCount(6);
            data.CivilTwilightDuration.Should().HaveCount(6);
        }

        [Test]
        public void Calculate_ElevationChanges_UsesMatchingObserverAndReusesOriginalNight() {
            var profile = CreateProfileService(52, 13, 0);
            var settings = Mock.Get(profile.Object.ActiveProfile.AstrometrySettings);
            var elevation = 0.0;
            settings.SetupGet(x => x.Elevation).Returns(() => elevation);
            var calculator = new NighttimeCalculator(profile.Object);
            var date = new DateTime(2026, 3, 20, 12, 0, 0, DateTimeKind.Utc);
            var seaLevel = calculator.Calculate(date);
            elevation = 3000;
            var mountain = calculator.Calculate(date);
            try {
                mountain.Should().NotBeSameAs(seaLevel);
                mountain.SunRiseAndSet.Elevation.Should().Be(3000);
                var expected = AstroUtil.GetSunRiseAndSet(date, 52, 13, 3000);
                mountain.SunRiseAndSet.Rise.Should().Be(expected.Rise);
                mountain.SunRiseAndSet.Set.Should().Be(expected.Set);
                elevation = 0;
                calculator.Calculate(date).Should().BeSameAs(seaLevel);
            } finally {
                seaLevel.Ticker.Stop();
                mountain.Ticker.Stop();
            }
        }

        [TestCase(52, 3, 20)]
        [TestCase(52, 6, 21)]
        [TestCase(69.65, 6, 21)]
        [TestCase(-69.65, 12, 21)]
        [TestCase(90, 6, 21)]
        [TestCase(-90, 12, 21)]
        public void Calculate_SharedPositions_PreserveLunarResultsAndEventRecomputation(double latitude, int month, int day) {
            var date = new DateTime(2026, month, day, 12, 0, 0, DateTimeKind.Utc);
            var observer = new ObserverInfo { Latitude = latitude, Longitude = 13, Elevation = 100 };
            var calculator = new NighttimeCalculator(CreateProfileService(latitude, 13, 100).Object);
            var night = calculator.Calculate(date);
            night.Ticker.Stop();
            night.MoonPhase.Should().Be(AstroUtil.GetMoonPhase(date, observer));
            night.Illumination.Should().Be(AstroUtil.GetMoonIllumination(date, observer));
            foreach (var events in new[] { night.SunRiseAndSet, night.TwilightRiseAndSet,
                night.CivilTwilightRiseAndSet, night.NauticalTwilightRiseAndSet }) {
                var rise = events.Rise;
                var set = events.Set;
                events.Compute();
                AssertSameEvent(events.Rise, rise);
                AssertSameEvent(events.Set, set);
            }
        }

        [TestCase(DateTimeKind.Local)]
        [TestCase(DateTimeKind.Unspecified)]
        public void Calculate_DateKindChanges_DoesNotReuseUtcEventRepresentation(DateTimeKind kind) {
            var calculator = new NighttimeCalculator(CreateProfileService(52, 13, 0).Object);
            var date = new DateTime(2026, 3, 20, 12, 0, 0, DateTimeKind.Utc);
            var utc = calculator.Calculate(date);
            var other = calculator.Calculate(DateTime.SpecifyKind(date, kind));
            utc.Ticker.Stop();
            other.Ticker.Stop();
            other.Should().NotBeSameAs(utc);
            (other.SunRiseAndSet.Rise ?? throw new AssertionException("Expected sunrise.")).Kind.Should().Be(kind);
            (other.SunRiseAndSet.Set ?? throw new AssertionException("Expected sunset.")).Kind.Should().Be(kind);
            calculator.Calculate(date).Should().BeSameAs(utc);
        }

        private static void AssertSameEvent(DateTime? actual, DateTime? expected) {
            actual.HasValue.Should().Be(expected.HasValue);
            if (expected.HasValue) {
                (actual ?? throw new AssertionException("Expected event.")).Should().BeCloseTo(expected.Value, TimeSpan.FromSeconds(0.1));
            }
        }

        private static Mock<IProfileService> CreateProfileService(double latitude, double longitude, double elevation) {
            Mock<IAstrometrySettings> astrometrySettings = new Mock<IAstrometrySettings>();
            astrometrySettings.SetupGet(x => x.Latitude).Returns(latitude);
            astrometrySettings.SetupGet(x => x.Longitude).Returns(longitude);
            astrometrySettings.SetupGet(x => x.Elevation).Returns(elevation);

            Mock<IProfile> profile = new Mock<IProfile>();
            profile.SetupGet(x => x.AstrometrySettings).Returns(astrometrySettings.Object);

            Mock<IProfileService> profileService = new Mock<IProfileService>();
            profileService.SetupGet(x => x.ActiveProfile).Returns(profile.Object);
            return profileService;
        }
    }
}
