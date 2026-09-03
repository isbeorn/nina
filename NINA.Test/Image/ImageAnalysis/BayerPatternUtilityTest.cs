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
using NINA.Core.Enum;
using NINA.Image.ImageAnalysis;

namespace NINA.Test.Image.ImageAnalysis {

    [TestFixture]
    public class BayerPatternUtilityTest {

        [TestCase(0, 0, SensorType.RGGB)]
        [TestCase(1, 0, SensorType.GRBG)]
        [TestCase(0, 1, SensorType.GBRG)]
        [TestCase(1, 1, SensorType.BGGR)]
        [TestCase(2, 4, SensorType.RGGB)]
        [TestCase(3, 4, SensorType.GRBG)]
        [TestCase(2, 5, SensorType.GBRG)]
        [TestCase(3, 5, SensorType.BGGR)]
        public void ApplyOffsets_RggbPattern_ResolvesOffsetParity(int offsetX, int offsetY, SensorType expected) {
            BayerPatternUtility.ApplyOffsets(SensorType.RGGB, offsetX, offsetY).Should().Be(expected);
        }

        [TestCase(SensorType.RGGB)]
        [TestCase(SensorType.GRBG)]
        [TestCase(SensorType.GBRG)]
        [TestCase(SensorType.BGGR)]
        [TestCase(SensorType.GRGB)]
        [TestCase(SensorType.RGBG)]
        [TestCase(SensorType.GBGR)]
        [TestCase(SensorType.BGRG)]
        public void ApplyOffsets_RepeatingAxisShift_RestoresPattern(SensorType pattern) {
            SensorType horizontalShift = BayerPatternUtility.ApplyOffsets(pattern, 1, 0);
            SensorType verticalShift = BayerPatternUtility.ApplyOffsets(pattern, 0, 1);

            BayerPatternUtility.ApplyOffsets(horizontalShift, 1, 0).Should().Be(pattern);
            BayerPatternUtility.ApplyOffsets(verticalShift, 0, 1).Should().Be(pattern);
        }

        [TestCase(SensorType.Monochrome)]
        [TestCase(SensorType.Color)]
        [TestCase(SensorType.CMYG)]
        [TestCase(SensorType.CMYG2)]
        [TestCase(SensorType.LRGB)]
        public void ApplyOffsets_NonRgbBayerCategory_RemainsUnchanged(SensorType pattern) {
            BayerPatternUtility.ApplyOffsets(pattern, 1, 1).Should().Be(pattern);
        }
    }
}