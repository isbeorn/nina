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
using System.Collections.Generic;
using System.Globalization;
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
        public void FirstDeclinationComponentEdit_UpdatesDeclinationExpression() {
            var sut = CreateConditionWithCoordinates();
            var altitudeUpdates = ObserveAltitudeUpdates(sut);

            sut.Data.Coordinates.DecDegrees = 45;

            sut.Data.Coordinates.Coordinates.Dec.Should().Be(45);
            sut.RaExpression.Definition.Should().Be("5 + 5");
            sut.DecExpression.Definition.Should().Be("45");
            altitudeUpdates.Should().ContainSingle().Which.Should().Be(sut.Data.CurrentAltitude);
        }

        [Test]
        public void FirstRightAscensionComponentEdit_UpdatesRightAscensionExpression() {
            var sut = CreateConditionWithCoordinates();
            var altitudeUpdates = ObserveAltitudeUpdates(sut);

            sut.Data.Coordinates.RAHours = 12;

            sut.Data.Coordinates.Coordinates.RA.Should().Be(12);
            sut.RaExpression.Definition.Should().Be("12");
            sut.DecExpression.Definition.Should().Be("20");
            altitudeUpdates.Should().ContainSingle().Which.Should().Be(sut.Data.CurrentAltitude);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SettingCoordinates_UpdatesBothExpressionsAndRefreshesAltitudeOnce(bool reuseCoordinates) {
            var sut = CreateConditionWithCoordinates();
            var altitudeUpdates = ObserveAltitudeUpdates(sut);
            var coordinates = reuseCoordinates ? sut.Data.Coordinates.Coordinates : new Coordinates(Angle.Zero, Angle.Zero, Epoch.J2000);
            coordinates.RA = 12;
            coordinates.Dec = -60;

            sut.Data.Coordinates.Coordinates = coordinates;

            sut.Data.Coordinates.Coordinates.Should().BeSameAs(coordinates);
            sut.RaExpression.Definition.Should().Be("12");
            sut.DecExpression.Definition.Should().Be("-60");
            altitudeUpdates.Should().ContainSingle().Which.Should().Be(sut.Data.CurrentAltitude);
        }

        [Test]
        public void ParentChanges_KeepOneCoordinateSubscription() {
            var sut = CreateConditionWithCoordinates();
            var firstParent = new Mock<IDeepSkyObjectContainer>();
            firstParent.SetupGet(x => x.Target).Returns(new InputTarget(Angle.Zero, Angle.Zero, null) {
                InputCoordinates = new InputCoordinates(new Coordinates(Angle.ByHours(5), Angle.ByDegree(30), Epoch.J2000))
            });
            var secondParent = new Mock<IDeepSkyObjectContainer>();
            secondParent.SetupGet(x => x.Target).Returns(new InputTarget(Angle.Zero, Angle.Zero, null) {
                InputCoordinates = new InputCoordinates(new Coordinates(Angle.ByHours(8), Angle.ByDegree(40), Epoch.J2000))
            });
            var altitudeUpdates = ObserveAltitudeUpdates(sut);

            foreach (var parent in new ISequenceContainer?[] { firstParent.Object, secondParent.Object, null, firstParent.Object }) {
                var expected = ((parent as IDeepSkyObjectContainer)?.Target.InputCoordinates.Coordinates
                    ?? sut.Data.Coordinates.Coordinates).Clone();
                sut.AttachNewParent(parent);
                sut.HasDsoParent.Should().Be(parent != null);
                sut.Data.Coordinates.Coordinates.RA.Should().Be(expected.RA);
                sut.Data.Coordinates.Coordinates.Dec.Should().Be(expected.Dec);
                sut.RaExpression.Definition.Should().Be(expected.RA.ToString(CultureInfo.InvariantCulture));
                sut.DecExpression.Definition.Should().Be(expected.Dec.ToString(CultureInfo.InvariantCulture));

                altitudeUpdates.Clear();
                sut.AfterParentChanged();

                altitudeUpdates.Should().BeEmpty("repeated parent notifications must not recalculate unchanged coordinates");
                sut.Data.Coordinates.Coordinates.RA.Should().Be(expected.RA);
                sut.Data.Coordinates.Coordinates.Dec.Should().Be(expected.Dec);
                sut.RaExpression.Definition.Should().Be(expected.RA.ToString(CultureInfo.InvariantCulture));
                sut.DecExpression.Definition.Should().Be(expected.Dec.ToString(CultureInfo.InvariantCulture));
                altitudeUpdates.Clear();

                sut.Data.Coordinates.RAHours = 11;

                sut.RaExpression.Definition.Should().Be("11");
                altitudeUpdates.Should().ContainSingle();
            }
        }

        [TestCase("RightAscension")]
        [TestCase("Declination")]
        [TestCase("Epoch")]
        [TestCase("Replacement")]
        public void InheritedCoordinates_RefreshOnlyForChangedState(string change) {
            var sut = CreateConditionWithCoordinates();
            var target = new InputTarget(Angle.Zero, Angle.Zero, null) {
                InputCoordinates = new InputCoordinates(new Coordinates(Angle.ByHours(5), Angle.ByDegree(30), Epoch.J2000))
            };
            var parent = new Mock<IDeepSkyObjectContainer>();
            parent.SetupGet(x => x.Target).Returns(target);
            sut.AttachNewParent(parent.Object);
            var altitudeUpdates = ObserveAltitudeUpdates(sut);

            for (int direction = 0; direction < 2; direction++) {
                var coordinates = target.InputCoordinates.Coordinates;
                switch (change) {
                    case "RightAscension": coordinates.RA = direction == 0 ? 12 : 5; break;
                    case "Declination": coordinates.Dec = direction == 0 ? -45 : 30; break;
                    case "Epoch": coordinates.Epoch = direction == 0 ? Epoch.JNOW : Epoch.J2000; break;
                    case "Replacement": target.InputCoordinates.Coordinates = coordinates.Clone(); break;
                }
                coordinates = target.InputCoordinates.Coordinates;
                double expectedRa = coordinates.RA;
                double expectedDec = coordinates.Dec;
                var expectedEpoch = coordinates.Epoch;

                sut.AfterParentChanged();

                sut.Data.Coordinates.Coordinates.Should().BeSameAs(coordinates);
                sut.Data.Coordinates.Coordinates.Epoch.Should().Be(expectedEpoch);
                sut.RaExpression.Definition.Should().Be(expectedRa.ToString(CultureInfo.InvariantCulture));
                sut.DecExpression.Definition.Should().Be(expectedDec.ToString(CultureInfo.InvariantCulture));
                altitudeUpdates.Should().ContainSingle("changed inherited coordinate state must refresh the prediction");
                altitudeUpdates.Clear();

                sut.AfterParentChanged();
                sut.AfterParentChanged();

                altitudeUpdates.Should().BeEmpty("repeated ancestor notifications must not recalculate unchanged coordinates");
            }
        }

        [Test]
        public void InheritedCoordinates_DetachAndReattachStillUpdatesWatchdogAndSubscription() {
            var sut = CreateConditionWithCoordinates();
            var watchdog = new Mock<IConditionWatchdog>();
            sut.ConditionWatchdog = watchdog.Object;
            var parent = new Mock<IDeepSkyObjectContainer>();
            parent.SetupGet(x => x.Target).Returns(new InputTarget(Angle.Zero, Angle.Zero, null) {
                InputCoordinates = new InputCoordinates(new Coordinates(Angle.ByHours(10), Angle.ByDegree(20), Epoch.J2000))
            });
            parent.SetupGet(x => x.Parent).Returns(Mock.Of<ISequenceRootContainer>());
            var altitudeUpdates = ObserveAltitudeUpdates(sut);

            for (int cycle = 0; cycle < 2; cycle++) {
                watchdog.Invocations.Clear();
                sut.AttachNewParent(parent.Object);
                sut.HasDsoParent.Should().BeTrue();
                watchdog.Verify(x => x.Start(), Times.Once);
                watchdog.Verify(x => x.Cancel(), Times.Never);
                altitudeUpdates.Clear();
                sut.AfterParentChanged();
                altitudeUpdates.Should().BeEmpty();

                watchdog.Invocations.Clear();
                sut.AttachNewParent(null);
                sut.HasDsoParent.Should().BeFalse();
                watchdog.Verify(x => x.Start(), Times.Never);
                watchdog.Verify(x => x.Cancel(), Times.Once);
                altitudeUpdates.Clear();
                sut.Data.Coordinates.DecDegrees = cycle == 0 ? 45 : 20;
                altitudeUpdates.Should().ContainSingle("detached coordinate editing must retain one subscription");
            }
        }

        [Test]
        public void Clone_CoordinateEditsRefreshOnlyTheirOwnAltitude() {
            var sut = CreateConditionWithCoordinates();
            var clone = (AltitudeCondition)sut.Clone();
            clone.AfterParentChanged();
            var originalUpdates = ObserveAltitudeUpdates(sut);
            var cloneUpdates = ObserveAltitudeUpdates(clone);

            clone.Data.Coordinates.RAHours = 12;

            cloneUpdates.Should().ContainSingle();
            originalUpdates.Should().BeEmpty();
            clone.RaExpression.Definition.Should().Be("12");
            sut.RaExpression.Definition.Should().Be("5 + 5");

            sut.Data.Coordinates.DecDegrees = 45;

            originalUpdates.Should().ContainSingle();
            cloneUpdates.Should().ContainSingle();
            sut.DecExpression.Definition.Should().Be("45");
            clone.DecExpression.Definition.Should().Be("20");
        }

        private static List<double> ObserveAltitudeUpdates(AltitudeCondition condition) {
            var updates = new List<double>();
            condition.Data.PropertyChanged += (_, e) => {
                if (e.PropertyName == nameof(condition.Data.CurrentAltitude)) {
                    updates.Add(condition.Data.CurrentAltitude);
                }
            };
            return updates;
        }

        private AltitudeCondition CreateConditionWithCoordinates() {
            var sut = new AltitudeCondition(profileServiceMock.Object);
            sut.Data.Coordinates = new InputCoordinates(new Coordinates(Angle.ByHours(10), Angle.ByDegree(20), Epoch.J2000));
            sut.RaExpression.Definition = "5 + 5";
            sut.DecExpression.Definition = "20";
            sut.AfterParentChanged();
            return sut;
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
            sut.Offset = targetAltitude;

            Assert.That(
                sut.Check(null, null),
                Is.True);
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
            sut.Offset = targetAltitude;

            Assert.That(
                sut.Check(null, null),
                Is.True);
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
            sut.Offset = targetAltitude;
            Assert.That(
                sut.Check(null, null),
                Is.False);
        }

        [Test]
        public void ToString_Test() {
            var sut = new AltitudeCondition(profileServiceMock.Object);
            sut.Offset = 30;
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