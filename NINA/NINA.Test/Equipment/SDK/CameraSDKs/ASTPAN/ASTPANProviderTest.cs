using FluentAssertions;
using Moq;
using NINA.Equipment.SDK.CameraSDKs.ASTPANSDK;
using NINA.Profile.Interfaces;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Test.Equipment.SDK.CameraSDKs.ASTPAN {
    [TestFixture]
    public class ASTPANProviderTest {
        private ImageDataFactoryTestUtility dataFactoryUtility;

        [SetUp]
        public void Setup() {
            dataFactoryUtility = new ImageDataFactoryTestUtility();
        }

        [Test]
        public void GetEquipment_NoCamerasFound_ReturnEmptyList() {
            var proxy = new Mock<IASTPANPInvokeProxy>();
            var count = 0;
            proxy.Setup(x => x.ASTPANGetNumOfCameras(out count)).Returns(0);
            var profile = new Mock<IProfileService>();

            var sut = new ASTPANProvider(profile.Object, dataFactoryUtility.ExposureDataFactory, proxy.Object);

            var cameras = sut.GetEquipment();

            cameras.Should().NotBeNull();
            cameras.Should().BeEmpty();
        }

        [Test]
        public void GetEquipment_TwoCamerasFound_ReturnTwo() {
            var proxy = new Mock<IASTPANPInvokeProxy>();
            var count = 2;
            proxy.Setup(x => x.ASTPANGetNumOfCameras(out count)).Returns(0);
            var profile = new Mock<IProfileService>();

            var sut = new ASTPANProvider(profile.Object, dataFactoryUtility.ExposureDataFactory, proxy.Object);

            var cameras = sut.GetEquipment();

            cameras.Should().HaveCount(2);
            ASTPAN_CAMERA_INFO dummy;
            proxy.Verify(x => x.ASTPANGetCameraInfo(out dummy, 0), Times.Once);
            proxy.Verify(x => x.ASTPANGetCameraInfo(out dummy, 1), Times.Once);
        }
    }
}
