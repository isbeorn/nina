using FluentAssertions;
using NINA.Core.Enum;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Profile;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;
using System.Windows.Media;

namespace NINA.Test.ProfileTest {

    [TestFixture]
    public class ProfileSettingsBehaviorTest {

        /// <summary>
        /// Verifies that image file naming uses image-type-specific overrides only when the override is non-empty.
        /// </summary>
        [Test]
        public void ImageFileSettings_GetFilePattern_UsesSpecificOverrideOnlyWhenPresent() {
            ImageFileSettings settings = new ImageFileSettings {
                FilePattern = "base/$$IMAGETYPE$$",
                FilePatternDARK = "dark/$$EXPOSURETIME$$",
                FilePatternFLAT = "flat/$$FILTER$$",
                FilePatternBIAS = "   "
            };

            settings.GetFilePattern("LIGHT").Should().Be("base/$$IMAGETYPE$$");
            settings.GetFilePattern("DARK").Should().Be("dark/$$EXPOSURETIME$$");
            settings.GetFilePattern("FLAT").Should().Be("flat/$$FILTER$$");
            settings.GetFilePattern("BIAS").Should().Be("base/$$IMAGETYPE$$");
            settings.GetFilePattern("dark").Should().Be("base/$$IMAGETYPE$$");
        }

        /// <summary>
        /// Verifies backward compatibility for obsolete compressed TIFF file-type values stored in older profiles.
        /// </summary>
        [Test]
        public void ImageFileSettings_FileType_MigratesObsoleteTiffCompressionValues() {
#pragma warning disable CS0612
            ImageFileSettings lzwSettings = new ImageFileSettings { FileType = FileTypeEnum.TIFF_LZW };
            ImageFileSettings zipSettings = new ImageFileSettings { FileType = FileTypeEnum.TIFF_ZIP };
#pragma warning restore CS0612

            lzwSettings.FileType.Should().Be(FileTypeEnum.TIFF);
            lzwSettings.TIFFCompressionType.Should().Be(TIFFCompressionTypeEnum.LZW);
            zipSettings.FileType.Should().Be(FileTypeEnum.TIFF);
            zipSettings.TIFFCompressionType.Should().Be(TIFFCompressionTypeEnum.ZIP);
        }

        /// <summary>
        /// Verifies that camera flat exposure bounds remain ordered and invalid negative readout modes are ignored.
        /// </summary>
        [Test]
        public void CameraSettings_EnforcesFlatExposureOrderAndIgnoresNegativeReadoutModes() {
            CameraSettings settings = new CameraSettings {
                MaxFlatExposureTime = 5d
            };
            List<string> propertyNames = CapturePropertyChanges(settings);

            settings.MinFlatExposureTime = 12d;
            settings.ReadoutMode = 2;
            settings.ReadoutMode = -1;
            settings.ReadoutModeForSnapImages = 3;
            settings.ReadoutModeForSnapImages = -1;
            settings.ReadoutModeForNormalImages = 4;
            settings.ReadoutModeForNormalImages = -1;

            settings.MinFlatExposureTime.Should().Be(12d);
            settings.MaxFlatExposureTime.Should().Be(12d);
            settings.ReadoutMode.Should().Be(2);
            settings.ReadoutModeForSnapImages.Should().Be(3);
            settings.ReadoutModeForNormalImages.Should().Be(4);
            propertyNames.Should().Contain(nameof(CameraSettings.MinFlatExposureTime));
            propertyNames.Should().Contain(nameof(CameraSettings.MaxFlatExposureTime));
            propertyNames.Should().Contain(nameof(CameraSettings.ReadoutMode));
            propertyNames.Should().Contain(nameof(CameraSettings.ReadoutModeForSnapImages));
            propertyNames.Should().Contain(nameof(CameraSettings.ReadoutModeForNormalImages));
        }

