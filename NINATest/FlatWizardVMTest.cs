using FluentAssertions;
using Moq;
using NINA.Locale;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Profile;
using NINA.ViewModel.FlatWizard;
using NINA.ViewModel.Interfaces;
using NUnit.Framework;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

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
        private Mock<IApplicationResourceDictionary> resourceDictionaryMock = new Mock<IApplicationResourceDictionary>();
        private Mock<IProfile> profileMock = new Mock<IProfile>();
        private Mock<IFlatWizardSettings> flatWizardSettingsMock = new Mock<IFlatWizardSettings>();
        private Mock<ICameraSettings> cameraSettingsMock = new Mock<ICameraSettings>();
        private Mock<IFilterWheelSettings> filterWheelSettingsMock = new Mock<IFilterWheelSettings>();

        [OneTimeSetUp]
        public void Init() {
            profileServiceMock.SetupGet(m => m.ActiveProfile).Returns(profileMock.Object);
            profileMock.SetupGet(m => m.FlatWizardSettings).Returns(flatWizardSettingsMock.Object);
            profileMock.SetupGet(m => m.CameraSettings).Returns(cameraSettingsMock.Object);
            profileMock.SetupGet(m => m.FilterWheelSettings).Returns(filterWheelSettingsMock.Object);
            filterWheelSettingsMock.SetupGet(m => m.FilterWheelFilters)
                .Returns(new ObserveAllCollection<FilterInfo>());
        }

        [Test]
        public void Constructor_WhenInitialized_DoAllNecessaryCallsAndVerifyData() {
            // setup (reinit all mocks due to checks for multiple usages)
            profileServiceMock = new Mock<IProfileService>();
            imagingVMMock = new Mock<IImagingVM>();
            cameraMediatorMock = new Mock<ICameraMediator>();
            applicationStatusMediatorMock = new Mock<IApplicationStatusMediator>();
            exposureServiceMock = new Mock<IFlatWizardExposureTimeFinderService>();
            localeMock = new Mock<ILoc>();
            resourceDictionaryMock = new Mock<IApplicationResourceDictionary>();
            profileMock = new Mock<IProfile>();
            flatWizardSettingsMock = new Mock<IFlatWizardSettings>();
            cameraSettingsMock = new Mock<ICameraSettings>();
            filterWheelSettingsMock = new Mock<IFilterWheelSettings>();
            profileServiceMock.SetupGet(m => m.ActiveProfile).Returns(profileMock.Object);
            profileMock.SetupGet(m => m.FlatWizardSettings).Returns(flatWizardSettingsMock.Object);
            profileMock.SetupGet(m => m.CameraSettings).Returns(cameraSettingsMock.Object);
            profileMock.SetupGet(m => m.FilterWheelSettings).Returns(filterWheelSettingsMock.Object);
            List<FilterInfo> filters = new List<FilterInfo>() {
                new FilterInfo("Filter", 0, 0), new FilterInfo("Filter2", 0, 0)
            };
            filterWheelSettingsMock.SetupGet(m => m.FilterWheelFilters)
                .Returns(new ObserveAllCollection<FilterInfo>(filters));

            // act

            sut = new FlatWizardVM(profileServiceMock.Object, imagingVMMock.Object,
                cameraMediatorMock.Object, resourceDictionaryMock.Object,
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

            sut.Filters.Select(f => f.Filter).Should().BeEquivalentTo(filters);
        }

        [Test]
        public void UpdateDeviceInfo_WhenCalled_SetVariablesAndCameraInfoInAllFilters() {
            filterWheelSettingsMock.SetupGet(m => m.FilterWheelFilters)
                .Returns(new ObserveAllCollection<FilterInfo>() {
                    new FilterInfo("Filter", 0, 0), new FilterInfo("Filter2", 0, 0)
                });
            // setup
            sut = new FlatWizardVM(profileServiceMock.Object, imagingVMMock.Object,
                cameraMediatorMock.Object, resourceDictionaryMock.Object,
                applicationStatusMediatorMock.Object) {
                FlatWizardExposureTimeFinderService = exposureServiceMock.Object,
                Locale = localeMock.Object,
            };

            CameraInfo cameraInfo = new CameraInfo {
                Connected = true,
                BitDepth = 16
            };

            // act
            sut.UpdateDeviceInfo(cameraInfo);

            // assert

            sut.CameraConnected.Should().BeTrue();
            sut.SingleFlatWizardFilterSettings.CameraInfo.Should().BeEquivalentTo(cameraInfo);
            sut.Filters.Select(m => m.CameraInfo).Should().AllBeEquivalentTo(cameraInfo);
        }

        [Test]
        public void UpdateFilterWheelSettings_WhenCalled_UpdateFiltersListAndKeepSelectedFilter() {
            // setup
            var filters = new List<FilterInfo>() { new FilterInfo("Filter", 0, 0), new FilterInfo("Filter2", 0, 0) };
            filterWheelSettingsMock.SetupGet(m => m.FilterWheelFilters)
                .Returns(new ObserveAllCollection<FilterInfo>(filters));

            sut = new FlatWizardVM(profileServiceMock.Object, imagingVMMock.Object,
                cameraMediatorMock.Object, resourceDictionaryMock.Object,
                applicationStatusMediatorMock.Object) {
                FlatWizardExposureTimeFinderService = exposureServiceMock.Object,
                Locale = localeMock.Object,
            };

            var selectedFilter = filters[1];
            sut.SelectedFilter = selectedFilter;

            CameraInfo cameraInfo = new CameraInfo {
                Connected = true,
                BitDepth = 16
            };

            // pre-assert
            sut.Filters.Select(f => f.Filter).Should().BeEquivalentTo(filters);
            sut.SelectedFilter.Should().BeEquivalentTo(selectedFilter);

            // remove one filter
            filters = new List<FilterInfo>() { new FilterInfo("Filter2", 0, 0) };
            filterWheelSettingsMock.SetupGet(m => m.FilterWheelFilters)
                .Returns(new ObserveAllCollection<FilterInfo>(filters));

            // act by firing the propertychanged event and also update the camera info on existing filters
            filterWheelSettingsMock.Raise(f => f.PropertyChanged += null, new PropertyChangedEventArgs(""));
            sut.UpdateDeviceInfo(cameraInfo);

            // assert cameraInfo on all filters and unused filters are removed
            sut.Filters.Select(f => f.Filter).Should().BeEquivalentTo(filters);
            sut.Filters.Select(f => f.CameraInfo).Should().AllBeEquivalentTo(cameraInfo);
            sut.SelectedFilter.Should().BeEquivalentTo(selectedFilter);

            // add another filter
            filters = new List<FilterInfo>() { new FilterInfo("Filter2", 0, 0), new FilterInfo("Filter3", 0, 0) };
            filterWheelSettingsMock.SetupGet(m => m.FilterWheelFilters)
                .Returns(new ObserveAllCollection<FilterInfo>(filters));

            // act by firing propertychanged again
            filterWheelSettingsMock.Raise(f => f.PropertyChanged += null, new PropertyChangedEventArgs(""));

            // assert cameraInfo still on all filters even on the ones that were added
            sut.Filters.Select(f => f.Filter).Should().BeEquivalentTo(filters);
            sut.Filters.Select(f => f.CameraInfo).Should().AllBeEquivalentTo(cameraInfo);
            sut.SelectedFilter.Should().BeEquivalentTo(selectedFilter);
        }

        [Test]
        [TestCase(10, 0.5, 30, 10, 1, 10, 0.5, 30, 10, 1, 0, 0, 0, 0, 0)]
        [TestCase(10, 0.5, 30, 10, 1, 20, 0.5, 30, 10, 1, 1, 0, 0, 0, 0)]
        [TestCase(10, 0.5, 30, 10, 1, 10, 0.6, 30, 10, 1, 0, 1, 0, 0, 0)]
        [TestCase(10, 0.5, 30, 10, 1, 10, 0.5, 31, 10, 1, 0, 0, 1, 0, 0)]
        [TestCase(10, 0.5, 30, 10, 1, 10, 0.5, 30, 11, 1, 0, 0, 0, 1, 0)]
        [TestCase(10, 0.5, 30, 10, 1, 10, 0.5, 30, 10, 2, 0, 0, 0, 0, 1)]
        public async Task UpdateSingleFlatWizardFilterSettings_WhenUpdateOccurs_UpdateNecessaryFieldsInProfileService(
            double histMeanTarget, double histTolerance, double maxFlatExposureTime, double minFlatExposureTime, double stepSize,
            double newHistMeanTarget, double newHistTolerance, double newMaxFlatExposureTime, double newMinFlatExposureTime, double newStepSize,
            int histMeanCallCount, int histTolCallCount, int maxFlatCallCount, int minFlatCallCount, int stepCallCount) {
            sut = new FlatWizardVM(profileServiceMock.Object, imagingVMMock.Object,
                cameraMediatorMock.Object, resourceDictionaryMock.Object,
                applicationStatusMediatorMock.Object) {
                FlatWizardExposureTimeFinderService = exposureServiceMock.Object,
                Locale = localeMock.Object,
            };

            flatWizardSettingsMock.SetupGet(m => m.HistogramMeanTarget).Returns(() => histMeanTarget);
            flatWizardSettingsMock.SetupGet(m => m.HistogramTolerance).Returns(() => histTolerance);
            flatWizardSettingsMock.SetupGet(m => m.StepSize).Returns(() => stepSize);
            cameraSettingsMock.SetupGet(m => m.MinFlatExposureTime).Returns(() => minFlatExposureTime);
            cameraSettingsMock.SetupGet(m => m.MaxFlatExposureTime).Returns(() => maxFlatExposureTime);

            flatWizardSettingsMock.SetupSet(m => m.HistogramMeanTarget = It.IsAny<double>())
                .Callback<double>(x => histMeanTarget = x);
            flatWizardSettingsMock.SetupSet(m => m.HistogramTolerance = It.IsAny<double>())
                .Callback<double>(x => histTolerance = x);
            flatWizardSettingsMock.SetupSet(m => m.StepSize = It.IsAny<double>())
                .Callback<double>(x => stepSize = x);
            cameraSettingsMock.SetupSet(m => m.MinFlatExposureTime = It.IsAny<double>())
                .Callback<double>(x => minFlatExposureTime = x);
            cameraSettingsMock.SetupSet(m => m.MaxFlatExposureTime = It.IsAny<double>())
                .Callback<double>(x => maxFlatExposureTime = x);

            sut.SingleFlatWizardFilterSettings.Settings.HistogramMeanTarget = newHistMeanTarget;
            sut.SingleFlatWizardFilterSettings.Settings.HistogramTolerance = newHistTolerance;
            sut.SingleFlatWizardFilterSettings.Settings.StepSize = newStepSize;
            sut.SingleFlatWizardFilterSettings.Settings.MinFlatExposureTime = newMinFlatExposureTime;
            sut.SingleFlatWizardFilterSettings.Settings.MaxFlatExposureTime = newMaxFlatExposureTime;

            // delay because DelayedPropertyChanged
            await Task.Delay(1000);

            // multiply call count by 5 because it's called 5 times for each property, but it would be called only once when it's set
            flatWizardSettingsMock.VerifySet(m => m.HistogramMeanTarget = newHistMeanTarget, Times.Exactly(histMeanCallCount));
            flatWizardSettingsMock.VerifySet(m => m.HistogramTolerance = newHistTolerance, Times.Exactly(histTolCallCount));
            flatWizardSettingsMock.VerifySet(m => m.StepSize = newStepSize, Times.Exactly(stepCallCount));
            cameraSettingsMock.VerifySet(m => m.MaxFlatExposureTime = newMaxFlatExposureTime, Times.Exactly(maxFlatCallCount));
            cameraSettingsMock.VerifySet(m => m.MinFlatExposureTime = newMinFlatExposureTime, Times.Exactly(minFlatCallCount));
        }
    }
}