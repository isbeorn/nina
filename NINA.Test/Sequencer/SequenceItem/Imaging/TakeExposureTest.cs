#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using Moq;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Model;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.ViewModel.ImageHistory;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using Nito.AsyncEx;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.SequenceItem.Imaging {

    [TestFixture]
    internal class TakeExposureTest {
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
            var sut = new TakeExposure(profileServiceMock.Object, cameraMediatorMock.Object, imagingMediatorMock.Object, imageSaveMediatorMock.Object, historyMock.Object);
            sut.Name = "SomeName";
            sut.Description = "SomeDescription";
            sut.Icon = new System.Windows.Media.GeometryGroup();
            sut.Gain = 10;
            sut.Offset = 100;
            sut.ImageType = "FLAT";
            sut.Binning = new BinningMode(2, 2);
            sut.ExposureTime = 100;
            sut.ExposureCount = 100;
            var item2 = (TakeExposure)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Name.Should().BeSameAs(sut.Name);
            item2.Description.Should().BeSameAs(sut.Description);
            item2.Icon.Should().BeSameAs(sut.Icon);
            item2.Binning.Should().NotBeNull();
            item2.ExposureCount.Should().Be(0);
            item2.ExposureTime.Should().Be(sut.ExposureTime);
            item2.Gain.Should().Be(sut.Gain);
            item2.Offset.Should().Be(sut.Offset);
            item2.ImageType.Should().Be(sut.ImageType);
        }

        [Test]
        public void Validate_NoIssues() {
            profileServiceMock.SetupGet(x => x.ActiveProfile.ImageFileSettings.FilePath).Returns(TestContext.CurrentContext.TestDirectory);
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = true });

            var sut = new TakeExposure(profileServiceMock.Object, cameraMediatorMock.Object, imagingMediatorMock.Object, imageSaveMediatorMock.Object, historyMock.Object);
            var valid = sut.Validate();

            valid.Should().BeTrue();

            sut.Issues.Should().BeEmpty();
        }

        [Test]
        public void Validate_NotConnected_OneIssue() {
            profileServiceMock.SetupGet(x => x.ActiveProfile.ImageFileSettings.FilePath).Returns(TestContext.CurrentContext.TestDirectory);
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = false });

            var sut = new TakeExposure(profileServiceMock.Object, cameraMediatorMock.Object, imagingMediatorMock.Object, imageSaveMediatorMock.Object, historyMock.Object);
            var valid = sut.Validate();

            valid.Should().BeFalse();

            sut.Issues.Should().HaveCount(1);
        }

        [Test]
        [TestCase(100, 10, 20, 1, "LIGHT", true, true, 1)]
        [TestCase(5.2, 1, 3, 2, "SNAPSHOT", true, true, 1)]
        [TestCase(100, 10, 20, 1, "DARK", null, false, 0)]
        [TestCase(100, 10, 20, 1, "FLAT", null, false, 0)]
        public async Task Execute_NoIssues_LogicCalled(double exposuretime, int gain, int offset, short binning, string imageType, bool? expectedStretch, bool? expectedDetect, int historycalls) {
            var imageMock = new Mock<IExposureData>();
            var imageDataMock = new Mock<IImageData>();
            var stats = new Mock<IImageStatistics>();
            profileServiceMock.SetupGet(x => x.ActiveProfile.ImageFileSettings.FilePath).Returns(TestContext.CurrentContext.TestDirectory);
            imageDataMock.SetupGet(x => x.Statistics).Returns(new AsyncLazy<IImageStatistics>(() => Task.FromResult(stats.Object)));
            imageDataMock.SetupGet(x => x.MetaData).Returns(new ImageMetaData() { Image = new ImageParameter { Id = 1 } });
            imageMock.SetupGet(x => x.MetaData).Returns(new ImageMetaData() { Image = new ImageParameter { Id = 1 } });
            imageMock.Setup(x => x.ToImageData(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(imageDataMock.Object));
            var imageTask = Task.FromResult(imageMock.Object);
            var prepareTask = Task.FromResult(new Mock<IRenderedImage>().Object);
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = true });
            imagingMediatorMock.Setup(x => x.CaptureImage(It.IsAny<CaptureSequence>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<string>())).Returns(imageTask);
            imagingMediatorMock.Setup(x => x.PrepareImage(It.IsAny<IImageData>(), It.IsAny<PrepareImageParameters>(), It.IsAny<CancellationToken>())).Returns(prepareTask);

            var sut = new TakeExposure(profileServiceMock.Object, cameraMediatorMock.Object, imagingMediatorMock.Object, imageSaveMediatorMock.Object, historyMock.Object);
            sut.ExposureTime = exposuretime;
            sut.Gain = gain;
            sut.Offset = offset;
            sut.Binning = new BinningMode(binning, binning);
            sut.ImageType = imageType;

            await sut.Execute(default, default);

            imagingMediatorMock.Verify(
                x => x.CaptureImage(
                    It.Is<CaptureSequence>(
                        cs =>
                            cs.ExposureTime == exposuretime
                            && cs.Binning.X == binning
                            && cs.Gain == gain
                            && cs.Offset == offset
                            && cs.ImageType == imageType
                            && cs.ProgressExposureCount == 0
                            && cs.TotalExposureCount == 1
                    ),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IProgress<ApplicationStatus>>(),
                    It.IsAny<string>()
                ), Times.Once);

            imagingMediatorMock.Verify(x => x.PrepareImage(It.Is<IImageData>(e => e == imageDataMock.Object), It.Is<PrepareImageParameters>(y => y.AutoStretch == expectedStretch && y.DetectStars == expectedDetect), It.IsAny<CancellationToken>()), Times.Once);

            imageSaveMediatorMock.Verify(x => x.Enqueue(It.Is<IImageData>(d => d == imageDataMock.Object), It.Is<Task<IRenderedImage>>(t => t == prepareTask), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
            historyMock.Verify(x => x.Add(1, imageType), Times.Exactly(historycalls));
        }

        [Test]
        [TestCase(1)]
        [TestCase(100)]
        [TestCase(0)]
        [TestCase(0.12)]
        public void GetEstimatedDuration_BasedOnParameters_ReturnsCorrectEstimate(double exposuretime) {
            var sut = new TakeExposure(profileServiceMock.Object, cameraMediatorMock.Object, imagingMediatorMock.Object, imageSaveMediatorMock.Object, historyMock.Object);
            sut.ExposureTime = exposuretime;

            var duration = sut.GetEstimatedDuration();

            duration.Should().Be(TimeSpan.FromSeconds(exposuretime));
        }
        
        [Test]
        public void DefaultGain_Plus_Clone_Test() {
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = true, DefaultGain = 23, DefaultOffset = 12 });
            // Validate() needs ImageFileSettings
            var profile = new NINA.Profile.Profile {
                ImageFileSettings = new ImageFileSettings {
                    FilePath = ""
                }
            };
            profileServiceMock.SetupGet(x => x.ActiveProfile).Returns(profile);
            var sut = new TakeExposure(profileServiceMock.Object, cameraMediatorMock.Object, imagingMediatorMock.Object, imageSaveMediatorMock.Object, historyMock.Object);

            sut.Gain = -1;
            sut.Validate();
            // After validation, Gain should be the default
            sut.Gain.Should().Be(23);

            var item2 = (TakeExposure)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Gain.Should().Be(23);
            item2.Offset.Should().Be(sut.Offset);
        }
        
        [Test]
        public void DefaultGain_Plus_Clone_Test_CameraNotConnected() {
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = false });
            // Validate() needs ImageFileSettings
            var profile = new NINA.Profile.Profile {
                ImageFileSettings = new ImageFileSettings {
                    FilePath = ""
                }
            };
            profileServiceMock.SetupGet(x => x.ActiveProfile).Returns(profile);
            var sut = new TakeExposure(profileServiceMock.Object, cameraMediatorMock.Object, imagingMediatorMock.Object, imageSaveMediatorMock.Object, historyMock.Object);
            sut.Gain = -1;
            sut.Validate();
            // After validation, Gain should still be -1, but it would never be used with a disconnected camera
            sut.Gain.Should().Be(-1);

            var item2 = (TakeExposure)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Gain.Should().Be(-1);
            item2.Offset.Should().Be(sut.Offset);

            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = true, DefaultGain = 22, DefaultOffset = 55 });
            sut.Validate();
            sut.Gain.Should().Be(22);
            sut.Offset.Should().Be(55);

            sut.GainDefinition = "88";
            sut.Gain.Should().Be(88);
        }
    }
}