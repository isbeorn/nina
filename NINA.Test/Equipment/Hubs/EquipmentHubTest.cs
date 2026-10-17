using FluentAssertions;
using Moq;
using NINA.Equipment.Equipment;
using NINA.Equipment.Equipment.Hubs;
using NINA.Equipment.Equipment.MySafetyMonitor;
using NINA.Equipment.Equipment.MySwitch;
using NINA.Equipment.Equipment.MyWeatherData;
using NINA.Equipment.Interfaces;
using NINA.Profile;
using NINA.Sequencer.Logic;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Equipment.Hubs {
    [TestFixture]
    public class EquipmentHubTest {
        private static Mock<T> Device<T>(string id, bool connects = true) where T : class, IDevice {
            var device = new Mock<T>();
            bool connected = false;
            device.SetupGet(x => x.Id).Returns(id);
            device.SetupGet(x => x.Name).Returns("Same display name");
            device.SetupGet(x => x.Connected).Returns(() => connected);
            device.Setup(x => x.Connect(It.IsAny<CancellationToken>())).Returns((CancellationToken token) => {
                connected = connects;
                return Task.FromResult(connects);
            });
            device.Setup(x => x.Disconnect()).Callback(() => connected = false);
            return device;
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task SafetyFailedChild_CleansEveryAttemptAndAllowsReconfiguration(bool throws) {
            var first = Device<ISafetyMonitor>("a");
            var second = Device<ISafetyMonitor>("b", false);
            if (throws) {
                second.Setup(x => x.Connect(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("failure"));
            }
            var hub = new SafetyMonitorHub("Safety");
            hub.Configure(new[] { new HubDevice(1, first.Object), new HubDevice(2, second.Object) });

            (await hub.Connect(CancellationToken.None)).Should().BeFalse();

            first.Verify(x => x.Disconnect(), Times.Once);
            second.Verify(x => x.Disconnect(), Times.Once);
            hub.IsSessionActive.Should().BeFalse();
            hub.Configure(new[] { new HubDevice(1, first.Object) });
            (await hub.Connect(CancellationToken.None)).Should().BeTrue();
            hub.Invoking(x => x.Configure(Array.Empty<HubDevice>())).Should().Throw<InvalidOperationException>();
            hub.Disconnect();
            hub.IsSessionActive.Should().BeFalse();
            first.Verify(x => x.Disconnect(), Times.Exactly(2));
        }

        [Test]
        public async Task SafetyReadFailure_IsUnsafeAndRecoversOnNextRead() {
            var first = Device<ISafetyMonitor>("a");
            var second = Device<ISafetyMonitor>("b");
            first.SetupGet(x => x.IsSafe).Returns(true);
            second.SetupSequence(x => x.IsSafe).Throws(new InvalidOperationException("sensor")).Returns(true).Returns(false);
            var hub = new SafetyMonitorHub("Safety");
            hub.Configure(new[] { new HubDevice(1, first.Object), new HubDevice(2, second.Object) });
            (await hub.Connect(CancellationToken.None)).Should().BeTrue();

            hub.IsSafe.Should().BeFalse();
            hub.DeviceStatuses[1].Error.Should().Be("sensor");
            hub.IsSafe.Should().BeTrue();
            hub.DeviceStatuses[1].Error.Should().BeEmpty();
            hub.IsSafe.Should().BeFalse();
        }

        [Test]
        public async Task SafetyStatuses_UseSameReadingsAndContinueAfterUnavailableChildren() {
            var first = Device<ISafetyMonitor>("first");
            var second = Device<ISafetyMonitor>("second");
            bool connected = true;
            bool safe = true;
            first.SetupGet(x => x.Connected).Returns(() => connected);
            first.SetupSequence(x => x.IsSafe).Returns(true).Throws(new InvalidOperationException("sensor failed")).Returns(true);
            second.SetupGet(x => x.IsSafe).Returns(() => safe);
            var hub = new SafetyMonitorHub("Safety");
            hub.Configure(new[] { new HubDevice(1, first.Object), new HubDevice(2, second.Object) });
            (await hub.Connect(CancellationToken.None)).Should().BeTrue();

            hub.IsSafe.Should().BeTrue();
            hub.DeviceStatuses.Select(x => x.IsSafe).Should().Equal(true, true);
            first.VerifyGet(x => x.IsSafe, Times.Once);
            second.VerifyGet(x => x.IsSafe, Times.Once);

            hub.IsSafe.Should().BeFalse();
            hub.DeviceStatuses[0].IsSafe.Should().BeFalse();
            hub.DeviceStatuses[0].Error.Should().Be("sensor failed");
            hub.DeviceStatuses[1].IsSafe.Should().BeTrue();
            hub.IsSafe.Should().BeTrue();
            hub.DeviceStatuses[0].Error.Should().BeEmpty();

            connected = false;
            safe = false;
            hub.RefreshConnectionStatuses();
            hub.IsSafe.Should().BeFalse();
            hub.DeviceStatuses.Select(x => x.IsSafe).Should().Equal(false, false);
            hub.DeviceStatuses[0].Connected.Should().BeFalse();
            hub.DeviceStatuses[0].Error.Should().NotBeEmpty();
            safe = true;
            hub.IsSafe.Should().BeFalse();
            hub.DeviceStatuses[1].IsSafe.Should().BeTrue();
            first.VerifyGet(x => x.IsSafe, Times.Exactly(3));

            connected = true;
            first.SetupGet(x => x.IsSafe).Returns(true);
            hub.RefreshConnectionStatuses();
            hub.IsSafe.Should().BeTrue();
            hub.DeviceStatuses.Should().OnlyContain(x => x.Connected && x.IsSafe && x.Error == "");
            hub.Disconnect();
            hub.DeviceStatuses.Should().OnlyContain(x => !x.Connected && !x.IsSafe);
        }

        [Test]
        public async Task SafetyConnectionReadException_IsUnsafeAndRecoversWithoutSuppressingOtherChildren() {
            var first = Device<ISafetyMonitor>("first");
            var second = Device<ISafetyMonitor>("second");
            bool connectionThrows = false;
            first.SetupGet(device => device.Connected).Returns(() => {
                if (connectionThrows) {
                    throw new InvalidOperationException("connection status failed");
                }

                return true;
            });
            first.SetupGet(device => device.IsSafe).Returns(true);
            second.SetupGet(device => device.IsSafe).Returns(true);
            var hub = new SafetyMonitorHub("Safety");
            hub.Configure(new[] { new HubDevice(1, first.Object), new HubDevice(2, second.Object) });
            (await hub.Connect(CancellationToken.None)).Should().BeTrue();
            hub.IsSafe.Should().BeTrue();

            connectionThrows = true;
            hub.RefreshConnectionStatuses();
            hub.IsSafe.Should().BeFalse();
            hub.DeviceStatuses[0].Connected.Should().BeFalse();
            hub.DeviceStatuses[0].IsSafe.Should().BeFalse();
            hub.DeviceStatuses[0].Error.Should().Be("connection status failed");
            hub.DeviceStatuses[1].IsSafe.Should().BeTrue();
            first.VerifyGet(device => device.IsSafe, Times.Once);
            second.VerifyGet(device => device.IsSafe, Times.Exactly(2));
            connectionThrows = false;
            hub.RefreshConnectionStatuses();
            hub.IsSafe.Should().BeTrue();
            hub.DeviceStatuses.Should().OnlyContain(status => status.Connected && status.IsSafe && status.Error == "");
            hub.Disconnect();
            hub.DeviceStatuses.Should().OnlyContain(status => !status.Connected && !status.IsSafe && status.Error == "");
        }

        [Test]
        public async Task WeatherRefresh_TracksOwnedConnectionsAndPreservesFailedCleanup() {
            var good = Device<IWeatherData>("good");
            var rejected = Device<IWeatherData>("rejected", false);
            var unused = Device<IWeatherData>("unused");
            bool connected = true;
            good.SetupGet(x => x.Connected).Returns(() => connected);
            rejected.SetupGet(x => x.Connected).Returns(true);
            rejected.Setup(x => x.Disconnect()).Throws(new InvalidOperationException("cleanup failed"));
            unused.SetupGet(x => x.Connected).Returns(true);
            var hub = new WeatherDataHub("Weather");
            hub.Configure(new[] { new HubDevice(1, good.Object), new HubDevice(2, rejected.Object), new HubDevice(3, unused.Object) },
                new Dictionary<WeatherDataField, string> { [WeatherDataField.Temperature] = "good", [WeatherDataField.WindSpeed] = "rejected" });
            (await hub.Connect(CancellationToken.None)).Should().BeTrue();

            hub.RefreshConnectionStatuses();
            hub.DeviceStatuses.Select(x => x.Connected).Should().Equal(true, false, false);
            hub.DeviceStatuses[1].Error.Should().Be("cleanup failed");
            unused.Verify(x => x.Connect(It.IsAny<CancellationToken>()), Times.Never);
            connected = false;
            hub.RefreshConnectionStatuses();
            hub.DeviceStatuses[0].Connected.Should().BeFalse();
            hub.DeviceStatuses[0].Error.Should().NotBeEmpty();
            hub.IsSessionActive.Should().BeTrue();
            connected = true;
            hub.RefreshConnectionStatuses();
            hub.DeviceStatuses[0].Connected.Should().BeTrue();
            hub.DeviceStatuses[0].Error.Should().BeEmpty();
            hub.Disconnect();
            hub.RefreshConnectionStatuses();
            hub.DeviceStatuses.Should().OnlyContain(x => !x.Connected && !x.IsSafe);
        }

        [Test]
        public async Task CancellationAfterChildConnect_RollsBackEveryAttempt() {
            using var cancellation = new CancellationTokenSource();
            var first = Device<ISafetyMonitor>("a");
            var second = Device<ISafetyMonitor>("b");
            second.Setup(x => x.Connect(It.IsAny<CancellationToken>())).Returns(() => {
                cancellation.Cancel();
                return Task.FromResult(true);
            });
            var hub = new SafetyMonitorHub("Safety");
            hub.Configure(new[] { new HubDevice(1, first.Object), new HubDevice(2, second.Object) });

            Func<Task> connect = () => hub.Connect(cancellation.Token);
            await connect.Should().ThrowAsync<OperationCanceledException>();

            first.Verify(x => x.Disconnect(), Times.Once);
            second.Verify(x => x.Disconnect(), Times.Once);
            hub.Connected.Should().BeFalse();
            hub.IsSessionActive.Should().BeFalse();
            hub.DeviceStatuses.Should().OnlyContain(status => !status.Connected && !status.IsSafe && status.Error == "");
            hub.Configure(new[] { new HubDevice(1, first.Object) });
            (await hub.Connect(CancellationToken.None)).Should().BeTrue();
            hub.Disconnect();
        }

        [Test]
        public async Task DisconnectDuringConnect_DisconnectsLateSuccessfulChild() {
            var child = Device<ISafetyMonitor>("a");
            var completion = new TaskCompletionSource<bool>();
            child.Setup(x => x.Connect(It.IsAny<CancellationToken>())).Returns(completion.Task);
            var hub = new SafetyMonitorHub("Safety");
            hub.Configure(new[] { new HubDevice(1, child.Object) });
            var pending = hub.Connect(CancellationToken.None);
            hub.Disconnect();
            completion.SetResult(true);

            Func<Task> finish = () => pending;
            await finish.Should().ThrowAsync<OperationCanceledException>();
            child.Verify(x => x.Disconnect(), Times.Exactly(2));
            hub.IsSessionActive.Should().BeFalse();
        }

        [Test]
        public async Task InvalidConfiguration_ReplacesPriorConfigurationAndCannotConnect() {
            var child = Device<ISafetyMonitor>("a");
            var hub = new SafetyMonitorHub("Safety");
            hub.Configure(new[] { new HubDevice(1, child.Object) });
            hub.Configure(new[] { new HubDevice(1, child.Object), new HubDevice(2, child.Object) });

            (await hub.Connect(CancellationToken.None)).Should().BeFalse();
            child.Verify(x => x.Connect(It.IsAny<CancellationToken>()), Times.Never);
            hub.Disconnect();
            hub.Configure(new[] { new HubDevice(1, new SafetyMonitorHub("Nested")) });
            (await hub.Connect(CancellationToken.None)).Should().BeFalse();
        }

        [TestCase(WeatherDataField.AveragePeriod)]
        [TestCase(WeatherDataField.CloudCover)]
        [TestCase(WeatherDataField.DewPoint)]
        [TestCase(WeatherDataField.Humidity)]
        [TestCase(WeatherDataField.Pressure)]
        [TestCase(WeatherDataField.RainRate)]
        [TestCase(WeatherDataField.SkyBrightness)]
        [TestCase(WeatherDataField.SkyQuality)]
        [TestCase(WeatherDataField.SkyTemperature)]
        [TestCase(WeatherDataField.StarFWHM)]
        [TestCase(WeatherDataField.Temperature)]
        [TestCase(WeatherDataField.WindDirection)]
        [TestCase(WeatherDataField.WindGust)]
        [TestCase(WeatherDataField.WindSpeed)]
        public async Task WeatherMappings_ReadExactlyChosenFieldAndConnectOnlyMappedSource(WeatherDataField field) {
            var chosen = Device<IWeatherData>("chosen");
            var unused = Device<IWeatherData>("unused");
            chosen.SetupGet(x => x.AveragePeriod).Returns(1);
            chosen.SetupGet(x => x.CloudCover).Returns(2);
            chosen.SetupGet(x => x.DewPoint).Returns(3);
            chosen.SetupGet(x => x.Humidity).Returns(4);
            chosen.SetupGet(x => x.Pressure).Returns(5);
            chosen.SetupGet(x => x.RainRate).Returns(6);
            chosen.SetupGet(x => x.SkyBrightness).Returns(7);
            chosen.SetupGet(x => x.SkyQuality).Returns(8);
            chosen.SetupGet(x => x.SkyTemperature).Returns(9);
            chosen.SetupGet(x => x.StarFWHM).Returns(10);
            chosen.SetupGet(x => x.Temperature).Returns(11);
            chosen.SetupGet(x => x.WindDirection).Returns(12);
            chosen.SetupGet(x => x.WindGust).Returns(13);
            chosen.SetupGet(x => x.WindSpeed).Returns(14);
            var hub = new WeatherDataHub("Weather");
            var sources = new Dictionary<WeatherDataField, string> { [field] = "chosen" };
            hub.Configure(new[] { new HubDevice(1, chosen.Object), new HubDevice(2, unused.Object) }, sources);
            sources[field] = "unused";

            (await hub.Connect(CancellationToken.None)).Should().BeTrue();

            unused.Verify(x => x.Connect(It.IsAny<CancellationToken>()), Times.Never);
            foreach (var property in typeof(IWeatherData).GetProperties()) {
                var actual = (double)property.GetValue(hub)!;
                if (property.Name == field.ToString()) {
                    actual.Should().Be((int)field + 1);
                } else {
                    double.IsNaN(actual).Should().BeTrue();
                }
            }
            hub.Disconnect();
            chosen.Verify(x => x.Disconnect(), Times.Once);
        }

        [Test]
        public async Task WeatherPartialFailure_RetainsGoodSourceAndFieldErrorsRecoverIndependently() {
            var good = Device<IWeatherData>("good");
            var bad = Device<IWeatherData>("bad", false);
            good.SetupSequence(x => x.Temperature).Throws(new InvalidOperationException("temperature")).Returns(5);
            good.SetupGet(x => x.Humidity).Returns(42);
            var hub = new WeatherDataHub("Weather");
            hub.Configure(new[] { new HubDevice(1, good.Object), new HubDevice(2, bad.Object) },
                new Dictionary<WeatherDataField, string> {
                    [WeatherDataField.Temperature] = "good",
                    [WeatherDataField.Humidity] = "good",
                    [WeatherDataField.Pressure] = "bad"
                });

            (await hub.Connect(CancellationToken.None)).Should().BeTrue();
            double.IsNaN(hub.Temperature).Should().BeTrue();
            hub.Humidity.Should().Be(42);
            hub.DeviceStatuses[0].Error.Should().Be("temperature");
            hub.Temperature.Should().Be(5);
            hub.DeviceStatuses[0].Error.Should().BeEmpty();
            double.IsNaN(hub.Pressure).Should().BeTrue();
            good.Verify(x => x.Disconnect(), Times.Never);
            bad.Verify(x => x.Disconnect(), Times.Once);
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task WeatherRejectedSourceWithFailedCleanup_CannotSupplyValuesOrKeepSessionHealthy(bool connectThrows) {
            var good = Device<IWeatherData>("good");
            good.SetupGet(x => x.Temperature).Returns(12);
            var rejected = Device<IWeatherData>("rejected", false);
            bool rejectedConnected = true;
            rejected.SetupGet(x => x.Connected).Returns(() => rejectedConnected);
            rejected.SetupGet(x => x.Humidity).Returns(95);
            if (connectThrows) {
                rejected.Setup(x => x.Connect(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("connect"));
            } else {
                rejected.Setup(x => x.Connect(It.IsAny<CancellationToken>())).ReturnsAsync(false);
            }

            rejected.Setup(x => x.Disconnect()).Throws(new InvalidOperationException("disconnect"));
            var hub = new WeatherDataHub("Weather");
            hub.Configure(new[] { new HubDevice(1, good.Object), new HubDevice(2, rejected.Object) },
                new Dictionary<WeatherDataField, string> {
                    [WeatherDataField.Temperature] = "good",
                    [WeatherDataField.Humidity] = "rejected"
                });

            (await hub.Connect(CancellationToken.None)).Should().BeTrue();
            hub.Temperature.Should().Be(12);
            double.IsNaN(hub.Humidity).Should().BeTrue();
            rejected.VerifyGet(x => x.Humidity, Times.Never);
            rejected.Verify(x => x.Disconnect(), Times.Once);

            good.SetupGet(x => x.Connected).Returns(false);
            good.Raise(x => x.PropertyChanged += null, new PropertyChangedEventArgs("Connected"));
            hub.Connected.Should().BeFalse();
            hub.IsSessionActive.Should().BeTrue();
            double.IsNaN(hub.Humidity).Should().BeTrue();

            rejected.Setup(x => x.Disconnect()).Callback(() => rejectedConnected = false);
            hub.Disconnect();

            rejected.Verify(x => x.Disconnect(), Times.Exactly(2));
            rejectedConnected.Should().BeFalse();
            hub.IsSessionActive.Should().BeFalse();
        }

        [Test]
        public async Task WeatherCancellation_RollsBackSuccessfulSources() {
            var good = Device<IWeatherData>("good");
            var cancelled = Device<IWeatherData>("cancelled");
            cancelled.Setup(x => x.Connect(It.IsAny<CancellationToken>())).ThrowsAsync(new OperationCanceledException());
            var hub = new WeatherDataHub("Weather");
            hub.Configure(new[] { new HubDevice(1, good.Object), new HubDevice(2, cancelled.Object) },
                new Dictionary<WeatherDataField, string> {
                    [WeatherDataField.Temperature] = "good",
                    [WeatherDataField.Humidity] = "cancelled"
                });

            Func<Task> connect = () => hub.Connect(CancellationToken.None);
            await connect.Should().ThrowAsync<OperationCanceledException>();
            good.Verify(x => x.Disconnect(), Times.Once);
            cancelled.Verify(x => x.Disconnect(), Times.Once);
        }

        [Test]
        public async Task SwitchChannels_PreserveIdentityValuesNotificationsAndRejectStaleWrites() {
            var native = new Mock<IWritableSwitch>();
            var notifier = native.As<INotifyPropertyChanged>();
            native.SetupGet(x => x.Id).Returns(7);
            native.SetupGet(x => x.Name).Returns("Power output");
            native.SetupGet(x => x.Value).Returns(3);
            native.SetupGet(x => x.Minimum).Returns(-2);
            native.SetupGet(x => x.Maximum).Returns(9);
            native.SetupGet(x => x.StepSize).Returns(0.5);
            native.SetupProperty(x => x.TargetValue, 4);
            native.Setup(x => x.Poll()).Returns(true);
            var readOnly = new Mock<ISwitch>();
            readOnly.SetupGet(x => x.Id).Returns(7);
            readOnly.SetupGet(x => x.Name).Returns("Power output");
            var first = Device<ISwitchHub>("a");
            var second = Device<ISwitchHub>("b");
            first.SetupGet(x => x.Switches).Returns(new[] { native.Object });
            second.SetupGet(x => x.Switches).Returns(new[] { readOnly.Object });
            var hub = new CompositeSwitchHub("Switch");
            hub.Configure(new[] { new HubDevice(9, first.Object), new HubDevice(2, second.Object) });
            (await hub.Connect(CancellationToken.None)).Should().BeTrue();
            var writable = (IWritableSwitch)hub.Switches.First();
            hub.Switches.Last().Should().NotBeAssignableTo<IWritableSwitch>();
            hub.Switches.Select(x => x.Id).Should().OnlyHaveUniqueItems();
            hub.Switches.Select(x => x.Name).Should().OnlyHaveUniqueItems();
            var identity = (ICompositeSwitchIdentity)writable;
            identity.DeviceId.Should().Be("a");
            identity.ChannelId.Should().Be(7);
            writable.Name.Should().Be("Switch 9 - Power output");
            hub.Switches.Last().Name.Should().Be("Switch 2 - Power output");
            SymbolBroker.SanitizeIdentifier(identity.ExpressionName).Should().Be("S9_C8_Power_output");
            writable.Value.Should().Be(3);
            writable.Minimum.Should().Be(-2);
            writable.Maximum.Should().Be(9);
            writable.StepSize.Should().Be(0.5);
            writable.TargetValue.Should().Be(4);
            var events = new List<string>();
            ((INotifyPropertyChanged)writable).PropertyChanged += (_, e) => events.Add(e.PropertyName ?? string.Empty);
            notifier.Raise(x => x.PropertyChanged += null, new PropertyChangedEventArgs("Value"));
            events.Should().Equal("Value");
            native.SetupGet(channel => channel.Name).Returns("Updated power output");
            notifier.Raise(channel => channel.PropertyChanged += null, new PropertyChangedEventArgs("Name"));
            events.Should().Equal("Value", "Name");
            writable.Name.Should().Be("Switch 9 - Updated power output");

            writable.TargetValue = 6;
            writable.SetValue();
            writable.Poll().Should().BeTrue();
            hub.Disconnect();
            (await hub.Connect(CancellationToken.None)).Should().BeTrue();

            writable.Invoking(x => x.SetValue()).Should().Throw<InvalidOperationException>();
            writable.Invoking(x => x.TargetValue = 8).Should().Throw<InvalidOperationException>();
            native.Verify(x => x.SetValue(), Times.Once);
            native.Object.TargetValue.Should().Be(6);
        }

        [TestCase(1, 2)]
        [TestCase(9, 2)]
        public async Task SwitchLabels_GroupWritableAndReadOnlyChannelsByConfiguredSource(int firstSource, int secondSource) {
            var output = Mock.Of<IWritableSwitch>(channel => channel.Id == 7 && channel.Name == "Power");
            var gauge = Mock.Of<ISwitch>(channel => channel.Id == 8 && channel.Name == "Voltage");
            var otherOutput = Mock.Of<IWritableSwitch>(channel => channel.Id == 7 && channel.Name == "Power");
            var first = Device<ISwitchHub>("first");
            var second = Device<ISwitchHub>("second");
            first.SetupGet(device => device.Switches).Returns(new ISwitch[] { output, gauge });
            second.SetupGet(device => device.Switches).Returns(new ISwitch[] { otherOutput });
            var hub = new CompositeSwitchHub("Hub");
            hub.Configure(new[] { new HubDevice(firstSource, first.Object), new HubDevice(secondSource, second.Object) });
            try {
                (await hub.Connect(CancellationToken.None)).Should().BeTrue();

                hub.Switches.Select(channel => channel.Name).Should().Equal(
                    $"Switch {firstSource} - Power",
                    $"Switch {firstSource} - Voltage",
                    $"Switch {secondSource} - Power");
                hub.Switches.Select(channel => channel.Id).Should().Equal((short)0, (short)1, (short)2);
                hub.Switches.Cast<ICompositeSwitchIdentity>()
                    .Select(channel => SymbolBroker.SanitizeIdentifier(channel.ExpressionName)).Should().Equal(
                        $"S{firstSource}_C8_Power",
                        $"S{firstSource}_C9_Voltage",
                        $"S{secondSource}_C8_Power");
            } finally {
                hub.Disconnect();
            }
        }

        [TestCase(0, "S1_C1_Power")]
        [TestCase(1, "S1_C2_Power")]
        [TestCase(short.MaxValue, "S1_C32768_Power")]
        public async Task SwitchExpressionChannelNumbers_StartAtOneAndPreserveNativeIds(short nativeChannelId, string expressionIdentifier) {
            var native = Mock.Of<IWritableSwitch>(channel => channel.Id == nativeChannelId && channel.Name == "Power");
            var source = Device<ISwitchHub>("source");
            source.SetupGet(device => device.Switches).Returns(new[] { native });
            var hub = new CompositeSwitchHub("Hub");
            hub.Configure(new[] { new HubDevice(1, source.Object) });
            try {
                (await hub.Connect(CancellationToken.None)).Should().BeTrue();

                var wrapped = hub.Switches.Single();
                var identity = (ICompositeSwitchIdentity)wrapped;
                SymbolBroker.SanitizeIdentifier(identity.ExpressionName).Should().Be(expressionIdentifier);
                identity.ChannelId.Should().Be(nativeChannelId);
                wrapped.Id.Should().Be(0);
            } finally {
                hub.Disconnect();
            }
        }

        [TestCase("USB power box (roof)", "Dew heater / 12V", "Switch 2 - Dew heater / 12V", "S2_C8_Dew_heater___12V")]
        [TestCase("USB_box", "Output_2", "Switch 2 - Output_2", "S2_C8_Output_2")]
        [TestCase("\u00c9quipement 1", "Ventil\u00e1tor", "Switch 2 - Ventil\u00e1tor", "S2_C8_Ventil_tor")]
        public async Task SwitchLabels_PreserveNativeTextAndExpressionIdentifiers(
                string deviceName, string channelName, string label, string expressionIdentifier) {
            var native = Mock.Of<ISwitch>(channel => channel.Id == 7 && channel.Name == channelName);
            var child = Device<ISwitchHub>("source");
            child.SetupGet(device => device.Name).Returns(deviceName);
            child.SetupGet(device => device.Switches).Returns(new[] { native });
            var hub = new CompositeSwitchHub("Hub");
            hub.Configure(new[] { new HubDevice(2, child.Object) });
            try {
                (await hub.Connect(CancellationToken.None)).Should().BeTrue();
                hub.Switches.Single().Name.Should().Be(label);
                SymbolBroker.SanitizeIdentifier(((ICompositeSwitchIdentity)hub.Switches.Single()).ExpressionName).Should().Be(expressionIdentifier);
            } finally {
                hub.Disconnect();
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task SwitchPollFailure_FaultsAllChannelsWithoutChangingPositions(bool throws) {
            var native = new Mock<IWritableSwitch>();
            native.SetupGet(x => x.Id).Returns(1);
            if (throws) {
                native.Setup(x => x.Poll()).Throws(new InvalidOperationException("poll"));
            } else {
                native.Setup(x => x.Poll()).Returns(false);
            }
            var other = new Mock<IWritableSwitch>();
            other.SetupGet(x => x.Id).Returns(2);
            var child = Device<ISwitchHub>("a");
            child.SetupGet(x => x.Switches).Returns(new[] { native.Object, other.Object });
            var hub = new CompositeSwitchHub("Switch");
            hub.Configure(new[] { new HubDevice(1, child.Object) });
            (await hub.Connect(CancellationToken.None)).Should().BeTrue();
            var channels = hub.Switches.ToArray();

            channels[0].Poll().Should().BeFalse();
            hub.Connected.Should().BeFalse();
            hub.IsSessionActive.Should().BeTrue();
            hub.Switches.Should().Equal(channels);
            channels[1].Invoking(x => x.Poll()).Should().Throw<InvalidOperationException>();
            ((IWritableSwitch)channels[1]).Invoking(x => x.SetValue()).Should().Throw<InvalidOperationException>();
            other.Verify(x => x.Poll(), Times.Never);
            other.Verify(x => x.SetValue(), Times.Never);
        }

        [Test]
        public async Task SwitchDuplicateNativeIds_RejectsSessionAndDisconnectsAllChildrenEvenWhenCleanupThrows() {
            var channel = Mock.Of<ISwitch>(x => x.Id == 1);
            var first = Device<ISwitchHub>("a");
            var second = Device<ISwitchHub>("b");
            first.SetupGet(x => x.Switches).Returns(new[] { channel, channel });
            second.SetupGet(x => x.Switches).Returns(Array.Empty<ISwitch>());
            first.Setup(x => x.Disconnect()).Throws(new InvalidOperationException("disconnect"));
            var hub = new CompositeSwitchHub("Switch");
            hub.Configure(new[] { new HubDevice(1, first.Object), new HubDevice(2, second.Object) });

            (await hub.Connect(CancellationToken.None)).Should().BeFalse();
            first.Verify(x => x.Disconnect(), Times.Once);
            second.Verify(x => x.Disconnect(), Times.Once);
            hub.Switches.Should().BeEmpty();
        }

        [Test]
        public async Task OfflineWeatherSource_DoesNotPreventOtherMappedSourceConnecting() {
            var good = Device<IWeatherData>("good");
            var hub = new WeatherDataHub("Weather");
            hub.Configure(new[] { new HubDevice(1, good.Object), new HubDevice(2, new OfflineDevice("offline", "Missing")) },
                new Dictionary<WeatherDataField, string> {
                    [WeatherDataField.Temperature] = "good",
                    [WeatherDataField.Humidity] = "offline"
                });

            (await hub.Connect(CancellationToken.None)).Should().BeTrue();
            double.IsNaN(hub.Humidity).Should().BeTrue();
            hub.DeviceStatuses[1].Connected.Should().BeFalse();
        }

        [Test]
        public async Task SafetyChildDisconnectingDuringConnect_RollsBackInsteadOfReturningAnUncleanFalse() {
            var first = Device<ISafetyMonitor>("a");
            var second = Device<ISafetyMonitor>("b");
            second.Setup(x => x.Connect(It.IsAny<CancellationToken>())).Returns(() => {
                first.SetupGet(x => x.Connected).Returns(false);
                second.SetupGet(x => x.Connected).Returns(true);
                return Task.FromResult(true);
            });
            var hub = new SafetyMonitorHub("Safety");
            hub.Configure(new[] { new HubDevice(1, first.Object), new HubDevice(2, second.Object) });

            (await hub.Connect(CancellationToken.None)).Should().BeFalse();

            first.Verify(x => x.Disconnect(), Times.Once);
            second.Verify(x => x.Disconnect(), Times.Once);
        }

        [Test]
        public async Task WeatherAllConnectionsFail_CleansAllAttemptsAndReleasesSession() {
            var first = Device<IWeatherData>("a", false);
            var second = Device<IWeatherData>("b", false);
            var hub = new WeatherDataHub("Weather");
            hub.Configure(new[] { new HubDevice(1, first.Object), new HubDevice(2, second.Object) },
                new Dictionary<WeatherDataField, string> {
                    [WeatherDataField.Temperature] = "a",
                    [WeatherDataField.Humidity] = "b"
                });

            (await hub.Connect(CancellationToken.None)).Should().BeFalse();

            first.Verify(x => x.Disconnect(), Times.Once);
            second.Verify(x => x.Disconnect(), Times.Once);
            hub.IsSessionActive.Should().BeFalse();
            hub.Disconnect();
            hub.IsSessionActive.Should().BeFalse();
            first.Verify(x => x.Disconnect(), Times.Once);
        }

        [Test]
        public async Task SwitchChildDisconnect_FaultsSessionEvenIfChildLaterReportsConnected() {
            var native = new Mock<IWritableSwitch>();
            native.SetupGet(x => x.Id).Returns(1);
            var child = Device<ISwitchHub>("a");
            child.SetupGet(x => x.Switches).Returns(new[] { native.Object });
            var hub = new CompositeSwitchHub("Switch");
            hub.Configure(new[] { new HubDevice(1, child.Object) });
            (await hub.Connect(CancellationToken.None)).Should().BeTrue();
            var channel = (IWritableSwitch)hub.Switches.Single();

            child.SetupGet(x => x.Connected).Returns(false);
            child.Raise(x => x.PropertyChanged += null, new PropertyChangedEventArgs("Connected"));
            child.SetupGet(x => x.Connected).Returns(true);
            child.Raise(x => x.PropertyChanged += null, new PropertyChangedEventArgs("Connected"));

            hub.Connected.Should().BeFalse();
            channel.Invoking(x => x.SetValue()).Should().Throw<InvalidOperationException>();
            native.Verify(x => x.SetValue(), Times.Never);
        }

        [Test]
        public async Task SwitchConnectionFalse_RollsBackAllAttemptedChildren() {
            var first = Device<ISwitchHub>("a");
            var second = Device<ISwitchHub>("b", false);
            var hub = new CompositeSwitchHub("Switch");
            hub.Configure(new[] { new HubDevice(1, first.Object), new HubDevice(2, second.Object) });

            (await hub.Connect(CancellationToken.None)).Should().BeFalse();

            first.Verify(x => x.Disconnect(), Times.Once);
            second.Verify(x => x.Disconnect(), Times.Once);
            hub.Switches.Should().BeEmpty();
        }

        [Test]
        public async Task EmptyAndPlaceholderConfigurations_CannotConnect() {
            EquipmentHub[] hubs = { new SafetyMonitorHub("Safety"), new CompositeSwitchHub("Switch") };
            foreach (var hub in hubs) {
                (await hub.Connect(CancellationToken.None)).Should().BeFalse();
                hub.Disconnect();
            }

            var dummy = Device<ISafetyMonitor>("No_Device");
            var safety = new SafetyMonitorHub("Safety");
            safety.Configure(new[] { new HubDevice(1, dummy.Object) });
            (await safety.Connect(CancellationToken.None)).Should().BeFalse();
            dummy.Verify(x => x.Connect(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public void UnsupportedActionsAndCommands_DoNotBroadcast() {
            var child = Device<ISafetyMonitor>("a");
            var hub = new SafetyMonitorHub("Safety");
            hub.Configure(new[] { new HubDevice(1, child.Object) });

            hub.SupportedActions.Should().BeEmpty();
            hub.Invoking(x => x.Action("action", "parameters")).Should().Throw<NotSupportedException>();
            hub.Invoking(x => x.SendCommandString("raw")).Should().Throw<NotSupportedException>();
            hub.Invoking(x => x.SendCommandBool("raw")).Should().Throw<NotSupportedException>();
            hub.Invoking(x => x.SendCommandBlind("raw")).Should().Throw<NotSupportedException>();
            child.Verify(x => x.Action(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            child.Verify(x => x.SendCommandString(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
            child.Verify(x => x.SendCommandBool(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
            child.Verify(x => x.SendCommandBlind(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }
    }
}
