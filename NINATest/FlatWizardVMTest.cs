using NUnit.Framework;
using NINA.ViewModel.FlatWizard;
using Moq;
using NINA.ViewModel.Interfaces;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Profile;
using NINA.Locale;
using FluentAssertions;
using NINA.Utility;

namespace NINATest {

    [TestFixture]
    public class FlatWizardVMTest {
        private IFlatWizardVM sut;

        private Mock<IProfileService> profileServiceMock = new Mock<IProfileService>();
        private Mock<IImagingVM> imagingVMMock = new Mock<IImagingVM>();
        private Mock<ICameraMediator> cameraMediatorMock = new Mock<ICameraMediator>();
        private Mock<IApplicationStatusMediator> applicationStatusMediatorMock = new Mock<IApplicationStatusMediator>();
        private Mock<IFlatWizardExposureTimeFinderService> exposureServiceMock = new Mock<IFlatWizardExposureTimeFinderService>();
        private Mock<ILoc> localeMock = new Mock<ILoc>();
        private Mock<IResourceUtil> resourceUtilMock = new Mock<IResourceUtil>();
        private Mock<IProfile> profileMock = new Mock<IProfile>();
        private Mock<IFlatWizardSettings> flatWizardSettingsMock = new Mock<IFlatWizardSettings>();
        private Mock<ICameraSettings> cameraSettingsMock = new Mock<ICameraSettings>();
        private Mock<IFilterWheelSettings> filterWheelSettingsMock = new Mock<IFilterWheelSettings>();

        [OneTimeSetUp]
        public void Init() {
        }

        [Test]
        public void TestConstructor() {
            // setup
            profileServiceMock.SetupGet(m => m.ActiveProfile).Returns(profileMock.Object);
            profileMock.SetupGet(m => m.FlatWizardSettings).Returns(flatWizardSettingsMock.Object);
            profileMock.SetupGet(m => m.CameraSettings).Returns(cameraSettingsMock.Object);
            profileMock.SetupGet(m => m.FilterWheelSettings).Returns(filterWheelSettingsMock.Object);
            filterWheelSettingsMock.SetupGet(m => m.FilterWheelFilters)
                .Returns(new ObserveAllCollection<NINA.Model.MyFilterWheel.FilterInfo>());

            // act
            sut = new FlatWizardVM(profileServiceMock.Object, imagingVMMock.Object,
                cameraMediatorMock.Object, resourceUtilMock.Object,
                applicationStatusMediatorMock.Object) {
                FlatWizardExposureTimeFinderService = exposureServiceMock.Object,
                Locale = localeMock.Object,
            };

            // assert
            sut.ImagingVM.Should().NotBeNull();
            imagingVMMock.Verify(m => m.SetAutoStretch(false), Times.Once);
            imagingVMMock.Verify(m => m.SetDetectStars(false), Times.Once);

            sut.StartFlatSequenceCommand.Should().NotBeNull();
            sut.CancelFlatExposureSequenceCommand.Should().NotBeNull();
            sut.PauseFlatExposureSequenceCommand.Should().NotBeNull();
            sut.ResumeFlatExposureSequenceCommand.Should().NotBeNull();

            sut.SingleFlatWizardFilterSettings.Should().NotBeNull();
            sut.SingleFlatWizardFilterSettings.Filter.Should().BeNull();

            flatWizardSettingsMock.Verify(m => m.HistogramMeanTarget, Times.Once);
            flatWizardSettingsMock.Verify(m => m.HistogramTolerance, Times.Once);
            flatWizardSettingsMock.Verify(m => m.StepSize, Times.Once);
            cameraSettingsMock.Verify(m => m.MaxFlatExposureTime, Times.Once);
            cameraSettingsMock.Verify(m => m.MinFlatExposureTime, Times.Once);

            flatWizardSettingsMock.Verify(m => m.FlatCount, Times.AtMost(2));
            flatWizardSettingsMock.Verify(m => m.BinningMode, Times.AtMost(2));

            filterWheelSettingsMock.Verify(m => m.FilterWheelFilters, Times.Once);

            cameraMediatorMock.Verify(m => m.RegisterConsumer(sut), Times.Once);
        }
    }
}