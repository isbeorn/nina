using FluentAssertions;
using Moq;
using NINA.Astrometry;
using NINA.Core.Model;
using NINA.Image.Interfaces;
using NINA.PlateSolving;
using NINA.PlateSolving.Interfaces;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NUnit.Framework;

namespace NINA.Test.PlateSolving {

    public partial class ImageSolverTest {

        [Test]
        public async Task SuccessfulPrimary_DoesNotResolveMissingBlindProvider() {
            var provider = new Mock<IPlateSolverProvider>();
            provider.SetupGet(x => x.Id).Returns("primary");
            provider.SetupGet(x => x.Name).Returns("Primary");
            provider.SetupGet(x => x.SupportsPrimary).Returns(true);
            provider.Setup(x => x.CreateSolver(It.IsAny<IPlateSolveSettings>())).Returns(plateSolverMock.Object);
            plateSolverMock.Setup(x => x.SolveAsync(It.IsAny<IImageData>(), It.IsAny<PlateSolveParameter>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PlateSolveResult { Success = true });
            PlateSolverFactory.RegisterProviders(new[] { provider.Object });
            try {
                var settings = new PlateSolveSettings { PlateSolverProviderId = "primary", BlindSolverProviderId = "missing-blind" };
                var factory = new PlateSolverFactoryProxy();
                var solver = factory.GetImageSolver(factory.GetPlateSolver(settings), factory.GetBlindSolver(settings));

                var result = await solver.Solve(null!, new PlateSolveParameter {
                    FocalLength = 500,
                    Coordinates = new Coordinates(Angle.ByDegree(10), Angle.ByDegree(20), Epoch.J2000),
                    BlindFailoverEnabled = true
                }, null!, CancellationToken.None);

                result.Success.Should().BeTrue();
                provider.Verify(x => x.CreateSolver(settings), Times.Once);
            } finally {
                PlateSolverFactory.RegisterProviders(Array.Empty<IPlateSolverProvider>());
            }
        }

        [Test]
        public async Task Failover_ClonesParameterAndClearsOnlyCoordinatesExactlyOnce() {
            var source = Mock.Of<IImageData>();
            IProgress<ApplicationStatus> progress = Mock.Of<IProgress<ApplicationStatus>>();
            using var cancellation = new CancellationTokenSource();
            var coordinates = new Coordinates(Angle.ByDegree(10), Angle.ByDegree(20), Epoch.J2000);
            var parameter = new PlateSolveParameter { FocalLength = 500, Coordinates = coordinates, BlindFailoverEnabled = true, SearchRadius = 12, DownSampleFactor = 3 };
            var originalCoordinates = parameter.Coordinates;
            PlateSolveParameter? blindParameter = null;
            plateSolverMock.Setup(x => x.SolveAsync(source, parameter, progress, cancellation.Token)).ReturnsAsync(new PlateSolveResult { Success = false });
            blindSolverMock.Setup(x => x.SolveAsync(source, It.IsAny<PlateSolveParameter>(), progress, cancellation.Token))
                .Callback<IImageData, PlateSolveParameter, IProgress<ApplicationStatus>, CancellationToken>((_, p, _, _) => blindParameter = p)
                .ReturnsAsync(new PlateSolveResult { Success = false });

            await new ImageSolver(plateSolverMock.Object, blindSolverMock.Object).Solve(source, parameter, progress, cancellation.Token);

            parameter.Coordinates.Should().BeSameAs(originalCoordinates);
            blindParameter.Should().NotBeSameAs(parameter);
            blindParameter!.Coordinates.Should().BeNull();
            blindParameter.FocalLength.Should().Be(parameter.FocalLength);
            blindParameter.SearchRadius.Should().Be(parameter.SearchRadius);
            blindParameter.DownSampleFactor.Should().Be(parameter.DownSampleFactor);
            blindSolverMock.Verify(x => x.SolveAsync(source, It.IsAny<PlateSolveParameter>(), progress, cancellation.Token), Times.Once);
            plateSolverMock.Verify(x => x.SolveAsync(source, parameter, progress, cancellation.Token), Times.Once);
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task PrimaryExceptionOrCancellation_PropagatesWithoutBlindFailover(bool cancelled) {
            Exception expected = cancelled ? new OperationCanceledException() : new InvalidOperationException("provider failure");
            plateSolverMock.Setup(x => x.SolveAsync(It.IsAny<IImageData>(), It.IsAny<PlateSolveParameter>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(expected);
            var parameter = new PlateSolveParameter { FocalLength = 500, Coordinates = new Coordinates(Angle.Zero, Angle.Zero, Epoch.J2000), BlindFailoverEnabled = true };

            Func<Task> solve = () => new ImageSolver(plateSolverMock.Object, blindSolverMock.Object).Solve(null!, parameter, null!, CancellationToken.None);

            (await solve.Should().ThrowAsync<Exception>()).Which.Should().BeSameAs(expected);
            blindSolverMock.Verify(x => x.SolveAsync(It.IsAny<IImageData>(), It.IsAny<PlateSolveParameter>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
