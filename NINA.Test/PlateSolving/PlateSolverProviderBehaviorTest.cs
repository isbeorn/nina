using FluentAssertions;
using Moq;
using NINA.Core.Model;
using NINA.Image.Interfaces;
using NINA.PlateSolving;
using NINA.PlateSolving.Interfaces;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NUnit.Framework;

namespace NINA.Test.PlateSolving {

    public partial class PlateSolverFactoryBehaviorTest {

        [TearDown]
        public void ClearProviders() {
            PlateSolverFactory.RegisterProviders(Array.Empty<IPlateSolverProvider>());
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public async Task ProviderSelection_IsLazyCapturesIdAndForwardsArguments(bool blind, bool proxy) {
            IPlateSolveSettings profileSettings = new PlateSolveSettings {
                PlateSolverProviderId = "first",
                BlindSolverProviderId = "first"
            };
            var engine = new Mock<IPlateSolver>();
            var expected = new PlateSolveResult { Success = true };
            var source = Mock.Of<IImageData>();
            var parameter = new PlateSolveParameter { FocalLength = 500 };
            IProgress<ApplicationStatus> progress = Mock.Of<IProgress<ApplicationStatus>>();
            using var cancellation = new CancellationTokenSource();
            engine.Setup(x => x.SolveAsync(source, parameter, progress, cancellation.Token)).ReturnsAsync(expected);
            var provider = Provider("first", engine.Object);
            var other = Provider("second", Mock.Of<IPlateSolver>());
            PlateSolverFactory.RegisterProviders(new[] { provider.Object, other.Object });
            IPlateSolver solver = proxy
                ? (blind ? new PlateSolverFactoryProxy().GetBlindSolver(profileSettings) : new PlateSolverFactoryProxy().GetPlateSolver(profileSettings))
                : (blind ? PlateSolverFactory.GetBlindSolver(profileSettings) : PlateSolverFactory.GetPlateSolver(profileSettings));
            provider.Verify(x => x.CreateSolver(It.IsAny<IPlateSolveSettings>()), Times.Never);
            profileSettings.PlateSolverProviderId = "second";
            profileSettings.BlindSolverProviderId = "second";

            var result = await solver.SolveAsync(source, parameter, progress, cancellation.Token);
            await solver.SolveAsync(source, parameter, progress, cancellation.Token);

            result.Should().BeSameAs(expected);
            provider.Verify(x => x.CreateSolver(profileSettings), Times.Once);
            other.Verify(x => x.CreateSolver(It.IsAny<IPlateSolveSettings>()), Times.Never);
            engine.Verify(x => x.SolveAsync(source, parameter, progress, cancellation.Token), Times.Exactly(2));
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public async Task MissingOrUnsupportedProvider_ThrowsOnlyWhenUsed(bool blind, bool installed) {
            var profileSettings = new PlateSolveSettings { PlateSolverProviderId = "selected", BlindSolverProviderId = "selected" };
            if (installed) {
                var provider = Provider("selected", Mock.Of<IPlateSolver>());
                provider.SetupGet(x => x.SupportsPrimary).Returns(blind);
                provider.SetupGet(x => x.SupportsBlind).Returns(!blind);
                PlateSolverFactory.RegisterProviders(new[] { provider.Object });
            } else {
                PlateSolverFactory.RegisterProviders(Array.Empty<IPlateSolverProvider>());
            }

            IPlateSolver solver = blind ? PlateSolverFactory.GetBlindSolver(profileSettings) : PlateSolverFactory.GetPlateSolver(profileSettings);
            Func<Task> solve = () => solver.SolveAsync(null!, new PlateSolveParameter(), null!, CancellationToken.None);

            await solve.Should().ThrowAsync<InvalidOperationException>().WithMessage(installed ? "*Test solver*" : "*selected*");
        }

        [Test]
        public async Task CancellationBeforeUse_DoesNotCreateProviderEngine() {
            var provider = Provider("cancel", Mock.Of<IPlateSolver>());
            PlateSolverFactory.RegisterProviders(new[] { provider.Object });
            var solver = PlateSolverFactory.GetPlateSolver(new PlateSolveSettings { PlateSolverProviderId = "cancel" });
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            Func<Task> solve = () => solver.SolveAsync(null!, new PlateSolveParameter(), null!, cancellation.Token);

            await solve.Should().ThrowAsync<OperationCanceledException>();
            provider.Verify(x => x.CreateSolver(It.IsAny<IPlateSolveSettings>()), Times.Never);
        }

        [Test]
        public async Task ConcurrentFirstUse_CreatesOneEngine() {
            var provider = Provider("concurrent", Mock.Of<IPlateSolver>());
            var engine = new Mock<IPlateSolver>();
            engine.Setup(x => x.SolveAsync(It.IsAny<IImageData>(), It.IsAny<PlateSolveParameter>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PlateSolveResult());
            provider.Setup(x => x.CreateSolver(It.IsAny<IPlateSolveSettings>())).Returns(() => {
                Thread.Sleep(20);
                return engine.Object;
            });
            PlateSolverFactory.RegisterProviders(new[] { provider.Object });
            var solver = PlateSolverFactory.GetPlateSolver(new PlateSolveSettings { PlateSolverProviderId = "concurrent" });

            await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() => solver.SolveAsync(null!, new PlateSolveParameter(), null!, CancellationToken.None))));

            provider.Verify(x => x.CreateSolver(It.IsAny<IPlateSolveSettings>()), Times.Once);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Registration_ExcludesEveryDuplicateAndInvalidEntryAndPublishesReadOnlySnapshot(bool reverse) {
            var first = Provider("duplicate", Mock.Of<IPlateSolver>());
            var second = Provider("duplicate", Mock.Of<IPlateSolver>());
            var valid = Provider("Duplicate", Mock.Of<IPlateSolver>());
            var noRole = Provider("no-role", Mock.Of<IPlateSolver>());
            noRole.SetupGet(x => x.SupportsPrimary).Returns(false);
            noRole.SetupGet(x => x.SupportsBlind).Returns(false);
            var throws = Provider("throws", Mock.Of<IPlateSolver>());
            throws.SetupGet(x => x.Id).Throws<InvalidOperationException>();
            var candidates = new[] { first.Object, second.Object, valid.Object, noRole.Object, throws.Object, Provider(" ", Mock.Of<IPlateSolver>()).Object };
            int changed = 0;
            EventHandler handler = (_, _) => changed++;
            PlateSolverFactory.ProvidersChanged += handler;
            try {
                var oldSnapshot = PlateSolverFactory.Providers;

                PlateSolverFactory.RegisterProviders(reverse ? candidates.Reverse() : candidates);

                changed.Should().Be(1);
                PlateSolverFactory.Providers.Should().Equal(valid.Object);
                oldSnapshot.Should().NotBeSameAs(PlateSolverFactory.Providers);
                ((IList<IPlateSolverProvider>)PlateSolverFactory.Providers).IsReadOnly.Should().BeTrue();
            } finally {
                PlateSolverFactory.ProvidersChanged -= handler;
            }
        }

        private static Mock<IPlateSolverProvider> Provider(string id, IPlateSolver solver) {
            var provider = new Mock<IPlateSolverProvider>();
            provider.SetupGet(x => x.Id).Returns(id);
            provider.SetupGet(x => x.Name).Returns("Test solver");
            provider.SetupGet(x => x.SupportsPrimary).Returns(true);
            provider.SetupGet(x => x.SupportsBlind).Returns(true);
            provider.Setup(x => x.CreateSolver(It.IsAny<IPlateSolveSettings>())).Returns(solver);
            return provider;
        }
    }
}
