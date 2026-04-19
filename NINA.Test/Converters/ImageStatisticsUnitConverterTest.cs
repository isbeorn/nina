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
using NINA.Astrometry.Converters;
using NINA.Core.Enum;
using NINA.Profile;
using NUnit.Framework;
using System.Globalization;

namespace NINA.Test.Converters {

    [TestFixture]
    public class ImageStatisticsUnitConverterTest {

        [Test]
        public void Convert_ReturnsPixelsByDefault() {
            var sut = new ImageStatisticsUnitConverter();

            var output = sut.Convert(new object[] { 2.5d, false, 3.76d, 952d }, typeof(string), null, CultureInfo.InvariantCulture);

            output.Should().Be("2.50 px");
        }

        [Test]
        public void Convert_ReturnsArcsecondsWhenEnabled() {
            var sut = new ImageStatisticsUnitConverter();
            var expected = (2.5d * AstroUtil.ArcsecPerPixel(3.76d, 952d)).ToString("0.00", CultureInfo.InvariantCulture) + "\"";

            var output = sut.Convert(new object[] { 2.5d, true, 3.76d, 952d }, typeof(string), null, CultureInfo.InvariantCulture);

            output.Should().Be(expected);
        }

        [Test]
        public void Convert_ReturnsArcsecondsWithoutPixelScaleWhenSourceIsArcseconds() {
            var sut = new ImageStatisticsUnitConverter();

            var output = sut.Convert(new object[] { 2.5d, true, double.NaN, 952d, StarMeasurementUnit.Arcseconds }, typeof(string), null, CultureInfo.InvariantCulture);

            output.Should().Be("2.50\"");
        }

        [Test]
        public void Convert_ReturnsPixelsWhenSourceIsArcseconds() {
            var sut = new ImageStatisticsUnitConverter();
            var arcsecPerPixel = AstroUtil.ArcsecPerPixel(3.76d, 952d);
            var expected = (2.5d / arcsecPerPixel).ToString("0.00", CultureInfo.InvariantCulture) + " px";

            var output = sut.Convert(new object[] { 2.5d, false, 3.76d, 952d, StarMeasurementUnit.Arcseconds }, typeof(string), null, CultureInfo.InvariantCulture);

            output.Should().Be(expected);
        }

        [Test]
        public void Convert_UsesUnitBindingBeforeFallbackParameter() {
            var sut = new ImageStatisticsUnitConverter();
            var expected = (2.5d * AstroUtil.ArcsecPerPixel(3.76d, 952d)).ToString("0.00", CultureInfo.InvariantCulture) + "\"";

            var output = sut.Convert(new object[] { 2.5d, true, 3.76d, 952d, StarMeasurementUnit.Pixels }, typeof(string), StarMeasurementUnit.Arcseconds, CultureInfo.InvariantCulture);

            output.Should().Be(expected);
        }

        [Test]
        public void Convert_UsesFallbackUnitWhenUnitBindingIsMissing() {
            var sut = new ImageStatisticsUnitConverter();

            var output = sut.Convert(new object[] { 2.5d, true, double.NaN, 952d }, typeof(string), StarMeasurementUnit.Arcseconds, CultureInfo.InvariantCulture);

            output.Should().Be("2.50\"");
        }

        [Test]
        public void Convert_ReturnsDoubleDashWhenArcsecondsCannotBeCalculated() {
            var sut = new ImageStatisticsUnitConverter();

            var output = sut.Convert(new object[] { 2.5d, true, double.NaN, 952d }, typeof(string), null, CultureInfo.InvariantCulture);

            output.Should().Be("--");
        }

        [Test]
        public void Convert_ReturnsDoubleDashWhenMeasurementIsNaN() {
            var sut = new ImageStatisticsUnitConverter();

            var output = sut.Convert(new object[] { double.NaN, false, 3.76d, 952d }, typeof(string), null, CultureInfo.InvariantCulture);

            output.Should().Be("--");
        }

        [Test]
        public void DockPanelSettings_DefaultsToPixels() {
            var sut = new DockPanelSettings();

            sut.StarMeasurementsInArcseconds.Should().BeFalse();
        }
    }
}
