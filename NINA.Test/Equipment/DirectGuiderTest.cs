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
using NINA.Core.Enum;
using NINA.Equipment.Equipment.MyGuider;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Equipment {

    [TestFixture]
    public class DirectGuiderTest {

        [Test]
        public void SelectOffset_WhenCandidateMovementIsBelowMinimum_RejectsCandidate() {
            Queue<DitherOffset> candidates = new Queue<DitherOffset>(new[] {
                new DitherOffset(4.8, 0.5),
                new DitherOffset(-2.0, 1.0)
            });
            DitherOffsetSelector sut = new DitherOffsetSelector(_ => candidates.Dequeue());

            DitherOffset result = sut.SelectOffset(new DitherOffset(5.0, 0.5), 5.0, 1.0, false);

            result.WestEastPixels.Should().Be(-2.0);
            result.NorthSouthPixels.Should().Be(1.0);
            candidates.Should().BeEmpty();
        }

        [Test]
        public void SelectOffset_WhenMovementEqualsMinimum_AcceptsCandidate() {
            Queue<DitherOffset> candidates = new Queue<DitherOffset>(new[] {
                new DitherOffset(3.0, 4.0)
            });
            DitherOffsetSelector sut = new DitherOffsetSelector(_ => candidates.Dequeue());

            DitherOffset result = sut.SelectOffset(new DitherOffset(0.0, 0.0), 5.0, 5.0, false);

            result.WestEastPixels.Should().Be(3.0);
            result.NorthSouthPixels.Should().Be(4.0);
            candidates.Should().BeEmpty();
        }

        [Test]
        public void SelectOffset_WhenMinimumIsZero_AcceptsFirstCandidate() {
            Queue<DitherOffset> candidates = new Queue<DitherOffset>(new[] {
                new DitherOffset(0.0, 0.0),
                new DitherOffset(5.0, 0.0)
            });
            DitherOffsetSelector sut = new DitherOffsetSelector(_ => candidates.Dequeue());

            DitherOffset result = sut.SelectOffset(new DitherOffset(0.0, 0.0), 5.0, 0.0, false);

            result.WestEastPixels.Should().Be(0.0);
            result.NorthSouthPixels.Should().Be(0.0);
            candidates.Should().ContainSingle();
        }

        [Test]
        public void SelectOffset_WhenDitheringRAOnly_RequiresRAMovementAndPreservesDeclinationOffset() {
            Queue<DitherOffset> candidates = new Queue<DitherOffset>(new[] {
                new DitherOffset(0.5, 10.0),
                new DitherOffset(-2.0, -10.0)
            });
            DitherOffsetSelector sut = new DitherOffsetSelector(_ => candidates.Dequeue());

            DitherOffset result = sut.SelectOffset(new DitherOffset(0.0, 3.0), 5.0, 1.0, true);

            result.WestEastPixels.Should().Be(-2.0);
            result.NorthSouthPixels.Should().Be(3.0);
            candidates.Should().BeEmpty();
        }

        [TestCase(-1.0, 0.0)]
        [TestCase(0.0, 0.0)]
        [TestCase(2.0, 2.0)]
        [TestCase(5.0, 5.0)]
        [TestCase(6.0, 5.0)]
        public void NormalizeMinimum_ConstrainsValueToConfiguredDitherPixels(double minimum, double expected) {
            DitherOffsetSelector.NormalizeMinimum(5.0, minimum).Should().Be(expected);
        }

        [TestCase(false, 3.0, 4.0)]
        [TestCase(false, -3.0, -4.0)]
        [TestCase(true, 5.0, 7.0)]
        [TestCase(true, -5.0, 7.0)]
        public void SelectOffset_WhenCandidatesAreExhausted_FallsBackToExactMinimum(bool raOnly, double previousWestEast, double previousNorthSouth) {
            DitherOffsetSelector sut = new DitherOffsetSelector(_ => new DitherOffset(previousWestEast, previousNorthSouth));
            DitherOffset previous = new DitherOffset(previousWestEast, previousNorthSouth);

            DitherOffset result = sut.SelectOffset(previous, 5.0, 2.0, raOnly);

            double westEastMovement = result.WestEastPixels - previous.WestEastPixels;
            double northSouthMovement = result.NorthSouthPixels - previous.NorthSouthPixels;
            double movement = raOnly
                ? Math.Abs(westEastMovement)
                : Math.Sqrt(westEastMovement * westEastMovement + northSouthMovement * northSouthMovement);
            movement.Should().BeApproximately(2.0, 1e-10);
            if (raOnly) {
                result.NorthSouthPixels.Should().Be(previousNorthSouth);
            }
        }

        [Test]
        public async Task Dither_WithConfiguredMinimum_UsesQualifyingMovementForPulseGuide() {
            Mock<IProfileService> profileService = new Mock<IProfileService>();
            Mock<IProfile> profile = new Mock<IProfile>();
            Mock<IGuiderSettings> guiderSettings = new Mock<IGuiderSettings>();
            Mock<ICameraSettings> cameraSettings = new Mock<ICameraSettings>();
            Mock<ITelescopeSettings> telescopeSettings = new Mock<ITelescopeSettings>();
            Mock<ITelescopeMediator> telescopeMediator = new Mock<ITelescopeMediator>();
            Queue<DitherOffset> candidates = new Queue<DitherOffset>(new[] {
                new DitherOffset(0.25, 0.25),
                new DitherOffset(2.0, -3.0)
            });
            DitherOffsetSelector selector = new DitherOffsetSelector(_ => candidates.Dequeue());
            guiderSettings.SetupGet(x => x.DitherPixels).Returns(5.0);
            guiderSettings.SetupGet(x => x.MountDitherMinimumPixels).Returns(1.0);
            guiderSettings.SetupGet(x => x.SettleTime).Returns(0);
            guiderSettings.SetupGet(x => x.DitherRAOnly).Returns(false);
            cameraSettings.SetupGet(x => x.PixelSize).Returns(3.76);
            telescopeSettings.SetupGet(x => x.FocalLength).Returns(600.0);
            profile.SetupGet(x => x.GuiderSettings).Returns(guiderSettings.Object);
            profile.SetupGet(x => x.CameraSettings).Returns(cameraSettings.Object);
            profile.SetupGet(x => x.TelescopeSettings).Returns(telescopeSettings.Object);
            profileService.SetupGet(x => x.ActiveProfile).Returns(profile.Object);
            DirectGuider sut = new DirectGuider(profileService.Object, telescopeMediator.Object, selector);
            sut.UpdateDeviceInfo(new TelescopeInfo {
                Connected = true,
                GuideRateRightAscensionArcsecPerSec = 1.0,
                GuideRateDeclinationArcsecPerSec = 1.0
            });
            sut.PixelScale = 0.001;
            sut.WestEastGuideRate = 1.0;
            sut.NorthSouthGuideRate = 1.0;

            bool result = await sut.Dither(null, CancellationToken.None);

            result.Should().BeTrue();
            telescopeMediator.Verify(x => x.PulseGuide(GuideDirections.guideEast, 2), Times.Once);
            telescopeMediator.Verify(x => x.PulseGuide(GuideDirections.guideSouth, 3), Times.Once);
            candidates.Should().BeEmpty();
        }
    }
}
