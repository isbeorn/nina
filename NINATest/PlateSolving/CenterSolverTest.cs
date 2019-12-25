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
                .Returns(coordinates2)
                .Returns(coordinates3);

            var sut = new CenteringSolver(plateSolverMock.Object, blindSolverMock.Object, null, telescopeMediatorMock.Object);
            sut.CaptureSolver = captureSolverMock.Object;

            var result = await sut.Center(seq, parameter, default, default, default);

            result.Success.Should().BeTrue();
            captureSolverMock.Verify(x => x.Solve(seq, It.IsAny<CaptureSolverParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
            telescopeMediatorMock.Verify(x => x.Sync(It.IsAny<Coordinates>()), Times.Exactly(2));
            telescopeMediatorMock.Verify(x => x.SlewToCoordinatesAsync(It.IsAny<Coordinates>()), Times.Exactly(2));
        }
    }
}