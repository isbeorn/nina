using FluentAssertions;
using Moq;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.WPF.Base.Mediator;

namespace NINA.Test.Mediator {

    [TestFixture]
    public class SwitchMediatorTest {

        [TestCase("mediator")]
        [TestCase("vm")]
        public async Task LegacyImplementation_UsesDefaultUnsupportedIdentityOperation(string contract) {
            var handler = new LegacySwitchVM();
            var mediator = new LegacySwitchMediator();
            mediator.RegisterHandler(handler);
            var operation = contract == "mediator"
                ? ((ISwitchMediator)mediator).SetSwitchValueByIdentity("driver", 7, 0.5, null, CancellationToken.None)
                : ((ISwitchVM)handler).SetSwitchValueByIdentity("driver", 7, 0.5, null, CancellationToken.None);
            Func<Task> write = () => operation;

            await write.Should().ThrowAsync<NotSupportedException>()
                .WithMessage(Loc.Instance["LblHubSwitchIdentityUnsupported"]);
            handler.NumericWrites.Should().Be(0);

            if (contract == "mediator") {
#pragma warning disable CS0618 // Exercise the retained index API or its compatibility fallback.
                await ((ISwitchMediator)mediator).SetSwitchValue(0, 0.5, null, CancellationToken.None);
#pragma warning restore CS0618
            } else {
#pragma warning disable CS0618 // Exercise the retained index API or its compatibility fallback.
                await ((ISwitchVM)handler).SetSwitchValue(0, 0.5, null, CancellationToken.None);
#pragma warning restore CS0618
            }

            handler.NumericWrites.Should().Be(1, "existing numeric operations remain usable");
        }

        [TestCase(typeof(ISwitchMediator))]
        [TestCase(typeof(ISwitchVM))]
        [TestCase(typeof(SwitchMediator))]
        [TestCase(typeof(NINA.WPF.Base.ViewModel.Equipment.Switch.SwitchVM))]
        public void IndexOperationIsObsoleteButRemainsSupported(Type contract) {
            var operation = contract.GetMethod("SetSwitchValue", new[] {
                typeof(short), typeof(double), typeof(IProgress<ApplicationStatus>), typeof(CancellationToken)
            });

            var attribute = operation!.GetCustomAttributes(typeof(ObsoleteAttribute), false)
                .Should().ContainSingle().Which.Should().BeOfType<ObsoleteAttribute>().Subject;
            attribute.IsError.Should().BeFalse();
            attribute.Message.Should().Contain("SetSwitchValueByIdentity");
        }

        [Test]
        public async Task IdentityWritesForwardToHandler() {
            var handler = new Mock<ISwitchVM>();
            ISwitchMediator mediator = new SwitchMediator();
            mediator.RegisterHandler(handler.Object);
            using var cancellation = new CancellationTokenSource();
            var progress = new Progress<ApplicationStatus>();
            handler.Setup(x => x.SetSwitchValueByIdentity("driver", 7, 0.5, progress, cancellation.Token)).Returns(Task.CompletedTask);

            await mediator.SetSwitchValueByIdentity("driver", 7, 0.5, progress, cancellation.Token);

            handler.Verify(x => x.SetSwitchValueByIdentity("driver", 7, 0.5, progress, cancellation.Token), Times.Once);
        }

        [Test]
        public async Task LegacyHandlerFailsWithoutNumericFallback() {
            var handler = new LegacySwitchVM();
            ISwitchMediator mediator = new SwitchMediator();
            mediator.RegisterHandler(handler);

            Func<Task> set = () => mediator.SetSwitchValueByIdentity("driver", 7, 0.5, null, CancellationToken.None);

            await set.Should().ThrowAsync<NotSupportedException>()
                .WithMessage(Loc.Instance["LblHubSwitchIdentityUnsupported"]);
            handler.NumericWrites.Should().Be(0);
        }
    }
}
