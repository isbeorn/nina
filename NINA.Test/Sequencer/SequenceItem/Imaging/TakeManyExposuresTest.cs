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
using NINA.Equipment.Equipment.MyCamera;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.Serialization;
using NINA.Equipment.Interfaces.Mediator;
using NINA.ViewModel.ImageHistory;
using Nito.AsyncEx;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;

namespace NINA.Test.Sequencer.SequenceItem.Imaging {

    [TestFixture]
    public class TakeManyExposuresTest {
        private Mock<ICameraMediator> cameraMediatorMock;
        private Mock<IImagingMediator> imagingMediatorMock;
        private Mock<IImageSaveMediator> imageSaveMediatorMock;
        private Mock<IImageHistoryVM> historyMock;
        private Mock<IProfileService> profileServiceMock;

        [SetUp]
        public void Setup() {
            cameraMediatorMock = new Mock<ICameraMediator>();
            imagingMediatorMock = new Mock<IImagingMediator>();
            imageSaveMediatorMock = new Mock<IImageSaveMediator>();
            historyMock = new Mock<IImageHistoryVM>();
            profileServiceMock = new Mock<IProfileService>();
        }

        [Test]
        public void Clone_ItemClonedProperly() {
            profileServiceMock.SetupGet(x => x.ActiveProfile.ImageFileSettings.FilePath).Returns(TestContext.CurrentContext.TestDirectory);
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = true });
            var sut = new TakeManyExposures(profileServiceMock.Object, cameraMediatorMock.Object, imagingMediatorMock.Object, imageSaveMediatorMock.Object, historyMock.Object);
            sut.Name = "SomeName";
            sut.Description = "SomeDescription";
            sut.Icon = new System.Windows.Media.GeometryGroup();
            var item2 = (TakeManyExposures)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Name.Should().BeSameAs(sut.Name);
            item2.Description.Should().BeSameAs(sut.Description);
            item2.Icon.Should().BeSameAs(sut.Icon);
            item2.Items.Count.Should().Be(1);
            item2.Conditions.Count.Should().Be(1);

