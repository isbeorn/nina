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
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Model;
using NINA.Image.ImageAnalysis;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.ViewModel.AutoFocus;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace NINA.Test.Autofocus {

    [TestFixture]
    public class AutofocusVMTest {
        private Mock<IProfileService> profileServiceMock;
        private Mock<ICameraMediator> cameraMediatorMock;
        private Mock<IFilterWheelMediator> fwMediatorMock;
        private Mock<IFocuserMediator> focuserMediatorMock;
        private Mock<IGuiderMediator> guiderMediatorMock;
        private Mock<IImagingMediator> imagingMediatorMock;
        private IExposureData focusedExposure;
        private IExposureData oneStepExposure;
        private IExposureData twoStepExposure;
        private IExposureData threeStepExposure;
        private ImageDataFactoryTestUtility dataFactoryUtility;
        private readonly Dictionary<IImageData, double> measurements = new Dictionary<IImageData, double>();

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            dataFactoryUtility = new ImageDataFactoryTestUtility();
            profileServiceMock = dataFactoryUtility.ProfileServiceMock;
            cameraMediatorMock = new Mock<ICameraMediator>();
            fwMediatorMock = new Mock<IFilterWheelMediator>();
            focuserMediatorMock = new Mock<IFocuserMediator>();
            guiderMediatorMock = new Mock<IGuiderMediator>();
            imagingMediatorMock = new Mock<IImagingMediator>();

            // HFR follows sqrt(1 + ((position - 5000) / 100)^2).
            focusedExposure = CreateExposure(1);
            oneStepExposure = CreateExposure(Math.Sqrt(2));
            twoStepExposure = CreateExposure(Math.Sqrt(5));
            threeStepExposure = CreateExposure(Math.Sqrt(10));
        }

        [SetUp]
        public void Setup() {
            profileServiceMock.Reset();
            cameraMediatorMock.Reset();
            fwMediatorMock.Reset();
            focuserMediatorMock.Reset();
            guiderMediatorMock.Reset();
            imagingMediatorMock.Reset();
            imagingMediatorMock
                .Setup(x => x.PrepareImage(It.IsAny<IImageData>(), It.IsAny<PrepareImageParameters>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IImageData imageData, PrepareImageParameters _, CancellationToken _) => imageData.RenderImage());
            dataFactoryUtility.StarDetectionMock.Invocations.Clear();
            dataFactoryUtility.StarDetectionMock
                .Setup(x => x.Detect(
                    It.IsAny<IRenderedImage>(),
                    It.IsAny<PixelFormat>(),
                    It.IsAny<StarDetectionParams>(),
                    It.IsAny<IProgress<ApplicationStatus>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((IRenderedImage image, PixelFormat _, StarDetectionParams _, IProgress<ApplicationStatus> _, CancellationToken _) => new StarDetectionResult {
                    AverageHFR = measurements[image.RawImageData],
                    HFRStdDev = 0.1
                });
        }

        [Test]
        public async Task StartAutofocus_Trendlines_IdealRun_FiveMeasurePoints_FocusStillAtInitialPosition() {
            var ct = new CancellationToken();

            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusMethod).Returns(AFMethodEnum.STARHFR);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusCurveFitting).Returns(AFCurveFittingEnum.TRENDLINES);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusNumberOfFramesPerPoint).Returns(1);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusInnerCropRatio).Returns(1);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusOuterCropRatio).Returns(1);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusInitialOffsetSteps).Returns(2);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusStepSize).Returns(100);

            profileServiceMock.SetupGet(x => x.ActiveProfile.ImageSettings.StarSensitivity).Returns(StarSensitivityEnum.Normal);
            profileServiceMock.SetupGet(x => x.ActiveProfile.ImageSettings.NoiseReduction).Returns(NoiseReductionEnum.None);

            int initialPosition = 5000;
            focuserMediatorMock.Setup(x => x.GetInfo()).Returns(new FocuserInfo() { Connected = true, Position = initialPosition });
            int position = initialPosition;
            focuserMediatorMock
                .Setup(x => x.MoveFocuserRelative(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int y, CancellationToken c) => { position = position + y; return position; });
            focuserMediatorMock
                .Setup(x => x.MoveFocuser(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int y, CancellationToken c) => { position = y; return position; });

            imagingMediatorMock
                .SetupSequence(x => x.CaptureImage(It.IsAny<CaptureSequence>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<string>()))
                .ReturnsAsync(focusedExposure)
                .ReturnsAsync(twoStepExposure)
                .ReturnsAsync(oneStepExposure)
                .ReturnsAsync(focusedExposure)
                .ReturnsAsync(oneStepExposure)
                .ReturnsAsync(twoStepExposure)
                .ReturnsAsync(focusedExposure);

            var sut = new AutoFocusVM(profileServiceMock.Object, cameraMediatorMock.Object, fwMediatorMock.Object, focuserMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object, dataFactoryUtility.StarDetectionSelectorMock.Object, dataFactoryUtility.StarAnnotatorSelectorMock.Object);

            var imagingFilter = new FilterInfo();

            var report = await sut.StartAutoFocus(imagingFilter, ct, new Progress<ApplicationStatus>());

            position.Should().Be(5000);
            report.CalculatedFocusPoint.Position.Should().Be(5000);
            report.MeasurePoints.Should().HaveCount(5);
            imagingMediatorMock.Verify(x => x.CaptureImage(It.IsAny<CaptureSequence>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<string>()), Times.Exactly(7));
            VerifyAutofocusMeasurements(7);
        }

        [Test]
        public async Task StartAutofocus_Trendlines_WithCameraDownloadFailures_FiveMeasurePoints_FocusStillAtInitialPosition() {
            var ct = new CancellationToken();

            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusMethod).Returns(AFMethodEnum.STARHFR);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusCurveFitting).Returns(AFCurveFittingEnum.TRENDLINES);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusNumberOfFramesPerPoint).Returns(1);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusInnerCropRatio).Returns(1);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusOuterCropRatio).Returns(1);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusInitialOffsetSteps).Returns(2);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusStepSize).Returns(100);

            profileServiceMock.SetupGet(x => x.ActiveProfile.ImageSettings.StarSensitivity).Returns(StarSensitivityEnum.Normal);
            profileServiceMock.SetupGet(x => x.ActiveProfile.ImageSettings.NoiseReduction).Returns(NoiseReductionEnum.None);

            int initialPosition = 5000;
            focuserMediatorMock.Setup(x => x.GetInfo()).Returns(new FocuserInfo() { Connected = true, Position = initialPosition });
            int position = initialPosition;
            focuserMediatorMock
                .Setup(x => x.MoveFocuserRelative(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int y, CancellationToken c) => { position = position + y; return position; });
            focuserMediatorMock
                .Setup(x => x.MoveFocuser(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int y, CancellationToken c) => { position = y; return position; });

            imagingMediatorMock
                .SetupSequence(x => x.CaptureImage(It.IsAny<CaptureSequence>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<string>()))
                .ReturnsAsync(focusedExposure)
                .ReturnsAsync((IExposureData)null!)
                .ReturnsAsync(twoStepExposure)
                .ReturnsAsync(oneStepExposure)
                .ReturnsAsync(focusedExposure)
                .ReturnsAsync((IExposureData)null!)
                .ReturnsAsync((IExposureData)null!)
                .ReturnsAsync(oneStepExposure)
                .ReturnsAsync(twoStepExposure)
                .ReturnsAsync(focusedExposure);

            var sut = new AutoFocusVM(profileServiceMock.Object, cameraMediatorMock.Object, fwMediatorMock.Object, focuserMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object, dataFactoryUtility.StarDetectionSelectorMock.Object, dataFactoryUtility.StarAnnotatorSelectorMock.Object);

            var imagingFilter = new FilterInfo();

            var report = await sut.StartAutoFocus(imagingFilter, ct, new Progress<ApplicationStatus>());

            position.Should().Be(5000);
            report.CalculatedFocusPoint.Position.Should().Be(5000);
            report.MeasurePoints.Should().HaveCount(5);
            imagingMediatorMock.Verify(x => x.CaptureImage(It.IsAny<CaptureSequence>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<string>()), Times.Exactly(10));
            VerifyAutofocusMeasurements(7);
        }

        [Test]
        public async Task StartAutofocus_TrendHyperbolic_ShiftedRightRun_SixMeasurePoints_RecoversFocusPosition() {
            var ct = new CancellationToken();

            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusMethod).Returns(AFMethodEnum.STARHFR);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusCurveFitting).Returns(AFCurveFittingEnum.TRENDHYPERBOLIC);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusNumberOfFramesPerPoint).Returns(1);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusInnerCropRatio).Returns(1);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusOuterCropRatio).Returns(1);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusInitialOffsetSteps).Returns(2);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusStepSize).Returns(100);

            profileServiceMock.SetupGet(x => x.ActiveProfile.ImageSettings.StarSensitivity).Returns(StarSensitivityEnum.Normal);
            profileServiceMock.SetupGet(x => x.ActiveProfile.ImageSettings.NoiseReduction).Returns(NoiseReductionEnum.None);

            int initialPosition = 5100;
            focuserMediatorMock.Setup(x => x.GetInfo()).Returns(new FocuserInfo() { Connected = true, Position = initialPosition });
            int position = initialPosition;
            focuserMediatorMock
                .Setup(x => x.MoveFocuserRelative(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int y, CancellationToken c) => { position = position + y; return position; });
            focuserMediatorMock
                .Setup(x => x.MoveFocuser(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int y, CancellationToken c) => { position = y; return position; });

            imagingMediatorMock
                .SetupSequence(x => x.CaptureImage(It.IsAny<CaptureSequence>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<string>()))
                .ReturnsAsync(oneStepExposure)
                .ReturnsAsync(threeStepExposure)
                .ReturnsAsync(twoStepExposure)
                .ReturnsAsync(oneStepExposure)
                .ReturnsAsync(focusedExposure)
                .ReturnsAsync(oneStepExposure)
                .ReturnsAsync(twoStepExposure)
                .ReturnsAsync(focusedExposure);

            var sut = new AutoFocusVM(profileServiceMock.Object, cameraMediatorMock.Object, fwMediatorMock.Object, focuserMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object, dataFactoryUtility.StarDetectionSelectorMock.Object, dataFactoryUtility.StarAnnotatorSelectorMock.Object);

            var imagingFilter = new FilterInfo();

            var report = await sut.StartAutoFocus(imagingFilter, ct, new Progress<ApplicationStatus>());

            // The asymmetric samples give a 5004.1023 trend intersection and a 5000 hyperbolic minimum.
            report.CalculatedFocusPoint.Position.Should().Be(5002);
            position.Should().Be((int)report.CalculatedFocusPoint.Position);
            report.MeasurePoints.Should().HaveCount(6);
            imagingMediatorMock.Verify(x => x.CaptureImage(It.IsAny<CaptureSequence>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<string>()), Times.Exactly(8));
            VerifyAutofocusMeasurements(8);
        }

        [Test]
        public async Task StartAutofocus_Trendlines_InfiniteRun_Prevent() {
            var ct = new CancellationToken();

            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusMethod).Returns(AFMethodEnum.STARHFR);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusCurveFitting).Returns(AFCurveFittingEnum.TRENDLINES);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusNumberOfFramesPerPoint).Returns(1);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusInnerCropRatio).Returns(1);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusOuterCropRatio).Returns(1);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusInitialOffsetSteps).Returns(4);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusStepSize).Returns(100);

            profileServiceMock.SetupGet(x => x.ActiveProfile.ImageSettings.StarSensitivity).Returns(StarSensitivityEnum.Normal);
            profileServiceMock.SetupGet(x => x.ActiveProfile.ImageSettings.NoiseReduction).Returns(NoiseReductionEnum.None);

            int initialPosition = 5000;
            int position = initialPosition;
            focuserMediatorMock.Setup(x => x.GetInfo()).Returns(new FocuserInfo() { Connected = true, Position = position });
            focuserMediatorMock
                .Setup(x => x.MoveFocuserRelative(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int y, CancellationToken c) => { position = position + y; return position; });
            focuserMediatorMock
                .Setup(x => x.MoveFocuser(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int y, CancellationToken c) => { position = y; return position; });

            imagingMediatorMock
                .SetupSequence(x => x.CaptureImage(It.IsAny<CaptureSequence>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<string>()))
                .ReturnsAsync(focusedExposure)
                .ReturnsAsync(oneStepExposure)
                .ReturnsAsync(focusedExposure)
                .ReturnsAsync(oneStepExposure)
                .ReturnsAsync(focusedExposure)
                .ReturnsAsync(oneStepExposure)
                .ReturnsAsync(focusedExposure)
                .ReturnsAsync(oneStepExposure)
                .ReturnsAsync(focusedExposure)
                .ReturnsAsync(oneStepExposure)
                .ReturnsAsync(focusedExposure)
                .ReturnsAsync(oneStepExposure)
                .ReturnsAsync(focusedExposure)
                .ReturnsAsync(oneStepExposure)
                .ReturnsAsync(focusedExposure)
                .ReturnsAsync(oneStepExposure)
                .ReturnsAsync(focusedExposure)
                .ReturnsAsync(oneStepExposure)
                .ReturnsAsync(focusedExposure)
                .ReturnsAsync(oneStepExposure)
                .ReturnsAsync(focusedExposure)
                .ReturnsAsync(oneStepExposure)
                .ReturnsAsync(focusedExposure)
                .ReturnsAsync(oneStepExposure)
                .ReturnsAsync(focusedExposure)
                .ReturnsAsync(oneStepExposure)
                .ReturnsAsync(focusedExposure)
                .ReturnsAsync(oneStepExposure)
                .ReturnsAsync(focusedExposure)
                .ReturnsAsync(oneStepExposure)
                .ReturnsAsync(focusedExposure)
                .ReturnsAsync(oneStepExposure)
                .ReturnsAsync(focusedExposure)
                .ReturnsAsync(oneStepExposure)
                .ReturnsAsync(focusedExposure)
                .ReturnsAsync(oneStepExposure)
                .ReturnsAsync(focusedExposure)
                .ReturnsAsync(oneStepExposure)
                .ReturnsAsync(focusedExposure)
                .ReturnsAsync(oneStepExposure)
                .ReturnsAsync(focusedExposure)
                .ReturnsAsync(oneStepExposure)
                .ReturnsAsync(focusedExposure)
                .ReturnsAsync(oneStepExposure)
                .ReturnsAsync(focusedExposure)
                .ReturnsAsync(oneStepExposure)
                .ReturnsAsync(focusedExposure)
                .ReturnsAsync(oneStepExposure);

            var sut = new AutoFocusVM(profileServiceMock.Object, cameraMediatorMock.Object, fwMediatorMock.Object, focuserMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object, dataFactoryUtility.StarDetectionSelectorMock.Object, dataFactoryUtility.StarAnnotatorSelectorMock.Object);

            var imagingFilter = new FilterInfo();

            var report = await sut.StartAutoFocus(imagingFilter, ct, new Progress<ApplicationStatus>());

            position.Should().Be(5000);
            report.Should().BeNull();

            imagingMediatorMock.Verify(x => x.CaptureImage(It.IsAny<CaptureSequence>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<string>()), Times.Exactly(42));
            VerifyAutofocusMeasurements(42);
        }

        private IExposureData CreateExposure(double hfr) {
            IImageData imageData = dataFactoryUtility.ImageDataFactory.CreateBaseImageData(
                new ushort[] { 100, 200, 300, 400 }, 2, 2, 16, false, new ImageMetaData());
            measurements.Add(imageData, hfr);

            return dataFactoryUtility.ExposureDataFactory.CreateCachedExposureData(imageData);
        }

        private void VerifyAutofocusMeasurements(int count) {
            dataFactoryUtility.StarDetectionMock.Verify(x => x.Detect(
                It.IsAny<IRenderedImage>(),
                PixelFormats.Gray16,
                It.Is<StarDetectionParams>(parameters => parameters.IsAutoFocus
                    && parameters.Sensitivity == StarSensitivityEnum.Normal
                    && parameters.NoiseReduction == NoiseReductionEnum.None
                    && !parameters.UseROI
                    && parameters.NumberOfAFStars == 0),
                It.IsAny<IProgress<ApplicationStatus>>(),
                It.IsAny<CancellationToken>()), Times.Exactly(count));
        }
    }
}
