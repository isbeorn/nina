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
using NINA.Image.ImageData;
using NINA.Profile.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.ViewModel;
using NINA.ViewModel.ImageHistory;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Utility.AutoFocus;
using NINA.Core.Enum;

namespace NINA.Test {

    [TestFixture]
    public class ImageHistoryVMTest {
        private Mock<IProfileService> profileServiceMock = new Mock<IProfileService>();
        private Mock<IImageSaveMediator> imageSaveMediatorMock = new Mock<IImageSaveMediator>();

        [Test]
        public void ImageHistory_ConcurrentId_Order_Test() {
            profileServiceMock.SetupGet(x => x.ActiveProfile.ImageHistorySettings.ImageHistoryLeftSelected).Returns(ImageHistoryEnum.HFR);
            profileServiceMock.SetupGet(x => x.ActiveProfile.ImageHistorySettings.ImageHistoryRightSelected).Returns(ImageHistoryEnum.Stars);

            var sut = new ImageHistoryVM(profileServiceMock.Object, imageSaveMediatorMock.Object);

            for (int i = 1; i < 101; i++) {
                sut.Add(i, null, "LIGHT");
            }

            for (int i = 0; i < 100; i++) {
                sut.ImageHistory[i].Id.Should().Be(i + 1);
            }
        }

        [Test]
        public void ImageHistory_Value_Test() {
            profileServiceMock.SetupGet(x => x.ActiveProfile.ImageHistorySettings.ImageHistoryLeftSelected).Returns(ImageHistoryEnum.HFR);
            profileServiceMock.SetupGet(x => x.ActiveProfile.ImageHistorySettings.ImageHistoryRightSelected).Returns(ImageHistoryEnum.Stars);
            var sut = new ImageHistoryVM(profileServiceMock.Object, imageSaveMediatorMock.Object);
            var hfr = 10.1234;
            var stars = 12323;
            var duration = 300;
            var filter = "Red";

            sut.Add(1, null, "LIGHT");
            sut.AppendImageProperties(new ImageSavedEventArgs() { StarDetectionAnalysis = new StarDetectionAnalysis() { DetectedStars = stars, HFR = hfr }, Duration = duration, Filter = filter, MetaData = new ImageMetaData { Image = new ImageParameter { Id = 1 } } });

            sut.ObservableImageHistory.First().HFR.Should().Be(hfr);
            sut.ObservableImageHistory.First().Stars.Should().Be(stars);
            sut.ImageHistory[0].HFR.Should().Be(hfr);
            sut.ImageHistory[0].Stars.Should().Be(stars);
            sut.ImageHistory[0].Duration.Should().Be(duration);
            sut.ImageHistory[0].Filter.Should().Be(filter);
        }

        [Test]
        public void ImageHistory_LimitedStack_FullConcurrency_Test() {
            profileServiceMock.SetupGet(x => x.ActiveProfile.ImageHistorySettings.ImageHistoryLeftSelected).Returns(ImageHistoryEnum.HFR);
            profileServiceMock.SetupGet(x => x.ActiveProfile.ImageHistorySettings.ImageHistoryRightSelected).Returns(ImageHistoryEnum.Stars);
            var sut = new ImageHistoryVM(profileServiceMock.Object, imageSaveMediatorMock.Object);

            for (int i = 0; i < 300; i++) {
                sut.Add(i + 1, null, "LIGHT");
                sut.AppendImageProperties(new ImageSavedEventArgs() { StarDetectionAnalysis = new StarDetectionAnalysis() { DetectedStars = 1, HFR = 1 }, MetaData = new ImageMetaData { Image = new ImageParameter { Id = i + 1 } } });
                sut.AppendAutoFocusPoint(new AutoFocusReport());
            }

            sut.AutoFocusPoints.Select(x => x.Id).Distinct().ToList().Count.Should().BeLessOrEqualTo(300);
            sut.ObservableImageHistory.Count.Should().Be(300);
            sut.ImageHistory.Count.Should().Be(300);
        }

        [Test]
        public void ImageHistory_ClearPlot_Test() {
            profileServiceMock.SetupGet(x => x.ActiveProfile.ImageHistorySettings.ImageHistoryLeftSelected).Returns(ImageHistoryEnum.HFR);
            profileServiceMock.SetupGet(x => x.ActiveProfile.ImageHistorySettings.ImageHistoryRightSelected).Returns(ImageHistoryEnum.Stars);
            var sut = new ImageHistoryVM(profileServiceMock.Object, imageSaveMediatorMock.Object);

            for (int i = 0; i < 100; i++) {
                sut.Add(i + 1, null, "LIGHT");
                sut.AppendImageProperties(new ImageSavedEventArgs() { StarDetectionAnalysis = new StarDetectionAnalysis() { DetectedStars = 1, HFR = 1 }, MetaData = new ImageMetaData { Image = new ImageParameter { Id = i + 1 } } });
                sut.AppendAutoFocusPoint(new AutoFocusReport());
            }

            sut.PlotClear();

            sut.ObservableImageHistory.Count.Should().Be(0);
            sut.AutoFocusPoints.Count.Should().Be(0);
            sut.ImageHistory.Count.Should().Be(0);
        }
    }
}