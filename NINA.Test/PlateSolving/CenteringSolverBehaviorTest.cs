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
using NINA.Astrometry;
using NINA.Core.Model;
using NINA.Equipment.Equipment.MyDome;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Model;
using NINA.PlateSolving;
using NINA.PlateSolving.Interfaces;
using NINA.Core.Locale;

namespace NINA.Test.PlateSolving {

    [TestFixture]
    public class CenteringSolverBehaviorTest {

        /// <summary>
        /// Verifies invalid centering input is rejected before capture, sync, slew, or filter operations are attempted.
        /// </summary>
        [Test]
        public async Task Center_InvalidParameterRejectsBeforeSideEffects() {
            var harness = new CenteringHarness();
            var sut = harness.CreateSolver();

            Func<Task> nullParameter = () => sut.Center(new CaptureSequence(), null, default, default, CancellationToken.None);
            Func<Task> missingCoordinates = () => sut.Center(new CaptureSequence(), new CenterSolveParameter { FocalLength = 700, Threshold = 1 }, default, default, CancellationToken.None);
            Func<Task> invalidThreshold = () => sut.Center(new CaptureSequence(), new CenterSolveParameter { FocalLength = 700, Threshold = 0, Coordinates = CreateCoordinates(10, 20) }, default, default, CancellationToken.None);

            await nullParameter.Should().ThrowAsync<ArgumentException>();
            await missingCoordinates.Should().ThrowAsync<ArgumentException>();
            await invalidThreshold.Should().ThrowAsync<ArgumentException>();
            harness.CaptureSolver.Verify(x => x.Solve(It.IsAny<CaptureSequence>(), It.IsAny<CaptureSolverParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Never());
            harness.TelescopeMediator.Verify(x => x.SlewToCoordinatesAsync(It.IsAny<Coordinates>(), It.IsAny<CancellationToken>()), Times.Never());
        }

        /// <summary>
        /// Verifies a connected dome can be synchronized during centering even when no application progress reporter is supplied.
        /// </summary>
        [Test]
        public async Task Center_DomeSynchronizationAllowsNullApplicationProgress() {
            var harness = new CenteringHarness();
            var target = CreateCoordinates(5, 3);
            var offTarget = CreateCoordinates(3, 3);
            var parameter = new CenterSolveParameter {
                Coordinates = target,
                FocalLength = 700,
                Threshold = 1
            };
            var sequence = new CaptureSequence();
            harness.CaptureSolver
                .SetupSequence(x => x.Solve(sequence, It.IsAny<CaptureSolverParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PlateSolveResult { Success = true, Coordinates = offTarget })
                .ReturnsAsync(new PlateSolveResult { Success = true, Coordinates = target });
            harness.TelescopeMediator
                .SetupSequence(x => x.GetCurrentPosition())
                .Returns(target)
                .Returns(CreateCoordinates(5.1, 3))
                .Returns(CreateCoordinates(5.1, 3))
                .Returns(target);
            harness.TelescopeMediator.Setup(x => x.Sync(It.IsAny<Coordinates>())).ReturnsAsync(true);
            harness.DomeMediator.Setup(x => x.GetInfo()).Returns(new DomeInfo { Connected = true, CanSetAzimuth = true });
            harness.DomeFollower.SetupGet(x => x.IsFollowing).Returns(false);
            harness.DomeFollower.Setup(x => x.TriggerTelescopeSync()).ReturnsAsync(true);
            var sut = harness.CreateSolver();

            PlateSolveResult result = await sut.Center(sequence, parameter, default, progress: null, CancellationToken.None);

            result.Success.Should().BeTrue();
            harness.DomeFollower.Verify(x => x.TriggerTelescopeSync(), Times.Once());
            harness.TelescopeMediator.Verify(x => x.SlewToCoordinatesAsync(It.IsAny<Coordinates>(), It.IsAny<CancellationToken>()), Times.Once());
        }

        /// <summary>
        /// Verifies centering stops after ten unsuccessful correction attempts and returns the measured failed centering history.
        /// </summary>
        [Test]
        public async Task Center_MaxSlewAttemptsReturnsFailureWithAttemptHistory() {
            var harness = new CenteringHarness();
            var target = CreateCoordinates(5, 3);
            var offTarget = CreateCoordinates(3, 3);
            var sequence = new CaptureSequence();
            var parameter = new CenterSolveParameter {
                Coordinates = target,
                FocalLength = 700,
                Threshold = 0.1,
                NoSync = true
            };
            harness.CaptureSolver
                .Setup(x => x.Solve(sequence, It.IsAny<CaptureSolverParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PlateSolveResult { Success = true, Coordinates = offTarget });
            harness.TelescopeMediator.Setup(x => x.GetCurrentPosition()).Returns(target);
            var sut = harness.CreateSolver();

            CenteringSolveResult result = await sut.CenterWithMeasurements(sequence, parameter, default, default, CancellationToken.None);

            result.Success.Should().BeFalse();
            result.Attempts.Should().HaveCount(10);
            result.Attempts.Should().OnlyContain(x => x.PlateSolveResult != null);
            harness.CaptureSolver.Verify(x => x.Solve(sequence, It.IsAny<CaptureSolverParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Exactly(10));
            harness.TelescopeMediator.Verify(x => x.SlewToCoordinatesAsync(It.IsAny<Coordinates>(), It.IsAny<CancellationToken>()), Times.Exactly(10));
            harness.TelescopeMediator.Verify(x => x.Sync(It.IsAny<Coordinates>()), Times.Never());
        }

        /// <summary>
        /// Verifies centering stops immediately when a correction slew is rejected by the mount.
        /// </summary>
        [Test]
        public async Task Center_CorrectionSlewFailureStopsCenteringWithFailureReason() {
            var harness = new CenteringHarness();
            var target = CreateCoordinates(5, 3);
            var offTarget = CreateCoordinates(3, 3);
            var sequence = new CaptureSequence();
            var parameter = new CenterSolveParameter {
                Coordinates = target,
                FocalLength = 700,
                Threshold = 0.1,
                NoSync = true
            };
            harness.CaptureSolver
                .Setup(x => x.Solve(sequence, It.IsAny<CaptureSolverParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PlateSolveResult { Success = true, Coordinates = offTarget });
            harness.TelescopeMediator.Setup(x => x.GetCurrentPosition()).Returns(target);
            harness.TelescopeMediator.Setup(x => x.SlewToCoordinatesAsync(It.IsAny<Coordinates>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            var sut = harness.CreateSolver();

            CenteringSolveResult result = await sut.CenterWithMeasurements(sequence, parameter, default, default, CancellationToken.None);

            result.Success.Should().BeFalse();
            result.Attempts.Should().HaveCount(1);
            harness.CaptureSolver.Verify(x => x.Solve(sequence, It.IsAny<CaptureSolverParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once());
            harness.TelescopeMediator.Verify(x => x.SlewToCoordinatesAsync(It.IsAny<Coordinates>(), It.IsAny<CancellationToken>()), Times.Once());
        }

        /// <summary>
        /// Verifies fallback correction slews use a spherical correction instead of raw RA/Dec addition.
        /// </summary>
        [Test]
        public async Task Center_CorrectionSlewUsesSphericalRotationForHighDeclinationFallback() {
            var harness = new CenteringHarness();
            var target = CreateCoordinates(25, 80);
            var offTarget = CreateCoordinates(20, 79.25);
            var sequence = new CaptureSequence();
            var parameter = new CenterSolveParameter {
                Coordinates = target,
                FocalLength = 700,
                Threshold = 0.1,
                NoSync = true
            };
            harness.CaptureSolver
                .SetupSequence(x => x.Solve(sequence, It.IsAny<CaptureSolverParameter>(), It.IsAny<IProgress<PlateSolveProgress>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PlateSolveResult { Success = true, Coordinates = offTarget })
                .ReturnsAsync(new PlateSolveResult { Success = true, Coordinates = target });
            harness.TelescopeMediator.Setup(x => x.GetCurrentPosition()).Returns(target);
            harness.TelescopeMediator.Setup(x => x.SlewToCoordinatesAsync(It.IsAny<Coordinates>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            var sut = harness.CreateSolver();

            CenteringSolveResult result = await sut.CenterWithMeasurements(sequence, parameter, default, default, CancellationToken.None);

            Coordinates expectedSlewCoordinates = ExpectedCorrectedTarget(target, target, offTarget);
            result.Success.Should().BeTrue();
            harness.TelescopeMediator.Verify(x => x.SlewToCoordinatesAsync(
                It.Is<Coordinates>(c => Math.Abs((c - expectedSlewCoordinates).Distance.ArcSeconds) <= 0.01),
                It.IsAny<CancellationToken>()), Times.Once());
        }

        private static Coordinates CreateCoordinates(double raDegrees, double decDegrees) {
            return new Coordinates(Angle.ByDegree(raDegrees), Angle.ByDegree(decDegrees), Epoch.J2000);
        }

        private static Coordinates ExpectedCorrectedTarget(Coordinates targetCoordinates, Coordinates reportedCoordinates, Coordinates solvedCoordinates) {
            RectangularCoordinates solved = RectangularCoordinates.FromPolar(solvedCoordinates);
            RectangularCoordinates reported = RectangularCoordinates.FromPolar(reportedCoordinates);
            RectangularCoordinates target = RectangularCoordinates.FromPolar(targetCoordinates);
            RectangularCoordinates axis = solved.Cross(reported);
            double sinAngle = axis.Distance;
            if (sinAngle < 1e-12) {
                return targetCoordinates;
            }

            double cosAngle = solved.Dot(reported);
            return target.RotateAroundAxis(axis / sinAngle, Angle.ByRadians(Math.Atan2(sinAngle, cosAngle))).ToPolar(targetCoordinates.Epoch);
        }

        private sealed class CenteringHarness {
            public Mock<IPlateSolver> PlateSolver { get; } = new();
            public Mock<IPlateSolver> BlindSolver { get; } = new();
            public Mock<IImagingMediator> ImagingMediator { get; } = new();
            public Mock<ITelescopeMediator> TelescopeMediator { get; } = new();
            public Mock<IFilterWheelMediator> FilterWheelMediator { get; } = new();
            public Mock<IDomeMediator> DomeMediator { get; } = new();
            public Mock<IDomeFollower> DomeFollower { get; } = new();
            public Mock<ICaptureSolver> CaptureSolver { get; } = new();

            public CenteringHarness() {
                DomeMediator.Setup(x => x.GetInfo()).Returns(new DomeInfo { Connected = false });
                TelescopeMediator.Setup(x => x.SlewToCoordinatesAsync(It.IsAny<Coordinates>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            }

            public CenteringSolver CreateSolver() {
                var solver = new CenteringSolver(PlateSolver.Object, BlindSolver.Object, ImagingMediator.Object, TelescopeMediator.Object, FilterWheelMediator.Object, DomeMediator.Object, DomeFollower.Object) {
                    CaptureSolver = CaptureSolver.Object
                };
                return solver;
            }
        }
    }
}
