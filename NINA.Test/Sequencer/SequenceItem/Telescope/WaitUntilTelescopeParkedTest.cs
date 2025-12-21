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
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Sequencer;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem.Telescope;
using NINA.Equipment.Interfaces.Mediator;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.SequenceItem.Telescope {

    [TestFixture]
    internal class WaitUntilTelescopeParkedTest {
        public Mock<ITelescopeMediator> telescopeMediatorMock;

        [SetUp]
        public void Setup() {
            telescopeMediatorMock = new Mock<ITelescopeMediator>();
        }

        [Test]
        public void Clone_ItemClonedProperly() {
            var sut = new WaitUntilTelescopeParked(telescopeMediatorMock.Object);
            sut.Name = "SomeName";
            sut.Description = "SomeDescription";
            sut.Icon = new System.Windows.Media.GeometryGroup();
            var item2 = (WaitUntilTelescopeParked)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Name.Should().BeSameAs(sut.Name);
            item2.Description.Should().BeSameAs(sut.Description);
            item2.Icon.Should().BeSameAs(sut.Icon);
        }

        [Test]
        public void Validate_NoIssues() {
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo() { Connected = true });

            var sut = new WaitUntilTelescopeParked(telescopeMediatorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeTrue();

            sut.Issues.Should().BeEmpty();
        }

        [Test]
        public void Validate_NotConnected_OneIssue() {
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo() { Connected = false });

            var sut = new WaitUntilTelescopeParked(telescopeMediatorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeFalse();

            sut.Issues.Should().HaveCount(1);
        }

        [Test]
        public async Task Execute_AlreadyParked_ReturnsImmediately() {
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo() { Connected = true, AtPark = true });

            var sut = new WaitUntilTelescopeParked(telescopeMediatorMock.Object);
            await sut.Execute(default, default);

            // Should only call GetInfo once since telescope is already parked
            telescopeMediatorMock.Verify(x => x.GetInfo(), Times.Once);
        }

        [Test]
        public async Task Execute_NotParked_WaitsUntilParked() {
            var callCount = 0;
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(() => {
                callCount++;
                // Return not parked for first 2 calls, then parked
                return new TelescopeInfo() { Connected = true, AtPark = callCount >= 3 };
            });

            var sut = new WaitUntilTelescopeParked(telescopeMediatorMock.Object);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await sut.Execute(default, cts.Token);

            // Should have polled multiple times before telescope was parked
            callCount.Should().BeGreaterOrEqualTo(3);
        }

        [Test]
        public async Task Execute_Cancelled_ThrowsOperationCancelled() {
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo() { Connected = true, AtPark = false });

            var sut = new WaitUntilTelescopeParked(telescopeMediatorMock.Object);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Func<Task> act = async () => await sut.Execute(default, cts.Token);
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Test]
        public void GetEstimatedDuration_ReturnsZero() {
            var sut = new WaitUntilTelescopeParked(telescopeMediatorMock.Object);

            var duration = sut.GetEstimatedDuration();

            duration.Should().Be(TimeSpan.Zero);
        }
    }
}
