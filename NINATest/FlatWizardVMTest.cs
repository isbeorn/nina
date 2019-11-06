using FluentAssertions;
using Moq;
using NINA.Locale;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.Profile;
using NINA.ViewModel.FlatWizard;
using NINA.ViewModel.Interfaces;
using NUnit.Framework;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using NINA.ViewModel.Equipment.FlatDevice;

namespace NINATest {

    [TestFixture]
    public class FlatWizardVMTest {
        private IFlatWizardVM sut;

        private Mock<IProfileService> profileServiceMock = new Mock<IProfileService>();
        private Mock<IImagingVM> imagingVMMock = new Mock<IImagingVM>();
        private Mock<ICameraMediator> cameraMediatorMock = new Mock<ICameraMediator>();
        private Mock<ITelescopeMediator> telescopeMediatorMock = new Mock<ITelescopeMediator>();
        private Mock<IFilterWheelMediator> filterWheelMediatorMock = new Mock<IFilterWheelMediator>();
        private Mock<IApplicationStatusMediator> applicationStatusMediatorMock = new Mock<IApplicationStatusMediator>();
        private Mock<IFlatWizardExposureTimeFinderService> exposureServiceMock = new Mock<IFlatWizardExposureTimeFinderService>();
        private Mock<ILoc> localeMock = new Mock<ILoc>();
        private Mock<IApplicationResourceDictionary> resourceDictionaryMock = new Mock<IApplicationResourceDictionary>();
        private Mock<IProfile> profileMock = new Mock<IProfile>();
        private Mock<IFlatWizardSettings> flatWizardSettingsMock = new Mock<IFlatWizardSettings>();
        private Mock<ICameraSettings> cameraSettingsMock = new Mock<ICameraSettings>();
        private Mock<IFilterWheelSettings> filterWheelSettingsMock = new Mock<IFilterWheelSettings>();
        private Mock<IFlatDeviceMediator> _flatDeviceMediatorMock = new Mock<IFlatDeviceMediator>();
        private Mock<IFlatDeviceVM> _flatDeviceVMMock = new Mock<IFlatDeviceVM>();

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
            telescopeMediatorMock = new Mock<ITelescopeMediator>();
            applicationStatusMediatorMock = new Mock<IApplicationStatusMediator>();
            exposureServiceMock = new Mock<IFlatWizardExposureTimeFinderService>();
            localeMock = new Mock<ILoc>();
            resourceDictionaryMock = new Mock<IApplicationResourceDictionary>();
            profileMock = new Mock<IProfile>();
            flatWizardSettingsMock = new Mock<IFlatWizardSettings>();
            cameraSettingsMock = new Mock<ICameraSettings>();
            filterWheelSettingsMock = new Mock<IFilterWheelSettings>();
            _flatDeviceMediatorMock = new Mock<IFlatDeviceMediator>();
            _flatDeviceVMMock = new Mock<IFlatDeviceVM>();
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
                cameraMediatorMock.Object, filterWheelMediatorMock.Object, telescopeMediatorMock.Object,
                _flatDeviceVMMock.Object, _flatDeviceMediatorMock.Object, resourceDictionaryMock.Object,
                applicationStatusMediatorMock.Object) {
                FlatWizardExposureTimeFinderService = exposureServiceMock.Object,
                Locale = localeMock.Object,
            };

            // assert
            sut.ImagingVM.Should().NotBeNull();

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

            filterWheelSettingsMock.Verify(m => m.FilterWheelFilters, Times.AtMost(2));

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
                cameraMediatorMock.Object, filterWheelMediatorMock.Object,
                telescopeMediatorMock.Object, _flatDeviceVMMock.Object,
                _flatDeviceMediatorMock.Object, resourceDictionaryMock.Object,
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
            sut.SingleFlatWizardFilterSettings.BitDepth.Should().Be(cameraInfo.BitDepth);
            sut.Filters.Select(m => m.BitDepth).Should().AllBeEquivalentTo(cameraInfo.BitDepth);
        }

        [Test]
        public void UpdateFilterWheelSettings_WhenCalled_UpdateFiltersListAndKeepSelectedFilter() {
            // setup
            var filters = new ObserveAllCollection<FilterInfo>() { new FilterInfo("Filter", 0, 0), new FilterInfo("Filter2", 0, 0) };
            filterWheelSettingsMock.SetupGet(m => m.FilterWheelFilters)
                .Returns(filters);

            sut = new FlatWizardVM(profileServiceMock.Object, imagingVMMock.Object,
                cameraMediatorMock.Object, filterWheelMediatorMock.Object, telescopeMediatorMock.Object,
                _flatDeviceVMMock.Object, _flatDeviceMediatorMock.Object, resourceDictionaryMock.Object,
                applicationStatusMediatorMock.Object) {
                FlatWizardExposureTimeFinderService = exposureServiceMock.Object,
                Locale = localeMock.Object,
            };

            var selectedFilter = filters[0];
            sut.SelectedFilter = selectedFilter;

            CameraInfo cameraInfo = new CameraInfo {
                Connected = true,
                BitDepth = 16
            };

            // pre-assert
            sut.Filters.Select(f => f.Filter).Should().BeEquivalentTo(filters);
            sut.SelectedFilter.Should().BeEquivalentTo(selectedFilter);

            // remove one filter
            filters.RemoveAt(1);

            // update the camera info on existing filters
            sut.UpdateDeviceInfo(cameraInfo);

            // assert cameraInfo on all filters and unused filters are removed
            sut.Filters.Select(f => f.Filter).Should().BeEquivalentTo(filters);
            sut.Filters.Select(f => f.BitDepth).Should().AllBeEquivalentTo(cameraInfo.BitDepth);
            sut.SelectedFilter.Should().BeEquivalentTo(selectedFilter);

            // add another filter
            filters.Add(new FilterInfo("Filter2", 0, 0));
            filters.Add(new FilterInfo("Filter3", 0, 0));

            // assert cameraInfo still on all filters even on the ones that were added
            sut.Filters.Select(f => f.Filter).Should().BeEquivalentTo(filters);
            sut.Filters.Select(f => f.BitDepth).Should().AllBeEquivalentTo(cameraInfo.BitDepth);
            sut.SelectedFilter.Should().BeEquivalentTo(selectedFilter);
        }
    }
}