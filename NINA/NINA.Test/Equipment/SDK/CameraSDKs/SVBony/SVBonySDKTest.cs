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
using NINA.Core.Enum;
using NINA.Equipment.SDK.CameraSDKs.SVBonySDK;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Equipment.SDK.CameraSDKs.SVBony {

    [TestFixture]
    public class SVBonySDKTest {

        [Test]
        public void Ctor_PropertiesSet() {
            var pinvoke = new Mock<ISVBonyPInvokeProxy>();

            var sut = new SVBonySDK(10, pinvoke.Object);

            sut.Connected.Should().BeFalse();
        }

        [Test]
        public void Connect_NoSpecialPropertiesAvailable_Successfully() {
            var pinvoke = new Mock<ISVBonyPInvokeProxy>();
            var id = 12;

            var sut = new SVBonySDK(id, pinvoke.Object);
            sut.Connect();

            sut.Connected.Should().BeTrue();
            pinvoke.Verify(x => x.SVBOpenCamera(id), Times.Exactly(2)); // Should be one, but is two due to the offset problem workaround
            pinvoke.Verify(x => x.SVBSetCameraMode(id, SVB_CAMERA_MODE.SVB_MODE_TRIG_SOFT), Times.Once);
            SVB_CAMERA_PROPERTY dummy;
            pinvoke.Verify(x => x.SVBGetCameraProperty(id, out dummy), Times.Exactly(2));
            pinvoke.Verify(x => x.SVBStartVideoCapture(id), Times.Exactly(2));
        }

        [Test]
        [TestCase(SVB_IMG_TYPE.SVB_IMG_RAW8, 8)]
        [TestCase(SVB_IMG_TYPE.SVB_IMG_RAW10, 10)]
        [TestCase(SVB_IMG_TYPE.SVB_IMG_RAW12, 12)]
        [TestCase(SVB_IMG_TYPE.SVB_IMG_RAW14, 14)]
        [TestCase(SVB_IMG_TYPE.SVB_IMG_RAW16, 16)]
        [TestCase(SVB_IMG_TYPE.SVB_IMG_Y8, 8)]
        [TestCase(SVB_IMG_TYPE.SVB_IMG_Y10, 10)]
        [TestCase(SVB_IMG_TYPE.SVB_IMG_Y12, 12)]
        [TestCase(SVB_IMG_TYPE.SVB_IMG_Y14, 14)]
        [TestCase(SVB_IMG_TYPE.SVB_IMG_Y16, 16)]
        public void Connect_VideoFormats_HighestBitDepthSuccessfullySet(SVB_IMG_TYPE supportedVideoFormat, int expectedBitDepth) {
            var pinvoke = new Mock<ISVBonyPInvokeProxy>();
            SVB_CAMERA_PROPERTY expected = new SVB_CAMERA_PROPERTY() { SupportedVideoFormat = new SVB_IMG_TYPE[] { supportedVideoFormat, SVB_IMG_TYPE.SVB_IMG_RGB24, SVB_IMG_TYPE.SVB_IMG_END } };
            pinvoke.Setup(x => x.SVBGetCameraProperty(It.IsAny<int>(), out expected)).Verifiable();
            var id = 12;

            var sut = new SVBonySDK(id, pinvoke.Object);
            sut.Connect();

            sut.Connected.Should().BeTrue();
            sut.GetBitDepth().Should().Be(expectedBitDepth);
        }

        [Test]
        public void Connect_Controls_AddedAndSetToDefault() {
            var pinvoke = new Mock<ISVBonyPInvokeProxy>();
            int numOfControls = 3;
            pinvoke.Setup(x => x.SVBGetNumOfControls(It.IsAny<int>(), out numOfControls)).Verifiable();

            var cap1 = new SVB_CONTROL_CAPS() { ControlType = SVB_CONTROL_TYPE.SVB_GAIN, DefaultValue = 10, MinValue = 0, MaxValue = 100 };

            pinvoke.Setup(x => x.SVBGetControlCaps(It.IsAny<int>(), It.IsAny<int>(), out cap1));

            var id = 12;

            var sut = new SVBonySDK(id, pinvoke.Object);
            sut.Connect();

            pinvoke.Verify(x => x.SVBSetControlValue(id, cap1.ControlType, cap1.DefaultValue, SVB_BOOL.SVB_FALSE), Times.Once);
        }

        [Test]
        public void Connect_ROISetCorrectly() {
            var pinvoke = new Mock<ISVBonyPInvokeProxy>();
            SVB_CAMERA_PROPERTY expected = new SVB_CAMERA_PROPERTY() { MaxWidth = 96, MaxHeight = 200 };
            pinvoke.Setup(x => x.SVBGetCameraProperty(It.IsAny<int>(), out expected)).Verifiable();

            var id = 12;

            var sut = new SVBonySDK(id, pinvoke.Object);
            sut.Connect();

            pinvoke.Verify(x => x.SVBSetROIFormat(id, 0, 0, 96, 200, 1), Times.Once);
        }

        [Test]
        public void SetROI_NewROI_WithBinning_SetCorrectly() {
            var pinvoke = new Mock<ISVBonyPInvokeProxy>();
            int x = 0, y = 0, width = 96, height = 200, bin = 1;
            pinvoke.Setup(f => f.SVBGetROIFormat(It.IsAny<int>(), out x, out y, out width, out height, out bin)).Verifiable();

            var id = 12;

            var sut = new SVBonySDK(id, pinvoke.Object);
            sut.SetROI(10, 10, 80, 100, 2);

            pinvoke.Verify(f => f.SVBStopVideoCapture(id), Times.Once);
            pinvoke.Verify(f => f.SVBSetROIFormat(id, 5, 5, 40, 50, 2), Times.Once);
            pinvoke.Verify(f => f.SVBStartVideoCapture(id), Times.Once);
        }

        [Test]
        public void SetROI_ROIAlreadySetCorrectly_NotChanged() {
            var pinvoke = new Mock<ISVBonyPInvokeProxy>();
            int x = 0, y = 0, width = 96, height = 200, bin = 1;
            pinvoke.Setup(f => f.SVBGetROIFormat(It.IsAny<int>(), out x, out y, out width, out height, out bin)).Verifiable();

            var id = 12;

            var sut = new SVBonySDK(id, pinvoke.Object);
            sut.SetROI(0, 0, 96, 200, 1);
            sut.SetROI(0, 0, 96, 200, 1);
            sut.SetROI(0, 0, 96, 200, 1);

            pinvoke.Verify(f => f.SVBSetROIFormat(id, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Test]
        [TestCase(SVB_BOOL.SVB_TRUE, SVB_BAYER_PATTERN.SVB_BAYER_GB, SensorType.GBRG)]
        [TestCase(SVB_BOOL.SVB_TRUE, SVB_BAYER_PATTERN.SVB_BAYER_GR, SensorType.GRBG)]
        [TestCase(SVB_BOOL.SVB_TRUE, SVB_BAYER_PATTERN.SVB_BAYER_BG, SensorType.BGGR)]
        [TestCase(SVB_BOOL.SVB_TRUE, SVB_BAYER_PATTERN.SVB_BAYER_RG, SensorType.RGGB)]
        [TestCase(SVB_BOOL.SVB_FALSE, SVB_BAYER_PATTERN.SVB_BAYER_GB, SensorType.Monochrome)]
        [TestCase(SVB_BOOL.SVB_FALSE, SVB_BAYER_PATTERN.SVB_BAYER_GR, SensorType.Monochrome)]
        [TestCase(SVB_BOOL.SVB_FALSE, SVB_BAYER_PATTERN.SVB_BAYER_BG, SensorType.Monochrome)]
        [TestCase(SVB_BOOL.SVB_FALSE, SVB_BAYER_PATTERN.SVB_BAYER_RG, SensorType.Monochrome)]
        public void GetSensor_CorrectlyTranslated(SVB_BOOL colorcam, SVB_BAYER_PATTERN pattern, SensorType expectedSensor) {
            var pinvoke = new Mock<ISVBonyPInvokeProxy>();
            var prop = new SVB_CAMERA_PROPERTY() { IsColorCam = colorcam, BayerPattern = pattern };
            pinvoke.Setup(x => x.SVBGetCameraProperty(It.IsAny<int>(), out prop));

            var id = 12;

            var sut = new SVBonySDK(id, pinvoke.Object);
            var sensor = sut.GetSensorInfo();

            sensor.Should().Be(expectedSensor);
        }

        [Test]
        public void GetBinningInfo_CorrectlyReturned() {
            var pinvoke = new Mock<ISVBonyPInvokeProxy>();
            var prop = new SVB_CAMERA_PROPERTY() { SupportedBins = new int[] { 1, 4, 6, 0, 0, 0, 0, 3 } };
            pinvoke.Setup(x => x.SVBGetCameraProperty(It.IsAny<int>(), out prop));

            var id = 12;

            var sut = new SVBonySDK(id, pinvoke.Object);
            var binnings = sut.GetBinningInfo();

            binnings.Should().BeEquivalentTo(new int[] { 1, 4, 6 });
        }

        [Test]
        public async Task StartExposure_Successful_ReturnedData() {
            var pinvoke = new Mock<ISVBonyPInvokeProxy>();
            var id = 12;

            var sut = new SVBonySDK(id, pinvoke.Object);
            sut.Connect();
            sut.StartExposure(0.1, 80, 100);
            var data = await sut.GetExposure(0.1, 80, 100, default);

            pinvoke.Verify(x => x.SVBGetVideoDataMono16(id, It.IsAny<ushort[]>(), 80 * 100 * 2, (int)(0.1 * 2 * 100) + 500), Times.Once);
            pinvoke.Verify(x => x.SVBSendSoftTrigger(id), Times.Once);
            data.Should().NotBeEmpty();
            data.Length.Should().Be(80 * 100);
        }

        [Test]
        public async Task StartExposure_UnSuccessful_Cancelled_ReturnedNull() {
            var pinvoke = new Mock<ISVBonyPInvokeProxy>();
            var id = 12;
            pinvoke.Setup(x => x.SVBGetVideoDataMono16(id, It.IsAny<ushort[]>(), It.IsAny<int>(), It.IsAny<int>())).Returns(SVB_ERROR_CODE.SVB_ERROR_GENERAL_ERROR);

            var sut = new SVBonySDK(id, pinvoke.Object);
            sut.Connect();
            var cts = new CancellationTokenSource();
            cts.Cancel();
            sut.StartExposure(10, 80, 100);            
            var data = await sut.GetExposure(10, 80, 100, cts.Token);

            data.Should().BeNull();
        }

        [Test]
        public async Task StartExposure_UnSuccessful_NotConnected_ReturnedNull() {
            var pinvoke = new Mock<ISVBonyPInvokeProxy>();
            var id = 12;
            pinvoke.Setup(x => x.SVBGetVideoDataMono16(id, It.IsAny<ushort[]>(), It.IsAny<int>(), It.IsAny<int>())).Returns(SVB_ERROR_CODE.SVB_ERROR_GENERAL_ERROR);

            var sut = new SVBonySDK(id, pinvoke.Object);
            sut.StartExposure(0.1, 80, 100);
            var data = await sut.GetExposure(0.1, 80, 100, default);

            pinvoke.Verify(x => x.SVBGetVideoDataMono16(id, It.IsAny<ushort[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
            data.Should().BeNull();
        }

        [Test]
        public async Task StartExposure_UnSuccessful_UnexpectedException_ReturnedNull() {
            var pinvoke = new Mock<ISVBonyPInvokeProxy>();
            var id = 12;
            pinvoke.Setup(x => x.SVBGetVideoDataMono16(id, It.IsAny<ushort[]>(), It.IsAny<int>(), It.IsAny<int>())).Throws(new AccessViolationException());

            var sut = new SVBonySDK(id, pinvoke.Object);
            sut.StartExposure(0.1, 80, 100);
            var data = await sut.GetExposure(0.1, 80, 100, default);


            pinvoke.Verify(x => x.SVBGetVideoDataMono16(id, It.IsAny<ushort[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
            data.Should().BeNull();
        }
    }
}