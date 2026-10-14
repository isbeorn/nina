using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Test.Mediator;
using NINA.Equipment.Equipment.MySwitch;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem.Switch;
using NINA.Sequencer.Serialization;
using System.Collections.ObjectModel;

namespace NINA.Test.Sequencer.SequenceItem.Switch {

    [TestFixture]
    public class SetSwitchValueStableTargetTest {
        private Mock<ISwitchMediator> mediator;
        private SwitchInfo info;

        [SetUp]
        public void SetUp() {
            mediator = new Mock<ISwitchMediator>();
            mediator.Setup(x => x.SetSwitchValueByIdentity(
                It.IsAny<string>(), It.IsAny<short>(), It.IsAny<double>(),
                It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            info = new SwitchInfo { Connected = true };
            mediator.Setup(x => x.GetInfo()).Returns(() => info);
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task Execute_LegacyIndexCapturesStableIdentityWithOrWithoutValidation(bool validateFirst) {
            var target = Channel("driver-b", 7);
            SetChannels(Channel("driver-a", 0), target);
            var item = new SetSwitchValue(mediator.Object) { SwitchIndex = 1, ValueDefinition = "4 + 6.5" };
            if (validateFirst) {
                item.Validate().Should().BeTrue();
            }

            await item.Execute(null, CancellationToken.None);

            item.HubDeviceId.Should().Be("driver-b");
            item.HubChannelId.Should().Be(7);
            mediator.Verify(x => x.SetSwitchValueByIdentity("driver-b", 7, 10.5, null, CancellationToken.None), Times.Once);
            VerifyNoNumericWrite();
        }

        [Test]
        public async Task StableTargetSurvivesInsertionAndRemovalBeforeIt() {
            var target = Channel("driver-b", 4);
            SetChannels(Channel("driver-a", 0), target);
            var item = new SetSwitchValue(mediator.Object) { SwitchIndex = 1 };
            item.Validate().Should().BeTrue();

            SetChannels(Channel("driver-new", 0), Channel("driver-a", 0), target);
            item.Validate().Should().BeTrue();
            item.SwitchIndex.Should().Be(2);
            item.SelectedSwitch.Should().BeSameAs(target);

            SetChannels(target);
            await item.Execute(null, CancellationToken.None);

            item.SwitchIndex.Should().Be(0);
            mediator.Verify(x => x.SetSwitchValueByIdentity("driver-b", 4, 1, null, CancellationToken.None), Times.Once);
            VerifyNoNumericWrite();
        }

        [TestCase("missing")]
        [TestCase("duplicate")]
        [TestCase("read-only")]
        [TestCase("mixed-duplicate")]
        [TestCase("device-only")]
        [TestCase("channel-only")]
        [TestCase("empty-device")]
        public async Task InvalidStableTargetNeverFallsBackToNumericIndex(string scenario) {
            var target = Channel("driver-b", 7);
            SetChannels(Channel("driver-a", 0));
            var item = new SetSwitchValue(mediator.Object) { SwitchIndex = 0, HubDeviceId = "driver-b", HubChannelId = 7 };
            if (scenario == "duplicate") {
                SetChannels(target, Channel("driver-b", 7));
            } else if (scenario == "read-only" || scenario == "mixed-duplicate") {
                var readOnly = new Mock<ISwitch>();
                var identity = readOnly.As<ICompositeSwitchIdentity>();
                identity.SetupGet(x => x.DeviceId).Returns("driver-b");
                identity.SetupGet(x => x.ChannelId).Returns(7);
                info.ReadonlySwitches = new ReadOnlyCollection<ISwitch>(new[] { readOnly.Object });
                if (scenario == "mixed-duplicate") {
                    SetChannels(target);
                }
            } else if (scenario == "device-only") {
                item.HubChannelId = null;
            } else if (scenario == "channel-only") {
                item.HubDeviceId = null;
            } else if (scenario == "empty-device") {
                item.HubDeviceId = "";
            }

            item.Validate().Should().BeFalse();
            item.SelectedSwitch.Should().BeNull();
            Func<Task> execute = () => item.Execute(null, CancellationToken.None);
            await execute.Should().ThrowAsync<SequenceEntityFailedException>();

            VerifyNoNumericWrite();
            mediator.Verify(x => x.SetSwitchValueByIdentity(It.IsAny<string>(), It.IsAny<short>(),
                It.IsAny<double>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task LegacyMediatorUsesCurrentIndexWhenIdentityOperationIsUnavailable() {
            SetChannels(Channel("driver-a", 0), Channel("driver-b", 7));
            var handler = new LegacySwitchVM(info);
            var conventionalMediator = new LegacySwitchMediator();
            conventionalMediator.RegisterHandler(handler);
            var item = new SetSwitchValue(conventionalMediator) { HubDeviceId = "driver-b", HubChannelId = 7 };

            await item.Execute(null, CancellationToken.None);

            handler.NumericWrites.Should().Be(1);
            handler.LastNumericIndex.Should().Be(1);
        }

        [Test]
        public void SelectionCloneAttachmentDisconnectAndTransientNullPreserveIdentity() {
            var target = Channel("driver-b", 7);
            SetChannels(Channel("driver-a", 0), target);
            var item = new SetSwitchValue(mediator.Object);
            item.Validate();
            item.SelectedSwitch = target;
            item.SwitchIndex.Should().Be(1);
            var clone = (SetSwitchValue)item.Clone();
            clone.HubDeviceId.Should().Be("driver-b");
            clone.HubChannelId.Should().Be(7);
            clone.SwitchIndex.Should().Be(1);
            clone.AttachNewParent(new SequentialContainer());
            clone.SelectedSwitch.Should().BeSameAs(target);

            clone.SelectedSwitch = null;
            info.Connected = false;
            clone.Validate().Should().BeFalse();
            clone.HubDeviceId.Should().Be("driver-b");
            clone.HubChannelId.Should().Be(7);
            clone.SelectedSwitch.Should().BeNull();
            info.Connected = true;
            var replacement = Channel("driver-b", 7);
            SetChannels(replacement);
            clone.Validate().Should().BeTrue();
            clone.SelectedSwitch.Should().BeSameAs(replacement);
        }

        [Test]
        public void ExplicitConventionalSelectionClearsIdentity() {
            var conventional = new Mock<IWritableSwitch>();
            conventional.SetupGet(x => x.Maximum).Returns(100);
            SetChannels(Channel("driver-b", 7), conventional.Object);
            var item = new SetSwitchValue(mediator.Object);
            item.Validate();

            item.SelectedSwitch = conventional.Object;

            item.HubDeviceId.Should().BeNull();
            item.HubChannelId.Should().BeNull();
            item.SwitchIndex.Should().Be(1);
        }

        [Test]
        public void DisconnectedDummySelectionDoesNotCreateIdentity() {
            info.Connected = false;
            var item = new SetSwitchValue(mediator.Object);
            item.SelectedSwitch = item.WritableSwitches[3];
            item.Validate();

            item.SwitchIndex.Should().Be(3);
            item.HubDeviceId.Should().BeNull();
            item.HubChannelId.Should().BeNull();
        }

        [Test]
        public void ValidateRefreshesReplacementLimitsAndEmptyList() {
            SetChannels(Channel("driver-b", 7));
            var item = new SetSwitchValue(mediator.Object) { Value = 50 };
            item.Validate().Should().BeTrue();
            var replacement = Channel("driver-b", 7, maximum: 10);
            SetChannels(replacement);

            item.Validate().Should().BeFalse();
            item.SelectedSwitch.Should().BeSameAs(replacement);
            item.SelectedSwitch.Maximum.Should().Be(10);
            SetChannels();
            item.Validate().Should().BeFalse();
            item.WritableSwitches.Should().BeEmpty();
            item.SelectedSwitch.Should().BeNull();
            item.HubDeviceId.Should().Be("driver-b");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void JsonPropertyOrderPreservesIdentity(bool indexFirst) {
            var item = new SetSwitchValue(mediator.Object);
            var identity = "\"HubDeviceId\":\"driver-b\",\"HubChannelId\":7";
            JsonConvert.PopulateObject(indexFirst ? "{\"SwitchIndex\":12," + identity + "}" : "{" + identity + ",\"SwitchIndex\":12}", item);

            item.HubDeviceId.Should().Be("driver-b");
            item.HubChannelId.Should().Be(7);
            item.SwitchIndex.Should().Be(12);
        }

        [Test]
        public void SequenceJsonConverterRoundTripsStableTargetAndLegacyJsonOmitsIdentity() {
            var factory = new Mock<ISequencerFactory>();
            factory.SetupGet(x => x.Upgraders).Returns(Array.Empty<ISequenceEntityUpgrader>());
            factory.Setup(x => x.GetContainer<SequentialContainer>()).Returns(() => new SequentialContainer());
            factory.Setup(x => x.GetItem<SetSwitchValue>()).Returns(() => new SetSwitchValue(mediator.Object));
            var converter = new SequenceJsonConverter(factory.Object);
            var source = new SequentialContainer();
            source.Items.Add(new SetSwitchValue(mediator.Object) { SwitchIndex = 12, HubDeviceId = "driver-b", HubChannelId = 7 });
            info.Connected = false;

            var json = converter.Serialize(source);
            var restored = converter.Deserialize(json);
            var item = restored.Items.Should().ContainSingle().Which.Should().BeOfType<SetSwitchValue>().Subject;

            item.HubDeviceId.Should().Be("driver-b");
            item.HubChannelId.Should().Be(7);
            item.SwitchIndex.Should().Be(12);
            JsonConvert.SerializeObject(new SetSwitchValue(mediator.Object) { SwitchIndex = 4 })
                .Should().Contain("\"SwitchIndex\":4").And.NotContain("HubDeviceId").And.NotContain("HubChannelId");
        }

        [Test]
        public async Task ExecuteForwardsCancellationAndProgressToStableController() {
            SetChannels(Channel("driver-b", 7));
            var item = new SetSwitchValue(mediator.Object) { HubDeviceId = "driver-b", HubChannelId = 7 };
            using var cancellation = new CancellationTokenSource();
            var progress = new Progress<ApplicationStatus>();

            await item.Execute(progress, cancellation.Token);

            mediator.Verify(x => x.SetSwitchValueByIdentity("driver-b", 7, 1, progress, cancellation.Token), Times.Once);
            cancellation.Cancel();
            Func<Task> execute = () => item.Execute(progress, cancellation.Token);
            await execute.Should().ThrowAsync<OperationCanceledException>();
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task OrdinaryDriverUsesDeviceAndNativeChannelIdentity(bool selectExplicitly) {
            info.DeviceId = "ordinary-driver";
            var target = NativeChannel(7);
            SetChannels(NativeChannel(3), target);
            var item = new SetSwitchValue(mediator.Object) { SwitchIndex = 1, ValueDefinition = "4 + 6.5" };
            if (selectExplicitly) {
                item.SelectedSwitch = target;
            }

            await item.Execute(null, CancellationToken.None);

            item.HubDeviceId.Should().Be("ordinary-driver");
            item.HubChannelId.Should().Be(7);
            mediator.Verify(x => x.SetSwitchValueByIdentity("ordinary-driver", 7, 10.5, null, CancellationToken.None), Times.Once);
            VerifyNoNumericWrite();
        }

        [Test]
        public async Task OrdinaryIdentitySurvivesReorderReconnectCloneAndSerialization() {
            info.DeviceId = "ordinary-driver";
            var target = NativeChannel(7);
            SetChannels(NativeChannel(3), target);
            var item = new SetSwitchValue(mediator.Object) { SwitchIndex = 1 };
            item.Validate().Should().BeTrue();
            var clone = (SetSwitchValue)item.Clone();
            var restored = new SetSwitchValue(mediator.Object);
            JsonConvert.PopulateObject(JsonConvert.SerializeObject(clone), restored);
            info.Connected = false;
            restored.Validate().Should().BeFalse();
            info.Connected = true;
            SetChannels(NativeChannel(7), NativeChannel(3));

            await restored.Execute(null, CancellationToken.None);

            restored.SwitchIndex.Should().Be(0);
            mediator.Verify(x => x.SetSwitchValueByIdentity("ordinary-driver", 7, 1, null, CancellationToken.None), Times.Once);
            VerifyNoNumericWrite();
        }

        [TestCase("changed-driver")]
        [TestCase("missing")]
        [TestCase("duplicate")]
        [TestCase("read-only")]
        [TestCase("mixed-duplicate")]
        public async Task InvalidOrdinaryIdentityCannotRedirectToIndex(string scenario) {
            info.DeviceId = "ordinary-driver";
            SetChannels(NativeChannel(7));
            var item = new SetSwitchValue(mediator.Object);
            item.Validate().Should().BeTrue();
            if (scenario == "changed-driver") {
                info.DeviceId = "other-driver";
            } else if (scenario == "missing") {
                SetChannels(NativeChannel(3));
            } else if (scenario == "duplicate") {
                SetChannels(NativeChannel(7), NativeChannel(7));
            } else {
                var readOnly = new Mock<ISwitch>();
                readOnly.SetupGet(x => x.Id).Returns(7);
                info.ReadonlySwitches = new ReadOnlyCollection<ISwitch>(new[] { readOnly.Object });
                if (scenario == "read-only") {
                    SetChannels();
                }
            }

            Func<Task> execute = () => item.Execute(null, CancellationToken.None);

            await execute.Should().ThrowAsync<SequenceEntityFailedException>();
            VerifyNoNumericWrite();
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task UnsupportedIdentityReResolvesIndexBeforeFallback(bool throwsSynchronously) {
            SetChannels(Channel("driver-a", 0), Channel("driver-b", 7));
            var item = new SetSwitchValue(mediator.Object) { SwitchIndex = 1, ValueDefinition = "4 + 6.5" };
            using var cancellation = new CancellationTokenSource();
            var progress = new Progress<ApplicationStatus>();
            mediator.Setup(x => x.SetSwitchValueByIdentity("driver-b", 7, 10.5, progress, cancellation.Token))
                .Returns(() => {
                    SetChannels(Channel("driver-b", 7));
                    if (throwsSynchronously) {
                        throw new NotSupportedException();
                    }
                    return Task.FromException(new NotSupportedException());
                });

            await item.Execute(progress, cancellation.Token);

#pragma warning disable CS0618 // Exercise the retained index API or its compatibility fallback.
            mediator.Verify(x => x.SetSwitchValue(0, 10.5, progress, cancellation.Token), Times.Once);
#pragma warning restore CS0618
            item.SwitchIndex.Should().Be(0);
        }

        [TestCase("missing")]
        [TestCase("disconnect")]
        [TestCase("cancel")]
        [TestCase("failure")]
        [TestCase("timeout")]
        public async Task IdentityFailureNeverWritesByIndexUnlessUnavailableAndStillValid(string scenario) {
            SetChannels(Channel("driver-b", 7));
            var item = new SetSwitchValue(mediator.Object);
            using var cancellation = new CancellationTokenSource();
            mediator.Setup(x => x.SetSwitchValueByIdentity("driver-b", 7, 1, null, cancellation.Token))
                .Returns(() => {
                    if (scenario == "missing") {
                        SetChannels(Channel("driver-a", 0));
                    } else if (scenario == "disconnect") {
                        info.Connected = false;
                    } else if (scenario == "cancel") {
                        cancellation.Cancel();
                    }
                    Exception error = scenario == "failure" ? new InvalidOperationException()
                        : scenario == "timeout" ? new TimeoutException() : new NotSupportedException();
                    return Task.FromException(error);
                });

            Func<Task> execute = () => item.Execute(null, cancellation.Token);

            await execute.Should().ThrowAsync<Exception>();
            VerifyNoNumericWrite();
        }

        private static IWritableSwitch NativeChannel(short channelId) {
            var channel = new Mock<IWritableSwitch>();
            channel.SetupGet(x => x.Id).Returns(channelId);
            channel.SetupGet(x => x.Maximum).Returns(100);
            return channel.Object;
        }

        private static IWritableSwitch Channel(string deviceId, short channelId, double maximum = 100) {
            var channel = new Mock<IWritableSwitch>();
            channel.SetupGet(x => x.Maximum).Returns(maximum);
            var identity = channel.As<ICompositeSwitchIdentity>();
            identity.SetupGet(x => x.DeviceId).Returns(deviceId);
            identity.SetupGet(x => x.ChannelId).Returns(channelId);
            return channel.Object;
        }

        private void SetChannels(params IWritableSwitch[] channels) {
            info.WritableSwitches = new ReadOnlyCollection<IWritableSwitch>(channels);
        }

        private void VerifyNoNumericWrite() {
#pragma warning disable CS0618 // Exercise the retained index API or its compatibility fallback.
            mediator.Verify(x => x.SetSwitchValue(It.IsAny<short>(), It.IsAny<double>(),
                It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Never);
#pragma warning restore CS0618
        }
    }
}
