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
using NINA.Core.Utility.Converters;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Test.Converters {

    [TestFixture]
    [SetCulture("en-GB")]
    public class UnitConverterTest {

        [Test]
        [TestCase(0, null, "0")]
        [TestCase(-11110, null, "-11110")]
        [TestCase(11110, null, "11110")]
        [TestCase(0, "", "0")]
        [TestCase(-11110, "", "-11110")]
        [TestCase(11110, "", "11110")]
        [TestCase(0, "°", "0°")]
        [TestCase(0, "somePostfix", "0somePostfix")]
        [TestCase(-11110, "somePostfix", "-11110somePostfix")]
        [TestCase(11110, "somePostfix", "11110somePostfix")]
        [TestCase(-11110, "somePostfix|2", "-11110somePostfix")]
        [TestCase(11110, "somePostfix|4", "11110somePostfix")]
        public void Convert_Integer(int value, object parameter, string expected) {
            var sut = new UnitConverter();

            var output = sut.Convert(value, typeof(string), parameter, CultureInfo.InvariantCulture);

            output.Should().Be(expected);
        }

        [Test]
        [TestCase(0d, null, "0")]
        [TestCase(-11110.11d, null, "-11110.11")]
        [TestCase(11110.111111d, null, "11110.111111")]
        [TestCase(0d, "", "0")]
        [TestCase(-11110.11d, "", "-11110.11")]
        [TestCase(11110.111111d, "", "11110.111111")]
        [TestCase(0d, "°", "0°")]
        [TestCase(0d, "somePostfix", "0somePostfix")]
        [TestCase(-11110d, "somePostfix", "-11110somePostfix")]
        [TestCase(11110d, "somePostfix", "11110somePostfix")]
        [TestCase(-11110d, "somePostfix|4", "-11110.0000somePostfix")]
        [TestCase(11110d, "somePostfix|2", "11110.00somePostfix")]
        [TestCase(-11110.23235234d, "somePostfix", "-11110.23235234somePostfix")]
        [TestCase(11110.345345364d, "somePostfix", "11110.345345364somePostfix")]
        [TestCase(-11110.23235234d, "somePostfix|2", "-11110.23somePostfix")]
        [TestCase(11110.345345364d, "somePostfix|4", "11110.3453somePostfix")]
        [TestCase(double.NaN, null, "--")]
        [TestCase(double.NaN, "somePostfix", "--")]
        [TestCase(double.NaN, "somePostfix|4", "--")]
        public void Convert_Double(double value, object parameter, string expected) {
            var sut = new UnitConverter();

            var output = sut.Convert(value, typeof(string), parameter, CultureInfo.InvariantCulture);

            output.Should().Be(expected);
        }

        [Test]
        [TestCase("0", null, "0")]
        [TestCase("-11110.11", null, "-11110.11")]
        [TestCase("11110.111111", null, "11110.111111")]
        [TestCase("0", "", "0")]
        [TestCase("-11110.11", "", "-11110.11")]
        [TestCase("11110.111111", "", "11110.111111")]
        [TestCase("0", "°", "0°")]
        [TestCase("0", "somePostfix", "0somePostfix")]
        [TestCase("-11110", "somePostfix", "-11110somePostfix")]
        [TestCase("11110", "somePostfix", "11110somePostfix")]
        [TestCase("-11110", "somePostfix|4", "-11110.0000somePostfix")]
        [TestCase("11110", "somePostfix|2", "11110.00somePostfix")]
        [TestCase("-11110.23235234", "somePostfix", "-11110.23235234somePostfix")]
        [TestCase("11110.345345364", "somePostfix", "11110.345345364somePostfix")]
        [TestCase("-11110.23235234", "somePostfix|2", "-11110.23somePostfix")]
        [TestCase("11110.345345364", "somePostfix|4", "11110.3453somePostfix")]
        public void Convert_Decimal(decimal value, object parameter, string expected) {
            var sut = new UnitConverter();

            var output = sut.Convert(value, typeof(string), parameter, CultureInfo.InvariantCulture);

            output.Should().Be(expected);
        }

        [Test]
        [TestCase("", typeof(int), null, 0)]
        [TestCase("0", typeof(int), null, 0)]
        [TestCase("0", typeof(int), "", 0)]
        [TestCase("0", typeof(int), "°", 0)]
        [TestCase("0°", typeof(int), "°", 0)]
        [TestCase("0", typeof(int), "somePostfix", 0)]
        [TestCase("0somePostfix", typeof(int), "somePostfix", 0)]
        [TestCase("-11110somePostfix", typeof(int), "somePostfix", -11110)]
        [TestCase("11110somePostfix", typeof(int), "somePostfix", 11110)]
        [TestCase("-11110.0000somePostfix", typeof(int), "somePostfix|4", -11110)]
        [TestCase("11110.00somePostfix", typeof(int), "somePostfix|2", 11110)]
        public void ConvertBack_Integer(string value, Type targetType, object parameter, int expected) {
            var sut = new UnitConverter();

            var output = sut.ConvertBack(value, targetType, parameter, CultureInfo.InvariantCulture);

            output.Should().Be(expected);
        }

        [Test]
        [TestCase("", typeof(short), null, 0)]
        [TestCase("0", typeof(short), null, 0)]
        [TestCase("0", typeof(int), "", 0)]
        [TestCase("0", typeof(short), "°", 0)]
        [TestCase("0°", typeof(short), "°", 0)]
        [TestCase("0", typeof(short), "somePostfix", 0)]
        [TestCase("0somePostfix", typeof(short), "somePostfix", 0)]
        [TestCase("-11110somePostfix", typeof(short), "somePostfix", -11110)]
        [TestCase("11110somePostfix", typeof(short), "somePostfix", 11110)]
        [TestCase("-11110.0000somePostfix", typeof(short), "somePostfix|4", -11110)]
        [TestCase("11110.00somePostfix", typeof(short), "somePostfix|2", 11110)]
        public void ConvertBack_Short(string value, Type targetType, object parameter, short expected) {
            var sut = new UnitConverter();

            var output = sut.ConvertBack(value, targetType, parameter, CultureInfo.InvariantCulture);

            output.Should().Be(expected);
        }

        [Test]
        [TestCase("", typeof(double), null, 0)]
        [TestCase("0", typeof(double), null, 0)]
        [TestCase("0", typeof(double), "", 0)]
        [TestCase("0", typeof(double), "°", 0)]
        [TestCase("0°", typeof(double), "°", 0)]
        [TestCase("0", typeof(double), "somePostfix", 0)]
        [TestCase("0somePostfix", typeof(double), "somePostfix", 0)]
        [TestCase("-11110somePostfix", typeof(double), "somePostfix", -11110)]
        [TestCase("11110somePostfix", typeof(double), "somePostfix", 11110)]
        [TestCase("-11110.0000somePostfix", typeof(double), "somePostfix|4", -11110)]
        [TestCase("11110.00somePostfix", typeof(double), "somePostfix|2", 11110)]
        [TestCase("-11110.12345somePostfix", typeof(double), "somePostfix|4", -11110.1234)]
        [TestCase("-11110.12349somePostfix", typeof(double), "somePostfix|4", -11110.1235)]
        [TestCase("11110.12345somePostfix", typeof(double), "somePostfix|2", 11110.12)]
        [TestCase("--", typeof(double), "somePostfix|2", double.NaN)]
        public void ConvertBack_Double(string value, Type targetType, object parameter, double expected) {
            var sut = new UnitConverter();

            var output = sut.ConvertBack(value, targetType, parameter, CultureInfo.InvariantCulture);

            output.Should().Be(expected);
        }

        [Test]
        [TestCase("", typeof(double), null, "0")]
        [TestCase("0", typeof(decimal), null, "0")]
        [TestCase("0", typeof(decimal), "", "0")]
        [TestCase("0", typeof(decimal), "°", "0")]
        [TestCase("0°", typeof(decimal), "°", "0")]
        [TestCase("0", typeof(decimal), "somePostfix", "0")]
        [TestCase("0somePostfix", typeof(decimal), "somePostfix", "0")]
        [TestCase("-11110somePostfix", typeof(decimal), "somePostfix", "-11110")]
        [TestCase("11110somePostfix", typeof(decimal), "somePostfix", "11110")]
        [TestCase("-11110.0000somePostfix", typeof(decimal), "somePostfix|4", "-11110")]
        [TestCase("11110.00somePostfix", typeof(decimal), "somePostfix|2", "11110")]
        [TestCase("-11110.12345somePostfix", typeof(decimal), "somePostfix|4", "-11110.1234")]
        [TestCase("-11110.12349somePostfix", typeof(decimal), "somePostfix|4", "-11110.1235")]
        [TestCase("11110.12345somePostfix", typeof(decimal), "somePostfix|2", "11110.12")]
        public void ConvertBack_Decimal(string value, Type targetType, object parameter, decimal expected) {
            var sut = new UnitConverter();

            var output = sut.ConvertBack(value, targetType, parameter, CultureInfo.InvariantCulture);

            output.Should().Be(expected);
        }
    }
}