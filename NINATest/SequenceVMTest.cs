using Microsoft.VisualStudio.TestTools.UnitTesting;
using NINA.Utility.Enum;
using NINA.Utility.Profile;
using NINA.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINATest {

    [TestClass]
    public class SequenceVMTest {
        public TestContext TestContext { get; set; }

        private SequenceProfileService profileService;

        [TestInitialize]
        public void SequenceVM_TestInit() {
            profileService = new SequenceProfileService();
            profileService.ActiveProfile = new SequenceProfile();
            profileService.ActiveProfile.ImageFileSettings.FilePath = TestContext.TestDir;
        }

        [TestMethod]
        public async Task ProcessSequence_Default() {
            var vm = new SequenceVM(profileService);

            //Act
            await vm.StartSequenceCommand.ExecuteAsync(null);

            //Assert
            Assert.AreEqual(0, vm.SelectedSequenceIdx);
            Assert.AreEqual(vm.IsPaused, false);
            Assert.AreEqual(vm.Sequence.IsRunning, false);
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