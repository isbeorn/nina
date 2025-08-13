#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using FluentAssertions;
using Newtonsoft.Json;
using NINA.Astrometry;
using NINA.Equipment.Model;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Test.AstrometryTest {

    [TestFixture]
    public class InputCoordinatesTest {

        [Test]
        [TestCase(1, 10, 20, 1, 10, 20, false)]
        [TestCase(1, 10, 20, 0, 10, 20, false)]
        [TestCase(1, 10, 20, -1, 10, 20, true)]
        [TestCase(1, 10, 20, -0, 10, 20, true)]
        [TestCase(1, 10, 20.0, 1, 10, 20.0, false)]
        [TestCase(1, 10, 20.7, 0, 10, 20.7, false)]
        [TestCase(1, 10, 20.989, -1, 10, 20.989, true)]
        [TestCase(1, 10, 20.32556, -0, 10, 20.32556, true)]
        [TestCase(5, 17, 28.0, 34, 25, 20.0, false)]
        [TestCase(1, 5, 0, 0, 0, 0, false)]
        [TestCase(0, 0, 0, -72, 5, 0, true)]
        public void SerializationAndDeserializationTest(int raHours, int raMinutes, double raSeconds, int decDegree, int decMinutes, double decSeconds, bool negativeDec) {

            var coordinates = new InputCoordinates {
                RAHours = raHours,
                RAMinutes = raMinutes,
                RASeconds = raSeconds,
                DecDegrees = decDegree,
                DecMinutes = decMinutes,
                DecSeconds = decSeconds,
                NegativeDec = negativeDec
            };

            var json = JsonConvert.SerializeObject(coordinates);

            var sut = JsonConvert.DeserializeObject<InputCoordinates>(json);

            sut.RAHours.Should().Be(raHours);
            sut.RAMinutes.Should().Be(raMinutes);
            sut.RASeconds.Should().Be(raSeconds);
            sut.DecDegrees.Should().Be(decDegree);
            sut.DecMinutes.Should().Be(decMinutes);
            sut.DecSeconds.Should().Be(decSeconds);
            sut.NegativeDec.Should().Be(negativeDec);

            if (negativeDec) {
                sut.Coordinates.Dec.Should().BeLessThan(0);
            } else {
                sut.Coordinates.Dec.Should().BeGreaterThanOrEqualTo(0);
            }

        }

        /* The tests below are regression tests over all possible RA and Dec combintations and will take a long time to compute
         * Uncomment them in case you need some changes to the coordinate logic and want to validate that all expected values are still matching
         */
        //[Test]
        //public void SerializationAndDeserialization_RightAscension_Test() {

        //    // Right Ascension Tests
        //    for (int raHours = 0; raHours < 24; raHours++) {
        //        for (int raMinutes = 0; raMinutes < 60; raMinutes++) {
        //            for (int raSeconds = 0; raSeconds < 60; raSeconds++) {


        //                var decDegree = 0;
        //                var decMinutes = 0;
        //                var decSeconds = 0;
        //                var negativeDec = false;

        //                var coordinates = new InputCoordinates();
        //                coordinates.RAHours = raHours;
        //                coordinates.RAMinutes = raMinutes;
        //                coordinates.RASeconds = raSeconds;
        //                coordinates.DecDegrees = decDegree;
        //                coordinates.DecMinutes = decMinutes;
        //                coordinates.DecSeconds = decSeconds;
        //                coordinates.NegativeDec = negativeDec;

        //                coordinates.RAHours.Should().Be(raHours, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                coordinates.RAMinutes.Should().Be(raMinutes, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                coordinates.RASeconds.Should().Be(raSeconds, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                coordinates.DecDegrees.Should().Be(decDegree, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                coordinates.DecMinutes.Should().Be(decMinutes, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                coordinates.DecSeconds.Should().Be(decSeconds, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                coordinates.NegativeDec.Should().Be(negativeDec, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");

        //                if (negativeDec) {
        //                    coordinates.Coordinates.Dec.Should().BeLessThan(0);
        //                } else {
        //                    coordinates.Coordinates.Dec.Should().BeGreaterThanOrEqualTo(0);
        //                }



        //                var json = JsonConvert.SerializeObject(coordinates);

        //                var sut = JsonConvert.DeserializeObject<InputCoordinates>(json);

        //                sut.RAHours.Should().Be(raHours, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                sut.RAMinutes.Should().Be(raMinutes, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                sut.RASeconds.Should().Be(raSeconds, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                sut.DecDegrees.Should().Be(decDegree, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                sut.DecMinutes.Should().Be(decMinutes, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                sut.DecSeconds.Should().Be(decSeconds, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                sut.NegativeDec.Should().Be(negativeDec, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");

        //                if (negativeDec) {
        //                    sut.Coordinates.Dec.Should().BeLessThan(0, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                } else {
        //                    sut.Coordinates.Dec.Should().BeGreaterThanOrEqualTo(0, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                }

        //            }
        //        }
        //    }
        //}

        //[Test]
        //public void SerializationAndDeserialization_NegativeDec_BetweenZeroAndMinusOne_Test() {


        //    // Special cases for negative declination between 0 and -1
        //    for (int decMinutes = 0; decMinutes < 60; decMinutes++) {
        //        for (int decSeconds = 0; decSeconds < 60; decSeconds++) {


        //            var raHours = 0;
        //            var raMinutes = 0;
        //            var raSeconds = 0;
        //            var negativeDec = true;

        //            var coordinates = new InputCoordinates();
        //            coordinates.RAHours = raHours;
        //            coordinates.RAMinutes = raMinutes;
        //            coordinates.RASeconds = raSeconds;
        //            coordinates.DecDegrees = 0;
        //            coordinates.NegativeDec = negativeDec;
        //            coordinates.DecMinutes = decMinutes;
        //            coordinates.DecSeconds = decSeconds;

        //            coordinates.RAHours.Should().Be(raHours, $"{raHours}:{raMinutes}:{raSeconds} | {0}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //            coordinates.RAMinutes.Should().Be(raMinutes, $"{raHours}:{raMinutes}:{raSeconds} | {0}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //            coordinates.RASeconds.Should().Be(raSeconds, $"{raHours}:{raMinutes}:{raSeconds} | {0}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //            coordinates.DecDegrees.Should().Be(0, $"{raHours}:{raMinutes}:{raSeconds} | {0}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //            coordinates.DecMinutes.Should().Be(decMinutes, $"{raHours}:{raMinutes}:{raSeconds} | {0}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //            coordinates.DecSeconds.Should().Be(decSeconds, $"{raHours}:{raMinutes}:{raSeconds} | {0}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //            coordinates.NegativeDec.Should().Be(negativeDec, $"{raHours}:{raMinutes}:{raSeconds} | {0}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");

        //            if (negativeDec && (decMinutes > 0 || decSeconds > 0)) {
        //                coordinates.Coordinates.Dec.Should().BeLessThan(0, $"{raHours}:{raMinutes}:{raSeconds} | {0}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //            } else {
        //                coordinates.Coordinates.Dec.Should().BeGreaterThanOrEqualTo(0, $"{raHours}:{raMinutes}:{raSeconds} | {0}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //            }



        //            var json = JsonConvert.SerializeObject(coordinates);

        //            var sut = JsonConvert.DeserializeObject<InputCoordinates>(json);

        //            sut.RAHours.Should().Be(raHours, $"{raHours}:{raMinutes}:{raSeconds} | {0}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //            sut.RAMinutes.Should().Be(raMinutes, $"{raHours}:{raMinutes}:{raSeconds} | {0}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //            sut.RASeconds.Should().Be(raSeconds, $"{raHours}:{raMinutes}:{raSeconds} | {0}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //            sut.DecDegrees.Should().Be(0, $"{raHours}:{raMinutes}:{raSeconds} | {0}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //            sut.DecMinutes.Should().Be(decMinutes, $"{raHours}:{raMinutes}:{raSeconds} | {0}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //            sut.DecSeconds.Should().Be(decSeconds, $"{raHours}:{raMinutes}:{raSeconds} | {0}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //            sut.NegativeDec.Should().Be(negativeDec, $"{raHours}:{raMinutes}:{raSeconds} | {0}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");

        //            if (negativeDec && (decMinutes > 0 || decSeconds > 0)) {
        //                sut.Coordinates.Dec.Should().BeLessThan(0, $"{raHours}:{raMinutes}:{raSeconds} | {0}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //            } else {
        //                sut.Coordinates.Dec.Should().BeGreaterThanOrEqualTo(0, $"{raHours}:{raMinutes}:{raSeconds} | {0}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //            }

        //        }

        //    }

        //}

        //[Test]
        //public void SerializationAndDeserialization_Declination_Test() {

        //    for (int decDegree = -89; decDegree < 90; decDegree++) {
        //        for (int decMinutes = 0; decMinutes < 60; decMinutes++) {
        //            for (int decSeconds = 0; decSeconds < 60; decSeconds++) {


        //                var raHours = 0;
        //                var raMinutes = 0;
        //                var raSeconds = 0;
        //                var negativeDec = decDegree < 0;

        //                var coordinates = new InputCoordinates();
        //                coordinates.RAHours = raHours;
        //                coordinates.RAMinutes = raMinutes;
        //                coordinates.RASeconds = raSeconds;
        //                coordinates.DecDegrees = decDegree;
        //                coordinates.DecMinutes = decMinutes;
        //                coordinates.DecSeconds = decSeconds;
        //                coordinates.NegativeDec = negativeDec;

        //                coordinates.RAHours.Should().Be(raHours, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                coordinates.RAMinutes.Should().Be(raMinutes, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                coordinates.RASeconds.Should().Be(raSeconds, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                coordinates.DecDegrees.Should().Be(decDegree, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                coordinates.DecMinutes.Should().Be(decMinutes, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                coordinates.DecSeconds.Should().Be(decSeconds, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                coordinates.NegativeDec.Should().Be(negativeDec, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");

        //                if (negativeDec) {
        //                    coordinates.Coordinates.Dec.Should().BeLessThan(0, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                } else {
        //                    coordinates.Coordinates.Dec.Should().BeGreaterThanOrEqualTo(0, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                }



        //                var json = JsonConvert.SerializeObject(coordinates);

        //                var sut = JsonConvert.DeserializeObject<InputCoordinates>(json);

        //                sut.RAHours.Should().Be(raHours, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                sut.RAMinutes.Should().Be(raMinutes, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                sut.RASeconds.Should().Be(raSeconds, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                sut.DecDegrees.Should().Be(decDegree, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                sut.DecMinutes.Should().Be(decMinutes, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                sut.DecSeconds.Should().Be(decSeconds, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                sut.NegativeDec.Should().Be(negativeDec, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");

        //                if (negativeDec) {
        //                    sut.Coordinates.Dec.Should().BeLessThan(0, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                } else {
        //                    sut.Coordinates.Dec.Should().BeGreaterThanOrEqualTo(0, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                }

        //            }
        //        }
        //    }
        //}

        //[Test]
        //public void CaptureSequenceList_SerializationAndDeserialization_RightAscension_Test() {

        //    // Right Ascension Tests
        //    for (int raHours = 0; raHours < 24; raHours++) {
        //        for (int raMinutes = 0; raMinutes < 60; raMinutes++) {
        //            for (int raSeconds = 0; raSeconds < 60; raSeconds++) {


        //                var decDegree = 0;
        //                var decMinutes = 0;
        //                var decSeconds = 0;
        //                var negativeDec = false;

        //                var coordinates = new CaptureSequenceList();
        //                coordinates.RAHours = raHours;
        //                coordinates.RAMinutes = raMinutes;
        //                coordinates.RASeconds = raSeconds;
        //                coordinates.DecDegrees = decDegree;
        //                coordinates.DecMinutes = decMinutes;
        //                coordinates.DecSeconds = decSeconds;
        //                coordinates.NegativeDec = negativeDec;

        //                coordinates.RAHours.Should().Be(raHours, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                coordinates.RAMinutes.Should().Be(raMinutes, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                coordinates.RASeconds.Should().Be(raSeconds, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                coordinates.DecDegrees.Should().Be(decDegree, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                coordinates.DecMinutes.Should().Be(decMinutes, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                coordinates.DecSeconds.Should().Be(decSeconds, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                coordinates.NegativeDec.Should().Be(negativeDec, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");

        //                if (negativeDec) {
        //                    coordinates.Coordinates.Dec.Should().BeLessThan(0, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                } else {
        //                    coordinates.Coordinates.Dec.Should().BeGreaterThanOrEqualTo(0, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                }

        //            }
        //        }
        //    }




        //}

        //[Test]
        //public void CaptureSequenceList_SerializationAndDeserialization_Declination_Test() {

        //    for (int decDegree = -89; decDegree < 90; decDegree++) {
        //        for (int decMinutes = 0; decMinutes < 60; decMinutes++) {
        //            for (int decSeconds = 0; decSeconds < 60; decSeconds++) {


        //                var raHours = 0;
        //                var raMinutes = 0;
        //                var raSeconds = 0;
        //                var negativeDec = decDegree < 0;

        //                var coordinates = new CaptureSequenceList();
        //                coordinates.RAHours = raHours;
        //                coordinates.RAMinutes = raMinutes;
        //                coordinates.RASeconds = raSeconds;
        //                coordinates.DecDegrees = decDegree;
        //                coordinates.DecMinutes = decMinutes;
        //                coordinates.DecSeconds = decSeconds;
        //                coordinates.NegativeDec = negativeDec;

        //                coordinates.RAHours.Should().Be(raHours, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s - DecNegative: {negativeDec}");
        //                coordinates.RAMinutes.Should().Be(raMinutes, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s - DecNegative: {negativeDec}");
        //                coordinates.RASeconds.Should().Be(raSeconds, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s - DecNegative: {negativeDec}");
        //                coordinates.DecDegrees.Should().Be(decDegree, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s - DecNegative: {negativeDec}");
        //                coordinates.DecMinutes.Should().Be(decMinutes, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s - DecNegative: {negativeDec}");
        //                coordinates.DecSeconds.Should().Be(decSeconds, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s - DecNegative: {negativeDec}");
        //                coordinates.NegativeDec.Should().Be(negativeDec, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s - DecNegative: {negativeDec}");

        //                if (negativeDec) {
        //                    coordinates.Coordinates.Dec.Should().BeLessThan(0, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                } else {
        //                    coordinates.Coordinates.Dec.Should().BeGreaterThanOrEqualTo(0, $"{raHours}:{raMinutes}:{raSeconds} | {decDegree}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //                }
        //            }
        //        }
        //    }



        //}

        //[Test]
        //public void CaptureSequenceList_SerializationAndDeserialization_NegativeDec_BetweenZeroAndMinusOne_Test() {


        //    // Special cases for negative declination between 0 and -1
        //    for (int decMinutes = 0; decMinutes < 60; decMinutes++) {
        //        for (int decSeconds = 0; decSeconds < 60; decSeconds++) {


        //            var raHours = 0;
        //            var raMinutes = 0;
        //            var raSeconds = 0;
        //            var negativeDec = true;

        //            var coordinates = new CaptureSequenceList();
        //            coordinates.RAHours = raHours;
        //            coordinates.RAMinutes = raMinutes;
        //            coordinates.RASeconds = raSeconds;
        //            coordinates.DecDegrees = 0;
        //            coordinates.NegativeDec = negativeDec;
        //            coordinates.DecMinutes = decMinutes;
        //            coordinates.DecSeconds = decSeconds;

        //            coordinates.RAHours.Should().Be(raHours, $"{raHours}:{raMinutes}:{raSeconds} | {0}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //            coordinates.RAMinutes.Should().Be(raMinutes, $"{raHours}:{raMinutes}:{raSeconds} | {0}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //            coordinates.RASeconds.Should().Be(raSeconds, $"{raHours}:{raMinutes}:{raSeconds} | {0}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //            coordinates.DecDegrees.Should().Be(0, $"{raHours}:{raMinutes}:{raSeconds} | {0}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //            coordinates.DecMinutes.Should().Be(decMinutes, $"{raHours}:{raMinutes}:{raSeconds} | {0}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //            coordinates.DecSeconds.Should().Be(decSeconds, $"{raHours}:{raMinutes}:{raSeconds} | {0}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //            coordinates.NegativeDec.Should().Be(negativeDec, $"{raHours}:{raMinutes}:{raSeconds} | {0}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");

        //            if (negativeDec && (decMinutes > 0 || decSeconds > 0)) {
        //                coordinates.Coordinates.Dec.Should().BeLessThan(0, $"{raHours}:{raMinutes}:{raSeconds} | {0}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //            } else {
        //                coordinates.Coordinates.Dec.Should().BeGreaterThanOrEqualTo(0, $"{raHours}:{raMinutes}:{raSeconds} | {0}d{decMinutes}m{decSeconds}s | DecNegative: {negativeDec}");
        //            }
        //        }

        //    }

        //}

        //[Test]
        //public void SerializationAndDeserialization_Altitude_Test() {

        //    // Right Ascension Tests
        //    for (int altDegree = 0; altDegree < 90; altDegree++) {
        //        for (int altMinutes = 0; altMinutes < 60; altMinutes++) {
        //            for (int altSeconds = 0; altSeconds < 60; altSeconds++) {

        //                var azDegree = 0;
        //                var azMinutes = 0;
        //                var azSeconds = 0;
        //                var negativeAlt = false;

        //                var coordinates = new InputTopocentricCoordinates(Angle.ByDegree(0), Angle.ByDegree(0));
        //                coordinates.AzDegrees = azDegree;
        //                coordinates.AzMinutes = azMinutes;
        //                coordinates.AzSeconds = azSeconds;
        //                coordinates.AltDegrees = altDegree;
        //                coordinates.AltMinutes = altMinutes;
        //                coordinates.AltSeconds = altSeconds;
        //                coordinates.NegativeAlt = negativeAlt;

        //                coordinates.AzDegrees.Should().Be(azDegree, $"{azDegree}:{azMinutes}:{azSeconds} | {altDegree}d{altMinutes}m{altSeconds}s | AltNegative: {negativeAlt}");
        //                coordinates.AzMinutes.Should().Be(azMinutes, $"{azDegree}:{azMinutes}:{azSeconds} | {altDegree}d{altMinutes}m{altSeconds}s | AltNegative: {negativeAlt}");
        //                coordinates.AzSeconds.Should().Be(azSeconds, $"{azDegree}:{azMinutes}:{azSeconds} | {altDegree}d{altMinutes}m{altSeconds}s | AltNegative: {negativeAlt}");
        //                coordinates.AltDegrees.Should().Be(altDegree, $"{azDegree}:{azMinutes}:{azSeconds} | {altDegree}d{altMinutes}m{altSeconds}s | AltNegative: {negativeAlt}");
        //                coordinates.AltMinutes.Should().Be(altMinutes, $"{azDegree}:{azMinutes}:{azSeconds} | {altDegree}d{altMinutes}m{altSeconds}s | AltNegative: {negativeAlt}");
        //                coordinates.AltSeconds.Should().Be(altSeconds, $"{azDegree}:{azMinutes}:{azSeconds} | {altDegree}d{altMinutes}m{altSeconds}s | AltNegative: {negativeAlt}");
        //                coordinates.NegativeAlt.Should().Be(negativeAlt, $"{azDegree}:{azMinutes}:{azSeconds} | {altDegree}d{altMinutes}m{altSeconds}s | AltNegative: {negativeAlt}");

        //                if (negativeAlt) {
        //                    coordinates.Coordinates.Altitude.Degree.Should().BeLessThan(0);
        //                } else {
        //                    coordinates.Coordinates.Altitude.Degree.Should().BeGreaterThanOrEqualTo(0);
        //                }



        //                var json = JsonConvert.SerializeObject(coordinates);

        //                var sut = JsonConvert.DeserializeObject<InputTopocentricCoordinates>(json);

        //                sut.AzDegrees.Should().Be(azDegree, $"{azDegree}:{azMinutes}:{azSeconds} | {altDegree}d{altMinutes}m{altSeconds}s | AltNegative: {negativeAlt}");
        //                sut.AzMinutes.Should().Be(azMinutes, $"{azDegree}:{azMinutes}:{azSeconds} | {altDegree}d{altMinutes}m{altSeconds}s | AltNegative: {negativeAlt}");
        //                sut.AzSeconds.Should().Be(azSeconds, $"{azDegree}:{azMinutes}:{azSeconds} | {altDegree}d{altMinutes}m{altSeconds}s | AltNegative: {negativeAlt}");
        //                sut.AltDegrees.Should().Be(altDegree, $"{azDegree}:{azMinutes}:{azSeconds} | {altDegree}d{altMinutes}m{altSeconds}s | AltNegative: {negativeAlt}");
        //                sut.AltMinutes.Should().Be(altMinutes, $"{azDegree}:{azMinutes}:{azSeconds} | {altDegree}d{altMinutes}m{altSeconds}s | AltNegative: {negativeAlt}");
        //                sut.AltSeconds.Should().Be(altSeconds, $"{azDegree}:{azMinutes}:{azSeconds} | {altDegree}d{altMinutes}m{altSeconds}s | AltNegative: {negativeAlt}");
        //                sut.NegativeAlt.Should().Be(negativeAlt, $"{azDegree}:{azMinutes}:{azSeconds} | {altDegree}d{altMinutes}m{altSeconds}s | AltNegative: {negativeAlt}");

        //                if (negativeAlt) {
        //                    sut.Coordinates.Altitude.Degree.Should().BeLessThan(0, $"{azDegree}:{azMinutes}:{azSeconds} | {altDegree}d{altMinutes}m{altSeconds}s | AltNegative: {negativeAlt}");
        //                } else {
        //                    sut.Coordinates.Altitude.Degree.Should().BeGreaterThanOrEqualTo(0, $"{azDegree}:{azMinutes}:{azSeconds} | {altDegree}d{altMinutes}m{altSeconds}s | AltNegative: {negativeAlt}");
        //                }

        //            }
        //        }
        //    }
        //}

    }
}
