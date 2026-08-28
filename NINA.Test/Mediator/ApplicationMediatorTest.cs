using FluentAssertions;
using Moq;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.Mediator;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Mediator {

    [TestFixture]
    public class ApplicationMediatorTest {

        /// <summary>
        /// Verifies that application tab changes are forwarded to the registered shell handler without translating the enum value.
        /// This protects the app-wide navigation contract used by lower-level components that cannot reference the concrete shell.
        /// </summary>
        [Test]
        public void ChangeTab_ForwardsRequestedApplicationTabToRegisteredHandler() {
            Mock<IApplicationVM> applicationVM = new Mock<IApplicationVM>();
            ApplicationMediator mediator = new ApplicationMediator();

            mediator.RegisterHandler(applicationVM.Object);
            mediator.ChangeTab(ApplicationTab.FRAMINGASSISTANT);

            applicationVM.Verify(x => x.ChangeTab(ApplicationTab.FRAMINGASSISTANT), Times.Once);
        }

        [Test]
        public async Task LoadImagingLayout_ForwardsPathAndCancellationToken() {
            Mock<IApplicationVM> applicationVM = new Mock<IApplicationVM>();
            ApplicationMediator mediator = new ApplicationMediator();
            using var cancellationTokenSource = new CancellationTokenSource();
            const string filePath = @"C:\Layouts\Imaging.dock.config";

            mediator.RegisterHandler(applicationVM.Object);
            await mediator.LoadImagingLayout(filePath, cancellationTokenSource.Token);

            applicationVM.Verify(x => x.LoadImagingLayout(filePath, cancellationTokenSource.Token), Times.Once);
        }

        [Test]
        public async Task LoadImagingLayout_WhenHandlerFails_PropagatesFailure() {
            Mock<IApplicationVM> applicationVM = new Mock<IApplicationVM>();
            applicationVM.Setup(x => x.LoadImagingLayout(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Load failed"));
            ApplicationMediator mediator = new ApplicationMediator();
            mediator.RegisterHandler(applicationVM.Object);

            await mediator.Invoking(x => x.LoadImagingLayout(@"C:\Layouts\Invalid.dock.config", CancellationToken.None))
                .Should().ThrowAsync<InvalidOperationException>();
        }

        /// <summary>
        /// Verifies that replacing an application handler is rejected because the mediator is designed around a single shell owner.
        /// This prevents ambiguous routing when multiple UI roots accidentally register for the same app-wide command channel.
        /// </summary>
        [Test]
        public void RegisterHandler_WhenHandlerAlreadyRegistered_Throws() {
            Mock<IApplicationVM> firstHandler = new Mock<IApplicationVM>();
            Mock<IApplicationVM> secondHandler = new Mock<IApplicationVM>();
            ApplicationMediator mediator = new ApplicationMediator();

            mediator.RegisterHandler(firstHandler.Object);
            Action registerSecondHandler = () => mediator.RegisterHandler(secondHandler.Object);

            registerSecondHandler.Should().Throw<Exception>().WithMessage("Handler already registered!");
        }

        /// <summary>
        /// Verifies that status updates are optional before a UI handler exists, then forwarded unchanged after registration.
        /// This covers startup and shutdown phases where background services may report status while the status pane is absent.
        /// </summary>
        [Test]
        public void StatusUpdate_BeforeAndAfterRegistration_IsNullSafeThenForwardsStatus() {
            Mock<IApplicationStatusVM> statusVM = new Mock<IApplicationStatusVM>();
            ApplicationStatusMediator mediator = new ApplicationStatusMediator();
            ApplicationStatus status = new ApplicationStatus { Source = "Autofocus", Status = "Measuring" };

            Action updateWithoutHandler = () => mediator.StatusUpdate(status);
            updateWithoutHandler.Should().NotThrow();

            mediator.RegisterHandler(statusVM.Object);
            mediator.StatusUpdate(status);

            statusVM.Verify(x => x.StatusUpdate(status), Times.Once);
        }

        /// <summary>
        /// Verifies that a second status handler is rejected so status messages have one deterministic UI destination.
        /// This matches the single-handler invariant used by the other WPF base mediators.
        /// </summary>
        [Test]
        public void StatusRegisterHandler_WhenHandlerAlreadyRegistered_Throws() {
            ApplicationStatusMediator mediator = new ApplicationStatusMediator();

            mediator.RegisterHandler(new Mock<IApplicationStatusVM>().Object);
            Action registerSecondHandler = () => mediator.RegisterHandler(new Mock<IApplicationStatusVM>().Object);

            registerSecondHandler.Should().Throw<Exception>().WithMessage("Handler already registered!");
        }
    }
}
