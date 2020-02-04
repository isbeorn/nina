#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using FluentAssertions;
using Moq;
using NINA.Model;
using NINA.Model.ImageData;
using NINA.PlateSolving;
using NINA.Utility.Mediator;
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
    public class CaptureSolverTest {
        private Mock<IPlateSolver> plateSolverMock;
        private Mock<IPlateSolver> blindSolverMock;
        private Mock<IImagingMediator> imagingMediatorMock;
        private Mock<IImageSolver> imageSolverMock;

        [SetUp]
        public void Setup() {
            plateSolverMock = new Mock<IPlateSolver>();
            blindSolverMock = new Mock<IPlateSolver>();
            imagingMediatorMock = new Mock<IImagingMediator>();
            imageSolverMock = new Mock<IImageSolver>();
        }

        [Test]
        public async Task Successful_CaptureAndSolving_Test() {
            var imageDataMock = new Mock<IImageData>();
            var renderedImageMock = new Mock<IRenderedImage>();
            renderedImageMock.SetupGet(x => x.RawImageData).Returns(imageDataMock.Object);
            var testResult = new PlateSolveResult() {
                Success = true
            };
            var seq = new CaptureSequence();
            var parameter = new CaptureSolverParameter() { FocalLength = 700 };

            imagingMediatorMock.Setup(x => x.CaptureAndPrepareImage(seq, It.IsAny<PrepareImageParameters>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>())).ReturnsAsync(renderedImageMock.Object);
            imageSolverMock.Setup(x => x.Solve(imageDataMock.Object, It.IsAny<PlateSolveParameter>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).ReturnsAsync(testResult);

            var sut = new CaptureSolver(plateSolverMock.Object, blindSolverMock.Object, imagingMediatorMock.Object);
            sut.ImageSolver = imageSolverMock.Object;

            var result = await sut.Solve(seq, parameter, default, default, default);

            result.Success.Should().BeTrue();
            imagingMediatorMock.Verify(x => x.CaptureAndPrepareImage(seq, It.IsAny<PrepareImageParameters>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>()), Times.Once());
            imageSolverMock.Verify(x => x.Solve(imageDataMock.Object, It.IsAny<PlateSolveParameter>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once());
        }

        [Test]
        public async Task Successful_CaptureAndSolving_WithReattempt_Test() {
            var imageDataMock = new Mock<IImageData>();
            var renderedImageMock = new Mock<IRenderedImage>();
            renderedImageMock.SetupGet(x => x.RawImageData).Returns(imageDataMock.Object);
            var failedResult = new PlateSolveResult() {
                Success = false
            };
            var testResult = new PlateSolveResult() {
                Success = true
            };
            var seq = new CaptureSequence();
            var parameter = new CaptureSolverParameter() { FocalLength = 700, Attempts = 5, ReattemptDelay = TimeSpan.FromMilliseconds(5) };

            imagingMediatorMock.Setup(x => x.CaptureAndPrepareImage(seq, It.IsAny<PrepareImageParameters>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>())).ReturnsAsync(renderedImageMock.Object);
            imageSolverMock
                .SetupSequence(x => x.Solve(imageDataMock.Object, It.IsAny<PlateSolveParameter>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(failedResult)
                .ReturnsAsync(failedResult)
                .ReturnsAsync(testResult);

            var sut = new CaptureSolver(plateSolverMock.Object, blindSolverMock.Object, imagingMediatorMock.Object);
            sut.ImageSolver = imageSolverMock.Object;

            var result = await sut.Solve(seq, parameter, default, default, default);

            result.Success.Should().BeTrue();
            imagingMediatorMock.Verify(x => x.CaptureAndPrepareImage(seq, It.IsAny<PrepareImageParameters>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>()), Times.Exactly(3));
            imageSolverMock.Verify(x => x.Solve(imageDataMock.Object, It.IsAny<PlateSolveParameter>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        }

        [Test]
        public async Task Unsuccessful_Solving_WithReattempt_Test() {
            var imageDataMock = new Mock<IImageData>();
            var renderedImageMock = new Mock<IRenderedImage>();
            renderedImageMock.SetupGet(x => x.RawImageData).Returns(imageDataMock.Object);
            var failedResult = new PlateSolveResult() {
                Success = false
            };
            var testResult = new PlateSolveResult() {
                Success = true
            };
            var seq = new CaptureSequence();
            var parameter = new CaptureSolverParameter() { FocalLength = 700, Attempts = 3, ReattemptDelay = TimeSpan.FromMilliseconds(5) };

            imagingMediatorMock.Setup(x => x.CaptureAndPrepareImage(seq, It.IsAny<PrepareImageParameters>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>())).ReturnsAsync(renderedImageMock.Object);
            imageSolverMock
                .SetupSequence(x => x.Solve(imageDataMock.Object, It.IsAny<PlateSolveParameter>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(failedResult)
                .ReturnsAsync(failedResult)
                .ReturnsAsync(failedResult);

            var sut = new CaptureSolver(plateSolverMock.Object, blindSolverMock.Object, imagingMediatorMock.Object);
            sut.ImageSolver = imageSolverMock.Object;

            var result = await sut.Solve(seq, parameter, default, default, default);

            result.Success.Should().BeFalse();
            imagingMediatorMock.Verify(x => x.CaptureAndPrepareImage(seq, It.IsAny<PrepareImageParameters>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>()), Times.Exactly(3));
            imageSolverMock.Verify(x => x.Solve(imageDataMock.Object, It.IsAny<PlateSolveParameter>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        }

        [Test]
        public async Task Unsuccessful_Capture_WithReattempt_Test() {
            var imageDataMock = new Mock<IImageData>();
            var renderedImageMock = new Mock<IRenderedImage>();
            renderedImageMock.SetupGet(x => x.RawImageData).Returns(imageDataMock.Object);
            var testResult = new PlateSolveResult() {
                Success = true
            };
            var seq = new CaptureSequence();
            var parameter = new CaptureSolverParameter() { FocalLength = 700, Attempts = 3, ReattemptDelay = TimeSpan.FromMilliseconds(5) };

            imagingMediatorMock
                .Setup(x => x.CaptureAndPrepareImage(seq, It.IsAny<PrepareImageParameters>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>()))
                .ReturnsAsync((IRenderedImage)null);
            imageSolverMock
                .Setup(x => x.Solve(imageDataMock.Object, It.IsAny<PlateSolveParameter>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(testResult);

            var sut = new CaptureSolver(plateSolverMock.Object, blindSolverMock.Object, imagingMediatorMock.Object);
            sut.ImageSolver = imageSolverMock.Object;

            var result = await sut.Solve(seq, parameter, default, default, default);

            result.Success.Should().BeFalse();
            imagingMediatorMock.Verify(x => x.CaptureAndPrepareImage(seq, It.IsAny<PrepareImageParameters>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>()), Times.Exactly(3));
            imageSolverMock.Verify(x => x.Solve(imageDataMock.Object, It.IsAny<PlateSolveParameter>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Exactly(0));
        }

        [Test]
        public async Task Successful_AfterTwoFailedCaptures_WithReattempt_Test() {
            var imageDataMock = new Mock<IImageData>();
            var renderedImageMock = new Mock<IRenderedImage>();
            renderedImageMock.SetupGet(x => x.RawImageData).Returns(imageDataMock.Object);
            var testResult = new PlateSolveResult() {
                Success = true
            };
            var seq = new CaptureSequence();
            var parameter = new CaptureSolverParameter() { FocalLength = 700, Attempts = 5, ReattemptDelay = TimeSpan.FromMilliseconds(5) };

            imagingMediatorMock
                .SetupSequence(x => x.CaptureAndPrepareImage(seq, It.IsAny<PrepareImageParameters>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>()))
                .ReturnsAsync((IRenderedImage)null)
                .ReturnsAsync((IRenderedImage)null)
                .ReturnsAsync(renderedImageMock.Object);
            imageSolverMock
                .Setup(x => x.Solve(imageDataMock.Object, It.IsAny<PlateSolveParameter>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(testResult);

            var sut = new CaptureSolver(plateSolverMock.Object, blindSolverMock.Object, imagingMediatorMock.Object);
            sut.ImageSolver = imageSolverMock.Object;

            var result = await sut.Solve(seq, parameter, default, default, default);

            result.Success.Should().BeTrue();
            imagingMediatorMock.Verify(x => x.CaptureAndPrepareImage(seq, It.IsAny<PrepareImageParameters>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>()), Times.Exactly(3));
            imageSolverMock.Verify(x => x.Solve(imageDataMock.Object, It.IsAny<PlateSolveParameter>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
        }
    }
}