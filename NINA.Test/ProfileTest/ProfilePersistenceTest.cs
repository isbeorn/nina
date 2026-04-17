using FluentAssertions;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NUnit.Framework;
using System;
using System.IO;
using ProfileModel = NINA.Profile.Profile;

namespace NINA.Test.ProfileTest {

    [TestFixture]
    [NonParallelizable]
    public class ProfilePersistenceTest {
        private string originalApplicationTempPath;
        private string testRoot;

        [SetUp]
        public void SetUp() {
            originalApplicationTempPath = CoreUtil.APPLICATIONTEMPPATH;
            testRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "ProfilePersistence", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(testRoot, "Profiles"));
            CoreUtil.APPLICATIONTEMPPATH = testRoot;
        }

        [TearDown]
        public void TearDown() {
            CoreUtil.APPLICATIONTEMPPATH = originalApplicationTempPath;
            if (!string.IsNullOrWhiteSpace(testRoot) && Directory.Exists(testRoot)) {
                Directory.Delete(testRoot, true);
            }
        }

        /// <summary>
        /// Verifies that Save, Peek, and Load preserve profile metadata, profile settings, and plugin-specific values.
        /// </summary>
        [Test]
        public void SavePeekAndLoad_RoundTripsProfileMetadataSettingsAndPluginValues() {
            Guid pluginId = Guid.Parse("b9396410-eab1-42d1-a91b-6f3f59f01337");
            ProfileModel profile = new ProfileModel("Portable Rig") {
                Description = "Travel imaging setup"
            };
            profile.CameraSettings.PixelSize = 2.9d;
            profile.FilterWheelSettings.FilterWheelFilters.Add(new FilterInfo("SII", 5, 4));
            profile.PluginSettings.SetValue(pluginId, "profileSpecificGain", 105);

            string location = profile.Location;
            profile.Save();
            profile.Dispose();

            File.Exists(location).Should().BeTrue();
            ProfileMeta meta = ProfileModel.Peek(location);
            meta.Should().NotBeNull();
            meta.Id.Should().Be(profile.Id);
            meta.Name.Should().Be("Portable Rig");
            meta.Description.Should().Be("Travel imaging setup");
            meta.Location.Should().Be(location);

            using IProfile loadedProfile = ProfileModel.Load(location);
            loadedProfile.Id.Should().Be(profile.Id);
            loadedProfile.Name.Should().Be("Portable Rig");
            loadedProfile.Description.Should().Be("Travel imaging setup");
            loadedProfile.CameraSettings.PixelSize.Should().Be(2.9d);
            loadedProfile.FilterWheelSettings.FilterWheelFilters.Should().ContainSingle(filter => filter.Name == "SII" && filter.Position == 4);
            loadedProfile.PluginSettings.TryGetValue(pluginId, "profileSpecificGain", out int gain).Should().BeTrue();
            gain.Should().Be(105);
            loadedProfile.LastUsed.Should().BeAfter(DateTime.MinValue);
        }

        /// <summary>
        /// Verifies that Peek restores a valid journal file before reading metadata when the primary profile file is corrupt.
        /// </summary>
        [Test]
        public void Peek_RestoresJournalWhenPrimaryProfileIsCorrupt() {
            ProfileModel profile = SaveProfile("Journal Recovery");
            string location = profile.Location;
            profile.Dispose();
            byte[] validProfileBytes = File.ReadAllBytes(location);

            File.WriteAllText(location, "not a profile document");
            File.WriteAllBytes(location + ".journal", validProfileBytes);

            ProfileMeta meta = ProfileModel.Peek(location);

            meta.Should().NotBeNull();
            meta.Name.Should().Be("Journal Recovery");
            File.Exists(location + ".journal").Should().BeFalse();
            File.ReadAllBytes(location).Should().Equal(validProfileBytes);
        }

        /// <summary>
        /// Verifies that Load falls back to the backup file when the primary file cannot be deserialized and no journal is available.
        /// </summary>
        [Test]
        public void Load_RestoresBackupWhenPrimaryProfileIsCorrupt() {
            ProfileModel profile = SaveProfile("Backup Recovery");
            string location = profile.Location;
            profile.Dispose();
            byte[] validProfileBytes = File.ReadAllBytes(location);

            File.WriteAllText(location, "not a profile document");
            File.WriteAllBytes(location + ".bkp", validProfileBytes);

            using IProfile loadedProfile = ProfileModel.Load(location);

            loadedProfile.Name.Should().Be("Backup Recovery");
            loadedProfile.Id.Should().Be(profile.Id);
        }

        /// <summary>
        /// Verifies that loading a profile rebinds a persisted plate-solve filter to the matching filter-wheel list entry.
        /// </summary>
        [Test]
        public void Load_RebindsPlateSolveFilterToMatchingFilterWheelEntry() {
            ProfileModel profile = SaveProfile("Filter Rebind", profileToSave => {
                profileToSave.FilterWheelSettings.FilterWheelFilters.Add(new FilterInfo("Luminance", 0, 0));
                profileToSave.FilterWheelSettings.FilterWheelFilters.Add(new FilterInfo("Hydrogen Alpha", 15, 3));
                profileToSave.PlateSolveSettings.Filter = new FilterInfo("Hydrogen Alpha", 999, 99);
            });
            string location = profile.Location;
            profile.Dispose();

            using IProfile loadedProfile = ProfileModel.Load(location);

            loadedProfile.PlateSolveSettings.Filter.Should().BeSameAs(loadedProfile.FilterWheelSettings.FilterWheelFilters[1]);
            loadedProfile.PlateSolveSettings.Filter.Position.Should().Be(3);
        }

        /// <summary>
        /// Verifies that Remove deletes an unlocked profile file and reports success to profile management callers.
        /// </summary>
        [Test]
        public void Remove_DeletesProfileFileAndReturnsTrue() {
            ProfileModel profile = SaveProfile("Remove Me");
            string location = profile.Location;
            profile.Dispose();

            bool removed = ProfileModel.Remove(new ProfileMeta { Location = location });

            removed.Should().BeTrue();
            File.Exists(location).Should().BeFalse();
        }

        private static ProfileModel SaveProfile(string name, Action<ProfileModel> configure = null) {
            ProfileModel profile = new ProfileModel(name);
            configure?.Invoke(profile);
            profile.Save();
            return profile;
        }
    }
}
