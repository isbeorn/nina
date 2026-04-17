using FluentAssertions;
using Moq;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.WPF.Base.Mediator;
using OxyPlot;

namespace NINA.Test.Mediator {

    [TestFixture]
    public class FocuserMediatorTest {

        /// <summary>
        /// Verifies that a successful autofocus notification records the focused temperature and then notifies consumers.
        /// This preserves the invariant that consumers observing the event see a handler that has already accepted the new focus temperature.
        /// </summary>
        [Test]
        public void BroadcastSuccessfulAutoFocusRun_UpdatesFocusedTemperatureAndNotifiesConsumers() {
            AutoFocusInfo autoFocusInfo = new AutoFocusInfo(-7.25, 15234, "Ha", new DateTime(2026, 4, 16, 22, 15, 0));
            Mock<IFocuserVM> handler = new Mock<IFocuserVM>();
            Mock<IFocuserConsumer> consumer = new Mock<IFocuserConsumer>();
            MockSequence sequence = new MockSequence();
            FocuserMediator mediator = new FocuserMediator();

            handler.Setup(x => x.GetDeviceInfo()).Returns(new FocuserInfo());
            handler.InSequence(sequence).Setup(x => x.SetFocusedTemperature(autoFocusInfo.Temperature));
            consumer.InSequence(sequence).Setup(x => x.UpdateEndAutoFocusRun(autoFocusInfo));

            mediator.RegisterHandler(handler.Object);
            mediator.RegisterConsumer(consumer.Object);
            mediator.BroadcastSuccessfulAutoFocusRun(autoFocusInfo);

            handler.Verify(x => x.SetFocusedTemperature(autoFocusInfo.Temperature), Times.Once);
            consumer.Verify(x => x.UpdateEndAutoFocusRun(autoFocusInfo), Times.Once);
        }

        /// <summary>
        /// Verifies that a user-focused notification updates the handler temperature and continues past a failing consumer.
        /// This matches the shared broadcast isolation behavior for manual focus updates.
        /// </summary>
        [Test]
        public void BroadcastUserFocused_WhenConsumerThrows_StillNotifiesRemainingConsumers() {
            FocuserInfo focuserInfo = new FocuserInfo { Temperature = 3.5, Position = 9988 };
            Mock<IFocuserVM> handler = new Mock<IFocuserVM>();
            Mock<IFocuserConsumer> failingConsumer = new Mock<IFocuserConsumer>();
            Mock<IFocuserConsumer> succeedingConsumer = new Mock<IFocuserConsumer>();
            FocuserMediator mediator = new FocuserMediator();

            handler.Setup(x => x.GetDeviceInfo()).Returns(new FocuserInfo());
            failingConsumer.Setup(x => x.UpdateUserFocused(focuserInfo)).Throws(new InvalidOperationException("consumer failed"));

            mediator.RegisterHandler(handler.Object);
            mediator.RegisterConsumer(failingConsumer.Object);
            mediator.RegisterConsumer(succeedingConsumer.Object);

            Action broadcast = () => mediator.BroadcastUserFocused(focuserInfo);

            broadcast.Should().NotThrow();
            handler.Verify(x => x.SetFocusedTemperature(focuserInfo.Temperature), Times.Once);
            succeedingConsumer.Verify(x => x.UpdateUserFocused(focuserInfo), Times.Once);
        }

        /// <summary>
        /// Verifies that autofocus run-start notifications are delivered to all registered consumers.
        /// This covers the beginning of an autofocus sequence where consumers reset state before points arrive.
        /// </summary>
        [Test]
        public void BroadcastAutoFocusRunStarting_NotifiesEveryRegisteredConsumer() {
            Mock<IFocuserConsumer> firstConsumer = new Mock<IFocuserConsumer>();
            Mock<IFocuserConsumer> secondConsumer = new Mock<IFocuserConsumer>();
            FocuserMediator mediator = new FocuserMediator();

            mediator.RegisterConsumer(firstConsumer.Object);
            mediator.RegisterConsumer(secondConsumer.Object);
            mediator.BroadcastAutoFocusRunStarting();

            firstConsumer.Verify(x => x.AutoFocusRunStarting(), Times.Once);
            secondConsumer.Verify(x => x.AutoFocusRunStarting(), Times.Once);
        }

        /// <summary>
        /// Verifies that new autofocus measurement points are forwarded without mutation.
        /// This preserves scientific traceability for focus curves plotted from the mediator stream.
        /// </summary>
        [Test]
        public void BroadcastNewAutoFocusPoint_ForwardsPointToConsumers() {
            DataPoint focusPoint = new DataPoint(15200, 2.31);
            Mock<IFocuserConsumer> consumer = new Mock<IFocuserConsumer>();
            FocuserMediator mediator = new FocuserMediator();

            mediator.RegisterConsumer(consumer.Object);
            mediator.BroadcastNewAutoFocusPoint(focusPoint);

            consumer.Verify(x => x.NewAutoFocusPoint(focusPoint), Times.Once);
        }
    }
}
