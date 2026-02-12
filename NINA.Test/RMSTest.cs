#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Model;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Test {

    [TestFixture]
    public class RMSTest {

        [Test]
        public void RMS_DefaultConstructorTest() {
            RMS rms = new RMS();

            Assert.That(rms.Scale, Is.EqualTo(1));
            Assert.That(rms.RA, Is.EqualTo(0));
            Assert.That(rms.Dec, Is.EqualTo(0));
            Assert.That(rms.Total, Is.EqualTo(0));
        }

        [Test]
        public void RMS_AddSingleValue_CalculateCorrect() {
            RMS rms = new RMS();

            rms.AddDataPoint(10, 10);

            Assert.That(rms.RA, Is.EqualTo(0));
            Assert.That(rms.Dec, Is.EqualTo(0));
            Assert.That(rms.Total, Is.EqualTo(0));
        }

        [Test]
        public void RMS_AddMultipleDataPoints_CalculateCorrect() {
            RMS rms = new RMS();

            rms.AddDataPoint(25, 1296);
            rms.AddDataPoint(625, 36);
            rms.AddDataPoint(25, 1296);
            rms.AddDataPoint(625, 36);

            Assert.That(rms.RA, Is.EqualTo(300));
            Assert.That(rms.Dec, Is.EqualTo(630));
            var total = Math.Sqrt((Math.Pow(300, 2) + Math.Pow(630, 2)));
            Assert.That(rms.Total, Is.EqualTo(total));
        }

        [Test]
        public void RMS_AddMultipleDataPoints2_CalculateCorrect() {
            RMS rms = new RMS();

            rms.AddDataPoint(-25, -36);
            rms.AddDataPoint(-625, -1296);
            rms.AddDataPoint(-25, -36);
            rms.AddDataPoint(-625, -1296);

            Assert.That(rms.RA, Is.EqualTo(300));
            Assert.That(rms.Dec, Is.EqualTo(630));
            var total = Math.Sqrt((Math.Pow(300, 2) + Math.Pow(630, 2)));
            Assert.That(rms.Total, Is.EqualTo(total));
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

            Assert.That(rms.RA, Is.EqualTo(300));
            Assert.That(rms.Dec, Is.EqualTo(630));
            var total = Math.Sqrt((Math.Pow(300, 2) + Math.Pow(630, 2)));
            Assert.That(rms.Total, Is.EqualTo(total));
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

            Assert.That(rms.Scale, Is.EqualTo(scale));
            Assert.That(rms.RA, Is.EqualTo(0));
            Assert.That(rms.Dec, Is.EqualTo(0));
            Assert.That(rms.Total, Is.EqualTo(0));
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

            Assert.That(rms.Scale, Is.EqualTo(scale));
            Assert.That(rms.RA, Is.EqualTo(0));
            Assert.That(rms.Dec, Is.EqualTo(0));
            Assert.That(rms.Total, Is.EqualTo(0));
        }
    }
}