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
using NINA.Sequencer;
using NINA.Sequencer.SequenceItem.Utility;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.SequenceItem.Utility {

    [TestFixture]
    public class WaitForTimeSpanTest {

        [Test]
        public void WaitForTime_Clone_GoodClone() {
            var sut = new WaitForTimeSpan();
            sut.Icon = new System.Windows.Media.GeometryGroup();
            var item2 = (WaitForTimeSpan)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Name.Should().BeSameAs(sut.Name);
            item2.Description.Should().BeSameAs(sut.Description);
            item2.Icon.Should().BeSameAs(sut.Icon);
            item2.Time.Should().Be(sut.Time);
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(5, 5)]
        [TestCase(100, 100)]
        public void WaitForTimeSpan_GetEstimatedDuration_Test(int time, int expectedTime) {
            var sut = new WaitForTimeSpan();
            sut.Time = time;

            var estimate = sut.GetEstimatedDuration();

            estimate.Should().Be(TimeSpan.FromSeconds(expectedTime));
        }
    }
}