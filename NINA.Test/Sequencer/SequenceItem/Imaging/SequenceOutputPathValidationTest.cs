#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using Moq;
using NINA.Core.Locale;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.Validations;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using System.IO;

namespace NINA.Test.Sequencer.SequenceItem.Imaging {
    [TestFixture]
    public class SequenceOutputPathValidationTest {
        private string directory = null!;
        private string imageDirectory = null!;
        private string alternateDirectory = null!;
        private Mock<IProfileService> profileService = null!;
        private Mock<ICameraMediator> camera = null!;
        private IProfile activeProfile = null!;

        [SetUp]
        public void SetUp() {
            directory = Path.Combine(Path.GetTempPath(), "nina-output-path-" + Guid.NewGuid().ToString("N"));
            imageDirectory = Path.Combine(directory, "images");
            alternateDirectory = Path.Combine(directory, "alternate");
            Directory.CreateDirectory(directory);
            profileService = new Mock<IProfileService>();
            profileService.SetupGet(x => x.ActiveProfile).Returns(() => activeProfile);
            camera = new Mock<ICameraMediator>();
            camera.Setup(x => x.GetInfo()).Returns(new CameraInfo { Connected = true });
        }

        [TearDown]
        public void TearDown() {
            activeProfile?.Dispose();
            if (Directory.Exists(imageDirectory)) Directory.Delete(imageDirectory);
            if (Directory.Exists(alternateDirectory)) Directory.Delete(alternateDirectory);
            Directory.Delete(directory);
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void Validate_SharesDirectoryResultAcrossExposureInstancesUntilPathChanges(bool initiallyExists, bool firstSubframe) {
            SetDirectoryExists(imageDirectory, initiallyExists);
            var settings = new ImageFileSettings { FilePath = imageDirectory };
            activeProfile = CreateProfile(settings);
            var first = CreateExposure(firstSubframe);
            AssertValidation(first, initiallyExists);

            SetDirectoryExists(imageDirectory, !initiallyExists);
            var second = CreateExposure(!firstSubframe);
            AssertValidation(second, initiallyExists);
            AssertValidation(first, initiallyExists);
            settings.FilePath = imageDirectory;
            AssertValidation(first, initiallyExists);
            AssertValidation(second, initiallyExists);

            SetDirectoryExists(alternateDirectory, !initiallyExists);
            settings.FilePath = alternateDirectory;
            AssertValidation(first, !initiallyExists);
            AssertValidation(second, !initiallyExists);
            settings.FilePath = imageDirectory;
            AssertValidation(first, !initiallyExists);
            AssertValidation(second, !initiallyExists);

            SetDirectoryExists(imageDirectory, initiallyExists);
            AssertValidation(first, !initiallyExists);
            AssertValidation(second, !initiallyExists);

            settings.FilePath = alternateDirectory;
            settings.FilePath = imageDirectory;
            AssertValidation(first, initiallyExists);
            AssertValidation(second, initiallyExists);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Validate_UsesSelectedProfileSettingsWithoutRetainingPreviousResult(bool initiallyExists) {
            SetDirectoryExists(imageDirectory, initiallyExists);
            var firstSettings = new ImageFileSettings { FilePath = imageDirectory };
            var firstProfile = CreateProfile(firstSettings);
            activeProfile = firstProfile;
            var fullFrame = CreateExposure(false);
            var subframe = CreateExposure(true);
            AssertValidation(fullFrame, initiallyExists);
            AssertValidation(subframe, initiallyExists);

            SetDirectoryExists(imageDirectory, !initiallyExists);
            var secondSettings = new ImageFileSettings { FilePath = imageDirectory };
            var secondProfile = CreateProfile(secondSettings);
            activeProfile = secondProfile;
            AssertValidation(fullFrame, !initiallyExists);
            AssertValidation(subframe, !initiallyExists);

            activeProfile = firstProfile;
            AssertValidation(fullFrame, initiallyExists);
            AssertValidation(subframe, initiallyExists);
            activeProfile = secondProfile;
            AssertValidation(fullFrame, !initiallyExists);
            AssertValidation(subframe, !initiallyExists);
        }

        private static IProfile CreateProfile(ImageFileSettings settings) {
            var profile = new Mock<IProfile>();
            profile.SetupGet(x => x.ImageFileSettings).Returns(settings);
            return profile.Object;
        }

        private IValidatable CreateExposure(bool subframe) => subframe
            ? new TakeSubframeExposure(profileService.Object, camera.Object, Mock.Of<IImagingMediator>(), Mock.Of<IImageSaveMediator>(), Mock.Of<IImageHistoryVM>())
            : new TakeExposure(profileService.Object, camera.Object, Mock.Of<IImagingMediator>(), Mock.Of<IImageSaveMediator>(), Mock.Of<IImageHistoryVM>());

        private static void AssertValidation(IValidatable exposure, bool expected) {
            exposure.Validate().Should().Be(expected);
            if (expected) {
                exposure.Issues.Should().BeEmpty();
            } else {
                string issue = exposure is TakeSubframeExposure
                    ? "Lbl_SequenceItem_Imaging_TakeSubframeExposure_Validation_FilePathInvalid"
                    : "Lbl_SequenceItem_Imaging_TakeExposure_Validation_FilePathInvalid";
                exposure.Issues.Should().Equal(Loc.Instance[issue]);
            }
        }

        private static void SetDirectoryExists(string path, bool exists) {
            if (exists) Directory.CreateDirectory(path);
            else if (Directory.Exists(path)) Directory.Delete(path);
        }
    }
}