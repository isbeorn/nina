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
using NINA.Equipment.Equipment.MyDome;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Sequencer.SequenceItem.Dome;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.SequenceItem.Dome {

    [TestFixture]
    internal class SlewDomeAzimuthTest {
        private Mock<IDomeMediator> domeMediatorMock;

        [SetUp]
        public void Setup() {
            domeMediatorMock = new Mock<IDomeMediator>();
        }

        /// <summary>
        /// Verifies the Clone Copies Azimuth Expression Independently scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void Clone_CopiesAzimuthExpressionIndependently() {
            SlewDomeAzimuth sut = new SlewDomeAzimuth(domeMediatorMock.Object);
            sut.Icon = new System.Windows.Media.GeometryGroup();
            sut.AzimuthDegreesDefinition = "90 + 45";

            SlewDomeAzimuth clone = (SlewDomeAzimuth)sut.Clone();

            clone.Should().NotBeSameAs(sut);
            clone.Icon.Should().BeSameAs(sut.Icon);
            clone.AzimuthDegrees.Should().Be(135);
            clone.AzimuthDegreesExpression.Should().NotBeSameAs(sut.AzimuthDegreesExpression);
            clone.AzimuthDegreesExpression.Definition.Should().Be("90 + 45");

            clone.AzimuthDegreesDefinition = "180";

            sut.AzimuthDegreesExpression.Definition.Should().Be("90 + 45");
            sut.AzimuthDegrees.Should().Be(135);
        }

        /// <summary>
        /// Verifies the Validate Connected And Can Set Azimuth No Issues scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void Validate_ConnectedAndCanSetAzimuth_NoIssues() {
            domeMediatorMock.Setup(x => x.GetInfo()).Returns(new DomeInfo() { Connected = true, CanSetAzimuth = true });
            SlewDomeAzimuth sut = new SlewDomeAzimuth(domeMediatorMock.Object) {
                AzimuthDegreesDefinition = "180"
            };

            bool valid = sut.Validate();

            valid.Should().BeTrue();
            sut.Issues.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies the Validate Not Connected Returns Issue scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void Validate_NotConnected_ReturnsIssue() {
            domeMediatorMock.Setup(x => x.GetInfo()).Returns(new DomeInfo() { Connected = false });
            SlewDomeAzimuth sut = new SlewDomeAzimuth(domeMediatorMock.Object);

            bool valid = sut.Validate();

            valid.Should().BeFalse();
            sut.Issues.Should().HaveCount(1);
        }

        /// <summary>
        /// Verifies the Validate Cannot Set Azimuth Returns Issue scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public void Validate_CannotSetAzimuth_ReturnsIssue() {
            domeMediatorMock.Setup(x => x.GetInfo()).Returns(new DomeInfo() { Connected = true, CanSetAzimuth = false });
            SlewDomeAzimuth sut = new SlewDomeAzimuth(domeMediatorMock.Object);

            bool valid = sut.Validate();

            valid.Should().BeFalse();
            sut.Issues.Should().HaveCount(1);
        }

        /// <summary>
        /// Verifies the Execute Uses Evaluated Azimuth Expression scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        public async Task Execute_UsesEvaluatedAzimuthExpression() {
            CancellationTokenSource cts = new CancellationTokenSource();
            domeMediatorMock.Setup(x => x.SlewToAzimuth(It.IsAny<double>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            SlewDomeAzimuth sut = new SlewDomeAzimuth(domeMediatorMock.Object) {
                AzimuthDegreesDefinition = "90 + 45"
            };

            await sut.Execute(default, cts.Token);

            domeMediatorMock.Verify(x => x.SlewToAzimuth(135, cts.Token), Times.Once);
        }
    }
}
