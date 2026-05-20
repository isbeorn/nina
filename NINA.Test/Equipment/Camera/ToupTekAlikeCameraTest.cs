#region "copyright"

/*
    Copyright (c) 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using Moq;
using NINA.Core.Model.Equipment;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyCamera.ToupTekAlike;
using NINA.Equipment.Interfaces;
using NINA.Image.ImageData;
using NINA.Profile.Interfaces;
using NUnit.Framework;
using System;
using System.Text;
using System.Threading.Tasks;
using ToupTek;

namespace NINA.Test.Equipment.Camera {

    [TestFixture]
    public class ToupTekAlikeCameraTest {
        private ImageDataFactoryTestUtility dataFactoryUtility;

        [SetUp]
        public void Setup() {
            dataFactoryUtility = new ImageDataFactoryTestUtility();
        }

        [Test]
        public async Task Connect_WhenInitializationFails_ReturnsFalseAndClosesSdk() {
            var sdk = CreateConnectableSdk();
            sdk.Setup(x => x.put_Option(ToupTekAlikeOption.OPTION_BITDEPTH, 1)).Returns(false);
            var sut = CreateCamera(sdk.Object, CreateProfileService().Object);

            var connected = await sut.Connect(default);

            connected.Should().BeFalse();
            sut.Connected.Should().BeFalse();
            sdk.Verify(x => x.Close(), Times.Once);
        }

        [Test]
        public async Task Connect_WithValidStoredTecTarget_DoesNotScaleSetpointTwice() {
            var sdk = CreateConnectableSdk();
            var target = -100;
            sdk.Setup(x => x.get_Option(ToupTekAlikeOption.OPTION_TECTARGET, out target));
            var sut = CreateCamera(
                sdk.Object,
                CreateProfileService().Object,
                flags: ToupTekAlikeFlag.FLAG_TRIGGER_SOFTWARE | ToupTekAlikeFlag.FLAG_TEC_ONOFF);

            var connected = await sut.Connect(default);
            sut.Disconnect();

            connected.Should().BeTrue();
            sdk.Verify(x => x.put_Option(ToupTekAlikeOption.OPTION_TECTARGET, -100), Times.Once);
            sdk.Verify(x => x.put_Option(ToupTekAlikeOption.OPTION_TECTARGET, -1000), Times.Never);
        }

        [Test]
        public void FanSpeed_ClampsValueBeforeSendingToSdk() {
            var sdk = new Mock<IToupTekAlikeCameraSDK>();
            sdk.SetupGet(x => x.Category).Returns("ToupTek");
            var currentFanSpeed = 0;
            sdk.Setup(x => x.get_Option(ToupTekAlikeOption.OPTION_FAN, out currentFanSpeed));
            var sut = CreateCamera(sdk.Object, CreateProfileService().Object, maxFanSpeed: 5);

            sut.FanSpeed = 99;

            sdk.Verify(x => x.put_Option(ToupTekAlikeOption.OPTION_FAN, 5), Times.Once);
            sdk.Verify(x => x.put_Option(ToupTekAlikeOption.OPTION_FAN, 99), Times.Never);
        }

        [Test]
        public void SetBinning_ClampsValuesBelowOne() {
            var sdk = new Mock<IToupTekAlikeCameraSDK>();
            sdk.SetupGet(x => x.Category).Returns("ToupTek");
            sdk.Setup(x => x.put_Option(It.IsAny<ToupTekAlikeOption>(), It.IsAny<int>())).Returns(true);
            var sut = CreateCamera(sdk.Object, CreateProfileService().Object);

            sut.SetBinning(0, 0);

            sdk.Verify(x => x.put_Option(ToupTekAlikeOption.OPTION_BINNING, 1), Times.Once);
        }

        [Test]
        public void ReadoutModeSelections_CanBeSetBeforeConnect() {
            var sdk = new Mock<IToupTekAlikeCameraSDK>();
            sdk.SetupGet(x => x.Category).Returns("ToupTek");
            var sut = CreateCamera(sdk.Object, CreateProfileService().Object);

            sut.ReadoutModeForNormalImages = 0;
            sut.ReadoutModeForSnapImages = 99;

            sut.ReadoutModeForNormalImages.Should().Be(0);
            sut.ReadoutModeForSnapImages.Should().Be(0);
        }

        [Test]
        public async Task Connect_WithFanSupport_ExposesFanSpeedAction() {
            var sdk = CreateConnectableSdk();
            var sut = CreateCamera(sdk.Object, CreateProfileService().Object, maxFanSpeed: 5);

            var connected = await sut.Connect(default);

            connected.Should().BeTrue();
            sut.SupportedActions.Should().Contain("Fan Speed");
        }

        [Test]
        public void ToupTekEnumExtensions_ConvertBetweenSharedAndNativeEnums() {
            ToupTekAlikeOption.OPTION_RAW.ToToupTek().Should().Be(ToupCam.eOPTION.OPTION_RAW);
            ToupTekAlikeAAF.AAF_GETPOSITION.ToToupTek().Should().Be(ToupCam.eAAF.AAF_GETPOSITION);
            ToupCam.eEVENT.EVENT_IMAGE.ToEvent().Should().Be(ToupTekAlikeEvent.EVENT_IMAGE);
        }

        private ToupTekAlikeCamera CreateCamera(
            IToupTekAlikeCameraSDK sdk,
            IProfileService profileService,
            ToupTekAlikeFlag flags = ToupTekAlikeFlag.FLAG_TRIGGER_SOFTWARE,
            uint maxFanSpeed = 0) {
            return new ToupTekAlikeCamera(
                CreateDeviceInfo(flags, maxFanSpeed),
                sdk,
                profileService,
                dataFactoryUtility.ExposureDataFactory);
        }

        private static ToupTekAlikeDeviceInfo CreateDeviceInfo(
            ToupTekAlikeFlag flags = ToupTekAlikeFlag.FLAG_TRIGGER_SOFTWARE,
            uint maxFanSpeed = 0) {
            return new ToupTekAlikeDeviceInfo {
                displayname = "ToupTek Test Camera",
                id = @"vid_1234&pid_abcd#camera",
                model = new ToupTekAlikeModel {
                    flag = (ulong)flags,
                    maxfanspeed = maxFanSpeed,
                    xpixsz = 3.76f,
                    ypixsz = 3.76f
                }
            };
        }

        private static Mock<IProfileService> CreateProfileService() {
            var cameraSettings = new Mock<ICameraSettings>();
            cameraSettings.SetupProperty(x => x.BitScaling, false);
            cameraSettings.SetupProperty(x => x.BinAverageEnabled, false);
            cameraSettings.SetupProperty(x => x.TouptekAlikeDewHeaterStrength, -1);
            cameraSettings.SetupProperty(x => x.TouptekAlikeUltraMode, false);
            cameraSettings.SetupProperty(x => x.TouptekAlikeHighFullwell, false);
            cameraSettings.SetupProperty(x => x.TouptekAlikeLEDLights, false);

            var profile = new Mock<IProfile>();
            profile.SetupGet(x => x.CameraSettings).Returns(cameraSettings.Object);

            var profileService = new Mock<IProfileService>();
            profileService.SetupGet(x => x.ActiveProfile).Returns(profile.Object);
            return profileService;
        }

        private static Mock<IToupTekAlikeCameraSDK> CreateConnectableSdk() {
            var sdk = new Mock<IToupTekAlikeCameraSDK>();
            sdk.SetupGet(x => x.Category).Returns("ToupTek");
            sdk.Setup(x => x.Open(It.IsAny<string>())).Returns(sdk.Object);
            sdk.Setup(x => x.put_Option(It.IsAny<ToupTekAlikeOption>(), It.IsAny<int>())).Returns(true);
            sdk.Setup(x => x.put_AutoExpoEnable(false)).Returns(true);
            sdk.Setup(x => x.StartPullModeWithCallback(It.IsAny<ToupTekAlikeCallback>())).Returns(true);
            sdk.SetupGet(x => x.MonoMode).Returns(false);

            var width = 1920;
            var height = 1080;
            sdk.Setup(x => x.get_Size(out width, out height));

            var fourCC = BitConverter.ToUInt32(Encoding.ASCII.GetBytes("RGGB"), 0);
            uint bitDepth = 12;
            sdk.Setup(x => x.get_RawFormat(out fourCC, out bitDepth)).Returns(true);

            return sdk;
        }
    }
}
