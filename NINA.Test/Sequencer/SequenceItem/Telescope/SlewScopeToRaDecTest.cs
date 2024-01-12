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
using NINA.Sequencer.Container;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem.Telescope;
using NINA.Astrometry;
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
    internal class SlewScopeToRaDecTest {
        public Mock<ITelescopeMediator> telescopeMediatorMock;
        public Mock<IGuiderMediator> guiderMediatorMock;

        [SetUp]
        public void Setup() {
            telescopeMediatorMock = new Mock<ITelescopeMediator>();
            guiderMediatorMock = new Mock<IGuiderMediator>();
        }

        [Test]
        public void Clone_ItemClonedProperly() {
            var sut = new SlewScopeToRaDec(telescopeMediatorMock.Object, guiderMediatorMock.Object);
            sut.Name = "SomeName";
            sut.Description = "SomeDescription";
            sut.Icon = new System.Windows.Media.GeometryGroup();
            sut.Coordinates.Coordinates = new Coordinates(Angle.ByDegree(1), Angle.ByDegree(10), Epoch.J2000);
            var item2 = (SlewScopeToRaDec)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Name.Should().BeSameAs(sut.Name);
            item2.Description.Should().BeSameAs(sut.Description);
            item2.Icon.Should().BeSameAs(sut.Icon);
            item2.Coordinates.Should().NotBeSameAs(sut.Coordinates);
            item2.Coordinates.Coordinates.RA.Should().Be(sut.Coordinates.Coordinates.RA);
            item2.Coordinates.Coordinates.Dec.Should().Be(sut.Coordinates.Coordinates.Dec);
        }

        [Test]
        public void Validate_NoIssues() {
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo() { Connected = true });

            var sut = new SlewScopeToRaDec(telescopeMediatorMock.Object, guiderMediatorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeTrue();

            sut.Issues.Should().BeEmpty();
        }

        [Test]
        public void Validate_NotConnected_OneIssue() {
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo() { Connected = false });

            var sut = new SlewScopeToRaDec(telescopeMediatorMock.Object, guiderMediatorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeFalse();

            sut.Issues.Should().HaveCount(1);
        }

        [Test]
        public async Task Execute_NoIssues_LogicCalled() {
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo() { Connected = true });
            guiderMediatorMock.Setup(x => x.StopGuiding(It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            var coordinates = new Coordinates(Angle.ByDegree(1), Angle.ByDegree(2), Epoch.J2000);

            var sut = new SlewScopeToRaDec(telescopeMediatorMock.Object, guiderMediatorMock.Object);
            sut.Coordinates.Coordinates = coordinates;
            await sut.Execute(default, default);

            telescopeMediatorMock.Verify(x => x.SlewToCoordinatesAsync(It.Is<Coordinates>(c => c == coordinates), It.IsAny<CancellationToken>()), Times.Once);
            guiderMediatorMock.Verify(x => x.StopGuiding(It.IsAny<CancellationToken>()), Times.Once);
            guiderMediatorMock.Verify(x => x.StartGuiding(It.IsAny<bool>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Execute_NoIssues_LogicCalled_GuidingNotStoppedAndNotResumed() {
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo() { Connected = true });
            guiderMediatorMock.Setup(x => x.StopGuiding(It.IsAny<CancellationToken>())).Returns(Task.FromResult(false));
            var coordinates = new Coordinates(Angle.ByDegree(1), Angle.ByDegree(2), Epoch.J2000);

            var sut = new SlewScopeToRaDec(telescopeMediatorMock.Object, guiderMediatorMock.Object);
            sut.Coordinates.Coordinates = coordinates;
            await sut.Execute(default, default);

            telescopeMediatorMock.Verify(x => x.SlewToCoordinatesAsync(It.Is<Coordinates>(c => c == coordinates), It.IsAny<CancellationToken>()), Times.Once);
            guiderMediatorMock.Verify(x => x.StopGuiding(It.IsAny<CancellationToken>()), Times.Once);
            guiderMediatorMock.Verify(x => x.StartGuiding(It.IsAny<bool>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public void GetEstimatedDuration_BasedOnParameters_ReturnsCorrectEstimate() {
            var sut = new SlewScopeToRaDec(telescopeMediatorMock.Object, guiderMediatorMock.Object);

            var duration = sut.GetEstimatedDuration();

            duration.Should().Be(TimeSpan.Zero);
        }

        [Test]
        public void AttachNewParent_HasDSOContainerParent_RetrieveParentCoordinates() {
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo() { Connected = true });

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

            var sut = new SlewScopeToRaDec(telescopeMediatorMock.Object, guiderMediatorMock.Object);
            sut.AttachNewParent(parentMock.Object);

            sut.Coordinates.Coordinates.RA.Should().Be(coordinates.RA);
            sut.Coordinates.Coordinates.Dec.Should().Be(coordinates.Dec);
        }
    }
}