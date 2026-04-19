#region "copyright"

/*
    Copyright (c) 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility.Converters;
using NUnit.Framework;
using System;
using System.Globalization;
using System.Windows;

namespace NINA.Test.Converters {

    [TestFixture]
    public class CoreUtilityConvertersTest {

        /// <summary>
        /// Verifies boolean multi-value conversion behaves like logical AND and rejects reverse conversion.
        /// </summary>
        [Test]
        public void BooleanAndConverter_MixedBooleanInputs_ReturnsLogicalAnd() {
            BooleanAndConverter converter = new BooleanAndConverter();

            Assert.That(converter.Convert(new object[] { true, true }, typeof(bool), null, CultureInfo.InvariantCulture), Is.EqualTo(true));
            Assert.That(converter.Convert(new object[] { true, false, true }, typeof(bool), null, CultureInfo.InvariantCulture), Is.EqualTo(false));
            Assert.That(converter.Convert(new object[] { true, "ignored" }, typeof(bool), null, CultureInfo.InvariantCulture), Is.EqualTo(true));
            Assert.Throws<NotSupportedException>(() => converter.ConvertBack(true, new[] { typeof(bool) }, null, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Verifies boolean visibility conversion maps true to Visible and all non-visible states back to false.
        /// </summary>
        [Test]
        public void BooleanToVisibilityCollapsedConverter_ConvertAndConvertBack_UsesCollapsedForFalse() {
            BooleanToVisibilityCollapsedConverter converter = new BooleanToVisibilityCollapsedConverter();

            Assert.That(converter.Convert(true, typeof(Visibility), null, CultureInfo.InvariantCulture), Is.EqualTo(Visibility.Visible));
            Assert.That(converter.Convert(false, typeof(Visibility), null, CultureInfo.InvariantCulture), Is.EqualTo(Visibility.Collapsed));
            Assert.That(converter.ConvertBack(Visibility.Visible, typeof(bool), null, CultureInfo.InvariantCulture), Is.EqualTo(true));
            Assert.That(converter.ConvertBack(Visibility.Hidden, typeof(bool), null, CultureInfo.InvariantCulture), Is.EqualTo(false));
            Assert.That(converter.ConvertBack(Visibility.Collapsed, typeof(bool), null, CultureInfo.InvariantCulture), Is.EqualTo(false));
        }

        /// <summary>
        /// Verifies clamp conversion respects invariant min/max parameters and leaves invalid inputs unchanged.
        /// </summary>
        [Test]
        public void ClampDoubleConverter_ValidAndInvalidParameters_ClampsOrReturnsOriginalValue() {
            ClampDoubleConverter converter = new ClampDoubleConverter();

            Assert.That(converter.Convert(12.5d, typeof(double), "0|10", CultureInfo.InvariantCulture), Is.EqualTo(10d));
            Assert.That(converter.Convert(-2d, typeof(double), "0|10", CultureInfo.InvariantCulture), Is.EqualTo(0d));
            Assert.That(converter.Convert(4d, typeof(double), "0|10", CultureInfo.InvariantCulture), Is.EqualTo(4d));
            Assert.That(converter.Convert(4d, typeof(double), "bad", CultureInfo.InvariantCulture), Is.EqualTo(4d));
            Assert.Throws<NotImplementedException>(() => converter.ConvertBack(4d, typeof(double), null, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Verifies NaN text conversion preserves real values and maps blank user input back to NaN.
        /// </summary>
        [Test]
        public void NaNToEmptyTextConverter_NaNAndNumericValues_MapsBlankTextToNaN() {
            NaNToEmptyTextConverter converter = new NaNToEmptyTextConverter();

            Assert.That(converter.Convert(double.NaN, typeof(string), null, CultureInfo.InvariantCulture), Is.EqualTo(string.Empty));
            Assert.That(converter.Convert(3.25d, typeof(string), null, CultureInfo.InvariantCulture), Is.EqualTo(3.25d));
            object blankResult = converter.ConvertBack(" ", typeof(double), null, CultureInfo.InvariantCulture);
            object numericResult = converter.ConvertBack("3.25", typeof(double), null, CultureInfo.InvariantCulture);

            Assert.That((double)blankResult, Is.NaN);
            Assert.That(numericResult, Is.EqualTo("3.25"));
        }
    }
}
