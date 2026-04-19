using FluentAssertions;
using NINA.Core.Utility;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using ProfileModel = NINA.Profile.Profile;

namespace NINA.Test.ProfileTest {

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    public class ProfileServiceBehaviorTest {
        private string originalApplicationTempPath;
        private string originalProfileFolder;
        private string testRoot;
        private readonly List<ProfileService> services = new List<ProfileService>();

        [SetUp]
        public void SetUp() {
            EnsureWpfApplication();
            originalApplicationTempPath = CoreUtil.APPLICATIONTEMPPATH;
            originalProfileFolder = ProfileService.PROFILEFOLDER;
            testRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "ProfileService", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(testRoot, "Profiles"));
            CoreUtil.APPLICATIONTEMPPATH = testRoot;
            ProfileService.PROFILEFOLDER = Path.Combine(testRoot, "Profiles");
        }

        [TearDown]
        public void TearDown() {
            foreach (ProfileService service in services) {
                service.Release();
            }
            services.Clear();
            CoreUtil.APPLICATIONTEMPPATH = originalApplicationTempPath;
            ProfileService.PROFILEFOLDER = originalProfileFolder;
            if (!string.IsNullOrWhiteSpace(testRoot) && Directory.Exists(testRoot)) {
                DeleteDirectoryWithRetry(testRoot);
            }
        }

        /// <summary>
        /// Verifies that TryLoad creates, persists, selects, and exposes a default profile when no profile exists.
        /// </summary>
        [Test]
        public void TryLoad_WithoutExistingProfiles_CreatesAndSelectsDefaultProfile() {
            ProfileService service = CreateService();

            bool loaded = service.TryLoad(null);

            loaded.Should().BeTrue();
            service.ProfileWasSpecifiedFromCommandLineArgs.Should().BeFalse();
            service.ActiveProfile.Should().NotBeNull();
            service.Profiles.Should().ContainSingle();
            service.Profiles[0].IsActive.Should().BeTrue();
            service.Profiles[0].Id.Should().Be(service.ActiveProfile.Id);
            File.Exists(service.ActiveProfile.Location).Should().BeTrue();
            Application.Current.Resources["ActiveProfile"].Should().BeSameAs(service.ActiveProfile);
        }

        /// <summary>
        /// Verifies that requesting a specific missing profile id fails without creating a fallback default profile.
        /// </summary>
        [Test]
        public void TryLoad_WithMissingRequestedProfile_ReturnsFalseWithoutCreatingDefaultProfile() {
            ProfileService service = CreateService();

            bool loaded = service.TryLoad(Guid.NewGuid().ToString());

            loaded.Should().BeFalse();
            service.ProfileWasSpecifiedFromCommandLineArgs.Should().BeTrue();
            service.Profiles.Should().BeEmpty();
            service.ActiveProfile.Should().BeNull();
        }

        /// <summary>
        /// Verifies that command-line profile selection loads the requested profile instead of falling back to the newest profile.
        /// </summary>
        [Test]
        public void TryLoad_WithExistingRequestedProfile_SelectsRequestedProfile() {
            ProfileMeta first = SaveProfile("First Requested Candidate");
            ProfileMeta second = SaveProfile("Second Requested Candidate");
            ProfileService service = CreateService();

            bool loaded = service.TryLoad(second.Id.ToString());

            loaded.Should().BeTrue();
            service.ProfileWasSpecifiedFromCommandLineArgs.Should().BeTrue();
            service.Profiles.Should().HaveCount(2);
            service.ActiveProfile.Id.Should().Be(second.Id);
            service.ActiveProfile.Name.Should().Be("Second Requested Candidate");
            service.Profiles.Should().Contain(profile => profile.Id == first.Id && !profile.IsActive);
            service.Profiles.Should().Contain(profile => profile.Id == second.Id && profile.IsActive);
        }

        /// <summary>
        /// Verifies that Add creates another persisted profile without disturbing the currently selected profile.
        /// </summary>
        [Test]
        public void Add_CreatesAdditionalPersistedProfileWithoutChangingActiveProfile() {
            ProfileService service = CreateService();
            service.TryLoad(null).Should().BeTrue();
            Guid activeProfileId = service.ActiveProfile.Id;

            service.Add();

            service.Profiles.Should().HaveCount(2);
            service.ActiveProfile.Id.Should().Be(activeProfileId);
            service.Profiles.Should().Contain(profile => profile.Id == activeProfileId && profile.IsActive);
            service.Profiles.Should().Contain(profile => profile.Id != activeProfileId && File.Exists(profile.Location));
        }

