using FluentAssertions;
using Moq;
using NINA.Core.Model;
using NINA.Image.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.Mediator;

namespace NINA.Test.Mediator {

    [TestFixture]
    public class ImageSaveMediatorTest {

        /// <summary>
        /// Verifies that queued image saves are forwarded with the original image, prepared-image task, progress object, and cancellation token.
        /// This protects the save pipeline from accidentally losing metadata or cancellation semantics at the mediator boundary.
        /// </summary>
        [Test]
        public async Task Enqueue_ForwardsOriginalArgumentsToRegisteredController() {
            Mock<IImageSaveController> controller = new Mock<IImageSaveController>();
            Mock<IImageData> imageData = new Mock<IImageData>();
            Mock<IRenderedImage> renderedImage = new Mock<IRenderedImage>();
            Task<IRenderedImage> prepareTask = Task.FromResult(renderedImage.Object);
            Progress<ApplicationStatus> progress = new Progress<ApplicationStatus>();
            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            ImageSaveMediator mediator = new ImageSaveMediator();

            controller
                .Setup(x => x.Enqueue(imageData.Object, prepareTask, progress, cancellationTokenSource.Token))
                .Returns(Task.CompletedTask)
                .Verifiable();

            mediator.RegisterHandler(controller.Object);
            await mediator.Enqueue(imageData.Object, prepareTask, progress, cancellationTokenSource.Token);

            controller.Verify();
        }

        /// <summary>
        /// Verifies that event subscriptions are attached to and removed from the registered save controller.
        /// This ensures plugins that observe save events through the mediator are wired to the real controller event source.
        /// </summary>
        [Test]
        public void Events_AddAndRemove_AreForwardedToRegisteredController() {
            Mock<IImageSaveController> controller = new Mock<IImageSaveController>();
            ImageSaveMediator mediator = new ImageSaveMediator();
            Func<object, BeforeImageSavedEventArgs, Task> beforeImageSaved = (_, _) => Task.CompletedTask;
            Func<object, BeforeFinalizeImageSavedEventArgs, Task> beforeFinalizeImageSaved = (_, _) => Task.CompletedTask;
            EventHandler<ImageSavedEventArgs> imageSaved = (_, _) => { };
            Func<object, ImageSaveFailedEventArgs, Task> imageSaveFailed = (_, _) => Task.CompletedTask;

            controller.SetupAdd(x => x.BeforeImageSaved += beforeImageSaved).Verifiable();
            controller.SetupRemove(x => x.BeforeImageSaved -= beforeImageSaved).Verifiable();
            controller.SetupAdd(x => x.BeforeFinalizeImageSaved += beforeFinalizeImageSaved).Verifiable();
            controller.SetupRemove(x => x.BeforeFinalizeImageSaved -= beforeFinalizeImageSaved).Verifiable();
            controller.SetupAdd(x => x.ImageSaved += imageSaved).Verifiable();
            controller.SetupRemove(x => x.ImageSaved -= imageSaved).Verifiable();
            controller.SetupAdd(x => x.ImageSaveFailed += imageSaveFailed).Verifiable();
            controller.SetupRemove(x => x.ImageSaveFailed -= imageSaveFailed).Verifiable();

            mediator.RegisterHandler(controller.Object);
            mediator.BeforeImageSaved += beforeImageSaved;
            mediator.BeforeImageSaved -= beforeImageSaved;
            mediator.BeforeFinalizeImageSaved += beforeFinalizeImageSaved;
            mediator.BeforeFinalizeImageSaved -= beforeFinalizeImageSaved;
            mediator.ImageSaved += imageSaved;
            mediator.ImageSaved -= imageSaved;
            mediator.ImageSaveFailed += imageSaveFailed;
            mediator.ImageSaveFailed -= imageSaveFailed;

            controller.Verify();
        }

        /// <summary>
        /// Verifies that shutdown is safe before registration and forwarded after registration.
        /// This covers shutdown paths where the application can dispose services even if image saving was never initialized.
        /// </summary>
        [Test]
        public void Shutdown_BeforeAndAfterRegistration_IsNullSafeThenForwards() {
            Mock<IImageSaveController> controller = new Mock<IImageSaveController>();
            ImageSaveMediator mediator = new ImageSaveMediator();

            Action shutdownWithoutHandler = mediator.Shutdown;
            shutdownWithoutHandler.Should().NotThrow();

            mediator.RegisterHandler(controller.Object);
            mediator.Shutdown();

            controller.Verify(x => x.Shutdown(), Times.Once);
        }

        /// <summary>
        /// Verifies that x86 save-event helpers invoke all subscribers and preserve the event argument instances.
        /// This covers the sequential x86 save path's public event surface without depending on file-system image serialization.
        /// </summary>
        [Test]
        public async Task X86EventHelpers_InvokeSubscribersWithOriginalEventArgs() {
            ImageSaveMediatorX86 mediator = new ImageSaveMediatorX86(Mock.Of<NINA.Profile.Interfaces.IProfileService>());
            Mock<IImageData> imageData = new Mock<IImageData>();
            Mock<IRenderedImage> renderedImage = new Mock<IRenderedImage>();
            Task<IRenderedImage> prepareTask = Task.FromResult(renderedImage.Object);
            BeforeImageSavedEventArgs beforeArgs = new BeforeImageSavedEventArgs(imageData.Object, prepareTask);
            BeforeFinalizeImageSavedEventArgs finalizeArgs = new BeforeFinalizeImageSavedEventArgs(renderedImage.Object);
            ImageSavedEventArgs savedArgs = new ImageSavedEventArgs();
            bool beforeRaised = false;
            bool finalizeRaised = false;
            bool savedRaised = false;

            mediator.BeforeImageSaved += (sender, args) => {
                sender.Should().BeSameAs(mediator);
                args.Should().BeSameAs(beforeArgs);
                beforeRaised = true;
                return Task.CompletedTask;
            };
            mediator.BeforeFinalizeImageSaved += (sender, args) => {
                sender.Should().BeSameAs(mediator);
                args.Should().BeSameAs(finalizeArgs);
                finalizeRaised = true;
                return Task.CompletedTask;
            };
            mediator.ImageSaved += (sender, args) => {
                sender.Should().BeSameAs(mediator);
                args.Should().BeSameAs(savedArgs);
                savedRaised = true;
            };

            await mediator.OnBeforeImageSaved(beforeArgs);
            await mediator.OnBeforeFinalizeImageSaved(finalizeArgs);
            mediator.OnImageSaved(savedArgs);

            beforeRaised.Should().BeTrue();
            finalizeRaised.Should().BeTrue();
            savedRaised.Should().BeTrue();
        }
    }
}
