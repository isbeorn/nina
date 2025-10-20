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
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Trigger.Platesolving;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.Trigger.Platesolving {

    [TestFixture]
    public class CenterAfterDriftTriggerTest {
        private Mock<IProfileService> profileServiceMock;
        private Mock<ITelescopeMediator> telescopeMediatorMock;
        private Mock<IGuiderMediator> guiderMediatorMock;
        private Mock<IImagingMediator> imagingMediatorMock;
        private Mock<IApplicationStatusMediator> applicationStatusMediatorMock;
        private Mock<IFilterWheelMediator> filterMediatorMock;
        private Mock<ICameraMediator> cameraMediatorMock;
        private Mock<IImageSaveMediator> imageSaveMediatorMock;
        private Mock<IDomeMediator> domeMediatorMock;
        private Mock<IDomeFollower> domeFollowerMock;
        private Mock<ISafetyMonitorMediator> safetyMonitorMediatorMock;

        [SetUp]
        public void Setup() {
            profileServiceMock = new Mock<IProfileService>();
            telescopeMediatorMock = new Mock<ITelescopeMediator>();
            guiderMediatorMock = new Mock<IGuiderMediator>();
            imagingMediatorMock = new Mock<IImagingMediator>();
            applicationStatusMediatorMock = new Mock<IApplicationStatusMediator>();
            filterMediatorMock = new Mock<IFilterWheelMediator>();
            cameraMediatorMock = new Mock<ICameraMediator>();
            imageSaveMediatorMock = new Mock<IImageSaveMediator>();
            domeMediatorMock = new Mock<IDomeMediator>();
            domeFollowerMock = new Mock<IDomeFollower>();
            safetyMonitorMediatorMock = new Mock<ISafetyMonitorMediator>();
        }

        [Test]
        [TestCase(3.8, 400, 1, 30)]
        [TestCase(3.8, 800, 1, 61)]
        [TestCase(3.8, 1600, 1, 122)]
        public void DistanceArcMinutes_CorrectlyCalculates_DistancePixels(double pixelsize, int focallength, double arcmin, double expectedPixels) {
            profileServiceMock.SetupGet(x => x.ActiveProfile.CameraSettings.PixelSize).Returns(pixelsize);
            profileServiceMock.SetupGet(x => x.ActiveProfile.TelescopeSettings.FocalLength).Returns(focallength);

            var sut = new CenterAfterDriftTrigger(
                profileServiceMock.Object,
                telescopeMediatorMock.Object,
                filterMediatorMock.Object,
                guiderMediatorMock.Object,
                imagingMediatorMock.Object,
                cameraMediatorMock.Object,
                domeMediatorMock.Object,
                domeFollowerMock.Object,
                imageSaveMediatorMock.Object,
                applicationStatusMediatorMock.Object,
                safetyMonitorMediatorMock.Object);

            sut.DistanceArcMinutes = arcmin;

            sut.DistancePixels.Should().BeApproximately(expectedPixels, 1);
        }
    }
}