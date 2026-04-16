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
            data.CivilTwilightRiseAndSet.Set.Should().BeBefore(data.NauticalTwilightRiseAndSet.Set.Value);
            data.NauticalTwilightRiseAndSet.Set.Should().BeBefore(data.TwilightRiseAndSet.Set.Value);
            data.TwilightRiseAndSet.Rise.Should().BeBefore(data.NauticalTwilightRiseAndSet.Rise.Value);
            data.NauticalTwilightRiseAndSet.Rise.Should().BeBefore(data.CivilTwilightRiseAndSet.Rise.Value);
            data.Illumination.Should().BeInRange(0.0, 1.0);
            data.ReferenceDateSpan.Should().HaveCount(2);
            data.NightDuration.Should().HaveCount(2);
            data.TwilightDuration.Should().HaveCount(6);
            data.NauticalTwilightDuration.Should().HaveCount(6);
            data.CivilTwilightDuration.Should().HaveCount(6);
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