        /// <summary>
        /// Verifies autofocus bounds that protect the UI and autofocus routine from nonsensical persisted values.
        /// </summary>
        [Test]
        public void FocuserSettings_ClampsAutofocusControlValues() {
            FocuserSettings settings = new FocuserSettings();

            settings.AutoFocusInitialOffsetSteps = 0;
            settings.AutoFocusInitialOffsetSteps.Should().Be(1);
            settings.AutoFocusInitialOffsetSteps = 11;
            settings.AutoFocusInitialOffsetSteps.Should().Be(10);
            settings.AutoFocusTotalNumberOfAttempts = 0;
            settings.AutoFocusTotalNumberOfAttempts.Should().Be(1);
            settings.AutoFocusTotalNumberOfAttempts = 6;
            settings.AutoFocusTotalNumberOfAttempts.Should().Be(5);
            settings.AutoFocusNumberOfFramesPerPoint = 0;
            settings.AutoFocusNumberOfFramesPerPoint.Should().Be(1);
            settings.AutoFocusBinning = 7;
            settings.AutoFocusBinning.Should().Be(4);
            settings.RSquaredThreshold = -0.2d;
            settings.RSquaredThreshold.Should().Be(0d);
            settings.RSquaredThreshold = 1.2d;
            settings.RSquaredThreshold.Should().Be(1d);
        }

        /// <summary>
        /// Verifies that meridian flip timing settings keep requested and maximum delay values internally consistent.
        /// </summary>
        [Test]
        public void MeridianFlipSettings_KeepsRequestedAndMaximumDelayConsistent() {
            MeridianFlipSettings settings = new MeridianFlipSettings();

            settings.MinutesAfterMeridian = 15d;
            settings.MinutesAfterMeridian.Should().Be(15d);
            settings.MaxMinutesAfterMeridian.Should().Be(15d);

            settings.MaxMinutesAfterMeridian = 8d;
            settings.MaxMinutesAfterMeridian.Should().Be(8d);
            settings.MinutesAfterMeridian.Should().Be(8d);
        }

        /// <summary>
        /// Verifies that disabling debayering also disables features that require debayered image data.
        /// </summary>
        [Test]
        public void ImageSettings_DisablingDebayerAlsoDisablesDependentDebayeredFeatures() {
            ImageSettings settings = new ImageSettings {
                DebayeredHFR = true,
                UnlinkedStretch = true
            };

            settings.DebayerImage = false;

            settings.DebayerImage.Should().BeFalse();
            settings.DebayeredHFR.Should().BeFalse();
            settings.UnlinkedStretch.Should().BeFalse();

            settings.UnlinkedStretch = true;
            settings.UnlinkedStretch.Should().BeTrue();
            settings.DebayerImage.Should().BeTrue();
        }

        /// <summary>
        /// Verifies that plate-solver URLs and API keys are normalized while filesystem paths still expand environment variables.
        /// </summary>
        [Test]
        public void PlateSolveSettings_NormalizesWhitespaceAndExpandsEnvironmentPaths() {
            PlateSolveSettings settings = new PlateSolveSettings();
            string cygwinPath = @"%TEMP%\nina-profile-cygwin";

            settings.AstrometryURL = " http://nova.astrometry.net \r\n";
            settings.AstrometryAPIKey = " key with spaces\t";
            settings.PinPointAllSkyApiKey = " pin point key ";
            settings.PinPointAllSkyApiHost = " nova.astrometry.net ";
            settings.CygwinLocation = cygwinPath;

            settings.AstrometryURL.Should().Be("http://nova.astrometry.net");
            settings.AstrometryAPIKey.Should().Be("keywithspaces");
            settings.PinPointAllSkyApiKey.Should().Be("pinpointkey");
            settings.PinPointAllSkyApiHost.Should().Be("nova.astrometry.net");
            settings.CygwinLocation.Should().Be(Environment.ExpandEnvironmentVariables(cygwinPath));
        }

        /// <summary>
        /// Verifies that selected pluggable behavior lookups stay synchronized with the serialized collection.
        /// </summary>
        [Test]
        public void ApplicationSettings_SelectedPluggableBehaviors_MaintainsLookupOnCollectionChanges() {
            ApplicationSettings settings = new ApplicationSettings();
            List<string> propertyNames = CapturePropertyChanges(settings);

            settings.SelectedPluggableBehaviors.Add(new KeyValuePair<string, string>("guider", "PHD2"));
            settings.SelectedPluggableBehaviors.Add(new KeyValuePair<string, string>("rotator", "Falcon"));

            settings.SelectedPluggableBehaviorsLookup.Should().ContainKey("guider").WhoseValue.Should().Be("PHD2");
            settings.SelectedPluggableBehaviorsLookup.Should().ContainKey("rotator").WhoseValue.Should().Be("Falcon");
            propertyNames.Should().Contain(nameof(ApplicationSettings.SelectedPluggableBehaviors));
            propertyNames.Should().Contain(nameof(ApplicationSettings.SelectedPluggableBehaviorsLookup));
        }

