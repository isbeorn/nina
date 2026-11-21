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
using NINA.Core.Model;
using NUnit.Framework;
using System;
using System.IO;

namespace NINA.Test.AstrometryTest {

    [TestFixture]
    public class CustomHorizonUpperColumnTest {

        private static CustomHorizon Parse(string content) => CustomHorizon.FromReader_Standard(new StringReader(content));

        [Test]
        public void TwoColumnFile_HasNoEmbeddedUpper() {
            var sut = Parse("0 10\n360 10");

            sut.EmbeddedUpper.Should().BeNull();
            sut.HasLowerData.Should().BeTrue();
            sut.GetAltitude(90).Should().Be(10);
        }

        [Test]
        public void ThreeColumnFile_ReadsLowerAndUpper() {
            var sut = Parse("0 10 80\n360 10 80");

            sut.GetAltitude(180).Should().Be(10);
            sut.EmbeddedUpper.Should().NotBeNull();
            sut.EmbeddedUpper.GetAltitude(180).Should().Be(80);
        }

        [Test]
        public void ColumnsAreInterpolatedIndependently_FromTheLinesThatHaveAValue() {
            // lower is only defined at 0 and 200, upper only at 140 and 160
            var sut = Parse("0 10 -\n140 - 60\n160 - 40\n200 30 -");

            sut.GetAltitude(100).Should().BeApproximately(10 + (30 - 10) * 100d / 200d, 0.001);
            sut.EmbeddedUpper.GetAltitude(150).Should().BeApproximately(50, 0.001);
            sut.EmbeddedUpper.GetAltitude(160).Should().BeApproximately(40, 0.001);
        }

        [Test]
        public void LineWithBothValues_FeedsBothColumns() {
            var sut = Parse("0 5 -\n100 20 70\n200 30 50\n360 5 -");

            sut.GetAltitude(100).Should().Be(20);
            sut.EmbeddedUpper.GetAltitude(100).Should().Be(70);
            sut.EmbeddedUpper.GetAltitude(200).Should().Be(50);
        }

        [Test]
        public void FileWithOnlyUpperValues_HasNoLowerData() {
            var sut = Parse("0 - 60\n360 - 60");

            sut.HasLowerData.Should().BeFalse();
            sut.GetAltitude(123).Should().Be(0);
            sut.GetMinAltitude().Should().Be(0);
            sut.GetMaxAltitude().Should().Be(0);
            sut.EmbeddedUpper.GetAltitude(10).Should().Be(60);
        }

        [Test]
        public void LineWithoutAnyValue_IsIgnored() {
            var sut = Parse("0 10\n90 - -\n360 10");

            sut.GetAltitude(90).Should().Be(10);
            sut.EmbeddedUpper.Should().BeNull();
        }

        [Test]
        public void NegativeLowerAltitude_IsNotConfusedWithNoValue() {
            var sut = Parse("0 -5 40\n360 -5 40");

            sut.GetAltitude(10).Should().Be(-5);
            sut.EmbeddedUpper.GetAltitude(10).Should().Be(40);
        }

        [Test]
        public void UpperValuesWrapAroundBetween359And0() {
            var sut = Parse("0 5 -\n10 - 30\n350 - 70\n360 5 -");

            // no upper value at 0/360: the nearest points are used for the groomed edges, so the wrap-around has no gap
            sut.EmbeddedUpper.GetAltitude(0).Should().Be(sut.EmbeddedUpper.GetAltitude(360));
        }

        [Test]
        public void SingleUpperValue_IsRejectedLikeASingleLowerValue() {
            Action act = () => Parse("0 10\n90 20\n360 10\n180 - 50");

            act.Should().Throw<ArgumentException>();
        }

    }
}
