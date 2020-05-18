#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using Moq;
using NINA.Model;
using NINA.PlateSolving;
using NINA.Utility.Astrometry;
using NINA.Utility.Mediator.Interfaces;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINATest.PlateSolving {

    [TestFixture]
    public class CenterSolverTest {
        private Mock<IPlateSolver> plateSolverMock;
        private Mock<IPlateSolver> blindSolverMock;
        private Mock<ITelescopeMediator> telescopeMediatorMock;
        private Mock<IImagingMediator> imagingMediatorMock;
        private Mock<ICaptureSolver> captureSolverMock;

        [SetUp]
        public void Setup() {
            plateSolverMock = new Mock<IPlateSolver>();
            blindSolverMock = new Mock<IPlateSolver>();
            captureSolverMock = new Mock<ICaptureSolver>();
            telescopeMediatorMock = new Mock<ITelescopeMediator>();
        }

        [Test]
        public async Task Successful_Centering_NoSeparation_Test() {
            var seq = new CaptureSequence();
            var coordinates = new Coordinates(Angle.ByDegree(5), Angle.ByDegree(3), Epoch.JNOW);
            var parameter = new CenterSolveParameter() {
                Coordinates = coordinates.Transform(Epoch.JNOW),
                FocalLength = 700,
                Threshold = 1
            };
            var testResult = new PlateSolveResult() {
                Success = true,
                Coordinates = coordinates.Transform(Epoch.JNOW)
            };

            captureSolverMock
                .Setup(x => x.Solve(seq, It.IsAny<CaptureSolverParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(testResult);
            telescopeMediatorMock
                .Setup(x => x.GetCurrentPosition())
                .Returns(coordinates);

            var sut = new CenteringSolver(plateSolverMock.Object, blindSolverMock.Object, null, telescopeMediatorMock.Object);
            sut.CaptureSolver = captureSolverMock.Object;

            var result = await sut.Center(seq, parameter, default, default, default);

            result.Success.Should().BeTrue();
            captureSolverMock.Verify(x => x.Solve(seq, It.IsAny<CaptureSolverParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once());
            telescopeMediatorMock.Verify(x => x.Sync(It.IsAny<Coordinates>()), Times.Never());
            telescopeMediatorMock.Verify(x => x.SlewToCoordinatesAsync(It.IsAny<Coordinates>()), Times.Never());
        }

        [Test]
        public async Task Successful_Centering_SeparationInsideTolerance_Test() {
            var seq = new CaptureSequence();
            var coordinates = new Coordinates(Angle.ByDegree(5), Angle.ByDegree(3), Epoch.JNOW);
            var parameter = new CenterSolveParameter() {
                Coordinates = coordinates.Transform(Epoch.JNOW),
                FocalLength = 700,
                Threshold = 1
            };
            var testResult = new PlateSolveResult() {
                Success = true,
                Coordinates = new Coordinates(Angle.ByDegree(4.98333), Angle.ByDegree(3), Epoch.JNOW)
            };

            captureSolverMock
                .Setup(x => x.Solve(seq, It.IsAny<CaptureSolverParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(testResult);
            telescopeMediatorMock
                .Setup(x => x.GetCurrentPosition())
                .Returns(coordinates);

            var sut = new CenteringSolver(plateSolverMock.Object, blindSolverMock.Object, null, telescopeMediatorMock.Object);
            sut.CaptureSolver = captureSolverMock.Object;

            var result = await sut.Center(seq, parameter, default, default, default);

            result.Success.Should().BeTrue();
            captureSolverMock.Verify(x => x.Solve(seq, It.IsAny<CaptureSolverParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once());
            telescopeMediatorMock.Verify(x => x.Sync(It.IsAny<Coordinates>()), Times.Never());
            telescopeMediatorMock.Verify(x => x.SlewToCoordinatesAsync(It.IsAny<Coordinates>()), Times.Never());
        }

        [Test]
        public async Task Successful_Centering_AfterTwoCorrections_Test() {
            var seq = new CaptureSequence();
            var coordinates1 = new Coordinates(Angle.ByDegree(3), Angle.ByDegree(3), Epoch.JNOW);
            var coordinates2 = new Coordinates(Angle.ByDegree(4), Angle.ByDegree(3), Epoch.JNOW);
            var coordinates3 = new Coordinates(Angle.ByDegree(5), Angle.ByDegree(3), Epoch.JNOW);
            var parameter = new CenterSolveParameter() {
                Coordinates = coordinates3.Transform(Epoch.JNOW),
                FocalLength = 700,
                Threshold = 1
            };
            var testResult = new PlateSolveResult() {
                Success = true,
                Coordinates = new Coordinates(Angle.ByDegree(5), Angle.ByDegree(3), Epoch.JNOW)
            };

            captureSolverMock
                .Setup(x => x.Solve(seq, It.IsAny<CaptureSolverParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(testResult);
            telescopeMediatorMock
                .SetupSequence(x => x.GetCurrentPosition())
                .Returns(coordinates1)
                .Returns(testResult.Coordinates)
                .Returns(coordinates2)
                .Returns(testResult.Coordinates)
                .Returns(coordinates3)
                .Returns(testResult.Coordinates);
            telescopeMediatorMock
                .SetupSequence(x => x.Sync(It.IsAny<Coordinates>()))
                .Returns(Task.FromResult(true))
                .Returns(Task.FromResult(true))
                .Returns(Task.FromResult(true));

            var sut = new CenteringSolver(plateSolverMock.Object, blindSolverMock.Object, null, telescopeMediatorMock.Object);
            sut.CaptureSolver = captureSolverMock.Object;

            var result = await sut.Center(seq, parameter, default, default, default);

            result.Success.Should().BeTrue();
            captureSolverMock.Verify(x => x.Solve(seq, It.IsAny<CaptureSolverParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
            telescopeMediatorMock.Verify(x => x.Sync(It.IsAny<Coordinates>()), Times.Exactly(2));
            telescopeMediatorMock.Verify(x => x.SlewToCoordinatesAsync(It.IsAny<Coordinates>()), Times.Exactly(2));
        }

        [Test]
        public async Task Successful_Centering_FailedSyncs_CenteringWithOffset_Test() {
            var seq = new CaptureSequence();
            var coordinates1 = new Coordinates(Angle.ByDegree(3), Angle.ByDegree(3), Epoch.JNOW);
            var coordinates2 = new Coordinates(Angle.ByDegree(4), Angle.ByDegree(3), Epoch.JNOW);
            var coordinates3 = new Coordinates(Angle.ByDegree(5), Angle.ByDegree(3), Epoch.JNOW);
            var parameter = new CenterSolveParameter() {
                Coordinates = coordinates3.Transform(Epoch.JNOW),
                FocalLength = 700,
                Threshold = 1
            };
            var testResult = new PlateSolveResult() {
                Success = true,
                Coordinates = new Coordinates(Angle.ByDegree(5), Angle.ByDegree(3), Epoch.JNOW)
            };

            captureSolverMock
                .Setup(x => x.Solve(seq, It.IsAny<CaptureSolverParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(testResult);
            telescopeMediatorMock
                .SetupSequence(x => x.GetCurrentPosition())
                .Returns(coordinates1)
                .Returns(coordinates1);
            telescopeMediatorMock
                .SetupSequence(x => x.Sync(It.IsAny<Coordinates>()))
                .Returns(Task.FromResult(false));

            var sut = new CenteringSolver(plateSolverMock.Object, blindSolverMock.Object, null, telescopeMediatorMock.Object);
            sut.CaptureSolver = captureSolverMock.Object;

            var result = await sut.Center(seq, parameter, default, default, default);

            result.Success.Should().BeTrue();
            captureSolverMock.Verify(x => x.Solve(seq, It.IsAny<CaptureSolverParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            telescopeMediatorMock.Verify(x => x.Sync(It.IsAny<Coordinates>()), Times.Exactly(1));
            telescopeMediatorMock.Verify(x => x.SlewToCoordinatesAsync(It.IsAny<Coordinates>()), Times.Exactly(1));
        }

        [Test]
        public async Task Successful_Centering_SkippedSyncs_CenteringWithOffset_Test() {
            var seq = new CaptureSequence();
            var coordinates1 = new Coordinates(Angle.ByDegree(3), Angle.ByDegree(3), Epoch.JNOW);
            var coordinates2 = new Coordinates(Angle.ByDegree(4), Angle.ByDegree(3), Epoch.JNOW);
            var coordinates3 = new Coordinates(Angle.ByDegree(5), Angle.ByDegree(3), Epoch.JNOW);
            var parameter = new CenterSolveParameter() {
                Coordinates = coordinates3.Transform(Epoch.JNOW),
                FocalLength = 700,
                Threshold = 1,
                NoSync = true
            };
            var testResult = new PlateSolveResult() {
                Success = true,
                Coordinates = new Coordinates(Angle.ByDegree(5), Angle.ByDegree(3), Epoch.JNOW)
            };

            captureSolverMock
                .Setup(x => x.Solve(seq, It.IsAny<CaptureSolverParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(testResult);
            telescopeMediatorMock
                .SetupSequence(x => x.GetCurrentPosition())
                .Returns(coordinates1)
                .Returns(coordinates1);
            telescopeMediatorMock
                .SetupSequence(x => x.Sync(It.IsAny<Coordinates>()))
                .Returns(Task.FromResult(false));

            var sut = new CenteringSolver(plateSolverMock.Object, blindSolverMock.Object, null, telescopeMediatorMock.Object);
            sut.CaptureSolver = captureSolverMock.Object;

            var result = await sut.Center(seq, parameter, default, default, default);

            result.Success.Should().BeTrue();
            captureSolverMock.Verify(x => x.Solve(seq, It.IsAny<CaptureSolverParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            telescopeMediatorMock.Verify(x => x.Sync(It.IsAny<Coordinates>()), Times.Exactly(0));
            telescopeMediatorMock.Verify(x => x.SlewToCoordinatesAsync(It.IsAny<Coordinates>()), Times.Exactly(1));
        }

        [Test]
        public async Task Successful_Centering_SilentlyFailedSyncs_CenteringWithOffset_Test() {
            var seq = new CaptureSequence();
            var coordinates1 = new Coordinates(Angle.ByDegree(3), Angle.ByDegree(3), Epoch.JNOW);
            var coordinates2 = new Coordinates(Angle.ByDegree(4), Angle.ByDegree(3), Epoch.JNOW);
            var coordinates3 = new Coordinates(Angle.ByDegree(5), Angle.ByDegree(3), Epoch.JNOW);
            var parameter = new CenterSolveParameter() {
                Coordinates = coordinates3.Transform(Epoch.JNOW),
                FocalLength = 700,
                Threshold = 1
            };
            var testResult = new PlateSolveResult() {
                Success = true,
                Coordinates = new Coordinates(Angle.ByDegree(5), Angle.ByDegree(3), Epoch.JNOW)
            };

            captureSolverMock
                .Setup(x => x.Solve(seq, It.IsAny<CaptureSolverParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(testResult);
            telescopeMediatorMock
                .SetupSequence(x => x.GetCurrentPosition())
                .Returns(coordinates1)
                .Returns(coordinates1)
                .Returns(coordinates1);
            telescopeMediatorMock
                .SetupSequence(x => x.Sync(It.IsAny<Coordinates>()))
                .Returns(Task.FromResult(true));

            var sut = new CenteringSolver(plateSolverMock.Object, blindSolverMock.Object, null, telescopeMediatorMock.Object);
            sut.CaptureSolver = captureSolverMock.Object;

            var result = await sut.Center(seq, parameter, default, default, default);

            result.Success.Should().BeTrue();
            captureSolverMock.Verify(x => x.Solve(seq, It.IsAny<CaptureSolverParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            telescopeMediatorMock.Verify(x => x.Sync(It.IsAny<Coordinates>()), Times.Exactly(1));
            telescopeMediatorMock.Verify(x => x.SlewToCoordinatesAsync(It.IsAny<Coordinates>()), Times.Exactly(1));
        }
    }
}
