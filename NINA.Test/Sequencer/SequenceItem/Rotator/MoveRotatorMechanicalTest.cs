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
using NINA.Equipment.Equipment.MyRotator;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem.Rotator;
using NINA.Equipment.Interfaces.Mediator;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.SequenceItem.Rotator {

    [TestFixture]
    internal class MoveRotatorMechanicalTest {
        public Mock<IRotatorMediator> rotatorMediatorMock;

        [SetUp]
        public void Setup() {
            rotatorMediatorMock = new Mock<IRotatorMediator>();
        }

        [Test]
        public void Clone_ItemClonedProperly() {
            var sut = new MoveRotatorMechanical(rotatorMediatorMock.Object);
            sut.Name = "SomeName";
            sut.Description = "SomeDescription";
            sut.Icon = new System.Windows.Media.GeometryGroup();
            var item2 = (MoveRotatorMechanical)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Name.Should().BeSameAs(sut.Name);
            item2.Description.Should().BeSameAs(sut.Description);
            item2.Icon.Should().BeSameAs(sut.Icon);
            item2.MechanicalPosition.Should().Be(sut.MechanicalPosition);
        }

        [Test]
        public void Validate_NoIssues() {
            rotatorMediatorMock.Setup(x => x.GetInfo()).Returns(new RotatorInfo() { Connected = true });

            var sut = new MoveRotatorMechanical(rotatorMediatorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeTrue();

            sut.Issues.Should().BeEmpty();
        }

        [Test]
        public void Validate_NotConnected_OneIssue() {
            rotatorMediatorMock.Setup(x => x.GetInfo()).Returns(new RotatorInfo() { Connected = false });

            var sut = new MoveRotatorMechanical(rotatorMediatorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeFalse();

            sut.Issues.Should().HaveCount(1);
        }

        [Test]
        [TestCase(0)]
        [TestCase(100)]
        [TestCase(1000)]
        public async Task Execute_NoIssues_LogicCalled(int position) {
            rotatorMediatorMock.Setup(x => x.GetInfo()).Returns(new RotatorInfo() { Connected = true });

            var sut = new MoveRotatorMechanical(rotatorMediatorMock.Object);
            sut.MechanicalPosition = position;

            var cts = new CancellationTokenSource();
            await sut.Execute(default, cts.Token);

            rotatorMediatorMock.Verify(x => x.MoveMechanical(It.Is<float>(p => p == position), cts.Token), Times.Once);
        }

        [Test]
        public void GetEstimatedDuration_BasedOnParameters_ReturnsCorrectEstimate() {
            var sut = new MoveRotatorMechanical(rotatorMediatorMock.Object);

            var duration = sut.GetEstimatedDuration();

            duration.Should().Be(TimeSpan.Zero);
        }
    }
}