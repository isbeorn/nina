#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NINA.Locale;
using NINA.Model.MyFlatDevice;
using NUnit.Framework;

namespace NINATest.FlatDevice {
    [TestFixture]
    internal class FlatDeviceInfoTest {
        [Test]
        [TestCase(CoverState.NeitherOpenNorClosed)]
        [TestCase(CoverState.Closed)]
        [TestCase(CoverState.Open)]
        [TestCase(CoverState.Unknown)]
        public void TestLocalizedCoverState(CoverState expected) {
            var sut = new FlatDeviceInfo {
                CoverState = expected
            };
            Assert.That(sut.CoverState, Is.EqualTo(expected));
            Assert.That(sut.LocalizedCoverState, Is.EqualTo(Loc.Instance[$"LblFlatDevice{expected}"]));
        }
    }
}