        /// <summary>
        /// Verifies that selecting profiles updates active metadata, WPF resources, culture, and the documented profile events.
        /// </summary>
        [Test]
        public void SelectProfile_RaisesEventsAndUpdatesActiveProfileResource() {
            ProfileMeta first = SaveProfile("First", profile => profile.ApplicationSettings.Language = new CultureInfo("en-GB"));
            ProfileMeta second = SaveProfile("Second", profile => profile.ApplicationSettings.Language = new CultureInfo("de-DE"));
            ProfileService service = CreateService();
            service.Profiles.Add(first);
            service.Profiles.Add(second);
            int beforeChangingCount = 0;
            int profileChangedCount = 0;
            int localeChangedCount = 0;
            int locationChangedCount = 0;
            int horizonChangedCount = 0;
            ProfileChangedEventArgs lastProfileChangedArgs = null;
            service.BeforeProfileChanging += (object sender, EventArgs args) => beforeChangingCount++;
            service.ProfileChanged += (object sender, EventArgs args) => {
                profileChangedCount++;
                lastProfileChangedArgs = (ProfileChangedEventArgs)args;
            };
            service.LocaleChanged += (object sender, EventArgs args) => localeChangedCount++;
            service.LocationChanged += (object sender, EventArgs args) => locationChangedCount++;
            service.HorizonChanged += (object sender, EventArgs args) => horizonChangedCount++;

            service.SelectProfile(first).Should().BeTrue();
            IProfile oldProfile = service.ActiveProfile;
            service.SelectProfile(second).Should().BeTrue();

            beforeChangingCount.Should().Be(2);
            profileChangedCount.Should().Be(2);
            localeChangedCount.Should().Be(2);
            locationChangedCount.Should().Be(2);
            horizonChangedCount.Should().Be(2);
            lastProfileChangedArgs.OldProfile.Should().BeSameAs(oldProfile);
            lastProfileChangedArgs.NewProfile.Should().BeSameAs(service.ActiveProfile);
            first.IsActive.Should().BeFalse();
            second.IsActive.Should().BeTrue();
            service.ActiveProfile.Name.Should().Be("Second");
            Thread.CurrentThread.CurrentUICulture.Name.Should().Be("de-DE");
            Application.Current.Resources["ActiveProfile"].Should().BeSameAs(service.ActiveProfile);
        }

        /// <summary>
        /// Verifies that profile service change helpers update the active profile and raise the appropriate domain events.
        /// </summary>
        [Test]
        public void ChangeHelpers_UpdateActiveProfileAndRaiseDomainEvents() {
            ProfileMeta profile = SaveProfile("Change Helpers", profileToSave => profileToSave.ApplicationSettings.Language = new CultureInfo("en-GB"));
            ProfileService service = CreateService();
            service.Profiles.Add(profile);
            service.SelectProfile(profile).Should().BeTrue();
            int localeChangedCount = 0;
            int locationChangedCount = 0;
            int horizonChangedCount = 0;
            service.LocaleChanged += (object sender, EventArgs args) => localeChangedCount++;
            service.LocationChanged += (object sender, EventArgs args) => locationChangedCount++;
            service.HorizonChanged += (object sender, EventArgs args) => horizonChangedCount++;

            service.ChangeLocale(new CultureInfo("de-DE"));
            service.ChangeLatitude(52.52d);
            service.ChangeLongitude(13.405d);
            service.ChangeElevation(34d);
            service.ChangeHorizon(string.Empty);
            Thread.Sleep(1200);

            service.ActiveProfile.ApplicationSettings.Culture.Should().Be("de-DE");
            service.ActiveProfile.AstrometrySettings.Latitude.Should().Be(52.52d);
            service.ActiveProfile.AstrometrySettings.Longitude.Should().Be(13.405d);
            service.ActiveProfile.AstrometrySettings.Elevation.Should().Be(34d);
            service.ActiveProfile.AstrometrySettings.HorizonFilePath.Should().BeEmpty();
            service.ActiveProfile.AstrometrySettings.Horizon.Should().BeNull();
            localeChangedCount.Should().Be(1);
            locationChangedCount.Should().Be(3);
            horizonChangedCount.Should().Be(1);
        }

        /// <summary>
        /// Verifies that cloning the active profile creates a persisted independent profile and matching metadata entry.
        /// </summary>
        [Test]
        public void Clone_ActiveProfile_CreatesPersistedIndependentProfileMetadata() {
            ProfileMeta source = SaveProfile("Clone Source", profile => {
                profile.CameraSettings.PixelSize = 3.76d;
                profile.Description = "source description";
            });
            ProfileService service = CreateService();
            service.Profiles.Add(source);
            service.SelectProfile(source).Should().BeTrue();

            bool cloned = service.Clone(source);

            cloned.Should().BeTrue();
            service.Profiles.Should().HaveCount(2);
            ProfileMeta cloneMeta = service.Profiles[1];
            cloneMeta.Id.Should().NotBe(source.Id);
            cloneMeta.Name.Should().Be("Clone Source Copy");
            cloneMeta.Description.Should().Be("source description");
            File.Exists(cloneMeta.Location).Should().BeTrue();

            using IProfile clone = ProfileModel.Load(cloneMeta.Location);
            clone.CameraSettings.PixelSize.Should().Be(3.76d);
            clone.Id.Should().Be(cloneMeta.Id);
        }

