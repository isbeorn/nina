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
using NINA.Core.Model.Equipment;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Trigger.Platesolving;
using NINA.WPF.Base.Interfaces.Mediator;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.Trigger.Platesolving {

    [TestFixture]
    public class PlatesolvingImageFollowerTest {

        /// <summary>
        /// Verifies the Before Image Saved Ignores Non Light Frames And Counts Light Frames Until Threshold scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task BeforeImageSaved_IgnoresNonLightFramesAndCountsLightFramesUntilThreshold() {
            Mock<IImageSaveMediator> imageSaveMediatorMock = new Mock<IImageSaveMediator>();
            Func<object, BeforeImageSavedEventArgs, Task> handler = null;
            imageSaveMediatorMock
                .SetupAdd(x => x.BeforeImageSaved += It.IsAny<Func<object, BeforeImageSavedEventArgs, Task>>())
                .Callback<Func<object, BeforeImageSavedEventArgs, Task>>(h => handler += h);
            imageSaveMediatorMock
                .SetupRemove(x => x.BeforeImageSaved -= It.IsAny<Func<object, BeforeImageSavedEventArgs, Task>>())
                .Callback<Func<object, BeforeImageSavedEventArgs, Task>>(h => handler -= h);
            Mock<ITelescopeMediator> telescopeMediatorMock = new Mock<ITelescopeMediator>();
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo { Connected = false });
            PlatesolvingImageFollower sut = new PlatesolvingImageFollower(
                Mock.Of<IProfileService>(),
                telescopeMediatorMock.Object,
                imageSaveMediatorMock.Object,
                Mock.Of<IApplicationStatusMediator>()) {
                AfterExposures = 3
            };

            await handler.Invoke(this, CreateArgs("DARK"));
            sut.ProgressExposures.Should().Be(0);

            await handler.Invoke(this, CreateArgs("LIGHT"));
            await handler.Invoke(this, CreateArgs("LIGHT"));

            sut.ProgressExposures.Should().Be(2);

            sut.Dispose();

            handler.Should().BeNull();
            imageSaveMediatorMock.VerifyAdd(x => x.BeforeImageSaved += It.IsAny<Func<object, BeforeImageSavedEventArgs, Task>>(), Times.Once);
            imageSaveMediatorMock.VerifyRemove(x => x.BeforeImageSaved -= It.IsAny<Func<object, BeforeImageSavedEventArgs, Task>>(), Times.Once);
        }

        /// <summary>
        /// Verifies the Properties Raise Change Notifications scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void Properties_RaiseChangeNotifications() {
            PlatesolvingImageFollower sut = new PlatesolvingImageFollower(
                Mock.Of<IProfileService>(),
                Mock.Of<ITelescopeMediator>(),
                Mock.Of<IImageSaveMediator>(),
                Mock.Of<IApplicationStatusMediator>());
            using (sut) {
                using FluentAssertions.Events.IMonitor<PlatesolvingImageFollower> monitor = sut.Monitor();

                sut.AfterExposures = 4;
                sut.ProgressExposures = 2;
                sut.LastCoordinates = new NINA.Astrometry.Coordinates(
                    NINA.Astrometry.Angle.ByHours(1),
                    NINA.Astrometry.Angle.ByDegree(2),
                    NINA.Astrometry.Epoch.J2000);

                sut.AfterExposures.Should().Be(4);
                sut.ProgressExposures.Should().Be(2);
                sut.LastCoordinates.Should().NotBeNull();
                monitor.Should().RaisePropertyChangeFor(x => x.AfterExposures);
                monitor.Should().RaisePropertyChangeFor(x => x.ProgressExposures);
                monitor.Should().RaisePropertyChangeFor(x => x.LastCoordinates);
            }
        }

        private static BeforeImageSavedEventArgs CreateArgs(string imageType) {
            Mock<IImageData> imageMock = new Mock<IImageData>();
            imageMock.SetupGet(x => x.MetaData).Returns(new ImageMetaData {
                Image = new ImageParameter { ImageType = imageType }
            });
            return new BeforeImageSavedEventArgs(imageMock.Object, Task.FromResult<IRenderedImage>(null));
        }
    }
}