        /// <summary>
        /// Verifies that filter-wheel deserialization repairs old filter entries and permits only one autofocus filter.
        /// </summary>
        [Test]
        public void FilterWheelSettings_OnDeserialized_AddsFlatWizardDefaultsAndKeepsOnlyOneAutoFocusFilter() {
            FilterWheelSettings settings = new FilterWheelSettings();
            FilterInfo luminance = new FilterInfo("L", 0, 0) {
                AutoFocusFilter = true,
                FlatWizardFilterSettings = null
            };
            FilterInfo red = new FilterInfo("R", 10, 1) {
                AutoFocusFilter = true,
                FlatWizardFilterSettings = null
            };
            settings.FilterWheelFilters.Add(luminance);
            settings.FilterWheelFilters.Add(red);

            FilterWheelSettings roundTripped = RoundTrip(settings);

            roundTripped.FilterWheelFilters.Should().HaveCount(2);
            roundTripped.FilterWheelFilters.Should().OnlyContain(filter => filter.FlatWizardFilterSettings != null);
            roundTripped.FilterWheelFilters.Should().ContainSingle(filter => filter.AutoFocusFilter);
            roundTripped.FilterWheelFilters[0].AutoFocusFilter.Should().BeTrue();
            roundTripped.FilterWheelFilters[1].AutoFocusFilter.Should().BeFalse();
        }

        [Test]
        public void SwitchSettings_RoundTrip_PreservesKnownSymbolNames() {
            SwitchSettings settings = new SwitchSettings {
                KnownReadonlySwitchSymbols = new List<string> { "InputVoltage", "DewPoint" },
                KnownWritableSwitchSymbols = new List<string> { "DewHeater", "PowerPort" }
            };

            SwitchSettings roundTripped = RoundTrip(settings);

            roundTripped.KnownReadonlySwitchSymbols.Should().Equal("InputVoltage", "DewPoint");
            roundTripped.KnownWritableSwitchSymbols.Should().Equal("DewHeater", "PowerPort");
        }

        /// <summary>
        /// Verifies that sequence settings recover from missing folders and template files during profile deserialization.
        /// </summary>
        [Test]
        public void SequenceSettings_OnDeserialized_ClearsMissingTemplatesAndResetsMissingFolders() {
            string missingRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "MissingSequencePaths", Guid.NewGuid().ToString("N"));
            SequenceSettings settings = new SequenceSettings {
                DefaultSequenceFolder = Path.Combine(missingRoot, "Sequences"),
                SequencerTemplatesFolder = Path.Combine(missingRoot, "Templates"),
                SequencerTargetsFolder = Path.Combine(missingRoot, "Targets"),
                TemplatePath = Path.Combine(missingRoot, "legacy.xml"),
                StartupSequenceTemplate = Path.Combine(missingRoot, "startup.json"),
                EstimatedDownloadTime = TimeSpan.FromSeconds(12.5d)
            };

            SequenceSettings roundTripped = RoundTrip(settings);

            roundTripped.DefaultSequenceFolder.Should().NotBe(settings.DefaultSequenceFolder);
            roundTripped.SequencerTemplatesFolder.Should().NotBe(settings.SequencerTemplatesFolder);
            roundTripped.SequencerTargetsFolder.Should().NotBe(settings.SequencerTargetsFolder);
            roundTripped.TemplatePath.Should().BeEmpty();
            roundTripped.StartupSequenceTemplate.Should().BeEmpty();
            roundTripped.EstimatedDownloadTime.Should().Be(TimeSpan.FromSeconds(12.5d));
        }

