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
using NINA.Image.Interfaces;
using NINA.PlateSolving;
using NINA.PlateSolving.Interfaces;

namespace NINA.Test.PlateSolving {

    [TestFixture]
    public class ImageSolverBehaviorTest {

        /// <summary>
        /// Verifies a failed hinted solve does not invoke the blind solver when blind failover is explicitly disabled.
        /// </summary>
        [Test]
        public async Task Solve_PrimaryFailureDoesNotBlindFailoverWhenDisabled() {
            var plateSolver = new Mock<IPlateSolver>();
            var blindSolver = new Mock<IPlateSolver>(MockBehavior.Strict);
            var source = Mock.Of<IImageData>();
            var statuses = new List<ApplicationStatus>();
            var progress = new RecordingProgress<ApplicationStatus>(statuses);
            var parameter = new PlateSolveParameter {
                FocalLength = 700,
                BlindFailoverEnabled = false,
                Coordinates = new Coordinates(Angle.ByDegree(15), Angle.ByDegree(-10), Epoch.J2000)
            };
            plateSolver
                .Setup(x => x.SolveAsync(source, parameter, progress, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PlateSolveResult { Success = false });
            var sut = new ImageSolver(plateSolver.Object, blindSolver.Object);

            PlateSolveResult result = await sut.Solve(source, parameter, progress, CancellationToken.None);

            result.Success.Should().BeFalse();
            blindSolver.Verify(x => x.SolveAsync(It.IsAny<IImageData>(), It.IsAny<PlateSolveParameter>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Never());
            statuses.Should().Contain(x => !string.IsNullOrWhiteSpace(x.Status));
            statuses.Last().Status.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies blind failover preserves the original hinted solve parameters while using a cloned blind parameter without coordinates.
        /// </summary>
        [Test]
        public async Task Solve_BlindFailoverClonesParameterAndClearsCoordinatesOnlyForBlindSolver() {
            var plateSolver = new Mock<IPlateSolver>();
            var blindSolver = new Mock<IPlateSolver>();
            var source = Mock.Of<IImageData>();
            PlateSolveParameter blindParameter = null;
            var originalCoordinates = new Coordinates(Angle.ByDegree(15), Angle.ByDegree(-10), Epoch.J2000);
            var parameter = new PlateSolveParameter {
                FocalLength = 700,
                PixelSize = 2.4,
                Binning = 2,
                SearchRadius = 3,
                BlindFailoverEnabled = true,
                DisableNotifications = true,
                Coordinates = originalCoordinates
            };
            plateSolver
                .Setup(x => x.SolveAsync(source, parameter, It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PlateSolveResult { Success = false });
            blindSolver
                .Setup(x => x.SolveAsync(source, It.IsAny<PlateSolveParameter>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .Callback<IImageData, PlateSolveParameter, IProgress<ApplicationStatus>, CancellationToken>((_, p, _, _) => blindParameter = p)
                .ReturnsAsync(new PlateSolveResult { Success = true });
            var sut = new ImageSolver(plateSolver.Object, blindSolver.Object);

            PlateSolveResult result = await sut.Solve(source, parameter, default, CancellationToken.None);

            result.Success.Should().BeTrue();
            blindParameter.Should().NotBeNull();
            blindParameter.Should().NotBeSameAs(parameter);
            blindParameter.Coordinates.Should().BeNull();
            blindParameter.FocalLength.Should().Be(parameter.FocalLength);
            blindParameter.PixelSize.Should().Be(parameter.PixelSize);
            blindParameter.SearchRadius.Should().Be(parameter.SearchRadius);
            blindParameter.DisableNotifications.Should().BeTrue();
            parameter.Coordinates.Should().NotBeNull();
            parameter.Coordinates.RADegrees.Should().BeApproximately(originalCoordinates.RADegrees, 1e-10);
            parameter.Coordinates.Dec.Should().BeApproximately(originalCoordinates.Dec, 1e-10);
        }

        private sealed class RecordingProgress<T> : IProgress<T> {
            private readonly IList<T> values;

            public RecordingProgress(IList<T> values) {
                this.values = values;
            }

            public void Report(T value) {
                values.Add(value);
            }
        }
    }
}
