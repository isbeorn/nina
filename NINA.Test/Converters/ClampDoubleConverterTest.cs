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
using NINA.Core.Utility.Converters;
using NUnit.Framework;
using System.Globalization;

namespace NINA.Test.Converters {

    [TestFixture]
    public class ClampDoubleConverterTest {

        [Test]
        public void Convert_ValueWithinRange_ReturnsOriginalValue() {
            // Arrange
            var sut = new ClampDoubleConverter();
            double input = 50.0;
            string parameter = "0|100";

            // Act
            var result = sut.Convert(input, typeof(double), parameter, CultureInfo.InvariantCulture);

            // Assert
            result.Should().Be(50.0);
        }

        [Test]
        public void Convert_ValueBelowMinimum_ReturnsMinimum() {
            // Arrange
            var sut = new ClampDoubleConverter();
            double input = -5.0;
            string parameter = "10|90";

            // Act
            var result = sut.Convert(input, typeof(double), parameter, CultureInfo.InvariantCulture);

            // Assert
            result.Should().Be(10.0);
        }

        [Test]
        public void Convert_ValueAboveMaximum_ReturnsMaximum() {
            // Arrange
            var sut = new ClampDoubleConverter();
            double input = 95.0;
            string parameter = "10|90";

            // Act
            var result = sut.Convert(input, typeof(double), parameter, CultureInfo.InvariantCulture);

            // Assert
            result.Should().Be(90.0);
        }

        [Test]
        public void Convert_ValueAtMinimum_ReturnsMinimum() {
            // Arrange
            var sut = new ClampDoubleConverter();
            double input = 10.0;
            string parameter = "10|90";

            // Act
            var result = sut.Convert(input, typeof(double), parameter, CultureInfo.InvariantCulture);

            // Assert
            result.Should().Be(10.0);
        }

        [Test]
        public void Convert_ValueAtMaximum_ReturnsMaximum() {
            // Arrange
            var sut = new ClampDoubleConverter();
            double input = 90.0;
            string parameter = "10|90";

            // Act
            var result = sut.Convert(input, typeof(double), parameter, CultureInfo.InvariantCulture);

            // Assert
            result.Should().Be(90.0);
        }

        [Test]
        public void Convert_NegativeRange_WorksCorrectly() {
            // Arrange
            var sut = new ClampDoubleConverter();
            double input = -50.0;
            string parameter = "-100|0";

            // Act
            var result = sut.Convert(input, typeof(double), parameter, CultureInfo.InvariantCulture);

            // Assert
            result.Should().Be(-50.0);
        }

        [Test]
        public void Convert_InvalidParameter_ReturnsOriginalValue() {
            // Arrange
            var sut = new ClampDoubleConverter();
            double input = 50.0;
            string parameter = "invalid";

            // Act
            var result = sut.Convert(input, typeof(double), parameter, CultureInfo.InvariantCulture);

            // Assert
            result.Should().Be(50.0);
        }

        [Test]
        public void Convert_NonDoubleValue_ReturnsOriginalValue() {
            // Arrange
            var sut = new ClampDoubleConverter();
            string input = "not a double";
            string parameter = "10|90";

            // Act
            var result = sut.Convert(input, typeof(double), parameter, CultureInfo.InvariantCulture);

            // Assert
            result.Should().Be(input);
        }
    }
}
