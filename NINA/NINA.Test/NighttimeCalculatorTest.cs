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
            ClassicAssert.AreEqual(expectedDate, referenceDate);
        }

        [Test]
        public void BeforeNoonTest() {
            var date = new DateTime(2020, 5, 4, 10, 0, 0);
            var referenceDate = NighttimeCalculator.GetReferenceDate(date);
            var dayBefore = date.AddDays(-1);
            var expectedDate = new DateTime(dayBefore.Year, dayBefore.Month, dayBefore.Day, 12, 0, 0);
            ClassicAssert.AreEqual(expectedDate, referenceDate);
        }

        [Test]
        public void AtNoonSlightlyBeforeTest() {
            var date = new DateTime(2020, 5, 4, 11, 59, 0);
            var referenceDate = NighttimeCalculator.GetReferenceDate(date);
            var dayBefore = date.AddDays(-1);
            var expectedDate = new DateTime(dayBefore.Year, dayBefore.Month, dayBefore.Day, 12, 0, 0);
            ClassicAssert.AreEqual(expectedDate, referenceDate);
        }

        [Test]
        public void AtNoonTest() {
            var date = new DateTime(2020, 5, 4, 12, 0, 0);
            var referenceDate = NighttimeCalculator.GetReferenceDate(date);
            var dayBefore = date.AddDays(-1);
            var expectedDate = new DateTime(date.Year, date.Month, date.Day, 12, 0, 0);
            ClassicAssert.AreEqual(expectedDate, referenceDate);
        }

        [Test]
        public void AtNoonSlightlyAfterTest() {
            var date = new DateTime(2020, 5, 4, 12, 1, 0);
            var referenceDate = NighttimeCalculator.GetReferenceDate(date);
            var expectedDate = new DateTime(date.Year, date.Month, date.Day, 12, 0, 0);
            ClassicAssert.AreEqual(expectedDate, referenceDate);
        }
    }
}