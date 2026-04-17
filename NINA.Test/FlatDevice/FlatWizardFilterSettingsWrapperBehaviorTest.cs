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
using NINA.Core.Model.Equipment;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyFlatDevice;
using NINA.Image.ImageAnalysis;
using NINA.ViewModel.FlatWizard;

namespace NINA.Test.FlatDevice {

    [TestFixture]
    public class FlatWizardFilterSettingsWrapperBehaviorTest {

        /// <summary>
        /// Verifies flat-wizard wrappers convert normalized histogram targets into ADU values using the current camera bit depth.
        /// </summary>
        [Test]
        public void FlatWizardFilterSettingsWrapper_ComputesAduDisplayFromHistogramSettingsAndBitDepth() {
            var settings = new FlatWizardFilterSettings {
                HistogramMeanTarget = 0.5,
                HistogramTolerance = 0.1
            };
            var filter = new FilterInfo("L", 0, 0) {
                FlatWizardFilterSettings = settings
            };
            var sut = new FlatWizardFilterSettingsWrapper(filter, settings, bitDepth: 16, new CameraInfo(), new FlatDeviceInfo());
            var changed = new List<string>();
            sut.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

            sut.HistogramMeanTargetADU.Should().Be(HistogramMath.HistogramMeanAndCameraBitDepthToAdu(0.5, 16).ToString("0"));
            sut.HistogramToleranceADU.Should().Be(
                HistogramMath.GetLowerToleranceBoundInAdu(0.5, 16, 0.1).ToString("0")
                + " - " + HistogramMath.GetUpperToleranceBoundInAdu(0.5, 16, 0.1).ToString("0"));

            sut.BitDepth = 12;

            sut.HistogramMeanTargetADU.Should().Be(HistogramMath.HistogramMeanAndCameraBitDepthToAdu(0.5, 12).ToString("0"));
            changed.Should().Contain(nameof(FlatWizardFilterSettingsWrapper.BitDepth));
            changed.Should().Contain(nameof(FlatWizardFilterSettingsWrapper.HistogramMeanTargetADU));
            changed.Should().Contain(nameof(FlatWizardFilterSettingsWrapper.HistogramToleranceADU));
        }

        /// <summary>
        /// Verifies wrapped filter settings stay synchronized when flat-wizard settings change and UI selection state is independently observable.
        /// </summary>
        [Test]
        public void FlatWizardFilterSettingsWrapper_PropagatesSettingsChangesToFilterAndSelectionState() {
            var original = new FlatWizardFilterSettings {
                HistogramMeanTarget = 0.4,
                HistogramTolerance = 0.05
            };
            var replacement = new FlatWizardFilterSettings {
                HistogramMeanTarget = 0.6,
                HistogramTolerance = 0.2
            };
            var filter = new FilterInfo("Ha", 12, 2) {
                FlatWizardFilterSettings = original
            };
            var sut = new FlatWizardFilterSettingsWrapper(filter, original, bitDepth: 14, new CameraInfo(), new FlatDeviceInfo());
            var changed = new List<string>();
            sut.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

            sut.IsChecked = true;
            sut.Settings = replacement;
            replacement.HistogramMeanTarget = 0.7;

            sut.IsChecked.Should().BeTrue();
            filter.FlatWizardFilterSettings.Should().BeSameAs(replacement);
            changed.Should().Contain(nameof(FlatWizardFilterSettingsWrapper.IsChecked));
            changed.Should().Contain(nameof(FlatWizardFilterSettingsWrapper.Settings));
            changed.Should().Contain(nameof(FlatWizardFilterSettingsWrapper.HistogramMeanTargetADU));
            changed.Should().Contain(nameof(FlatWizardFilterSettingsWrapper.HistogramToleranceADU));
        }
    }
}
