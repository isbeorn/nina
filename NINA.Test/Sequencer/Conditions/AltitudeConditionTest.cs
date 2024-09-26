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
using NINA.Astrometry;
using NUnit.Framework;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.Container;
using NUnit.Framework.Legacy;

namespace NINA.Test.Sequencer.Conditions {

    [TestFixture]
    public class AltitudeConditionTest {
        private Mock<IProfileService> profileServiceMock;

        [SetUp]
        public void Setup() {
            profileServiceMock = new Mock<IProfileService>();
            profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Latitude).Returns(0);
            profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Longitude).Returns(0);
        }

        [Test]
        public void AltitudeConditionTest_Clone_GoodClone() {
            var sut = new AltitudeCondition(profileServiceMock.Object);
            sut.Icon = new System.Windows.Media.GeometryGroup();
            var item2 = (AltitudeCondition)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Icon.Should().BeSameAs(sut.Icon);
            item2.Data.Offset.Should().Be(sut.Data.Offset);
        }

        [Test]
        [TestCase(30, 30)]
        [TestCase(40, 35)]
        [TestCase(35, 40)]
        public void Check_When_Scope_Pointing_East_Returns_True(double currentAltitude, double targetAltitude) {
            var altaz = new TopocentricCoordinates(Angle.ByDegree(90), Angle.ByDegree(currentAltitude), Angle.Zero, Angle.Zero, 0);
            var coords = altaz.Transform(Epoch.J2000);

            var sut = new AltitudeCondition(profileServiceMock.Object);
            sut.Data.Coordinates.Coordinates = coords;
            sut.Data.Offset = targetAltitude;

            ClassicAssert.IsTrue(
                sut.Check(null, null));
        }

        [Test]
        [TestCase(30, 30)]
        [TestCase(40, 35)]
        [TestCase(0, 0)]
        [TestCase(10, 0)]
        [TestCase(-10, -11)]
        [TestCase(-10, -10)]
        public void Check_When_Scope_Is_Pointing_West_Above_Target_Alt_Returns_True(double currentAltitude, double targetAltitude) {
            var altaz = new TopocentricCoordinates(Angle.ByDegree(270), Angle.ByDegree(currentAltitude), Angle.Zero, Angle.Zero, 0);
            var coords = altaz.Transform(Epoch.J2000);

            var sut = new AltitudeCondition(profileServiceMock.Object);
            sut.Data.Coordinates.Coordinates = coords;
            sut.Data.Offset = targetAltitude;

            ClassicAssert.IsTrue(
                sut.Check(null, null));
        }

        [Test]
        [TestCase(29, 30)]
        [TestCase(-10, 0)]
        [TestCase(-20, -10)]
        public void Check_When_Scope_Is_Pointing_West_Below_Target_Alt_Returns_False(double currentAltitude, double targetAltitude) {
            var altaz = new TopocentricCoordinates(Angle.ByDegree(270), Angle.ByDegree(currentAltitude), Angle.Zero, Angle.Zero, 0);
            var coords = altaz.Transform(Epoch.J2000);

            var sut = new AltitudeCondition(profileServiceMock.Object);
            sut.Data.Coordinates.Coordinates = coords;
            sut.Data.Offset = targetAltitude;
            ClassicAssert.IsFalse(
                sut.Check(null, null));
        }

        [Test]
        public void ToString_Test() {
            var sut = new AltitudeCondition(profileServiceMock.Object);
            sut.Data.Offset = 30;
            sut.ToString().Should().Be("Condition: AltitudeCondition, Altitude >= 30");
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

            var sut = new AltitudeCondition(profileServiceMock.Object);
            sut.AttachNewParent(parentMock.Object);

            sut.HasDsoParent.Should().BeTrue();
            sut.Data.Coordinates.Coordinates.RA.Should().Be(coordinates.RA);
            sut.Data.Coordinates.Coordinates.Dec.Should().Be(coordinates.Dec);
        }

        [Test]
        public void AfterParentChanged_NoParent_WatchdogNotStarted() {
            var watchdogMock = new Mock<IConditionWatchdog>();

            var sut = new AltitudeCondition(profileServiceMock.Object);
            sut.ConditionWatchdog = watchdogMock.Object;

            sut.AfterParentChanged();

            watchdogMock.Verify(x => x.Start(), Times.Never);
            watchdogMock.Verify(x => x.Cancel(), Times.Once);
        }

        [Test]
        public void AfterParentChanged_NotInRootContainer_WatchdogNotStarted() {
            var watchdogMock = new Mock<IConditionWatchdog>();
            var parentMock = new Mock<ISequenceContainer>();

            var sut = new AltitudeCondition(profileServiceMock.Object);
            sut.ConditionWatchdog = watchdogMock.Object;
            sut.Parent = parentMock.Object;

            sut.AfterParentChanged();

            watchdogMock.Verify(x => x.Start(), Times.Never);
            watchdogMock.Verify(x => x.Cancel(), Times.Once);
        }

        [Test]
        public void AfterParentChanged_InRootContainer_WatchdogStarted() {
            var watchdogMock = new Mock<IConditionWatchdog>();
            var parentMock = new Mock<ISequenceRootContainer>();

            var sut = new AltitudeCondition(profileServiceMock.Object);
            sut.ConditionWatchdog = watchdogMock.Object;
            sut.Parent = parentMock.Object;

            sut.AfterParentChanged();

            watchdogMock.Verify(x => x.Start(), Times.Once);
            watchdogMock.Verify(x => x.Cancel(), Times.Never);
        }

        [Test]
        public void OnDeserialized_NoParent_WatchdogNotStarted() {
            var watchdogMock = new Mock<IConditionWatchdog>();

            var sut = new AltitudeCondition(profileServiceMock.Object);
            sut.ConditionWatchdog = watchdogMock.Object;

            sut.OnDeserialized(default);

            watchdogMock.Verify(x => x.Start(), Times.Never);
            watchdogMock.Verify(x => x.Cancel(), Times.Once);
        }

        [Test]
        public void OnDeserialized_NotInRootContainer_WatchdogNotStarted() {
            var watchdogMock = new Mock<IConditionWatchdog>();
            var parentMock = new Mock<ISequenceContainer>();

            var sut = new AltitudeCondition(profileServiceMock.Object);
            sut.ConditionWatchdog = watchdogMock.Object;
            sut.Parent = parentMock.Object;

            sut.OnDeserialized(default);

            watchdogMock.Verify(x => x.Start(), Times.Never);
            watchdogMock.Verify(x => x.Cancel(), Times.Once);
        }

        [Test]
        public void OnDeserialized_InRootContainer_WatchdogStarted() {
            var watchdogMock = new Mock<IConditionWatchdog>();
            var parentMock = new Mock<ISequenceRootContainer>();

            var sut = new AltitudeCondition(profileServiceMock.Object);
            sut.ConditionWatchdog = watchdogMock.Object;
            sut.Parent = parentMock.Object;

            sut.OnDeserialized(default);

            watchdogMock.Verify(x => x.Start(), Times.Once);
            watchdogMock.Verify(x => x.Cancel(), Times.Never);
        }
    }
}