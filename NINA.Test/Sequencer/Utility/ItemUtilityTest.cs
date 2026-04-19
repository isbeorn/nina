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
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.ImageAnalysis;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Trigger.MeridianFlip;
using NINA.Sequencer.Utility;
using NINA.WPF.Base.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.Utility {

    [TestFixture]
    public class ItemUtilityTest {

        [Test]
        public void RetrieveContextCoordinates_IsNull_NoParent_ReturnNull() {
            var coordinates = ItemUtility.RetrieveContextCoordinates(null);

            coordinates.Should().BeNull();
        }

        [Test]
        public void RetrieveContextCoordinates_NoDSOContainer_NoParent_ReturnNull() {
            var containerMock = new Mock<ISequenceContainer>();

            var coordinates = ItemUtility.RetrieveContextCoordinates(containerMock.Object);

            coordinates.Should().BeNull();
        }

        [Test]
        public void RetrieveContextCoordinates_NoDSOContainer_HasParent_ReturnNull() {
            var parentMock = new Mock<ISequenceContainer>();
            var containerMock = new Mock<ISequenceContainer>();
            containerMock.SetupGet(x => x.Parent).Returns(parentMock.Object);

            var coordinates = ItemUtility.RetrieveContextCoordinates(containerMock.Object);

            coordinates.Should().BeNull();
        }

        [Test]
        public void RetrieveContextCoordinates_IsDSOContainer_NoParent_ReturnCoordinates() {
            var containerMock = new Mock<IDeepSkyObjectContainer>();

            var target = new InputTarget(Angle.ByDegree(10), Angle.ByDegree(10), null);
            var coords = new Coordinates(Angle.ByDegree(5), Angle.ByDegree(20), Epoch.J2000);
            var inputCoords = new InputCoordinates(coords);
            target.InputCoordinates = inputCoords;
            target.PositionAngle = 100;
            containerMock.SetupGet(x => x.Target).Returns(target);

            var coordinates = ItemUtility.RetrieveContextCoordinates(containerMock.Object);

            coordinates.Should().NotBeNull();
            coordinates.Coordinates.RA.Should().Be(coords.RA);
            coordinates.Coordinates.Dec.Should().Be(coords.Dec);
            coordinates.PositionAngle.Should().Be(100);
        }

        [Test]
        public void RetrieveContextCoordinates_IsNotDSOContainer_HasDSOParent_ReturnCoordinates() {
            var parentMock = new Mock<IDeepSkyObjectContainer>();
            var containerMock = new Mock<ISequenceContainer>();
            containerMock.SetupGet(x => x.Parent).Returns(parentMock.Object);

            var target = new InputTarget(Angle.ByDegree(10), Angle.ByDegree(10), null);
            var coords = new Coordinates(Angle.ByDegree(5), Angle.ByDegree(20), Epoch.J2000);
            var inputCoords = new InputCoordinates(coords);
            target.InputCoordinates = inputCoords;
            target.PositionAngle = 100;
            parentMock.SetupGet(x => x.Target).Returns(target);

            var coordinates = ItemUtility.RetrieveContextCoordinates(containerMock.Object);

            coordinates.Should().NotBeNull();
            coordinates.Coordinates.RA.Should().Be(coords.RA);
            coordinates.Coordinates.Dec.Should().Be(coords.Dec);
            coordinates.PositionAngle.Should().Be(100);
        }

        [Test]
        public void IsInRootContainer_IsNull_ReturnFalse() {
            var isIn = ItemUtility.IsInRootContainer(null);

            isIn.Should().BeFalse();
        }

        [Test]
        public void IsInRootContainer_IsNoRootContainer_NoParent_ReturnFalse() {
            var containerMock = new Mock<ISequenceContainer>();

            var isIn = ItemUtility.IsInRootContainer(containerMock.Object);

            isIn.Should().BeFalse();
        }

        [Test]
        public void IsInRootContainer_IsNoRootContainer_HasParent_ReturnFalse() {
            var parentMock = new Mock<ISequenceContainer>();
            var containerMock = new Mock<ISequenceContainer>();
            containerMock.SetupGet(x => x.Parent).Returns(parentMock.Object);

            var isIn = ItemUtility.IsInRootContainer(containerMock.Object);

            isIn.Should().BeFalse();
        }

        [Test]
        public void IsInRootContainer_IsRootContainer_NoParent_ReturnTrue() {
            var containerMock = new Mock<ISequenceRootContainer>();

            var isIn = ItemUtility.IsInRootContainer(containerMock.Object);

            isIn.Should().BeTrue();
        }

        [Test]
        public void IsInRootContainer_IsNoRootContainer_HasRootParent_ReturnTrue() {
            var parentMock = new Mock<ISequenceRootContainer>();
            var containerMock = new Mock<ISequenceContainer>();
            containerMock.SetupGet(x => x.Parent).Returns(parentMock.Object);

            var isIn = ItemUtility.IsInRootContainer(containerMock.Object);

            isIn.Should().BeTrue();
        }

        [Test]
        public void GetMeridianFlipTime_NoParent_NoMeridianFlip_Zero() {
            var containerMock = new Mock<ISequenceContainer>();

            var time = ItemUtility.GetMeridianFlipTime(containerMock.Object);

            time.Should().Be(DateTime.MinValue);
        }

        [Test]
        public void GetMeridianFlipTime_WithParent_NoMeridianFlip_Zero() {
            var parentMock = new Mock<ISequenceRootContainer>();
            var containerMock = new Mock<ISequenceContainer>();
            containerMock.SetupGet(x => x.Parent).Returns(parentMock.Object);

            var time = ItemUtility.GetMeridianFlipTime(containerMock.Object);

            time.Should().Be(DateTime.MinValue);
        }

        [Test]
        public void GetMeridianFlipTime_NoParent_NoMeridianFlip_ButOtherTriggers_Zero() {
            var containerMock = new Mock<ISequenceContainer>();

            var triggerableMock = containerMock.As<ITriggerable>();
            triggerableMock.Setup(x => x.GetTriggersSnapshot()).Returns(new List<ISequenceTrigger>() { new Mock<ISequenceTrigger>().Object });

            var time = ItemUtility.GetMeridianFlipTime(containerMock.Object);

            time.Should().Be(DateTime.MinValue);
        }

        [Test]
        public void GetMeridianFlipTime_WithParent_NoMeridianFlip_ButOtherTriggers_Zero() {
            var parentMock = new Mock<ISequenceRootContainer>();
            var containerMock = new Mock<ISequenceContainer>();
            containerMock.SetupGet(x => x.Parent).Returns(parentMock.Object);

            var triggerableMock = parentMock.As<ITriggerable>();
            triggerableMock.Setup(x => x.GetTriggersSnapshot()).Returns(new List<ISequenceTrigger>() { new Mock<ISequenceTrigger>().Object });

            var time = ItemUtility.GetMeridianFlipTime(containerMock.Object);

            time.Should().Be(DateTime.MinValue);
        }

        private IMeridianFlipTrigger PrepareTrigger(TimeSpan timeToFlip) {
            var profileServiceMock = new Mock<IProfileService>();
            var telescopeMediatorMock = new Mock<ITelescopeMediator>();
            var applicationStatusMediatorMock = new Mock<IApplicationStatusMediator>();
            var cameraMediatorMock = new Mock<ICameraMediator>();
            var focuserMediatorMock = new Mock<IFocuserMediator>();
            var meridianFlipVMFactoryMock = new Mock<IMeridianFlipVMFactory>();
            var safetyMonitorMediatorMock = new Mock<ISafetyMonitorMediator>();

            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo() {
                Connected = true,
                TimeToMeridianFlip = timeToFlip.TotalHours,
                TrackingEnabled = true
            });
            profileServiceMock.SetupGet(x => x.ActiveProfile.MeridianFlipSettings.UseSideOfPier).Returns(false);

            var flip = new MeridianFlipTrigger(profileServiceMock.Object, cameraMediatorMock.Object, telescopeMediatorMock.Object, focuserMediatorMock.Object, applicationStatusMediatorMock.Object, meridianFlipVMFactoryMock.Object, safetyMonitorMediatorMock.Object);

            flip.ShouldTrigger(null, null);

            return flip;
        }

        [Test]
        public void GetMeridianFlipTime_NoParent_WithMeridianFlip_ProperTime() {
            var containerMock = new Mock<ISequenceContainer>();

            var triggerableMock = containerMock.As<ITriggerable>();
            triggerableMock.Setup(x => x.GetTriggersSnapshot()).Returns(new List<ISequenceTrigger>() { new Mock<ISequenceTrigger>().Object, PrepareTrigger(TimeSpan.FromHours(1)) });

            var time = ItemUtility.GetMeridianFlipTime(containerMock.Object);

            time.Should().BeCloseTo(DateTime.Now + TimeSpan.FromHours(1), TimeSpan.FromMinutes(1));
        }

        [Test]
        public void GetMeridianFlipTime_WithParent_WithMeridianFlip_ProperTime() {
            var parentMock = new Mock<ISequenceRootContainer>();
            var containerMock = new Mock<ISequenceContainer>();
            containerMock.SetupGet(x => x.Parent).Returns(parentMock.Object);

            var triggerableMock = parentMock.As<ITriggerable>();
            triggerableMock.Setup(x => x.GetTriggersSnapshot()).Returns(new List<ISequenceTrigger>() { PrepareTrigger(TimeSpan.FromHours(1)), new Mock<ISequenceTrigger>().Object });

            var time = ItemUtility.GetMeridianFlipTime(containerMock.Object);

            time.Should().BeCloseTo(DateTime.Now + TimeSpan.FromHours(1), TimeSpan.FromMinutes(1));
        }

        [Test]
        [TestCase(1, 1, true)]
        [TestCase(1, 2, true)]
        [TestCase(2, 1, false)]
        public void IsTooCloseToMeridian_NoParent_WithMeridianFlip(int hoursToFlip, int estimatedTime, bool expected) {
            var containerMock = new Mock<ISequenceContainer>();

            var triggerableMock = containerMock.As<ITriggerable>();
            triggerableMock.Setup(x => x.GetTriggersSnapshot()).Returns(new List<ISequenceTrigger>() { PrepareTrigger(TimeSpan.FromHours(hoursToFlip)) });

            var isTooClose = ItemUtility.IsTooCloseToMeridianFlip(containerMock.Object, TimeSpan.FromHours(estimatedTime));

            isTooClose.Should().Be(expected);
        }

        [Test]
        [TestCase(1, 1, true)]
        [TestCase(1, 2, true)]
        [TestCase(2, 1, false)]
        public void IsTooCloseToMeridian_WithParent_WithMeridianFlip(int hoursToFlip, int estimatedTime, bool expected) {
            var parentMock = new Mock<ISequenceRootContainer>();
            var containerMock = new Mock<ISequenceContainer>();
            containerMock.SetupGet(x => x.Parent).Returns(parentMock.Object);

            var triggerableMock = parentMock.As<ITriggerable>();
            triggerableMock.Setup(x => x.GetTriggersSnapshot()).Returns(new List<ISequenceTrigger>() { PrepareTrigger(TimeSpan.FromHours(hoursToFlip)) });

            var isTooClose = ItemUtility.IsTooCloseToMeridianFlip(containerMock.Object, TimeSpan.FromHours(estimatedTime));

            isTooClose.Should().Be(expected);
        }

        /// <summary>
        /// Verifies downward target discovery returns direct and nested deep-sky containers while ignoring normal sequence items.
        /// </summary>
        [Test]
        public void LookForTargetsDownwards_ReturnsDirectAndNestedDeepSkyContainers() {
            Mock<IDeepSkyObjectContainer> directTarget = new Mock<IDeepSkyObjectContainer>();
            Mock<IDeepSkyObjectContainer> nestedTarget = new Mock<IDeepSkyObjectContainer>();
            Mock<ISequenceContainer> nestedContainer = new Mock<ISequenceContainer>();
            Mock<ISequenceItem> normalItem = new Mock<ISequenceItem>();
            Mock<ISequenceContainer> root = new Mock<ISequenceContainer>();
            nestedContainer.Setup(x => x.GetItemsSnapshot()).Returns(new List<ISequenceItem> { nestedTarget.Object });
            root.Setup(x => x.GetItemsSnapshot()).Returns(new List<ISequenceItem> {
                normalItem.Object,
                directTarget.Object,
                nestedContainer.Object
            });

            List<IDeepSkyObjectContainer> targets = ItemUtility.LookForTargetsDownwards(root.Object);

            targets.Should().Equal(directTarget.Object, nestedTarget.Object);
        }

        /// <summary>
        /// Verifies altitude iteration narrows an approximate rise time and records the first time that satisfies the requested threshold.
        /// </summary>
        [Test]
        public void Iterate_FindsFutureThresholdAndMarksApproximateResult() {
            WaitLoopData data = CreateWaitLoopData("IterateTarget");
            DateTime threshold = DateTime.Now.AddMinutes(20);
            data.ExpectedDateTime = DateTime.Now.AddMinutes(30);
            data.Offset = 10;
            ItemUtility.RiseSetMeridian riseSetMeridian = new ItemUtility.RiseSetMeridian(
                threshold,
                threshold.AddHours(2),
                threshold.AddHours(1),
                currentAltitude: 0,
                isRising: true);

            ItemUtility.Iterate(
                data,
                riseSetMeridian,
                greater: true,
                sense: true,
                allowance: 120,
                getCurrentAltitude: (when, observer) => when >= threshold ? 20 : 0);

            data.ExpectedDateTime.Should().BeOnOrAfter(threshold.AddMinutes(-1));
            data.TargetAltitude.Should().Be(10);
            data.Approximate.Should().Be("\u2248");
        }

        /// <summary>
        /// Verifies altitude iteration reports an unresolved expected time when no sampled point can satisfy the threshold.
        /// </summary>
        [Test]
        public void Iterate_ReportsUnresolvedTimeWhenNoSampleSatisfiesThreshold() {
            WaitLoopData data = CreateWaitLoopData("IterateTarget");
            DateTime baseTime = DateTime.Now.AddMinutes(30);
            data.ExpectedDateTime = baseTime;
            data.Offset = 12;
            data.TargetAltitude = 12;
            ItemUtility.RiseSetMeridian riseSetMeridian = new ItemUtility.RiseSetMeridian(
                baseTime,
                baseTime.AddMinutes(20),
                baseTime.AddMinutes(10),
                currentAltitude: 0,
                isRising: true);

            ItemUtility.Iterate(
                data,
                riseSetMeridian,
                greater: true,
                sense: true,
                allowance: 10,
                getCurrentAltitude: (when, observer) => 0);

            data.ExpectedTime.Should().Be("--");
            data.TargetAltitude.Should().Be(12);
        }

        /// <summary>
        /// Verifies common altitude-time calculation handles null and zero-coordinate inputs, plus the obsolete forwarding overload.
        /// </summary>
        [Test]
        public void CalculateExpectedTimeCommon_HandlesGuardClausesAndNowResult() {
            ItemUtility.CalculateExpectedTimeCommon(null, until: true, allowance: 10, getCurrentAltitude: (when, observer) => 0);
            WaitLoopData emptyCoordinates = CreateWaitLoopData("Empty");
            emptyCoordinates.Coordinates = null;

            ItemUtility.CalculateExpectedTimeCommon(emptyCoordinates, until: true, allowance: 10, getCurrentAltitude: (when, observer) => 0);

            emptyCoordinates.ExpectedDateTime.Should().Be(DateTime.MinValue);

#pragma warning disable CS0618
            WaitLoopData data = CreateWaitLoopData("Now");
            data.CurrentAltitude = 20;
            data.Offset = 10;
            data.Comparator = ComparisonOperatorEnum.GREATER_THAN;

            ItemUtility.CalculateExpectedTimeCommon(data, offset: 10, until: true, allowance: 120, getCurrentAltitude: (when, observer) => 20);
#pragma warning restore CS0618

            data.ExpectedTime.Should().NotBe("--");
            data.TargetAltitude.Should().Be(10);
        }

        /// <summary>
        /// Verifies altitude rise/set helpers cover normal, obsolete, and unreachable-altitude paths used by wait and condition entities.
        /// </summary>
        [Test]
        public void CalculateTimeAtAltitude_ReturnsRiseSetAndUnreachableResults() {
            Coordinates coordinates = new Coordinates(Angle.ByHours(5), Angle.ByDegree(20), Epoch.J2000);

            ItemUtility.RiseSetMeridian normal = ItemUtility.CalculateTimeAtAltitude(coordinates, 47, 11, 650, 20, new DateTime(2026, 4, 16, 12, 0, 0));
#pragma warning disable CS0618
            ItemUtility.RiseSetMeridian obsoleteWithoutElevation = ItemUtility.CalculateTimeAtAltitude(coordinates, 47, 11, 20, new DateTime(2026, 4, 16, 12, 0, 0));
            ItemUtility.RiseSetMeridian obsoleteCurrentTime = ItemUtility.CalculateTimeAtAltitude(coordinates, 47, 11, 20);
#pragma warning restore CS0618
            ItemUtility.RiseSetMeridian unreachable = ItemUtility.CalculateTimeAtAltitude(coordinates, 89, 11, 650, 89, new DateTime(2026, 4, 16, 12, 0, 0));

            normal.Rise.Should().NotBe(DateTime.MinValue);
            obsoleteWithoutElevation.Set.Should().NotBe(DateTime.MinValue);
            obsoleteCurrentTime.Meridian.Should().NotBe(DateTime.MinValue);
            unreachable.Rise.Should().Be(DateTime.MinValue);
            unreachable.Set.Should().Be(DateTime.MinValue);
            normal.ToString().Should().Contain("Altitude").And.Contain("Rise").And.Contain("Set");
        }

        private static WaitLoopData CreateWaitLoopData(string name) {
            NINA.Profile.Profile profile = new NINA.Profile.Profile();
            profile.AstrometrySettings.Latitude = 47;
            profile.AstrometrySettings.Longitude = 11;
            profile.AstrometrySettings.Elevation = 650;
            Mock<IProfileService> profileServiceMock = new Mock<IProfileService>();
            profileServiceMock.SetupGet(x => x.ActiveProfile).Returns(profile);
            WaitLoopData data = new WaitLoopData(profileServiceMock.Object, useCustomHorizon: false, name);
            data.Coordinates = new InputCoordinates(new Coordinates(Angle.ByHours(5), Angle.ByDegree(20), Epoch.J2000));
            return data;
        }
    }
}
