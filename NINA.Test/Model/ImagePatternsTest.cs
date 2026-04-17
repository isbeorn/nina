#region "copyright"

/*
    Copyright (c) 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Model;
using NUnit.Framework;
using System.IO;
using System.Linq;

namespace NINA.Test.Model {

    [TestFixture]
    public class ImagePatternsTest {

        /// <summary>
        /// Verifies string, integer, and floating-point macro values are sanitized and formatted using invariant precision.
        /// </summary>
        [Test]
        public void Set_ValidValues_StoresSanitizedAndFormattedMacroValues() {
            ImagePatterns patterns = new ImagePatterns();

            bool filterSet = patterns.Set(ImagePatternKeys.Filter, " Ha/OIII ");
            bool exposureSet = patterns.Set(ImagePatternKeys.ExposureTime, 120.567, 1);
            bool starCountSet = patterns.Set(ImagePatternKeys.StarCount, 1234);
            bool missingSet = patterns.Set("$$DOESNOTEXIST$$", "value");
            bool nanSet = patterns.Set(ImagePatternKeys.HFR, double.NaN);

            string result = patterns.GetImageFileString(
                ImagePatternKeys.Filter + "_" + ImagePatternKeys.ExposureTime + "_" + ImagePatternKeys.StarCount);

            Assert.That(filterSet, Is.True);
            Assert.That(exposureSet, Is.True);
            Assert.That(starCountSet, Is.True);
            Assert.That(missingSet, Is.False);
            Assert.That(nanSet, Is.False);
            Assert.That(result, Is.EqualTo("Ha-OIII_120.6_1234"));
        }

        /// <summary>
        /// Verifies file-string generation sanitizes each path segment and allows callers to override image type at render time.
        /// </summary>
        [Test]
        public void GetImageFileString_PathSegmentsAndImageTypeOverride_ReturnsSafePath() {
            ImagePatterns patterns = new ImagePatterns();
            patterns.Set(ImagePatternKeys.Filter, "Lum");
            patterns.Set(ImagePatternKeys.TargetName, "M31:Core");
            patterns.Set(ImagePatternKeys.ImageType, "LIGHT");

            string result = patterns.GetImageFileString(
                ImagePatternKeys.Filter + Path.DirectorySeparatorChar + ImagePatternKeys.TargetName + "_" + ImagePatternKeys.ImageType,
                "DARK");

            Assert.That(result, Is.EqualTo(Path.Combine("Lum", "M31_Core_DARK")));
            foreach (string segment in result.Split(Path.DirectorySeparatorChar)) {
                Assert.That(segment.IndexOfAny(Path.GetInvalidFileNameChars()), Is.EqualTo(-1));
            }
        }

        /// <summary>
        /// Verifies custom patterns can be added once and then participate in macro substitution.
        /// </summary>
        [Test]
        public void Add_NewPattern_AddsOnceAndReportsWhetherInserted() {
            ImagePatterns patterns = new ImagePatterns();
            ImagePattern custom = new ImagePattern("$$SESSION$$", "Session", "Acquisition") { Value = "S01" };

            bool added = patterns.Add(custom);
            bool duplicateAdded = patterns.Add(new ImagePattern("$$SESSION$$", "Duplicate") { Value = "S02" });
            string result = patterns.GetImageFileString("Target_" + custom.Key);

            Assert.That(added, Is.True);
            Assert.That(duplicateAdded, Is.False);
            Assert.That(result, Is.EqualTo("Target_S01"));
        }

        /// <summary>
        /// Verifies the example pattern set remains complete enough to render common acquisition metadata macros.
        /// </summary>
        [Test]
        public void CreateExample_CommonImagePattern_RendersRepresentativeMetadata() {
            ImagePatterns patterns = ImagePatterns.CreateExample();

            string result = patterns.GetImageFileString(
                ImagePatternKeys.TargetName + "_" + ImagePatternKeys.Filter + "_" + ImagePatternKeys.Binning + "_" + ImagePatternKeys.SequenceTitle);

            Assert.That(result, Is.EqualTo("M33_L_1x1_SequenceTitle"));
            Assert.That(patterns.Items.Any(x => x.Key == ImagePatternKeys.TargetName), Is.True);
            Assert.That(patterns.Items.Any(x => x.Key == ImagePatternKeys.SequenceTitle), Is.True);
        }
    }
}
