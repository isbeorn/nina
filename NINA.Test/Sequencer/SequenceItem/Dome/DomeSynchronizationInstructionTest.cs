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
using NINA.Core.Model;
using NINA.Equipment.Equipment.MyDome;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Sequencer;
using NINA.Sequencer.SequenceItem.Dome;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.SequenceItem.Dome {

    [TestFixture]
    public class DomeSynchronizationInstructionTest {
        private Mock<IDomeMediator> domeMediatorMock;
        private Mock<IDomeFollower> domeFollowerMock;
        private Mock<ITelescopeMediator> telescopeMediatorMock;

        [SetUp]
        public void SetUp() {
            domeMediatorMock = new Mock<IDomeMediator>();
            domeFollowerMock = new Mock<IDomeFollower>();
            telescopeMediatorMock = new Mock<ITelescopeMediator>();

            domeMediatorMock.Setup(x => x.GetInfo()).Returns(new DomeInfo { Connected = true, CanFindHome = true });
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo { Connected = true });
            domeFollowerMock.Setup(x => x.TriggerTelescopeSync()).ReturnsAsync(true);
            domeMediatorMock.Setup(x => x.DisableFollowing(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            domeMediatorMock.Setup(x => x.FindHome(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        }

        /// <summary>
        /// Verifies the Synchronize Dome Execute Requires Dome And Telescope And Triggers Follower Sync scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task SynchronizeDome_Execute_RequiresDomeAndTelescopeAndTriggersFollowerSync() {
            SynchronizeDome sut = new SynchronizeDome(domeMediatorMock.Object, domeFollowerMock.Object, telescopeMediatorMock.Object);

            sut.Validate().Should().BeTrue();
            await sut.Execute(default, CancellationToken.None);

            domeFollowerMock.Verify(x => x.TriggerTelescopeSync(), Times.Once);
        }

        /// <summary>
        /// Verifies the Synchronize Dome Execute Throws When Follower Sync Fails scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task SynchronizeDome_Execute_ThrowsWhenFollowerSyncFails() {
            domeFollowerMock.Setup(x => x.TriggerTelescopeSync()).ReturnsAsync(false);
            SynchronizeDome sut = new SynchronizeDome(domeMediatorMock.Object, domeFollowerMock.Object, telescopeMediatorMock.Object);

            Func<Task> act = () => sut.Execute(default, CancellationToken.None);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("*didn't complete successfully*");
        }

        /// <summary>
        /// Verifies the Synchronize Dome Execute Skips When Telescope Is Disconnected scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task SynchronizeDome_Execute_SkipsWhenTelescopeIsDisconnected() {
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo { Connected = false });
            SynchronizeDome sut = new SynchronizeDome(domeMediatorMock.Object, domeFollowerMock.Object, telescopeMediatorMock.Object);

            Func<Task> act = () => sut.Execute(default, CancellationToken.None);

            await act.Should().ThrowAsync<SequenceItemSkippedException>();
            sut.Issues.Should().ContainSingle();
            domeFollowerMock.Verify(x => x.TriggerTelescopeSync(), Times.Never);
        }

        /// <summary>
        /// Verifies the Disable Dome Synchronization Execute Requires Dome And Telescope And Disables Following scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task DisableDomeSynchronization_Execute_RequiresDomeAndTelescopeAndDisablesFollowing() {
            DisableDomeSynchronization sut = new DisableDomeSynchronization(domeMediatorMock.Object, telescopeMediatorMock.Object);

            sut.Validate().Should().BeTrue();
            await sut.Execute(default, CancellationToken.None);

            domeMediatorMock.Verify(x => x.DisableFollowing(It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Verifies the Disable Dome Synchronization Execute Skips When Dome Is Disconnected scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task DisableDomeSynchronization_Execute_SkipsWhenDomeIsDisconnected() {
            domeMediatorMock.Setup(x => x.GetInfo()).Returns(new DomeInfo { Connected = false });
            DisableDomeSynchronization sut = new DisableDomeSynchronization(domeMediatorMock.Object, telescopeMediatorMock.Object);

            Func<Task> act = () => sut.Execute(default, CancellationToken.None);

            await act.Should().ThrowAsync<SequenceItemSkippedException>();
            sut.Issues.Should().ContainSingle();
            domeMediatorMock.Verify(x => x.DisableFollowing(It.IsAny<CancellationToken>()), Times.Never);
        }

        /// <summary>
        /// Verifies the Find Home Dome Execute Requires Connected Dome With Home Capability scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task FindHomeDome_Execute_RequiresConnectedDomeWithHomeCapability() {
            FindHomeDome sut = new FindHomeDome(domeMediatorMock.Object);

            sut.Validate().Should().BeTrue();
            await sut.Execute(default, CancellationToken.None);

            domeMediatorMock.Verify(x => x.FindHome(It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Verifies the Find Home Dome Validate Reports Disconnected And Unsupported Home scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void FindHomeDome_Validate_ReportsDisconnectedAndUnsupportedHome() {
            domeMediatorMock.Setup(x => x.GetInfo()).Returns(new DomeInfo { Connected = false, CanFindHome = false });
            FindHomeDome sut = new FindHomeDome(domeMediatorMock.Object);

            sut.Validate().Should().BeFalse();

            sut.Issues.Should().HaveCount(2);
        }

        /// <summary>
        /// Verifies the Find Home Dome Execute Throws When Mediator Cannot Find Home scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task FindHomeDome_Execute_ThrowsWhenMediatorCannotFindHome() {
            domeMediatorMock.Setup(x => x.FindHome(It.IsAny<CancellationToken>())).ReturnsAsync(false);
            FindHomeDome sut = new FindHomeDome(domeMediatorMock.Object);

            Func<Task> act = () => sut.Execute(default, CancellationToken.None);

            await act.Should().ThrowAsync<Exception>();
        }
    }
}