            var originalExposure = sut.Items[0] as TakeExposure;
            var clonedExposure = item2.Items[0] as TakeExposure;
            clonedExposure.Should().NotBeSameAs(originalExposure);
            clonedExposure.Binning.Should().NotBeNull();
            clonedExposure.ExposureCount.Should().Be(0);
            clonedExposure.ExposureTime.Should().Be(originalExposure.ExposureTime);
            clonedExposure.Gain.Should().Be(originalExposure.Gain);
            clonedExposure.Offset.Should().Be(originalExposure.Offset);
            clonedExposure.ImageType.Should().Be(originalExposure.ImageType);
        }

        [Test]
        public void Validate_NoIssues() {
            profileServiceMock.SetupGet(x => x.ActiveProfile.ImageFileSettings.FilePath).Returns(TestContext.CurrentContext.TestDirectory);
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = true });

            var sut = new TakeManyExposures(profileServiceMock.Object, cameraMediatorMock.Object, imagingMediatorMock.Object, imageSaveMediatorMock.Object, historyMock.Object);
            var valid = sut.Validate();

            valid.Should().BeTrue();

            sut.Issues.Should().BeEmpty();
        }

        [Test]
        public void Validate_NotConnected_OneIssue() {
            profileServiceMock.SetupGet(x => x.ActiveProfile.ImageFileSettings.FilePath).Returns(TestContext.CurrentContext.TestDirectory);
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = false });

            var sut = new TakeManyExposures(profileServiceMock.Object, cameraMediatorMock.Object, imagingMediatorMock.Object, imageSaveMediatorMock.Object, historyMock.Object);
            var valid = sut.Validate();

            valid.Should().BeFalse();

            sut.Issues.Should().HaveCount(1);
        }

        //[Test]
        //[TestCase(100, 10, 20, 1, "LIGHT", true, true)]
        //[TestCase(5.2, 1, 3, 2, "SNAPSHOT", true, true)]
        //[TestCase(100, 10, 20, 1, "DARK", null, null)]
        //public async Task Execute_NoIssues_LogicCalled(double exposuretime, int gain, int offset, short binning, string imageType, bool? expectedStretch, bool? expectedDetect) {
        //    var imageMock = new Mock<IExposureData>();
        //    var imageDataMock = new Mock<IImageData>();
        //    var stats = new Mock<IImageStatistics>();
        //    imageDataMock.SetupGet(x => x.Statistics).Returns(new AsyncLazy<IImageStatistics>(() => Task.FromResult(stats.Object)));
        //    imageMock.Setup(x => x.ToImageData(It.IsAny<CancellationToken>())).Returns(Task.FromResult(imageDataMock.Object));
        //    var imageTask = Task.FromResult(imageMock.Object);
        //    var prepareTask = Task.FromResult(new Mock<IRenderedImage>().Object);
        //    cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = true });
        //    imagingMediatorMock.Setup(x => x.CaptureImage(It.IsAny<CaptureSequence>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<string>())).Returns(imageTask);
        //    imagingMediatorMock.Setup(x => x.PrepareImage(It.IsAny<IExposureData>(), It.IsAny<PrepareImageParameters>(), It.IsAny<CancellationToken>())).Returns(prepareTask);

        //    var sut = new TakeManyExposures(cameraMediatorMock.Object, imagingMediatorMock.Object, imageSaveMediatorMock.Object, historyMock.Object);
        //    sut.ExposureTime = exposuretime;
        //    sut.Gain = gain;
        //    sut.Offset = offset;
        //    sut.Binning = new BinningMode(binning, binning);
        //    sut.ImageType = imageType;

        //    await sut.Execute(default, default);

        //    imagingMediatorMock.Verify(
        //        x => x.CaptureImage(
        //            It.Is<CaptureSequence>(
        //                cs =>
        //                    cs.ExposureTime == exposuretime
        //                    && cs.Binning.X == binning
        //                    && cs.Gain == gain
        //                    && cs.Offset == offset
        //                    && cs.ImageType == imageType
        //                    && cs.ProgressExposureCount == 0
        //                    && cs.TotalExposureCount == 1
        //            ),
        //            It.IsAny<CancellationToken>(),
        //            It.IsAny<IProgress<ApplicationStatus>>(),
        //            It.IsAny<string>()
        //        ), Times.Once);

        //    imagingMediatorMock.Verify(x => x.PrepareImage(It.Is<IExposureData>(e => e == imageMock.Object), It.Is<PrepareImageParameters>(y => y.AutoStretch == expectedStretch && y.DetectStars == expectedDetect), It.IsAny<CancellationToken>()), Times.Once);

        //    imageSaveMediatorMock.Verify(x => x.Enqueue(It.Is<IImageData>(d => d == imageDataMock.Object), It.Is<Task<IRenderedImage>>(t => t == prepareTask), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
        //    historyMock.Verify(x => x.Add(It.Is<IImageStatistics>(s => s == stats.Object)), Times.Once);
        //}

        //[Test]
        //public Task Execute_HasIssues_LogicNotCalled() {
        //    cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = false });

        //    var sut = new TakeManyExposures(cameraMediatorMock.Object, imagingMediatorMock.Object, imageSaveMediatorMock.Object, historyMock.Object);
        //    Func<Task> act = () => { return sut.Execute(default, default); };

        //    return act.Should().ThrowAsync<SequenceItemSkippedException>(string.Join(",", sut.Issues));
        //}

        //[Test]
        //[TestCase(1)]
        //[TestCase(100)]
        //[TestCase(0)]
        //[TestCase(0.12)]
        //public void GetEstimatedDuration_BasedOnParameters_ReturnsCorrectEstimate(double exposuretime) {
        //    var sut = new TakeManyExposures(cameraMediatorMock.Object, imagingMediatorMock.Object, imageSaveMediatorMock.Object, historyMock.Object);
        //    sut.ExposureTime = exposuretime;

        //    var duration = sut.GetEstimatedDuration();

        //    duration.Should().Be(TimeSpan.FromSeconds(exposuretime));
        //}
    }
}