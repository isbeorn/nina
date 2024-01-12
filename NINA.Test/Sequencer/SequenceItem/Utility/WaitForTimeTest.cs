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
using NINA.Astrometry;
using NINA.Core.Utility;
using NINA.Sequencer;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Sequencer.Utility.DateTimeProvider;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.SequenceItem.Utility {

    [TestFixture]
    public class WaitForTimeTest {

        [Test]
        public void WaitForTime_Clone_GoodClone() {
            var l = new List<IDateTimeProvider>();
            var sut = new WaitForTime(l);
            sut.Icon = new System.Windows.Media.GeometryGroup();
            var item2 = (WaitForTime)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Icon.Should().BeSameAs(sut.Icon);
            item2.DateTimeProviders.Should().BeSameAs(l);
            item2.Hours.Should().Be(sut.Hours);
            item2.Minutes.Should().Be(sut.Minutes);
            item2.Seconds.Should().Be(sut.Seconds);
        }

        [Test]
        public void WaitForTime_Clone_GoodClone_TimeProviderDoesntOverwrite() {
            var hours = (int)AstroUtil.EuclidianModulus(DateTime.Now.Hour + 1, 24);

            var provider = new Mock<IDateTimeProvider>();
            provider.Setup(x => x.GetDateTime(It.IsAny<ISequenceEntity>())).Returns(new DateTime(2023, 01, 01, hours, 20, 30));

            var l = new List<IDateTimeProvider>() { provider.Object };
            var sut = new WaitForTime(l);
            sut.Icon = new System.Windows.Media.GeometryGroup();
            sut.SelectedProvider = l.First();
            sut.Hours = hours;
            sut.Minutes = 20;
            sut.Seconds = 30;
            var item2 = (WaitForTime)sut.Clone();
            item2.AfterParentChanged();

            item2.Should().NotBeSameAs(sut);
            item2.Icon.Should().BeSameAs(sut.Icon);
            item2.DateTimeProviders.Should().BeSameAs(l);
            item2.Hours.Should().Be(hours);
            item2.Minutes.Should().Be(20);
            item2.Seconds.Should().Be(30);
        }

        [Test]
        public void WaitForTime_NoProviderInConstructor_NoCrash() {
            var sut = new WaitForTime(null);

            sut.Hours.Should().Be(0);
            sut.Minutes.Should().Be(0);
            sut.Seconds.Should().Be(0);
        }

        [Test]
        public void WaitForTime_SelectProviderInConstructor_TimeExtracted() {
            var providerMock = new Mock<IDateTimeProvider>();
            providerMock.Setup(x => x.GetDateTime(It.IsAny<ISequenceEntity>())).Returns(new DateTime(2000, 2, 3, 4, 5, 6));

            var sut = new WaitForTime(new List<IDateTimeProvider>() { providerMock.Object });

            sut.Hours.Should().Be(4);
            sut.Minutes.Should().Be(5);
            sut.Seconds.Should().Be(6);
        }

        [Test]
        public void WaitForTime_SelectProvider_TimeExtracted() {
            var providerMock = new Mock<IDateTimeProvider>();
            providerMock.Setup(x => x.GetDateTime(It.IsAny<ISequenceEntity>())).Returns(new DateTime(1, 2, 3, 4, 5, 6));
            var provider2Mock = new Mock<IDateTimeProvider>();
            provider2Mock.Setup(x => x.GetDateTime(It.IsAny<ISequenceEntity>())).Returns(new DateTime(2000, 10, 30, 10, 20, 30));

            var sut = new WaitForTime(new List<IDateTimeProvider>() { providerMock.Object, provider2Mock.Object });
            sut.SelectedProvider = sut.DateTimeProviders.Last();

            sut.Hours.Should().Be(10);
            sut.Minutes.Should().Be(20);
            sut.Seconds.Should().Be(30);
        }

        [Test]
        [TestCase(0, 0, 0, 1, 0, 0, 3600)]
        [TestCase(18, 0, 0, 19, 0, 0, 3600)]
        [TestCase(20, 0, 0, 19, 0, 0, 0)]
        [TestCase(2, 0, 0, 3, 0, 0, 3600)]
        [TestCase(4, 0, 0, 3, 0, 0, 0)]
        [TestCase(22, 0, 0, 1, 0, 0, 10800)]
        [TestCase(18, 0, 0, 3, 0, 0, 32400)]
        [TestCase(18, 0, 0, 9, 0, 0, 54000)]
        [TestCase(18, 0, 0, 12, 0, 0, 0)]
        [TestCase(18, 0, 0, 11, 59, 59, 61200 + 3540 + 59)]
        [TestCase(12, 0, 0, 13, 0, 0, 3600)]
        [TestCase(12, 0, 0, 18, 0, 0, 21600)]
        [TestCase(12, 0, 0, 12, 0, 0, 0)]
        [TestCase(12, 0, 0, 12, 0, 1, 1)]
        [TestCase(11, 0, 0, 12, 0, 0, 0)]
        [TestCase(11, 0, 0, 12, 10, 0, 0)]
        [TestCase(12, 0, 0, 12, 10, 0, 600)]
        [TestCase(12, 0, 0, 4, 0, 0, 57600)]
        [TestCase(12, 10, 0, 12, 20, 0, 600)]
        [TestCase(12, 10, 0, 13, 10, 0, 3600)]
        [TestCase(12, 10, 0, 4, 0, 0, 57000)]
        public void WaitForTime__(int nowHours, int nowMinutes, int nowSeconds, int thenHours, int thenMinutes, int thenSeconds, int estimatedDurationSeconds) {
            var now = new Mock<ICustomDateTime>();
            now.SetupGet(x => x.Now).Returns(new DateTime(1999, 1, 1, nowHours, nowMinutes, nowSeconds));

            var providerMock = new Mock<IDateTimeProvider>();
            providerMock.Setup(x => x.GetDateTime(It.IsAny<ISequenceEntity>())).Returns(new DateTime(1, 2, 3, thenHours, thenMinutes, thenSeconds));
            providerMock.Setup(x => x.GetRolloverTime(It.IsAny<ISequenceEntity>())).Returns(new TimeOnly(12, 0, 0));

            var sut = new WaitForTime(new List<IDateTimeProvider>() { providerMock.Object, });
            sut.SelectedProvider = sut.DateTimeProviders.Last();
            sut.DateTime = now.Object;

            sut.GetEstimatedDuration().Should().Be(TimeSpan.FromSeconds(estimatedDurationSeconds));
        }
    }
}