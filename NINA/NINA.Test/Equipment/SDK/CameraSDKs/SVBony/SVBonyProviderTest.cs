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
using NINA.Equipment.SDK.CameraSDKs.SVBonySDK;
using NINA.Profile.Interfaces;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Test.Equipment.SDK.CameraSDKs.SVBony {

    [TestFixture]
    public class SVBonyProviderTest {
        private ImageDataFactoryTestUtility dataFactoryUtility;

        [SetUp]
        public void Setup() {
            dataFactoryUtility = new ImageDataFactoryTestUtility();
        }

        [Test]
        public void GetEquipment_NoCamerasFound_ReturnEmptyList() {
            var proxy = new Mock<ISVBonyPInvokeProxy>();
            proxy.Setup(x => x.SVBGetNumOfConnectedCameras()).Returns(0);
            var profile = new Mock<IProfileService>();

            var sut = new SVBonyProvider(profile.Object, dataFactoryUtility.ExposureDataFactory, proxy.Object);

            var cameras = sut.GetEquipment();

            cameras.Should().NotBeNull();
            cameras.Should().BeEmpty();
        }

        [Test]
        public void GetEquipment_TwoCamerasFound_ReturnTwo() {
            var proxy = new Mock<ISVBonyPInvokeProxy>();
            proxy.Setup(x => x.SVBGetNumOfConnectedCameras()).Returns(2);
            var profile = new Mock<IProfileService>();

            var sut = new SVBonyProvider(profile.Object, dataFactoryUtility.ExposureDataFactory, proxy.Object);

            var cameras = sut.GetEquipment();

            cameras.Should().HaveCount(2);
            proxy.Verify(x => x.GetCameraInfo(0), Times.Once);
            proxy.Verify(x => x.GetCameraInfo(1), Times.Once);
            proxy.Verify(x => x.GetSDKVersion(), Times.Exactly(2));
        }
    }
}