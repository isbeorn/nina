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
using Moq;
using NINA.Sequencer.Utility.DateTimeProvider;
using NINA.Core.Utility;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NINA.Astrometry.Interfaces;

namespace NINA.Test.Sequencer.Utility.DateTimeProvider {

    [TestFixture]
    public class TimeProviderTest {

        [Test]
        public void GetDateTime_NowIsReturned() {
            var referenceDate = new DateTime(2020, 1, 1, 0, 0, 0);

            var customDateTimeMock = new Mock<ICustomDateTime>();
            customDateTimeMock.SetupGet(x => x.Now).Returns(referenceDate);

            var sut = new NINA.Sequencer.Utility.DateTimeProvider.TimeProvider(new Mock<INighttimeCalculator>().Object);
            sut.DateTime = customDateTimeMock.Object;
            sut.GetDateTime(null).Should().Be(referenceDate);
        }
    }
}