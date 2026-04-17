using FluentAssertions;
using Moq;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.WPF.Base.Mediator;

namespace NINA.Test.Mediator {

    [TestFixture]
    public class DeviceMediatorTest {

        /// <summary>
        /// Verifies that consumers registered before the handler immediately receive the handler's current device info.
        /// This protects dock panels and sequencer consumers that subscribe before equipment view models finish initialization.
        /// </summary>
        [Test]
        public void RegisterHandler_BroadcastsCurrentDeviceInfoToExistingConsumers() {
            FocuserInfo currentInfo = new FocuserInfo { Position = 2048, Temperature = -4.5 };
            Mock<IFocuserVM> handler = CreateFocuserHandler(currentInfo);
            Mock<IFocuserConsumer> consumer = new Mock<IFocuserConsumer>();
            FocuserMediator mediator = new FocuserMediator();

            mediator.RegisterConsumer(consumer.Object);
            mediator.RegisterHandler(handler.Object);

            consumer.Verify(x => x.UpdateDeviceInfo(currentInfo), Times.Once);
        }

        /// <summary>
        /// Verifies that consumers registered after the handler receive an immediate snapshot of the current device info.
        /// This prevents late subscribers from showing stale or empty state until the next polling tick.
        /// </summary>
        [Test]
        public void RegisterConsumer_WhenHandlerExists_PushesCurrentDeviceInfoImmediately() {
            FocuserInfo currentInfo = new FocuserInfo { Position = 4096, Temperature = 1.25 };
            Mock<IFocuserVM> handler = CreateFocuserHandler(currentInfo);
            Mock<IFocuserConsumer> consumer = new Mock<IFocuserConsumer>();
            FocuserMediator mediator = new FocuserMediator();

            mediator.RegisterHandler(handler.Object);
            mediator.RegisterConsumer(consumer.Object);

            consumer.Verify(x => x.UpdateDeviceInfo(currentInfo), Times.Once);
        }

        /// <summary>
        /// Verifies that a failing consumer cannot prevent other registered consumers from receiving a broadcast.
        /// This covers the mediator's isolation invariant for plugin and UI consumers sharing the same equipment updates.
        /// </summary>
        [Test]
        public void Broadcast_WhenOneConsumerThrows_StillUpdatesRemainingConsumers() {
            FocuserInfo broadcastInfo = new FocuserInfo { Position = 1001 };
            Mock<IFocuserConsumer> failingConsumer = new Mock<IFocuserConsumer>();
            Mock<IFocuserConsumer> succeedingConsumer = new Mock<IFocuserConsumer>();
            FocuserMediator mediator = new FocuserMediator();

            failingConsumer.Setup(x => x.UpdateDeviceInfo(broadcastInfo)).Throws(new InvalidOperationException("consumer failed"));
            mediator.RegisterConsumer(failingConsumer.Object);
            mediator.RegisterConsumer(succeedingConsumer.Object);

            Action broadcast = () => mediator.Broadcast(broadcastInfo);

            broadcast.Should().NotThrow();
            succeedingConsumer.Verify(x => x.UpdateDeviceInfo(broadcastInfo), Times.Once);
        }

        /// <summary>
        /// Verifies that removed consumers are no longer included in later broadcasts.
        /// This guards the dispose/unsubscribe path used by shared device consumers and plugin-owned views.
        /// </summary>
        [Test]
        public void RemoveConsumer_PreventsFutureDeviceInfoUpdates() {
            FocuserInfo broadcastInfo = new FocuserInfo { Position = 1234 };
            Mock<IFocuserConsumer> consumer = new Mock<IFocuserConsumer>();
            FocuserMediator mediator = new FocuserMediator();

            mediator.RegisterConsumer(consumer.Object);
            mediator.RemoveConsumer(consumer.Object);
            mediator.Broadcast(broadcastInfo);

            consumer.Verify(x => x.UpdateDeviceInfo(It.IsAny<FocuserInfo>()), Times.Never);
        }

        /// <summary>
        /// Verifies that the base mediator forwards common equipment commands to the registered handler.
        /// This covers direct-action paths used when no strongly typed method exists for a device-specific command.
        /// </summary>
        [Test]
        public async Task CommonDeviceOperations_AreForwardedToRegisteredHandler() {
            FocuserInfo currentInfo = new FocuserInfo { Position = 77 };
            Mock<IDevice> device = new Mock<IDevice>();
            Mock<IFocuserVM> handler = CreateFocuserHandler(currentInfo);
            FocuserMediator mediator = new FocuserMediator();
            IList<string> devices = new List<string> { "Simulator" };

            handler.Setup(x => x.Rescan()).ReturnsAsync(devices);
            handler.Setup(x => x.Connect()).ReturnsAsync(true);
            handler.Setup(x => x.Disconnect()).Returns(Task.CompletedTask);
            handler.Setup(x => x.GetDevice()).Returns(device.Object);
            handler.Setup(x => x.Action("calibrate", "fast")).Returns("ok");
            handler.Setup(x => x.SendCommandString(":GV#", true)).Returns("1.0");
            handler.Setup(x => x.SendCommandBool(":CHK#", false)).Returns(true);

            mediator.RegisterHandler(handler.Object);

            (await mediator.Rescan()).Should().BeSameAs(devices);
            (await mediator.Connect()).Should().BeTrue();
            await mediator.Disconnect();
            mediator.GetInfo().Should().BeSameAs(currentInfo);
            mediator.GetDevice().Should().BeSameAs(device.Object);
            mediator.Action("calibrate", "fast").Should().Be("ok");
            mediator.SendCommandString(":GV#").Should().Be("1.0");
            mediator.SendCommandBool(":CHK#", false).Should().BeTrue();
            mediator.SendCommandBlind(":STOP#", false);

            handler.Verify(x => x.SendCommandBlind(":STOP#", false), Times.Once);
        }

        /// <summary>
        /// Verifies that common connect and rescan methods return null before a handler is registered.
        /// This documents the current nullable contract so callers can explicitly handle unregistered equipment channels.
        /// </summary>
        [Test]
        public void OptionalHandlerOperations_WhenUnregistered_ReturnNullTasksAndDefaultInfo() {
            FocuserMediator mediator = new FocuserMediator();

            mediator.Rescan().Should().BeNull();
            mediator.Connect().Should().BeNull();
            mediator.Disconnect().Should().BeNull();
            mediator.GetInfo().Should().BeNull();
        }

        /// <summary>
        /// Verifies that handler replacement is rejected for device mediators as well as app mediators.
        /// This prevents one equipment panel from silently taking ownership from another panel.
        /// </summary>
        [Test]
        public void RegisterHandler_WhenHandlerAlreadyRegistered_Throws() {
            FocuserMediator mediator = new FocuserMediator();

            mediator.RegisterHandler(CreateFocuserHandler(new FocuserInfo()).Object);
            Action registerSecondHandler = () => mediator.RegisterHandler(CreateFocuserHandler(new FocuserInfo()).Object);

            registerSecondHandler.Should().Throw<Exception>().WithMessage("Handler already registered!");
        }

        private static Mock<IFocuserVM> CreateFocuserHandler(FocuserInfo info) {
            Mock<IFocuserVM> handler = new Mock<IFocuserVM>();
            handler.Setup(x => x.GetDeviceInfo()).Returns(info);
            return handler;
        }
    }
}
