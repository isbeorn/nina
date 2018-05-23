using NINA.Model;
using NINA.PlateSolving;
using NINA.Utility.Astrometry;
using NINA.Utility.Enum;
using NINA.Utility.Mediator;
using NINA.Utility.Profile;
using NINA.ViewModel;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINATest {

    [TestFixture]
    public class SequenceVMTest {
        public TestContext TestContext { get; set; }

        private SequenceProfileService profileService;

        [SetUp]
        public void SequenceVM_TestInit() {
            profileService = new SequenceProfileService();
            profileService.ActiveProfile = new SequenceProfile();
            profileService.ActiveProfile.ImageFileSettings.FilePath = TestContext.CurrentContext.TestDirectory;
        }

        [TearDown]
        public void Cleanup() {
            Mediator.Instance.ClearAll();
        }

        [Test]
        public async Task ProcessSequence_Default() {
            var vm = new SequenceVM(profileService);

            //Act
            await vm.StartSequenceCommand.ExecuteAsync(null);

            //Assert
            Assert.AreEqual(0, vm.SelectedSequenceIdx);
            Assert.AreEqual(vm.IsPaused, false);
            Assert.AreEqual(vm.Sequence.IsRunning, false);
        }

        private CaptureSequenceList CreateDummySequenceList() {
            var l = new CaptureSequenceList();
            l.Add(new CaptureSequence() { TotalExposureCount = 10 });
            l.Add(new CaptureSequence() { TotalExposureCount = 20 });
            l.Add(new CaptureSequence() { TotalExposureCount = 5 });
            return l;
        }

        [Test]
        public async Task ProcessSequence_StartOptions_SlewToTargetTest() {
            var vm = new SequenceVM(profileService);
            var l = CreateDummySequenceList();
            l.SlewToTarget = true;
            vm.Sequence = l;

            var called = false;
            Mediator.Instance.RegisterAsyncRequest(
                new SlewTocoordinatesMessageHandle(async (SlewToCoordinatesMessage msg) => {
                    called = true;
                    return true;
                })
            );

            //Act
            await vm.StartSequenceCommand.ExecuteAsync(null);

            //Assert
            Assert.AreEqual(true, called);
        }

        [Test]
        public async Task ProcessSequence_StartOptions_SlewToTargetParameterTest() {
            var vm = new SequenceVM(profileService);
            var l = CreateDummySequenceList();
            l.SlewToTarget = true;
            var coords = new Coordinates(10, 10, Epoch.J2000, Coordinates.RAType.Degrees);
            l.Coordinates = coords;
            vm.Sequence = l;

            Coordinates actualcoords = null;
            Mediator.Instance.RegisterAsyncRequest(
                new SlewTocoordinatesMessageHandle(async (SlewToCoordinatesMessage msg) => {
                    actualcoords = msg.Coordinates;
                    return true;
                })
            );

            //Act
            await vm.StartSequenceCommand.ExecuteAsync(null);

            //Assert
            Assert.AreSame(coords, actualcoords);
        }

        [Test]
        public async Task ProcessSequence_StartOptions_DontSlewToTargetTest() {
            var vm = new SequenceVM(profileService);
            var l = CreateDummySequenceList();
            l.SlewToTarget = false;
            vm.Sequence = l;

            var called = false;
            Mediator.Instance.RegisterAsyncRequest(
                new SlewTocoordinatesMessageHandle(async  (SlewToCoordinatesMessage msg) => {
                    called = true;
                    return true;
                })
            );

            //Act
            await vm.StartSequenceCommand.ExecuteAsync(null);

            //Assert
            Assert.AreEqual(false, called);
        }

        [Test]
        public async Task ProcessSequence_StartOptions_CenterTargetTest() {
            var vm = new SequenceVM(profileService);
            var l = CreateDummySequenceList();
            l.CenterTarget = true;
            vm.Sequence = l;

            var slewCalled = false;
            Mediator.Instance.RegisterAsyncRequest(
                new SlewTocoordinatesMessageHandle(async (SlewToCoordinatesMessage msg) => {
                    slewCalled = true;
                    return true;
                })
            );

            var centerCalled = false;
            Mediator.Instance.RegisterAsyncRequest(
                new PlateSolveMessageHandle(async (PlateSolveMessage msg) => {
                    centerCalled = true;
                    return new PlateSolveResult();
                })
            );

            //Act
            await vm.StartSequenceCommand.ExecuteAsync(null);

            //Assert
            Assert.AreEqual(true, slewCalled);
            Assert.AreEqual(true, centerCalled);
        }

        [Test]
        public async Task ProcessSequence_StartOptions_CenterTargetParameterTest() {
            var vm = new SequenceVM(profileService);
            var l = CreateDummySequenceList();
            l.CenterTarget = true;
            vm.Sequence = l;

            var actualSyncSlewRepeat = false;
            Mediator.Instance.RegisterAsyncRequest(
                new PlateSolveMessageHandle(async (PlateSolveMessage msg) => {
                    actualSyncSlewRepeat = msg.SyncReslewRepeat;
                    return new PlateSolveResult();
                })
            );

            //Act
            await vm.StartSequenceCommand.ExecuteAsync(null);

            //Assert
            Assert.AreEqual(true, actualSyncSlewRepeat);
        }

        [Test]
        public async Task ProcessSequence_StartOptions_DontCenterTargetTest() {
            var vm = new SequenceVM(profileService);
            var l = CreateDummySequenceList();
            l.CenterTarget = false;
            vm.Sequence = l;

            var slewCalled = false;
            Mediator.Instance.RegisterAsyncRequest(
                new SlewTocoordinatesMessageHandle((SlewToCoordinatesMessage msg) => {
                    slewCalled = true;
                    return null;
                })
            );

            var centerCalled = false;
            Mediator.Instance.RegisterAsyncRequest(
                new PlateSolveMessageHandle((PlateSolveMessage msg) => {
                    centerCalled = true;
                    return null;
                })
            );

            //Act
            await vm.StartSequenceCommand.ExecuteAsync(null);

            //Assert
            Assert.AreEqual(false, slewCalled);
            Assert.AreEqual(false, centerCalled);
        }

        [Test]
        public async Task ProcessSequence_StartOptions_AutoFocusTest() {
            var vm = new SequenceVM(profileService);
            var l = CreateDummySequenceList();
            l.AutoFocusOnStart = true;
            vm.Sequence = l;

            var called = false;
            Mediator.Instance.RegisterAsyncRequest(
                new StartAutoFocusMessageHandle(async (StartAutoFocusMessage msg) => {
                    called = true;
                    return true;
                })
            );

            //Act
            await vm.StartSequenceCommand.ExecuteAsync(null);

            //Assert
            Assert.AreEqual(true, called);
        }

        [Test]
        public async Task ProcessSequence_StartOptions_AutoFocusParameterTest() {
            var vm = new SequenceVM(profileService);
            var l = CreateDummySequenceList();
            var filter = new NINA.Model.MyFilterWheel.FilterInfo("TestFilter", 0, 100);
            l.Items[0].FilterType = filter;
            l.Items[1].FilterType = new NINA.Model.MyFilterWheel.FilterInfo("TestFilter2", 2, 100);
            l.AutoFocusOnStart = true;
            vm.Sequence = l;

            NINA.Model.MyFilterWheel.FilterInfo actualFilter = null;
            Mediator.Instance.RegisterAsyncRequest(
                new StartAutoFocusMessageHandle(async (StartAutoFocusMessage msg) => {
                    actualFilter = msg.Filter;
                    return true;
                })
            );

            //Act
            await vm.StartSequenceCommand.ExecuteAsync(null);

            //Assert
            Assert.AreSame(filter, actualFilter);
        }

        [Test]
        public async Task ProcessSequence_StartOptions_DontAutoFocusTest() {
            var vm = new SequenceVM(profileService);
            var l = CreateDummySequenceList();
            l.AutoFocusOnStart = false;
            vm.Sequence = l;

            var called = false;
            Mediator.Instance.RegisterAsyncRequest(
                new StartAutoFocusMessageHandle(async (StartAutoFocusMessage msg) => {
                    called = true;
                    return true;
                })
            );

            //Act
            await vm.StartSequenceCommand.ExecuteAsync(null);

            //Assert
            Assert.AreEqual(false, called);
        }

        [Test]
        public async Task ProcessSequence_StartOptions_StartGuidingTest() {
            var vm = new SequenceVM(profileService);
            var l = CreateDummySequenceList();
            l.StartGuiding = true;
            vm.Sequence = l;

            var called = false;
            Mediator.Instance.RegisterAsyncRequest(
                new StartGuiderMessageHandle(async (StartGuiderMessage msg) => {
                    called = true;
                    return true;
                })
            );

            //Act
            await vm.StartSequenceCommand.ExecuteAsync(null);

            //Assert
            Assert.AreEqual(true, called);
        }

        [Test]
        public async Task ProcessSequence_StartOptions_DontStartGuidingTest() {
            var vm = new SequenceVM(profileService);
            var l = CreateDummySequenceList();
            l.StartGuiding = false;
            vm.Sequence = l;

            var called = false;
            Mediator.Instance.RegisterAsyncRequest(
                new StartGuiderMessageHandle(async (StartGuiderMessage msg) => {
                    called = true;
                    return true;
                })
            );

            //Act
            await vm.StartSequenceCommand.ExecuteAsync(null);

            //Assert
            Assert.AreEqual(false, called);
        }
    }

    internal class SequenceSettings : ISequenceSettings {
        public TimeSpan EstimatedDownloadTime { get; set; }

        public string TemplatePath { get; set; }

        public long TimeSpanInTicks { get; set; }
    }

    internal class SequenceImageFileSettings : IImageFileSettings {
        public string FilePath { get; set; }

        public string FilePattern { get; set; }

        public FileTypeEnum FileType { get; set; }
    }

    internal class SequenceProfile : IProfile {

        public IApplicationSettings ApplicationSettings {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public IAstrometrySettings AstrometrySettings {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public ICameraSettings CameraSettings {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public IColorSchemaSettings ColorSchemaSettings {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public IFilterWheelSettings FilterWheelSettings {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public IFocuserSettings FocuserSettings {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public IFramingAssistantSettings FramingAssistantSettings {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public IGuiderSettings GuiderSettings {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public Guid Id {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public IImageFileSettings ImageFileSettings { get; set; } = new SequenceImageFileSettings();

        public IImageSettings ImageSettings {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public bool IsActive {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public IMeridianFlipSettings MeridianFlipSettings {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public string Name {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public IPlateSolveSettings PlateSolveSettings {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public IPolarAlignmentSettings PolarAlignmentSettings {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public ISequenceSettings SequenceSettings { get; set; } = new SequenceSettings();

        public ITelescopeSettings TelescopeSettings {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public IWeatherDataSettings WeatherDataSettings {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public void MatchFilterSettingsWithFilterList() {
            throw new NotImplementedException();
        }
    }

    internal class SequenceProfileService : IProfileService {

        public Profiles Profiles {
            get {
                throw new NotImplementedException();
            }
        }

        public IProfile ActiveProfile { get; set; }

        public void Add() {
            new SequenceProfile();
        }

        public void Clone(Guid guid) {
            throw new NotImplementedException();
        }

        public void RemoveProfile(Guid guid) {
            throw new NotImplementedException();
        }

        public void SelectProfile(Guid guid) {
            throw new NotImplementedException();
        }
    }
}