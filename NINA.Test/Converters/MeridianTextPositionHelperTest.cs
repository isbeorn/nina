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
using NINA.Core.Utility.Converters;
using NUnit.Framework;

namespace NINA.Test.Converters {

    [TestFixture]
    public class MeridianTextPositionHelperTest {

        [Test]
        public void GetPositioningStrategy_WhenNowIsNaN_ReturnsCenterOnMeridian() {
            // Arrange
            double nowTime = double.NaN;
            double meridianTime = 0.5;

            // Act
            var result = MeridianTextPositionHelper.GetPositioningStrategy(nowTime, meridianTime);

            // Assert
            result.Should().Be(MeridianTextPositionHelper.PositioningStrategy.CenterOnMeridian);
        }

        [Test]
        public void GetPositioningStrategy_WhenMeridianIsNaN_ReturnsCenterOnMeridian() {
            // Arrange
            double nowTime = 0.5;
            double meridianTime = double.NaN;

            // Act
            var result = MeridianTextPositionHelper.GetPositioningStrategy(nowTime, meridianTime);

            // Assert
            result.Should().Be(MeridianTextPositionHelper.PositioningStrategy.CenterOnMeridian);
        }

        [Test]
        public void GetPositioningStrategy_WhenDistanceIsGreaterThan1Point5Hours_ReturnsCenterOnMeridian() {
            // Arrange
            double nowTime = 0.5;
            double meridianTime = 0.6; // 0.1 difference = ~2.4 hours

            // Act
            var result = MeridianTextPositionHelper.GetPositioningStrategy(nowTime, meridianTime);

            // Assert
            result.Should().Be(MeridianTextPositionHelper.PositioningStrategy.CenterOnMeridian);
        }

        [Test]
        public void GetPositioningStrategy_WhenNowIsToTheRightOfMeridian_ReturnsOffsetRight() {
            // Arrange
            double meridianTime = 0.5;
            double nowTime = 0.55; // Now is 0.05 ahead (to the right)

            // Act
            var result = MeridianTextPositionHelper.GetPositioningStrategy(nowTime, meridianTime);

            // Assert
            result.Should().Be(MeridianTextPositionHelper.PositioningStrategy.OffsetRight);
        }

        [Test]
        public void GetPositioningStrategy_WhenNowIsToTheLeftOfMeridian_ReturnsOffsetLeft() {
            // Arrange
            double meridianTime = 0.5;
            double nowTime = 0.45; // Now is 0.05 behind (to the left)

            // Act
            var result = MeridianTextPositionHelper.GetPositioningStrategy(nowTime, meridianTime);

            // Assert
            result.Should().Be(MeridianTextPositionHelper.PositioningStrategy.OffsetLeft);
        }

        [Test]
        public void GetPositioningStrategy_WhenDistanceIsExactly1Point5Hours_ReturnsOffsetRight() {
            // Arrange
            double meridianTime = 0.5;
            double nowTime = 0.5625; // Exactly 0.0625 ahead (1.5 hours)

            // Act
            var result = MeridianTextPositionHelper.GetPositioningStrategy(nowTime, meridianTime);

            // Assert
            result.Should().Be(MeridianTextPositionHelper.PositioningStrategy.OffsetRight);
        }

        [Test]
        public void GetXPosition_WhenStrategyCenterOnMeridian_ReturnsMeridianTime() {
            // Arrange
            double nowTime = 0.5;
            double meridianTime = 0.6;
            var strategy = MeridianTextPositionHelper.PositioningStrategy.CenterOnMeridian;

            // Act
            var result = MeridianTextPositionHelper.GetXPosition(nowTime, meridianTime, strategy);

            // Assert
            result.Should().Be(meridianTime);
        }

        [Test]
        public void GetXPosition_WhenStrategyOffsetRight_ReturnsNowMinusMargin() {
            // Arrange
            double nowTime = 0.5;
            double meridianTime = 0.45;
            var strategy = MeridianTextPositionHelper.PositioningStrategy.OffsetRight;

            // Act
            var result = MeridianTextPositionHelper.GetXPosition(nowTime, meridianTime, strategy);

            // Assert
            result.Should().BeApproximately(0.45, 0.001); // nowTime - 0.05
        }

        [Test]
        public void GetXPosition_WhenStrategyOffsetLeft_ReturnsNowPlusMargin() {
            // Arrange
            double nowTime = 0.5;
            double meridianTime = 0.55;
            var strategy = MeridianTextPositionHelper.PositioningStrategy.OffsetLeft;

            // Act
            var result = MeridianTextPositionHelper.GetXPosition(nowTime, meridianTime, strategy);

            // Assert
            result.Should().BeApproximately(0.56, 0.001); // nowTime + 0.06
        }

        [Test]
        [TestCase(0.5, 0.6, 0.6)] // Far apart -> center on meridian
        [TestCase(0.5, 0.45, 0.45)] // Close, now right -> offset right (nowTime - 0.05)
        [TestCase(0.5, 0.55, 0.56)] // Close, now left -> offset left (nowTime + 0.06)
        public void GetXPosition_IntegrationTest_ReturnsExpectedPosition(double nowTime, double meridianTime, double expectedPosition) {
            // Arrange
            var strategy = MeridianTextPositionHelper.GetPositioningStrategy(nowTime, meridianTime);

            // Act
            var result = MeridianTextPositionHelper.GetXPosition(nowTime, meridianTime, strategy);

            // Assert
            result.Should().BeApproximately(expectedPosition, 0.001);
        }

        [Test]
        public void GetXPosition_OffsetLeftNearRightEdge_ClampsToPreventOverflow() {
            // Arrange - Now is at 0.95 (22:48), meridian at 0.96
            double nowTime = 0.95;
            double meridianTime = 0.96;
            var strategy = MeridianTextPositionHelper.GetPositioningStrategy(nowTime, meridianTime);

            // Act
            var result = MeridianTextPositionHelper.GetXPosition(nowTime, meridianTime, strategy);

            // Assert
            strategy.Should().Be(MeridianTextPositionHelper.PositioningStrategy.OffsetLeft);
            // Would normally be 0.95 + 0.06 = 1.01, but should be clamped to 1.0 - 0.08 = 0.92
            result.Should().BeLessThan(0.93);
        }

        [Test]
        public void GetXPosition_OffsetRightNearLeftEdge_ClampsToPreventOverflow() {
            // Arrange - Now is at 0.05 (01:12), meridian at 0.04
            double nowTime = 0.05;
            double meridianTime = 0.04;
            var strategy = MeridianTextPositionHelper.GetPositioningStrategy(nowTime, meridianTime);

            // Act
            var result = MeridianTextPositionHelper.GetXPosition(nowTime, meridianTime, strategy);

            // Assert
            strategy.Should().Be(MeridianTextPositionHelper.PositioningStrategy.OffsetRight);
            // Would normally be 0.05 - 0.05 = 0.0, but should be clamped to >= 0.08
            result.Should().BeGreaterThan(0.07);
        }
    }
}
