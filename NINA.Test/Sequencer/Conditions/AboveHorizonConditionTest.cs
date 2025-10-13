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
using NINA.Profile.Interfaces;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Core.Utility;
using NINA.Astrometry;
using NUnit.Framework;
using System;
using System.IO;
using NINA.Core.Model;
using NINA.Sequencer.Interfaces;

namespace NINA.Test.Sequencer.Conditions {

    [TestFixture]
    public class AboveHorizonConditionTest {
        private Mock<IProfileService> profileServiceMock;

        [SetUp]
        public void Setup() {
            profileServiceMock = new Mock<IProfileService>();
            profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Latitude).Returns(0);
            profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Longitude).Returns(0);
        }

        [Test]
        public void AboveHorizonConditionTest_Clone_GoodClone() {
            var sut = new AboveHorizonCondition(profileServiceMock.Object);
            sut.Icon = new System.Windows.Media.GeometryGroup();
            sut.Data.Coordinates = new InputCoordinates(new Coordinates(20, 20, Epoch.J2000, Coordinates.RAType.Degrees));
            sut.Data.Offset = 10;
            var item2 = (AboveHorizonCondition)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Icon.Should().BeSameAs(sut.Icon);
            item2.Data.Offset.Should().Be(10);
            item2.Data.Coordinates.Should().NotBeSameAs(sut.Data.Coordinates);
            item2.Data.Coordinates.Coordinates.RA.Should().Be(sut.Data.Coordinates.Coordinates.RA);
            item2.Data.Coordinates.Coordinates.Dec.Should().Be(sut.Data.Coordinates.Coordinates.Dec);
        }

        [Test]
        public void StandardHorizon_AboveHorizon_CheckTrue() {
            var sut = new AboveHorizonCondition(profileServiceMock.Object);
            var mockDateProvider = new Mock<ICustomDateTime>();
            var coordinates = new Coordinates(Angle.ByDegree(1), Angle.ByDegree(2), Epoch.J2000, mockDateProvider.Object);
            sut.Data.Coordinates.Coordinates = coordinates;
            DateTime testDate = DateTime.ParseExact("20191231T23:00:00Z", "yyyyMMddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture);
            sut.Data.SetTargetAltitudeWithHorizon(testDate);
            mockDateProvider.SetupSequence(x => x.Now).Returns(testDate).Returns(testDate).Returns(testDate);
            sut.DateTime = mockDateProvider.Object;
            sut.Check(default, default).Should().BeTrue();
        }

        [Test]
        public void StandardHorizon_AboveHorizon_CheckFalse() {
            var sut = new AboveHorizonCondition(profileServiceMock.Object);
            var mockDateProvider = new Mock<ICustomDateTime>();
            mockDateProvider.SetupGet(x => x.Now).Returns(new DateTime(2020, 1, 1, 1, 0, 0));
            var coordinates = new Coordinates(Angle.ByDegree(1), Angle.ByDegree(2), Epoch.J2000, mockDateProvider.Object);
            sut.DateTime = mockDateProvider.Object;

            sut.Data.Coordinates.Coordinates = coordinates; 
            sut.Data.CurrentAltitude = 0;
            sut.CalculateExpectedTime(new DateTime(2020, 1, 1, 1, 0, 0));

            sut.Check(default, default).Should().BeFalse();
        }

        [Test]
        public void CustomHorizon_AboveHorizon_CheckTrue() {
            AboveHorizonCondition sut = new AboveHorizonCondition(profileServiceMock.Object);
            var horizonDefinition = $"20 20" + Environment.NewLine + "100 20";
            using (var sr = new StringReader(horizonDefinition)) {
                sut.Data.Horizon = CustomHorizon.FromReader_Standard(sr);
            }

            var mockDateProvider = new Mock<ICustomDateTime>();
            DateTime time = DateTime.ParseExact("20200101T22:00:00Z", "yyyyMMddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture);
            mockDateProvider.SetupSequence(x => x.Now).Returns(time).Returns(time).Returns(time);
            var coordinates = new Coordinates(Angle.ByDegree(1), Angle.ByDegree(2), Epoch.J2000, mockDateProvider.Object);

            sut.Data.Coordinates.Coordinates = coordinates; 
            sut.Data.CurrentAltitude = 0;
            sut.Data.SetTargetAltitudeWithHorizon(time);
            sut.DateTime = mockDateProvider.Object;
            sut.Check(default, default).Should().BeTrue();
        }

        [Test]
        public void CustomHorizon_AboveHorizon_CheckFalse() {
            var horizonDefinition = $"20 20" + Environment.NewLine + "100 20";
            using (var sr = new StringReader(horizonDefinition)) {
                var horizon = CustomHorizon.FromReader_Standard(sr);
                profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Horizon).Returns(horizon);
            }

            var sut = new AboveHorizonCondition(profileServiceMock.Object);
            var mockDateProvider = new Mock<ICustomDateTime>();
            mockDateProvider.SetupGet(x => x.Now).Returns(new DateTime(2020, 1, 1, 0, 0, 0));
            sut.DateTime = mockDateProvider.Object;
            var coordinates = new Coordinates(Angle.ByDegree(1), Angle.ByDegree(2), Epoch.J2000, mockDateProvider.Object);
            sut.Data.Coordinates.Coordinates = coordinates;
            DateTime time = new DateTime(2020, 1, 1, 0, 0, 0);
            sut.Data.SetTargetAltitudeWithHorizon(time);

            sut.Check(default, default).Should().BeFalse();
        }

        [Test]
        [TestCase(-21, true)]
        [TestCase(-10, false)]
        public void CustomHorizon_AboveHorizon_WithOffset_CheckFalse(int offset, bool expected) {
            var horizonDefinition = $"20 20" + Environment.NewLine + "100 20";
            using (var sr = new StringReader(horizonDefinition)) {
                var horizon = CustomHorizon.FromReader_Standard(sr);
                profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Horizon).Returns(horizon);
            }

            var sut = new AboveHorizonCondition(profileServiceMock.Object);
            sut.Data.Offset = offset;
            var mockDateProvider = new Mock<ICustomDateTime>();
            var date = new DateTime(2020, 1, 1, 23, 0, 0);
            date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
            mockDateProvider.SetupSequence(x => x.Now).Returns(date).Returns(date);
            var coordinates = new Coordinates(Angle.ByDegree(1), Angle.ByDegree(2), Epoch.J2000, mockDateProvider.Object);
            sut.Data.Coordinates.Coordinates = coordinates;
            sut.DateTime = mockDateProvider.Object;
            sut.Data.SetTargetAltitudeWithHorizon(date);
            sut.Check(default, default).Should().Be(expected);
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

            var sut = new AboveHorizonCondition(profileServiceMock.Object);
            sut.AttachNewParent(parentMock.Object);

            sut.Data.Coordinates.Coordinates.RA.Should().Be(coordinates.RA);
            sut.Data.Coordinates.Coordinates.Dec.Should().Be(coordinates.Dec);
        }

        [Test]
        public void AboveHorizonCondition_ToString() {
            var sut = new AboveHorizonCondition(profileServiceMock.Object);

            sut.ToString().Should().Be("Condition: AboveHorizonCondition");
        }

        [Test]
        public void AboveHorizonCondition_AfterParentChanged_NoParent_WatchdogNotStarted() {
            var watchdogMock = new Mock<IConditionWatchdog>();

            var sut = new AboveHorizonCondition(profileServiceMock.Object);
            sut.ConditionWatchdog = watchdogMock.Object;

            sut.AfterParentChanged();

            watchdogMock.Verify(x => x.Start(), Times.Never);
            watchdogMock.Verify(x => x.Cancel(), Times.Once);
        }

        [Test]
        public void AboveHorizonCondition_AfterParentChanged_NotInRootContainer_WatchdogNotStarted() {
            var watchdogMock = new Mock<IConditionWatchdog>();
            var parentMock = new Mock<ISequenceContainer>();

            var sut = new AboveHorizonCondition(profileServiceMock.Object);
            sut.ConditionWatchdog = watchdogMock.Object;
            sut.Parent = parentMock.Object;

            sut.AfterParentChanged();

            watchdogMock.Verify(x => x.Start(), Times.Never);
            watchdogMock.Verify(x => x.Cancel(), Times.Once);
        }

        [Test]
        public void AboveHorizonCondition_AfterParentChanged_InRootContainer_WatchdogStarted() {
            var watchdogMock = new Mock<IConditionWatchdog>();
            var parentMock = new Mock<ISequenceRootContainer>();

            var sut = new AboveHorizonCondition(profileServiceMock.Object);
            sut.ConditionWatchdog = watchdogMock.Object;
            sut.Parent = parentMock.Object;

            sut.AfterParentChanged();

            watchdogMock.Verify(x => x.Start(), Times.Once);
            watchdogMock.Verify(x => x.Cancel(), Times.Never);
        }

        [Test]
        public void AboveHorizonCondition_OnDeserialized_NoParent_WatchdogNotStarted() {
            var watchdogMock = new Mock<IConditionWatchdog>();

            var sut = new AboveHorizonCondition(profileServiceMock.Object);
            sut.ConditionWatchdog = watchdogMock.Object;

            sut.OnDeserialized(default);

            watchdogMock.Verify(x => x.Start(), Times.Never);
            watchdogMock.Verify(x => x.Cancel(), Times.Once);
        }

        [Test]
        public void AboveHorizonCondition_OnDeserialized_NotInRootContainer_WatchdogNotStarted() {
            var watchdogMock = new Mock<IConditionWatchdog>();
            var parentMock = new Mock<ISequenceContainer>();

            var sut = new AboveHorizonCondition(profileServiceMock.Object);
            sut.ConditionWatchdog = watchdogMock.Object;
            sut.Parent = parentMock.Object;

            sut.OnDeserialized(default);

            watchdogMock.Verify(x => x.Start(), Times.Never);
            watchdogMock.Verify(x => x.Cancel(), Times.Once);
        }

        [Test]
        public void AboveHorizonCondition_OnDeserialized_InRootContainer_WatchdogStarted() {
            var watchdogMock = new Mock<IConditionWatchdog>();
            var parentMock = new Mock<ISequenceRootContainer>();

            var sut = new AboveHorizonCondition(profileServiceMock.Object);
            sut.ConditionWatchdog = watchdogMock.Object;
            sut.Parent = parentMock.Object;

            sut.OnDeserialized(default);

            watchdogMock.Verify(x => x.Start(), Times.Once);
            watchdogMock.Verify(x => x.Cancel(), Times.Never);
        }
    }
}