        /// <summary>
        /// Verifies trained flat exposure lookup precedence from exact match to offset and gain wildcard fallbacks.
        /// </summary>
        [Test]
        public void FlatDeviceSettings_GetTrainedFlatExposureSetting_UsesDeterministicFallbackOrder() {
            FlatDeviceSettings settings = new FlatDeviceSettings();
            BinningMode binning = new BinningMode(2, 2);

            settings.AddTrainedFlatExposureSetting(3, binning, -1, -1, 10, 1.0d);
            settings.AddTrainedFlatExposureSetting(3, binning, -1, 20, 20, 2.0d);
            settings.AddTrainedFlatExposureSetting(3, binning, 100, -1, 30, 3.0d);

            TrainedFlatExposureSetting gainFallback = settings.GetTrainedFlatExposureSetting(3, binning, 100, 20);
            TrainedFlatExposureSetting offsetFallback = settings.GetTrainedFlatExposureSetting(3, binning, 200, 20);
            TrainedFlatExposureSetting fullWildcardFallback = settings.GetTrainedFlatExposureSetting(3, binning, 200, 30);

            gainFallback.Brightness.Should().Be(30);
            gainFallback.Time.Should().Be(3.0d);
            offsetFallback.Brightness.Should().Be(20);
            offsetFallback.Time.Should().Be(2.0d);
            fullWildcardFallback.Brightness.Should().Be(10);
            fullWildcardFallback.Time.Should().Be(1.0d);
        }

        /// <summary>
        /// Verifies that negative trained flat exposure values are normalized to safe non-negative values.
        /// </summary>
        [Test]
        public void TrainedFlatExposureSetting_ClampsNegativeBrightnessAndExposureTime() {
            TrainedFlatExposureSetting setting = new TrainedFlatExposureSetting(1, new BinningMode(1, 1), 100, 20, 10, 2d);

            setting.Brightness = -1;
            setting.Time = -0.5d;

            setting.Brightness.Should().Be(0);
            setting.Time.Should().Be(0d);
        }

        /// <summary>
        /// Verifies that color schema toggling and custom-copy operations preserve exact color channel values.
        /// </summary>
        [Test]
        public void ColorSchemaSettings_ToggleAndCopyToCustomPreserveSelectedColorValues() {
            ColorSchemaSettings settings = new ColorSchemaSettings();
            Color originalPrimary = settings.ColorSchema.PrimaryColor;
            string originalName = settings.ColorSchema.Name;
            string alternateName = settings.AltColorSchema.Name;

            settings.ToggleSchema();

            settings.ColorSchema.Name.Should().Be(alternateName);
            settings.AltColorSchema.Name.Should().Be(originalName);

            settings.ToggleSchema();
            settings.CopyToCustom();

            settings.ColorSchema.Name.Should().Be("Custom");
            settings.ColorSchema.PrimaryColor.Should().Be(originalPrimary);
        }

        [Test]
        public void FramingAssistantSettings_ProjectionModeAndHorizon_RoundTripWithCompatibleDefaults() {
            FramingAssistantSettings defaults = new FramingAssistantSettings();
            FramingAssistantSettings settings = new FramingAssistantSettings {
                SkyMapProjectionMode = SkyMapProjectionMode.AltAz,
                ShowHorizon = true
            };

            FramingAssistantSettings roundTripped = RoundTrip(settings);

            defaults.SkyMapProjectionMode.Should().Be(SkyMapProjectionMode.Equatorial);
            defaults.ShowHorizon.Should().BeFalse();
            roundTripped.SkyMapProjectionMode.Should().Be(SkyMapProjectionMode.AltAz);
            roundTripped.ShowHorizon.Should().BeTrue();
        }

        private static List<string> CapturePropertyChanges(INotifyPropertyChanged source) {
            List<string> propertyNames = new List<string>();
            source.PropertyChanged += (object sender, PropertyChangedEventArgs args) => propertyNames.Add(args.PropertyName);
            return propertyNames;
        }

        private static T RoundTrip<T>(T value) {
            DataContractSerializer serializer = new DataContractSerializer(typeof(T));
            using MemoryStream stream = new MemoryStream();
            serializer.WriteObject(stream, value);
            stream.Position = 0;
            return (T)serializer.ReadObject(stream);
        }
    }
}