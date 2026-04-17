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
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.PlateSolving;
using NINA.PlateSolving.Solvers;

namespace NINA.Test.PlateSolving {

    [TestFixture]
    public class BaseSolverBehaviorTest {

        /// <summary>
        /// Verifies the base solver validates configuration first and then derives immutable image geometry from the source image and plate solve parameters.
        /// </summary>
        [Test]
        public async Task SolveAsync_ValidatesAndCreatesImagePropertiesBeforeSolving() {
            var solver = new RecordingSolver();
            var imageData = new Mock<IImageData>();
            imageData.SetupGet(x => x.Properties).Returns(new ImageProperties(640, 480, 16, false, 0, 0));
            var parameter = new PlateSolveParameter {
                FocalLength = 600,
                PixelSize = 3.76,
                Binning = 2
            };

            PlateSolveResult result = await solver.SolveAsync(imageData.Object, parameter, default, CancellationToken.None);

            result.Success.Should().BeTrue();
            solver.ValidateCalled.Should().BeTrue();
            solver.SolveImplCalled.Should().BeTrue();
            solver.SeenImageProperties.FocalLength.Should().Be(600);
            solver.SeenImageProperties.PixelSize.Should().BeApproximately(7.52, 1e-10);
            solver.SeenImageProperties.ImageWidth.Should().Be(640);
            solver.SeenImageProperties.ImageHeight.Should().Be(480);
            solver.SeenImageProperties.ArcSecPerPixel.Should().BeGreaterThan(0);
            solver.SeenImageProperties.FoVW.Should().BeGreaterThan(solver.SeenImageProperties.FoVH);
        }

        /// <summary>
        /// Verifies validation failures stop the solve before image metadata is consumed or external solver work begins.
        /// </summary>
        [Test]
        public async Task SolveAsync_ValidationFailurePreventsSolverImplementation() {
            var solver = new RecordingSolver { ThrowDuringValidation = true };
            var imageData = new Mock<IImageData>(MockBehavior.Strict);
            var parameter = new PlateSolveParameter {
                FocalLength = 600,
                PixelSize = 3.76
            };

            Func<Task> act = () => solver.SolveAsync(imageData.Object, parameter, default, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("invalid solver");
            solver.ValidateCalled.Should().BeTrue();
            solver.SolveImplCalled.Should().BeFalse();
        }

        private sealed class RecordingSolver : BaseSolver {
            public bool ValidateCalled { get; private set; }
            public bool SolveImplCalled { get; private set; }
            public bool ThrowDuringValidation { get; set; }
            public PlateSolveImageProperties SeenImageProperties { get; private set; }

            protected override void EnsureSolverValid(PlateSolveParameter parameter) {
                ValidateCalled = true;
                if (ThrowDuringValidation) {
                    throw new InvalidOperationException("invalid solver");
                }
            }

            protected override Task<PlateSolveResult> SolveAsyncImpl(IImageData source, PlateSolveParameter parameter, PlateSolveImageProperties imageProperties, IProgress<ApplicationStatus> progress, CancellationToken canceltoken) {
                SolveImplCalled = true;
                SeenImageProperties = imageProperties;
                return Task.FromResult(new PlateSolveResult { Success = true });
            }
        }
    }
}
