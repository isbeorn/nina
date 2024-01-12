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
using NINA.Sequencer.Trigger.Guider;
using NINA.Equipment.Interfaces.Mediator;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NINA.Equipment.Equipment.MyGuider;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.Model;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;

namespace NINA.Test.Sequencer.Trigger.Guider {

    [TestFixture]
    public class RestoreGuidingTest {
        private Mock<IGuiderMediator> guiderMediatorMock;

        [SetUp]
        public void Setup() {
            guiderMediatorMock = new Mock<IGuiderMediator>();
            guiderMediatorMock.Setup(x => x.GetInfo()).Returns(new GuiderInfo() { Connected = true });
        }

        [Test]
        public void CloneTest() {
            var initial = new RestoreGuiding(guiderMediatorMock.Object);
            initial.Icon = new System.Windows.Media.GeometryGroup();

            var sut = (RestoreGuiding)initial.Clone();

            sut.Should().NotBeSameAs(initial);
            sut.Icon.Should().BeSameAs(initial.Icon);
        }

        [Test]
        public async Task ExecuteTest() {
            var sut = new RestoreGuiding(guiderMediatorMock.Object);
            await sut.Execute(default, default, default);

            guiderMediatorMock.Verify(x => x.StartGuiding(false, It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void InitializeTest() {
            var sut = new RestoreGuiding(guiderMediatorMock.Object);
            sut.SequenceBlockStarted();

            Assert.Pass();
        }

        [Test]
        public void ShouldTrigger_NextItemLightExposure() {
            var sut = new RestoreGuiding(guiderMediatorMock.Object);

            var profileServiceMock = new Mock<IProfileService>();
            var cameraMediatorMock = new Mock<ICameraMediator>();
            var imagingMediatorMock = new Mock<IImagingMediator>();
            var imageSaveMediatorMock = new Mock<IImageSaveMediator>();
            var historyMock = new Mock<IImageHistoryVM>();
            var takeDarkExposureItem = new TakeExposure(profileServiceMock.Object, cameraMediatorMock.Object, imagingMediatorMock.Object, imageSaveMediatorMock.Object, historyMock.Object) {
                ImageType = "DARK"
            };
            var takeLightExposureItem = new TakeExposure(profileServiceMock.Object, cameraMediatorMock.Object, imagingMediatorMock.Object, imageSaveMediatorMock.Object, historyMock.Object) {
                ImageType = "LIGHT"
            };

            var test1 = sut.ShouldTrigger(null, null);
            var test2 = sut.ShouldTrigger(null, takeDarkExposureItem);
            var test3 = sut.ShouldTrigger(null, takeLightExposureItem);

            test1.Should().BeFalse();
            test2.Should().BeFalse();
            test3.Should().BeTrue();
        }
    }
}