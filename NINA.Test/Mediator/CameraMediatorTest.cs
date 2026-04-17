using FluentAssertions;
using Moq;
using NINA.Equipment.Interfaces.Mediator;
using NINA.WPF.Base.Mediator;

namespace NINA.Test.Mediator {

    [TestFixture]
    public class CameraMediatorTest {

        /// <summary>
        /// Verifies that an unblocked camera is available to any consumer.
        /// This is the default invariant required before capture arbitration begins.
        /// </summary>
        [Test]
        public void IsFreeToCapture_WhenNoCaptureBlockExists_ReturnsTrueForAnyConsumer() {
            CameraMediator mediator = new CameraMediator();

            mediator.IsFreeToCapture(new object()).Should().BeTrue();
            mediator.IsFreeToCapture(Mock.Of<ICameraConsumer>()).Should().BeTrue();
        }

        /// <summary>
        /// Verifies that the active capture block is exclusive to the registering owner and rejects a second blocker.
        /// This protects long-running exposure operations from being interrupted by another subsystem taking the camera.
        /// </summary>
        [Test]
        public void RegisterCaptureBlock_WhenAlreadyBlocked_OnlyAllowsOriginalOwnerAndRejectsSecondBlocker() {
            CameraMediator mediator = new CameraMediator();
            object owner = new object();
            object other = new object();

            mediator.RegisterCaptureBlock(owner);

            mediator.IsFreeToCapture(owner).Should().BeTrue();
            mediator.IsFreeToCapture(other).Should().BeFalse();
            Action registerSecondBlocker = () => mediator.RegisterCaptureBlock(other);
            registerSecondBlocker.Should().Throw<Exception>().WithMessage("CameraMediator already blocked by *");
        }

        /// <summary>
        /// Verifies that a non-owner cannot release a camera block, but the owner can.
        /// This guards against unrelated consumers accidentally clearing another consumer's capture reservation.
        /// </summary>
        [Test]
        public void ReleaseCaptureBlock_OnlyMatchingOwnerCanReleaseBlock() {
            CameraMediator mediator = new CameraMediator();
            object owner = new object();
            object other = new object();

            mediator.RegisterCaptureBlock(owner);
            mediator.ReleaseCaptureBlock(other);
            mediator.IsFreeToCapture(other).Should().BeFalse();

            mediator.ReleaseCaptureBlock(owner);
            mediator.IsFreeToCapture(other).Should().BeTrue();
        }

        /// <summary>
        /// Verifies that the typed camera-consumer overloads use the same block state as the object overloads.
        /// This keeps plugin and internal callers consistent regardless of which overload they compile against.
        /// </summary>
        [Test]
        public void CaptureBlockTypedOverloads_ShareTheSameOwnershipState() {
            CameraMediator mediator = new CameraMediator();
            ICameraConsumer owner = Mock.Of<ICameraConsumer>();
            ICameraConsumer other = Mock.Of<ICameraConsumer>();

            mediator.RegisterCaptureBlock(owner);
            mediator.IsFreeToCapture(owner).Should().BeTrue();
            mediator.IsFreeToCapture(other).Should().BeFalse();

            mediator.ReleaseCaptureBlock(owner);
            mediator.IsFreeToCapture(other).Should().BeTrue();
        }
    }
}
