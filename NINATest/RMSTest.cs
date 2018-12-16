#region "copyright"

/*
    Copyright © 2016 - 2018 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using NINA.Model;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINATest {

    [TestFixture]
    public class RMSTest {

        [Test]
        public void RMS_DefaultConstructorTest() {
            RMS rms = new RMS();

            Assert.AreEqual(1, rms.Scale);
            Assert.AreEqual(0, rms.RA);
            Assert.AreEqual(0, rms.Dec);
            Assert.AreEqual(0, rms.Total);
        }

        [Test]
        public void RMS_AddSingleValue_CalculateCorrect() {
            RMS rms = new RMS();

            rms.AddDataPoint(10, 10);

            Assert.AreEqual(0, rms.RA);
            Assert.AreEqual(0, rms.Dec);
            Assert.AreEqual(0, rms.Total);
        }

        [Test]
        public void RMS_AddMultipleDataPoints_CalculateCorrect() {
            RMS rms = new RMS();

            rms.AddDataPoint(25, 1296);
            rms.AddDataPoint(625, 36);
            rms.AddDataPoint(25, 1296);
            rms.AddDataPoint(625, 36);

            Assert.AreEqual(300, rms.RA);
            Assert.AreEqual(630, rms.Dec);
            var total = Math.Sqrt((Math.Pow(300, 2) + Math.Pow(630, 2)));
            Assert.AreEqual(total, rms.Total);
        }

        [Test]
        public void RMS_AddMultipleDataPoints2_CalculateCorrect() {
            RMS rms = new RMS();

            rms.AddDataPoint(-25, -36);
            rms.AddDataPoint(-625, -1296);
            rms.AddDataPoint(-25, -36);
            rms.AddDataPoint(-625, -1296);

            Assert.AreEqual(300, rms.RA);
            Assert.AreEqual(630, rms.Dec);
            var total = Math.Sqrt((Math.Pow(300, 2) + Math.Pow(630, 2)));
            Assert.AreEqual(total, rms.Total);
        }

        [Test]
        public void RMS_AddMultipleDataPointsAndSetScale_CalculateCorrect() {
            RMS rms = new RMS();

            rms.AddDataPoint(-25, -36);
            rms.AddDataPoint(-625, -1296);
            rms.AddDataPoint(-25, -36);
            rms.AddDataPoint(-625, -1296);

            var scale = 1.59;
            rms.SetScale(scale);

            Assert.AreEqual(300, rms.RA);
            Assert.AreEqual(630, rms.Dec);
            var total = Math.Sqrt((Math.Pow(300, 2) + Math.Pow(630, 2)));
            Assert.AreEqual(total, rms.Total);
        }

        [Test]
        public void RMS_AddValuesAndClear_AllResetExceptScale() {
            RMS rms = new RMS();

            rms.AddDataPoint(-25, -36);
            rms.AddDataPoint(-625, -1296);
            rms.AddDataPoint(-25, -36);
            rms.AddDataPoint(-625, -1296);

            var scale = 1.59;
            rms.SetScale(scale);

            rms.Clear();

            Assert.AreEqual(scale, rms.Scale);
            Assert.AreEqual(0, rms.RA);
            Assert.AreEqual(0, rms.Dec);
            Assert.AreEqual(0, rms.Total);
        }

        [Test]
        public void RMS_AddValuesClearAndAddOneAgain_ValuesAppliedCorrectly() {
            RMS rms = new RMS();

            rms.AddDataPoint(-25, -36);
            rms.AddDataPoint(-625, -1296);
            rms.AddDataPoint(-25, -36);
            rms.AddDataPoint(-625, -1296);

            var scale = 1.59;
            rms.SetScale(scale);

            rms.Clear();
            rms.AddDataPoint(-25, -36);

            Assert.AreEqual(scale, rms.Scale);
            Assert.AreEqual(0, rms.RA);
            Assert.AreEqual(0, rms.Dec);
            Assert.AreEqual(0, rms.Total);
        }
    }
}