        /// <summary>
        /// Verifies that cloning an inactive profile loads it from disk and does not depend on the active profile instance.
        /// </summary>
        [Test]
        public void Clone_InactiveProfile_LoadsSourceProfileFromDisk() {
            ProfileMeta active = SaveProfile("Active Clone Anchor");
            ProfileMeta inactive = SaveProfile("Inactive Clone Source", profile => profile.CameraSettings.PixelSize = 2.4d);
            ProfileService service = CreateService();
            service.Profiles.Add(active);
            service.Profiles.Add(inactive);
            service.SelectProfile(active).Should().BeTrue();

            bool cloned = service.Clone(inactive);

            cloned.Should().BeTrue();
            service.Profiles.Should().HaveCount(3);
            ProfileMeta cloneMeta = service.Profiles[2];
            cloneMeta.Name.Should().Be("Inactive Clone Source Copy");
            cloneMeta.Id.Should().NotBe(inactive.Id);
            using IProfile clone = ProfileModel.Load(cloneMeta.Location);
            clone.CameraSettings.PixelSize.Should().Be(2.4d);
        }

        /// <summary>
        /// Verifies that removing an unlocked profile deletes the file and removes the profile metadata from the service list.
        /// </summary>
        [Test]
        public void RemoveProfile_ForUnlockedProfile_DeletesFileAndMetadata() {
            ProfileMeta profile = SaveProfile("Remove via Service");
            ProfileService service = CreateService();
            service.Profiles.Add(profile);

            bool removed = service.RemoveProfile(profile);

            removed.Should().BeTrue();
            service.Profiles.Should().BeEmpty();
            File.Exists(profile.Location).Should().BeFalse();
        }

        /// <summary>
        /// Verifies that removing a locked profile reports failure and leaves metadata intact for a later retry.
        /// </summary>
        [Test]
        public void RemoveProfile_ForLockedProfile_ReturnsFalseAndKeepsMetadata() {
            ProfileMeta profile = SaveProfile("Locked Remove");
            ProfileService service = CreateService();
            service.Profiles.Add(profile);
            using FileStream lockStream = new FileStream(profile.Location, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            bool removed = service.RemoveProfile(profile);

            removed.Should().BeFalse();
            service.Profiles.Should().ContainSingle().Which.Should().BeSameAs(profile);
            File.Exists(profile.Location).Should().BeTrue();
        }

        /// <summary>
        /// Verifies that TryLoad migrates legacy modularized namespace names while preserving an old-file backup.
        /// </summary>
        [Test]
        public void TryLoad_MigratesLegacyProfileNamespacesAndCreatesBackup() {
            string profilePath = Path.Combine(ProfileService.PROFILEFOLDER, $"{Guid.NewGuid()}.profile");
            File.WriteAllText(profilePath,
                "http://schemas.datacontract.org/2004/07/NINA.Utility " +
                "http://schemas.datacontract.org/2004/07/NINA.Model.MyFilterWheel " +
                "http://schemas.datacontract.org/2004/07/NINA.Model.MyCamera");
            ProfileService service = CreateService();

            bool loaded = service.TryLoad(Guid.NewGuid().ToString());

            loaded.Should().BeFalse();
            string migratedProfile = File.ReadAllText(profilePath);
            migratedProfile.Should().Contain("http://schemas.datacontract.org/2004/07/NINA.Core.Utility.ColorSchema");
            migratedProfile.Should().Contain("http://schemas.datacontract.org/2004/07/NINA.Core.Model.Equipment");
            string backupPath = Path.Combine(ProfileService.PROFILEFOLDER + "_old", Path.GetFileName(profilePath));
            File.Exists(backupPath).Should().BeTrue();
        }

        private ProfileService CreateService() {
            ProfileService service = new ProfileService();
            services.Add(service);
            return service;
        }

        private static ProfileMeta SaveProfile(string name, Action<ProfileModel> configure = null) {
            using ProfileModel profile = new ProfileModel(name);
            configure?.Invoke(profile);
            profile.Save();
            return new ProfileMeta {
                Id = profile.Id,
                Name = profile.Name,
                Description = profile.Description,
                Location = profile.Location,
                LastUsed = profile.LastUsed
            };
        }

        private static void EnsureWpfApplication() {
            if (Application.Current == null) {
                _ = new Application();
            }
        }

        private static void DeleteDirectoryWithRetry(string path) {
            for (int attempt = 0; attempt < 3; attempt++) {
                try {
                    Directory.Delete(path, true);
                    return;
                } catch (IOException) when (attempt < 2) {
                    Thread.Sleep(100);
                } catch (UnauthorizedAccessException) when (attempt < 2) {
                    Thread.Sleep(100);
                }
            }

            Directory.Delete(path, true);
        }
    }
}
