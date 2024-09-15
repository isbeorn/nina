using FluentAssertions;
using Moq;
using NINA.Astrometry;
using NINA.Astrometry.Interfaces;
using NINA.Core.Utility;
using NINA.Profile;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NINA.Equipment.Equipment.MyGPS.PegasusAstro.UnityApi.DriverUranusReport;
using static NINA.Sequencer.SequenceItem.FlatDevice.SkyFlat;

namespace NINA.Test.Sequencer.SequenceItem.FlatDevice {
    [TestFixture]
    public class SkyFlatExposureDeterminationTest {
        [Test]
        [TestCase(50, -95, 7, 11, 22, 0)]
        [TestCase(50, 0, 7, 11, 22, 0)]
        [TestCase(50, 195, 7, 11, 22, 0)]
        [TestCase(-50, -95, 7, 11, 17, 15)]

        [TestCase(50, -95, 1, 11, 22, 0)]
        [TestCase(50, 0, 1, 11, 22, 0)]
        [TestCase(50, 195, 1, 11, 22, 0)]
        [TestCase(-50, -95, 1, 11, 17, 15)]
        public void DuskFlats_ExposureTimeIncreases(double latitude, double longitude, int month, int day, int hour, int minute) {
            ITwilightCalculator twilightCalculator = new TwilightCalculator();

            var dt = new Mock<ICustomDateTime>();

            var spring = new DateTime(2024, 03, 20);
            var now = new DateTime(2024, month, day, hour, minute, 0);
            twilightCalculator.GetTwilightDuration(spring, 30.0, 0d);
            var springTwilight = twilightCalculator.GetTwilightDuration(spring, 30.0, 0d).TotalMilliseconds;
            var todayTwilight = twilightCalculator.GetTwilightDuration(now, latitude, longitude).TotalMilliseconds;

            dt.Setup(x => x.Now).Returns(now);
            var sut = new SkyFlatExposureDetermination(Stopwatch.StartNew(), 5, springTwilight, todayTwilight, dt.Object);

            var firstExposure = sut.GetNextExposureTime(TimeSpan.FromSeconds(0));
            var secondExposure = sut.GetNextExposureTime(TimeSpan.FromMinutes(5));
            var thirdExposure = sut.GetNextExposureTime(TimeSpan.FromMinutes(15));

            firstExposure.Should().BeLessThan(secondExposure);
            secondExposure.Should().BeLessThan(thirdExposure);
        }

        [Test]
        [TestCase(50, -95, 7, 11, 4, 40)]
        [TestCase(50, 0, 7, 11, 4, 40)]
        [TestCase(50, 195, 7, 11, 4, 40)]
        [TestCase(-50, -95, 7, 11, 7, 40)]

        [TestCase(50, -95, 1, 11, 4, 40)]
        [TestCase(50, 0, 1, 11, 4, 40)]
        [TestCase(50, 195, 1, 11, 4, 40)]
        [TestCase(-50, -95, 1, 11, 7, 40)]
        public void DawnFlats_ExposureTimeDecreases(double latitude, double longitude, int month, int day, int hour, int minute) {
            ITwilightCalculator twilightCalculator = new TwilightCalculator();

            var dt = new Mock<ICustomDateTime>();

            var spring = new DateTime(2024, 03, 20);
            var now = new DateTime(2024, month, day, hour, minute, 0);

            var springTwilight = twilightCalculator.GetTwilightDuration(spring, 30.0, 0d).TotalMilliseconds;
            var todayTwilight = twilightCalculator.GetTwilightDuration(now, latitude, longitude).TotalMilliseconds;

            dt.Setup(x => x.Now).Returns(now);
            var sut = new SkyFlatExposureDetermination(Stopwatch.StartNew(), 5, springTwilight, todayTwilight, dt.Object);

            var firstExposure = sut.GetNextExposureTime(TimeSpan.FromMinutes(0));
            var secondExposure = sut.GetNextExposureTime(TimeSpan.FromMinutes(5));
            var thirdExposure = sut.GetNextExposureTime(TimeSpan.FromMinutes(15));

            firstExposure.Should().BeGreaterThan(secondExposure);
            secondExposure.Should().BeGreaterThan(thirdExposure);
        }
    }
}
