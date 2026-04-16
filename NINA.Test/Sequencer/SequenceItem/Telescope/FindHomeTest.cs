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
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Sequencer.SequenceItem.Telescope;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.SequenceItem.Telescope {

    [TestFixture]
    public class FindHomeTest {
        private Mock<ITelescopeMediator> telescopeMediatorMock;
        private Mock<IGuiderMediator> guiderMediatorMock;

        [SetUp]
        public void SetUp() {
            telescopeMediatorMock = new Mock<ITelescopeMediator>();
            guiderMediatorMock = new Mock<IGuiderMediator>();

            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo { Connected = true, CanFindHome = true });
            telescopeMediatorMock.Setup(x => x.FindHome(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            guiderMediatorMock.Setup(x => x.StopGuiding(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        }

        /// <summary>
        /// Verifies the Execute Stops Guiding Before Finding Home scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task Execute_StopsGuidingBeforeFindingHome() {
            FindHome sut = new FindHome(telescopeMediatorMock.Object, guiderMediatorMock.Object);

            await sut.Execute(default, CancellationToken.None);

            guiderMediatorMock.Verify(x => x.StopGuiding(It.IsAny<CancellationToken>()), Times.Once);
            telescopeMediatorMock.Verify(x => x.FindHome(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Verifies the Validate Requires Connected Telescope With Find Home Capability scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void Validate_RequiresConnectedTelescopeWithFindHomeCapability() {
            FindHome sut = new FindHome(telescopeMediatorMock.Object, guiderMediatorMock.Object);

            sut.Validate().Should().BeTrue();

            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo { Connected = true, CanFindHome = false });
            sut.Validate().Should().BeFalse();
            sut.Issues.Should().ContainSingle();

            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo { Connected = false, CanFindHome = false });
            sut.Validate().Should().BeFalse();
            sut.Issues.Should().ContainSingle();
        }

        /// <summary>
        /// Verifies the Clone Copies Metadata scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void Clone_CopiesMetadata() {
            FindHome sut = new FindHome(telescopeMediatorMock.Object, guiderMediatorMock.Object) {
                Name = "Home",
                Description = "Find home",
                Icon = new System.Windows.Media.GeometryGroup()
            };

            FindHome clone = (FindHome)sut.Clone();

            clone.Should().NotBeSameAs(sut);
            clone.Name.Should().Be(sut.Name);
            clone.Description.Should().Be(sut.Description);
            clone.Icon.Should().BeSameAs(sut.Icon);
        }
    }
}
