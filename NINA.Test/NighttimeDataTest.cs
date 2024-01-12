#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry;
using NINA.Astrometry.RiseAndSet;
using NUnit.Framework;
using OxyPlot.Axes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NINA.Astrometry.AstroUtil;

namespace NINA.Test {

    [TestFixture]
    public class NighttimeDataTest {
        private DateTime date;
        private DateTime referenceDate;
        private double latitude;
        private double longitude;
        private RiseAndSetEvent twilightRiseAndSet;
        private RiseAndSetEvent nauticalTwilightRiseAndSet;
        private RiseAndSetEvent moonRiseAndSet;
        private RiseAndSetEvent sunRiseAndSet;
        private MoonPhase moonPhase;
        private double? illumination;

        [SetUp]
        public void Setup() {
            date = new DateTime(2020, 5, 5, 14, 0, 0);
            referenceDate = NighttimeCalculator.GetReferenceDate(date);
            latitude = 41.0;
            longitude = 70.3;
            nauticalTwilightRiseAndSet = AstroUtil.GetNauticalNightTimes(referenceDate, latitude, longitude);
            twilightRiseAndSet = AstroUtil.GetNightTimes(referenceDate, latitude, longitude);
            moonRiseAndSet = AstroUtil.GetMoonRiseAndSet(referenceDate, latitude, longitude);
            sunRiseAndSet = AstroUtil.GetSunRiseAndSet(referenceDate, latitude, longitude);
            moonPhase = MoonPhase.FullMoon;
            illumination = 100.0;
        }

        public NighttimeData GetData() {
            return new NighttimeData(date, referenceDate, moonPhase, illumination, twilightRiseAndSet, nauticalTwilightRiseAndSet, sunRiseAndSet, moonRiseAndSet);
        }

        public List<OxyPlot.DataPoint> GetExpectedTwilightDuration() {
            if (twilightRiseAndSet == null) {
                return new List<OxyPlot.DataPoint>();
            }
            var twilightSet = twilightRiseAndSet.Set.Value > twilightRiseAndSet.Rise.Value ? twilightRiseAndSet.Set.Value.AddDays(-1) : twilightRiseAndSet.Set.Value;
            if (sunRiseAndSet == null) {
                return new List<OxyPlot.DataPoint>() {
                    new OxyPlot.DataPoint(Axis.ToDouble(twilightSet), 90),
                    new OxyPlot.DataPoint(Axis.ToDouble(twilightRiseAndSet.Rise), 90) };
            }
            var sunRiseSet = sunRiseAndSet.Set.Value > sunRiseAndSet.Rise.Value ? sunRiseAndSet.Set.Value.AddDays(-1) : sunRiseAndSet.Set.Value;
            return new List<OxyPlot.DataPoint>() {
                new OxyPlot.DataPoint(Axis.ToDouble(twilightSet), 90),
                new OxyPlot.DataPoint(Axis.ToDouble(sunRiseSet), 90),
                new OxyPlot.DataPoint(Axis.ToDouble(sunRiseSet), 0),
                new OxyPlot.DataPoint(Axis.ToDouble(sunRiseAndSet.Rise), 0),
                new OxyPlot.DataPoint(Axis.ToDouble(sunRiseAndSet.Rise), 90),
                new OxyPlot.DataPoint(Axis.ToDouble(twilightRiseAndSet.Rise), 90) };
        }

        public List<OxyPlot.DataPoint> GetExpectedNightDuration() {
            if (twilightRiseAndSet == null) {
                return new List<OxyPlot.DataPoint>();
            }
            var twilightSet = twilightRiseAndSet.Set.Value > twilightRiseAndSet.Rise.Value ? twilightRiseAndSet.Set.Value.AddDays(-1) : twilightRiseAndSet.Set.Value;
            return new List<OxyPlot.DataPoint>() {
                new OxyPlot.DataPoint(Axis.ToDouble(twilightRiseAndSet.Rise), 90),
                new OxyPlot.DataPoint(Axis.ToDouble(twilightSet), 90) };
        }

        [Test]
        public void AllSetTest() {
            var data = GetData();
            Assert.AreEqual(moonPhase, data.MoonPhase);
            Assert.AreEqual(illumination, data.Illumination);
            Assert.AreEqual(date, data.Date);
            Assert.AreEqual(referenceDate, data.ReferenceDate);
            Assert.AreEqual(twilightRiseAndSet, data.TwilightRiseAndSet);
            Assert.AreEqual(moonRiseAndSet, data.MoonRiseAndSet);
            Assert.AreEqual(sunRiseAndSet, data.SunRiseAndSet);
            CollectionAssert.AreEqual(GetExpectedNightDuration(), data.NightDuration.ToImmutableList());
            CollectionAssert.AreEqual(GetExpectedTwilightDuration(), data.TwilightDuration.ToImmutableList());
        }

        [Test]
        public void MoonPhaseChangedTest() {
            moonPhase = MoonPhase.LastQuarter;
            var data = GetData();
            Assert.AreEqual(moonPhase, data.MoonPhase);
        }

        [Test]
        public void IlluminationChangedTest() {
            illumination = null;
            var data = GetData();
            Assert.IsFalse(data.Illumination.HasValue);
        }

        [Test]
        public void TwilightNotSetTest() {
            twilightRiseAndSet = null;
            var data = GetData();
            CollectionAssert.AreEqual(GetExpectedNightDuration(), data.NightDuration.ToImmutableList());
            CollectionAssert.AreEqual(GetExpectedTwilightDuration(), data.TwilightDuration.ToImmutableList());
        }

        [Test]
        public void SunriseNotSetTest() {
            sunRiseAndSet = null;
            var data = GetData();
            CollectionAssert.AreEqual(GetExpectedNightDuration(), data.NightDuration.ToImmutableList());
            CollectionAssert.AreEqual(GetExpectedTwilightDuration(), data.TwilightDuration.ToImmutableList());
        }

        [Test]
        public void TwilightAndSunriseNotSetTest() {
            twilightRiseAndSet = null;
            sunRiseAndSet = null;
            var data = GetData();
            CollectionAssert.AreEqual(GetExpectedNightDuration(), data.NightDuration.ToImmutableList());
            CollectionAssert.AreEqual(GetExpectedTwilightDuration(), data.TwilightDuration.ToImmutableList());
        }
    }
}