using NUnit.Framework;
using NINA.ViewModel.FlatWizard;
using Moq;
using NINA.ViewModel.Interfaces;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Profile;
using NINA.Locale;
using FluentAssertions;
using NINA.Utility;
using NINA.Model.MyCamera;
using System.Linq;
using NINA.Model.MyFilterWheel;
using System.Collections.Generic;
using System.ComponentModel;

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
            resourceUtilMock = new Mock<IResourceUtil>();
            profileMock = new Mock<IProfile>();
            flatWizardSettingsMock = new Mock<IFlatWizardSettings>();
            cameraSettingsMock = new Mock<ICameraSettings>();
            filterWheelSettingsMock = new Mock<IFilterWheelSettings>();
            profileServiceMock.SetupGet(m => m.ActiveProfile).Returns(profileMock.Object);
            profileMock.SetupGet(m => m.FlatWizardSettings).Returns(flatWizardSettingsMock.Object);
            profileMock.SetupGet(m => m.CameraSettings).Returns(cameraSettingsMock.Object);
            profileMock.SetupGet(m => m.FilterWheelSettings).Returns(filterWheelSettingsMock.Object);
            List<FilterInfo> filters = new List<FilterInfo>() {
                new FilterInfo("Filter", 0, 0), new FilterInfo("FIlter2", 0, 0)
            };
            filterWheelSettingsMock.SetupGet(m => m.FilterWheelFilters)
                .Returns(new ObserveAllCollection<FilterInfo>(filters));

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

            sut.Filters.Select(f => f.Filter).Should().BeEquivalentTo(filters);
        }

        [Test]
        public void UpdateDeviceInfo_WhenCalled_SetVariablesAndCameraInfoInAllFilters() {
            filterWheelSettingsMock.SetupGet(m => m.FilterWheelFilters)
                .Returns(new ObserveAllCollection<FilterInfo>() {
                    new FilterInfo("Filter", 0, 0), new FilterInfo("FIlter2", 0, 0)
                });
            // setup
            sut = new FlatWizardVM(profileServiceMock.Object, imagingVMMock.Object,
                cameraMediatorMock.Object, resourceUtilMock.Object,
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
                cameraMediatorMock.Object, resourceUtilMock.Object,
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
    }
}