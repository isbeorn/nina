#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using Moq;
using NINA.Model.ImageData;
using NINA.Profile;
using NINA.ViewModel;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINATest {

    [TestFixture]
    public class ImageHistoryVMTest {
        private Mock<IProfileService> profileServiceMock = new Mock<IProfileService>();

        [Test]
        public void ImageHistory_ConcurrentId_Order_Test() {
            var sut = new ImageHistoryVM(profileServiceMock.Object);

            Parallel.For(0, 100, (i) => {
                sut.Add(new StarDetectionAnalysis() { DetectedStars = i, HFR = i });
            });

            for (int i = 0; i < 100; i++) {
                sut.ImageHistory[i].Id.Should().Be(i + 1);
            }
        }

        [Test]
        public void ImageHistory_Value_Test() {
            var sut = new ImageHistoryVM(profileServiceMock.Object);
            var hfr = 10.1234;
            var stars = 12323;

            sut.Add(new StarDetectionAnalysis() { DetectedStars = stars, HFR = hfr });

            sut.LimitedImageHistoryStack.First().Value.HFR.Should().Be(hfr);
            sut.LimitedImageHistoryStack.First().Value.DetectedStars.Should().Be(stars);
            sut.ImageHistory[0].HFR.Should().Be(hfr);
            sut.ImageHistory[0].DetectedStars.Should().Be(stars);
        }

        [Test]
        public void ImageHistory_LimitedStack_FullConcurrency_Test() {
            var sut = new ImageHistoryVM(profileServiceMock.Object);

            Parallel.For(0, 300, (i) => {
                sut.Add(new StarDetectionAnalysis() { DetectedStars = i, HFR = i });
                sut.AppendAutoFocusPoint(new NINA.ViewModel.AutoFocus.AutoFocusReport());
            });

            sut.LimitedImageHistoryStack.Count.Should().Be(100);
            sut.AutoFocusPoints.Select(x => x.Id).Distinct().ToList().Count.Should().BeLessOrEqualTo(100);
            sut.ImageHistory.Count.Should().Be(300);
        }

        [Test]
        public void ImageHistory_LimitedStack_Concurrency_Test() {
            var sut = new ImageHistoryVM(profileServiceMock.Object);

            for (int i = 0; i < 1000; i++) {
                sut.Add(new StarDetectionAnalysis() { DetectedStars = i, HFR = i });
                sut.AppendAutoFocusPoint(new NINA.ViewModel.AutoFocus.AutoFocusReport());
            }

            sut.LimitedImageHistoryStack.Count.Should().Be(100);
            sut.AutoFocusPoints.Count.Should().Be(100);
            sut.ImageHistory.Count.Should().Be(1000);
        }

        [Test]
        public void ImageHistory_ClearPlot_Test() {
            var sut = new ImageHistoryVM(profileServiceMock.Object);

            Parallel.For(0, 100, (i) => {
                sut.Add(new StarDetectionAnalysis() { DetectedStars = i, HFR = i });
                sut.AppendAutoFocusPoint(new NINA.ViewModel.AutoFocus.AutoFocusReport());
            });

            sut.PlotClear();

            sut.LimitedImageHistoryStack.Count.Should().Be(0);
            sut.AutoFocusPoints.Count.Should().Be(0);
            sut.ImageHistory.Count.Should().Be(100);
        }
    }
}
