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
using System.Globalization;

namespace NINA.Test.Converters {

    [TestFixture]
    internal class IntNegativeOneToDoubleDashConverterTest {
        private IntNegativeOneToDoubleDashConverter sut;

        [SetUp]
        public void Init() {
            sut = new IntNegativeOneToDoubleDashConverter();
        }

        [Test]
        public void TestConvert() {
            sut.Convert(-1, null, null, CultureInfo.CurrentCulture).Should().Be("--");
            sut.Convert(-2, null, null, CultureInfo.CurrentCulture).Should().Be(-2);
            sut.Convert("anything", null, null, CultureInfo.CurrentCulture).Should().Be("anything");
            sut.Convert(null, null, null, CultureInfo.CurrentCulture).Should().BeNull();
        }

        [Test]
        public void TestConvertBack() {
            sut.ConvertBack("--", null, null, CultureInfo.CurrentCulture).Should().Be(-1);
            sut.ConvertBack(-2, null, null, CultureInfo.CurrentCulture).Should().Be(-2);
            sut.ConvertBack("anything", null, null, CultureInfo.CurrentCulture).Should().Be("anything");
            sut.ConvertBack(null, null, null, CultureInfo.CurrentCulture).Should().BeNull();
        }
    }
}