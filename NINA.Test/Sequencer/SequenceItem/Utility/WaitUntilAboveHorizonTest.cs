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
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem.Utility;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.SequenceItem.Utility {

    [TestFixture]
    public class WaitUntilAboveHorizonTest {
        private Mock<IProfileService> profileServiceMock;
        private WaitUntilAboveHorizon sut;

        [SetUp]
        public void Setup() {
            profileServiceMock = new Mock<IProfileService>();
            profileServiceMock.Setup(m => m.ActiveProfile.AstrometrySettings).Returns(new AstrometrySettings() {
                Latitude = 47,
                Longitude = 8
            });
            sut = new WaitUntilAboveHorizon(profileServiceMock.Object);
            sut.UpdateInterval = 0;
        }

        [Test]
        public void WaitUntilAboveHorizon_Clone_GoodClone() {
            sut.Icon = new System.Windows.Media.GeometryGroup();
            sut.Data.Coordinates = new InputCoordinates(new Coordinates(20, 20, Epoch.J2000, Coordinates.RAType.Degrees));
            sut.Data.Offset = 10;

            var item2 = (WaitUntilAboveHorizon)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Name.Should().BeSameAs(sut.Name);
            item2.Description.Should().BeSameAs(sut.Description);
            item2.Icon.Should().BeSameAs(sut.Icon);
            item2.Data.Offset.Should().Be(10);
            item2.Data.Coordinates.Should().NotBeSameAs(sut.Data.Coordinates);
            item2.Data.Coordinates.Coordinates.RA.Should().Be(sut.Data.Coordinates.Coordinates.RA);
            item2.Data.Coordinates.Coordinates.Dec.Should().Be(sut.Data.Coordinates.Coordinates.Dec);
        }

        [Test]
        public void WaitUntilAboveHorizonTest_GetEstimatedDuration_Test() {
            var estimate = sut.GetEstimatedDuration();

            estimate.Should().Be(TimeSpan.Zero);
        }

        [Test]
        public void AttachNewParent_HasDSOContainerParent_RetrieveParentCoordinates() {
            var coordinates = new Coordinates(Angle.ByDegree(1), Angle.ByDegree(2), Epoch.J2000);
            var parentMock = new Mock<IDeepSkyObjectContainer>();
            parentMock
                .SetupGet(x => x.Target)
                .Returns(
                new InputTarget(Angle.ByDegree(1), Angle.ByDegree(2), null) {
                    InputCoordinates = new InputCoordinates() {
                        Coordinates = coordinates
                    }
                }
            );

            //var sut = new WaitUntilAboveHorizon(profileServiceMock.Object);
            sut.AttachNewParent(parentMock.Object);

            sut.Data.Coordinates.Coordinates.RA.Should().Be(coordinates.RA);
            sut.Data.Coordinates.Coordinates.Dec.Should().Be(coordinates.Dec);
        }

        [Test]
        public async Task StandardHorizon_TargetIsAboveHorizon_ItemExecuted() {
            var mockDateProvider = new Mock<ICustomDateTime>();
            mockDateProvider.SetupGet(x => x.Now).Returns(DateTime.ParseExact("20191231T23:00:00Z", "yyyyMMddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture));
            var coordinates = new Coordinates(Angle.ByDegree(1), Angle.ByDegree(2), Epoch.J2000, mockDateProvider.Object);

            sut.Data.Coordinates.Coordinates = coordinates;
            sut.Data.Offset = 0;

            using (var cts = new CancellationTokenSource()) {
                cts.CancelAfter(1000);
                await sut.Run(default, cts.Token);
            }

            sut.Status.Should().Be(SequenceEntityStatus.FINISHED);
            mockDateProvider.VerifyGet(x => x.Now, Times.Exactly(2));
        }

        [Test]
        public async Task StandardHorizon_TargetStartsBelowHorizon_RisesAboveHorizon_ItemExecuted() {
            var mockDateProvider = new Mock<ICustomDateTime>();
            mockDateProvider.SetupSequence(x => x.Now)
                .Returns(DateTime.ParseExact("20200101T10:00:00Z", "yyyyMMddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture))
                .Returns(DateTime.ParseExact("20200101T10:30:00Z", "yyyyMMddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture))
                .Returns(DateTime.ParseExact("20200101T11:00:00Z", "yyyyMMddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture));
            var coordinates = new Coordinates(Angle.ByDegree(1), Angle.ByDegree(2), Epoch.J2000, mockDateProvider.Object);

            sut.Data.Coordinates.Coordinates = coordinates;
            sut.Data.Offset = 0;

            using (var cts = new CancellationTokenSource()) {
                cts.CancelAfter(1000);
                await sut.Run(default, cts.Token);
            }

            sut.Status.Should().Be(SequenceEntityStatus.FINISHED);
            mockDateProvider.VerifyGet(x => x.Now, Times.Exactly(3));
        }

        [Test]
        public async Task CustomHorizon_TargetIsAboveHorizon_ItemExecuted() {
            var horizonDefinition = $"20 20" + Environment.NewLine + "100 20";
            using (var sr = new StringReader(horizonDefinition)) {
                var horizon = CustomHorizon.FromReader_Standard(sr);
                profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Horizon).Returns(horizon);
            }

            var mockDateProvider = new Mock<ICustomDateTime>();
            mockDateProvider.SetupGet(x => x.Now).Returns(DateTime.ParseExact("20200101T22:00:00Z", "yyyyMMddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture));
            var coordinates = new Coordinates(Angle.ByDegree(1), Angle.ByDegree(2), Epoch.J2000, mockDateProvider.Object);

            sut.Data.Coordinates.Coordinates = coordinates;
            sut.Coordinates = sut.Data.Coordinates;
            sut.Data.Offset = 0;

            using (var cts = new CancellationTokenSource()) {
                cts.CancelAfter(1000);
                await sut.Run(default, cts.Token);
            }

            sut.Status.Should().Be(SequenceEntityStatus.FINISHED);
            mockDateProvider.VerifyGet(x => x.Now, Times.Exactly(2));
        }

        [Test]
        public async Task CustomHorizon_TargetStartsBelowHorizon_RisesAboveHorizon_ItemExecuted() {
            var horizonDefinition = $"20 20" + Environment.NewLine + "100 20";
            using (var sr = new StringReader(horizonDefinition)) {
                sut.Data.Horizon = CustomHorizon.FromReader_Standard(sr);
            }

            var mockDateProvider = new Mock<ICustomDateTime>();
            mockDateProvider.SetupSequence(x => x.Now)
                .Returns(DateTime.ParseExact("20200101T11:00:00Z", "yyyyMMddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture))
                .Returns(DateTime.ParseExact("20200101T11:30:00Z", "yyyyMMddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture))
                .Returns(DateTime.ParseExact("20200101T12:00:00Z", "yyyyMMddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture))
                .Returns(DateTime.ParseExact("20200101T13:00:00Z", "yyyyMMddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture));
            var coordinates = new Coordinates(Angle.ByDegree(1), Angle.ByDegree(2), Epoch.J2000, mockDateProvider.Object);

            sut.Data.Coordinates.Coordinates = coordinates;
            sut.Data.Offset = 0;

            await sut.Run(default, default);

            sut.Status.Should().Be(SequenceEntityStatus.FINISHED);
            mockDateProvider.VerifyGet(x => x.Now, Times.Exactly(4));
        }
    }
}