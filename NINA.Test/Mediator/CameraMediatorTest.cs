using FluentAssertions;
using Moq;
using NINA.Core.Model;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Equipment.Model;
using NINA.WPF.Base.Mediator;
using System.Threading;

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

        /// <summary>
        /// Verifies that aborting an exposure also cancels the active mediator capture while still forwarding the hardware abort.
        /// This protects plugin callers that abort independently of the token supplied when capture started.
        /// </summary>
        [Test]
        public async Task AbortExposure_WhenCaptureIsActive_CancelsCaptureAndForwardsAbort() {
            TaskCompletionSource<bool> captureStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
            Mock<ICameraVM> handler = new Mock<ICameraVM>();
            handler.Setup(x => x.Capture(It.IsAny<CaptureSequence>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>()))
                .Returns<CaptureSequence, CancellationToken, IProgress<ApplicationStatus>>(async (_, token, _) => {
                    captureStarted.TrySetResult(true);
                    await Task.Delay(Timeout.InfiniteTimeSpan, token);
                });
            CameraMediator mediator = CreateMediator(handler);

            Task captureTask = Capture(mediator, CancellationToken.None);
            await captureStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

            mediator.AbortExposure();

            Func<Task> waitForCapture = async () => await captureTask.WaitAsync(TimeSpan.FromSeconds(1));
            await waitForCapture.Should().ThrowAsync<OperationCanceledException>();
            handler.Verify(x => x.AbortExposure(), Times.Once);
        }

        /// <summary>
        /// Verifies that the caller-provided cancellation token retains its normal cancellation behavior.
        /// This protects built-in capture flows that own and cancel their operation token directly.
        /// </summary>
        [Test]
        public async Task Capture_WhenCallerTokenIsCancelled_CancelsCapture() {
            TaskCompletionSource<bool> captureStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
            Mock<ICameraVM> handler = new Mock<ICameraVM>();
            handler.Setup(x => x.Capture(It.IsAny<CaptureSequence>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>()))
                .Returns<CaptureSequence, CancellationToken, IProgress<ApplicationStatus>>(async (_, token, _) => {
                    captureStarted.TrySetResult(true);
                    await Task.Delay(Timeout.InfiniteTimeSpan, token);
                });
            CameraMediator mediator = CreateMediator(handler);
            using CancellationTokenSource callerCts = new CancellationTokenSource();
            Task captureTask = Capture(mediator, callerCts.Token);
            await captureStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

            callerCts.Cancel();

            Func<Task> waitForCapture = async () => await captureTask.WaitAsync(TimeSpan.FromSeconds(1));
            await waitForCapture.Should().ThrowAsync<OperationCanceledException>();
        }

        /// <summary>
        /// Verifies that an abort only cancels captures that were already in flight.
        /// This protects the supported workflow of starting a new exposure immediately after an abort.
        /// </summary>
        [Test]
        public async Task Capture_WhenStartedAfterAbort_UsesFreshCancellationToken() {
            TaskCompletionSource<bool> firstCaptureStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource<bool> secondCaptureStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource<bool> releaseSecondCapture = new(TaskCreationOptions.RunContinuationsAsynchronously);
            CancellationToken secondCaptureToken = default;
            int captureCount = 0;
            Mock<ICameraVM> handler = new Mock<ICameraVM>();
            handler.Setup(x => x.Capture(It.IsAny<CaptureSequence>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>()))
                .Returns<CaptureSequence, CancellationToken, IProgress<ApplicationStatus>>(async (_, token, _) => {
                    if (Interlocked.Increment(ref captureCount) == 1) {
                        firstCaptureStarted.TrySetResult(true);
                        await Task.Delay(Timeout.InfiniteTimeSpan, token);
                    } else {
                        secondCaptureToken = token;
                        secondCaptureStarted.TrySetResult(true);
                        await releaseSecondCapture.Task.WaitAsync(token);
                    }
                });
            CameraMediator mediator = CreateMediator(handler);

            Task firstCaptureTask = Capture(mediator, CancellationToken.None);
            await firstCaptureStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
            mediator.AbortExposure();
            Func<Task> waitForFirstCapture = async () => await firstCaptureTask.WaitAsync(TimeSpan.FromSeconds(1));
            await waitForFirstCapture.Should().ThrowAsync<OperationCanceledException>();

            Task secondCaptureTask = Capture(mediator, CancellationToken.None);
            await secondCaptureStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

            secondCaptureToken.IsCancellationRequested.Should().BeFalse();
            releaseSecondCapture.TrySetResult(true);
            await secondCaptureTask.WaitAsync(TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// Verifies that aborting after one capture has completed does not poison the next capture.
        /// This protects idle abort calls and cleanup races between successive captures.
        /// </summary>
        [Test]
        public async Task AbortExposure_AfterCaptureCompleted_DoesNotCancelLaterCapture() {
            TaskCompletionSource<bool> secondCaptureStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource<bool> releaseSecondCapture = new(TaskCreationOptions.RunContinuationsAsynchronously);
            CancellationToken secondCaptureToken = default;
            int captureCount = 0;
            Mock<ICameraVM> handler = new Mock<ICameraVM>();
            handler.Setup(x => x.Capture(It.IsAny<CaptureSequence>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>()))
                .Returns<CaptureSequence, CancellationToken, IProgress<ApplicationStatus>>(async (_, token, _) => {
                    if (Interlocked.Increment(ref captureCount) == 1) {
                        return;
                    }

                    secondCaptureToken = token;
                    secondCaptureStarted.TrySetResult(true);
                    await releaseSecondCapture.Task.WaitAsync(token);
                });
            CameraMediator mediator = CreateMediator(handler);

            await Capture(mediator, CancellationToken.None);
            mediator.AbortExposure();
            Task secondCaptureTask = Capture(mediator, CancellationToken.None);
            await secondCaptureStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

            secondCaptureToken.IsCancellationRequested.Should().BeFalse();
            releaseSecondCapture.TrySetResult(true);
            await secondCaptureTask.WaitAsync(TimeSpan.FromSeconds(1));
        }

        private static Task Capture(CameraMediator mediator, CancellationToken token) {
            return mediator.Capture(new CaptureSequence(), token, Mock.Of<IProgress<ApplicationStatus>>());
        }

        private static CameraMediator CreateMediator(Mock<ICameraVM> handler) {
            CameraMediator mediator = new CameraMediator();
            mediator.RegisterHandler(handler.Object);
            return mediator;
        }
    }
}
