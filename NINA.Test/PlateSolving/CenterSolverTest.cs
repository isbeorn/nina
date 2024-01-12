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
using NINA.PlateSolving;
using NINA.Astrometry;
using NINA.Equipment.Interfaces.Mediator;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Equipment.Model;
using NINA.Core.Model;
using NINA.PlateSolving.Interfaces;
using NINA.Equipment.Interfaces;

namespace NINA.Test.PlateSolving {

    [TestFixture]
    public class CenterSolverTest {
        private Mock<IPlateSolver> plateSolverMock;
        private Mock<IPlateSolver> blindSolverMock;
        private Mock<ITelescopeMediator> telescopeMediatorMock;
        private Mock<ICaptureSolver> captureSolverMock;
        private Mock<IFilterWheelMediator> filterMediatorMock;
        private Mock<IDomeMediator> domeMediatorMock;
        private Mock<IDomeFollower> domeFollowerMock;

        [SetUp]
        public void Setup() {
            plateSolverMock = new Mock<IPlateSolver>();
            blindSolverMock = new Mock<IPlateSolver>();
            captureSolverMock = new Mock<ICaptureSolver>();
            telescopeMediatorMock = new Mock<ITelescopeMediator>();
            filterMediatorMock = new Mock<IFilterWheelMediator>();
            domeMediatorMock = new Mock<IDomeMediator>();
            domeMediatorMock.Setup(m => m.GetInfo()).Returns(new NINA.Equipment.Equipment.MyDome.DomeInfo() { Connected = false });
            domeFollowerMock = new Mock<IDomeFollower>();
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

            var sut = new CenteringSolver(plateSolverMock.Object, blindSolverMock.Object, null, telescopeMediatorMock.Object, filterMediatorMock.Object, domeMediatorMock.Object, domeFollowerMock.Object);
            sut.CaptureSolver = captureSolverMock.Object;

            var result = await sut.Center(seq, parameter, default, default, default);

            result.Success.Should().BeTrue();
            captureSolverMock.Verify(x => x.Solve(seq, It.IsAny<CaptureSolverParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once());
            telescopeMediatorMock.Verify(x => x.Sync(It.IsAny<Coordinates>()), Times.Never());
            telescopeMediatorMock.Verify(x => x.SlewToCoordinatesAsync(It.IsAny<Coordinates>(), It.IsAny<CancellationToken>()), Times.Never());
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

            var sut = new CenteringSolver(plateSolverMock.Object, blindSolverMock.Object, null, telescopeMediatorMock.Object, filterMediatorMock.Object, domeMediatorMock.Object, domeFollowerMock.Object);
            sut.CaptureSolver = captureSolverMock.Object;

            var result = await sut.Center(seq, parameter, default, default, default);

            result.Success.Should().BeTrue();
            captureSolverMock.Verify(x => x.Solve(seq, It.IsAny<CaptureSolverParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once());
            telescopeMediatorMock.Verify(x => x.Sync(It.IsAny<Coordinates>()), Times.Never());
            telescopeMediatorMock.Verify(x => x.SlewToCoordinatesAsync(It.IsAny<Coordinates>(), It.IsAny<CancellationToken>()), Times.Never());
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

            captureSolverMock
                .SetupSequence(x => x.Solve(seq, It.IsAny<CaptureSolverParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PlateSolveResult() { Success = true, Coordinates = coordinates1 })
                .ReturnsAsync(new PlateSolveResult() { Success = true, Coordinates = coordinates2 })
                .ReturnsAsync(new PlateSolveResult() { Success = true, Coordinates = coordinates3 });
            telescopeMediatorMock
                .SetupSequence(x => x.GetCurrentPosition())
                .Returns(coordinates3) // Telescope think it is on target
                .Returns(coordinates1) // Coordinates after first solve after sync
                .Returns(coordinates1) // Coordinates after first solve after sync
                .Returns(coordinates3) // Telescope think it is on target
                .Returns(coordinates2) // Coordinates after second solve after sync
                .Returns(coordinates2) // Coordinates after second solve after sync
                .Returns(coordinates3) // Telescope think it is on target
                .Returns(coordinates3);// Telescope really is on target
            telescopeMediatorMock
                .SetupSequence(x => x.Sync(It.IsAny<Coordinates>()))
                .Returns(Task.FromResult(true))
                .Returns(Task.FromResult(true))
                .Returns(Task.FromResult(true));

            var sut = new CenteringSolver(plateSolverMock.Object, blindSolverMock.Object, null, telescopeMediatorMock.Object, filterMediatorMock.Object, domeMediatorMock.Object, domeFollowerMock.Object);
            sut.CaptureSolver = captureSolverMock.Object;

            var result = await sut.Center(seq, parameter, default, default, default);

            result.Success.Should().BeTrue();
            captureSolverMock.Verify(x => x.Solve(seq, It.IsAny<CaptureSolverParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
            telescopeMediatorMock.Verify(x => x.Sync(It.IsAny<Coordinates>()), Times.Exactly(2));
            telescopeMediatorMock.Verify(x => x.SlewToCoordinatesAsync(It.IsAny<Coordinates>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Test]
        public async Task Successful_Centering_FailedSyncs_CenteringWithOffset_Test() {
            var seq = new CaptureSequence();
            var coordinates1 = new Coordinates(Angle.ByDegree(3), Angle.ByDegree(3), Epoch.J2000);
            var coordinates2 = new Coordinates(Angle.ByDegree(4), Angle.ByDegree(3), Epoch.J2000);
            var coordinates3 = new Coordinates(Angle.ByDegree(5), Angle.ByDegree(3), Epoch.J2000);
            var parameter = new CenterSolveParameter() {
                Coordinates = coordinates3.Transform(Epoch.J2000),
                FocalLength = 700,
                Threshold = 1
            };

            captureSolverMock
                .SetupSequence(x => x.Solve(seq, It.IsAny<CaptureSolverParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PlateSolveResult() { Success = true, Coordinates = coordinates1 })
                .ReturnsAsync(new PlateSolveResult() { Success = true, Coordinates = coordinates3 });
            telescopeMediatorMock
                .SetupSequence(x => x.GetCurrentPosition())
                .Returns(coordinates3) // Telescope think it is on target
                .Returns(coordinates3) // Telescope doesn't accept syncs and is still reporting old coordinates
                .Returns(coordinates3);// Telescope doesn't accept syncs and is still reporting old coordinates
            telescopeMediatorMock
                .SetupSequence(x => x.Sync(It.IsAny<Coordinates>()))
                .ReturnsAsync(false)
                .ReturnsAsync(false);

            var sut = new CenteringSolver(plateSolverMock.Object, blindSolverMock.Object, null, telescopeMediatorMock.Object, filterMediatorMock.Object, domeMediatorMock.Object, domeFollowerMock.Object);
            sut.CaptureSolver = captureSolverMock.Object;

            var result = await sut.Center(seq, parameter, default, default, default);

            result.Success.Should().BeTrue();
            captureSolverMock.Verify(x => x.Solve(seq, It.IsAny<CaptureSolverParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            telescopeMediatorMock.Verify(x => x.Sync(It.IsAny<Coordinates>()), Times.Exactly(1));

            //Verify that the slew coordinates are using the offset
            telescopeMediatorMock.Verify(x => x.SlewToCoordinatesAsync(
                It.Is<Coordinates>(c => Math.Round(c.RA, 4) == Math.Round((coordinates3.RA + (coordinates3.RA - coordinates1.RA)), 4)
                                        && Math.Round(c.Dec, 4) == Math.Round((coordinates3.Dec + (coordinates3.Dec - coordinates1.Dec)), 4)
                                  ),
                It.IsAny<CancellationToken>()), Times.Exactly(1));
        }

        [Test]
        public async Task Successful_Centering_SkippedSyncs_CenteringWithOffset_Test() {
            var seq = new CaptureSequence();
            var coordinates1 = new Coordinates(Angle.ByDegree(3), Angle.ByDegree(3), Epoch.J2000);
            var coordinates2 = new Coordinates(Angle.ByDegree(4), Angle.ByDegree(3), Epoch.J2000);
            var coordinates3 = new Coordinates(Angle.ByDegree(5), Angle.ByDegree(3), Epoch.J2000);
            var parameter = new CenterSolveParameter() {
                Coordinates = coordinates3.Transform(Epoch.J2000),
                FocalLength = 700,
                Threshold = 1,
                NoSync = true
            };

            captureSolverMock
                .SetupSequence(x => x.Solve(seq, It.IsAny<CaptureSolverParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PlateSolveResult() { Success = true, Coordinates = coordinates1 })
                .ReturnsAsync(new PlateSolveResult() { Success = true, Coordinates = coordinates3 });
            telescopeMediatorMock
                .SetupSequence(x => x.GetCurrentPosition())
                .Returns(coordinates3) // Telescope think it is on target
                .Returns(coordinates3) // Telescope doesn't accept syncs and is still reporting old coordinates
                .Returns(coordinates3);// Telescope doesn't accept syncs and is still reporting old coordinates
            telescopeMediatorMock
                .SetupSequence(x => x.Sync(It.IsAny<Coordinates>()))
                .ReturnsAsync(false)
                .ReturnsAsync(false);

            var sut = new CenteringSolver(plateSolverMock.Object, blindSolverMock.Object, null, telescopeMediatorMock.Object, filterMediatorMock.Object, domeMediatorMock.Object, domeFollowerMock.Object);
            sut.CaptureSolver = captureSolverMock.Object;

            var result = await sut.Center(seq, parameter, default, default, default);

            result.Success.Should().BeTrue();
            captureSolverMock.Verify(x => x.Solve(seq, It.IsAny<CaptureSolverParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            telescopeMediatorMock.Verify(x => x.Sync(It.IsAny<Coordinates>()), Times.Exactly(0));
            //Verify that the slew coordinates are using the offset
            telescopeMediatorMock.Verify(x => x.SlewToCoordinatesAsync(
                It.Is<Coordinates>(c => Math.Round(c.RA, 4) == Math.Round((coordinates3.RA + (coordinates3.RA - coordinates1.RA)), 4)
                                        && Math.Round(c.Dec, 4) == Math.Round((coordinates3.Dec + (coordinates3.Dec - coordinates1.Dec)), 4)
                                  ),
                It.IsAny<CancellationToken>()), Times.Exactly(1));
        }

        [Test]
        public async Task Successful_Centering_SilentlyFailedSyncs_CenteringWithOffset_Test() {
            var seq = new CaptureSequence();
            var coordinates1 = new Coordinates(Angle.ByDegree(3), Angle.ByDegree(3), Epoch.J2000);
            var coordinates2 = new Coordinates(Angle.ByDegree(4), Angle.ByDegree(3), Epoch.J2000);
            var coordinates3 = new Coordinates(Angle.ByDegree(5), Angle.ByDegree(3), Epoch.J2000);
            var parameter = new CenterSolveParameter() {
                Coordinates = coordinates3.Transform(Epoch.J2000),
                FocalLength = 700,
                Threshold = 1
            };

            captureSolverMock
                .SetupSequence(x => x.Solve(seq, It.IsAny<CaptureSolverParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PlateSolveResult() { Success = true, Coordinates = coordinates1 })
                .ReturnsAsync(new PlateSolveResult() { Success = true, Coordinates = coordinates3 });
            telescopeMediatorMock
                .SetupSequence(x => x.GetCurrentPosition())
                .Returns(coordinates3) // Telescope think it is on target
                .Returns(coordinates3) // Telescope doesn't accept syncs and is still reporting old coordinates
                .Returns(coordinates3) // Telescope doesn't accept syncs and is still reporting old coordinates
                .Returns(coordinates3);// Telescope think it is on target
            telescopeMediatorMock
                .SetupSequence(x => x.Sync(It.IsAny<Coordinates>()))
                .Returns(Task.FromResult(true));

            var sut = new CenteringSolver(plateSolverMock.Object, blindSolverMock.Object, null, telescopeMediatorMock.Object, filterMediatorMock.Object, domeMediatorMock.Object, domeFollowerMock.Object);
            sut.CaptureSolver = captureSolverMock.Object;

            var result = await sut.Center(seq, parameter, default, default, default);

            result.Success.Should().BeTrue();
            captureSolverMock.Verify(x => x.Solve(seq, It.IsAny<CaptureSolverParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            telescopeMediatorMock.Verify(x => x.Sync(It.IsAny<Coordinates>()), Times.Exactly(1));
            //Verify that the slew coordinates are using the offset
            telescopeMediatorMock.Verify(x => x.SlewToCoordinatesAsync(
                It.Is<Coordinates>(c => Math.Round(c.RA, 4) == Math.Round((coordinates3.RA + (coordinates3.RA - coordinates1.RA)), 4)
                                        && Math.Round(c.Dec, 4) == Math.Round((coordinates3.Dec + (coordinates3.Dec - coordinates1.Dec)), 4)
                                  ),
                It.IsAny<CancellationToken>()), Times.Exactly(1));
        }
    }
}